using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Linq;
using TPEmail.BusinessModels.RequestModels;
using TPEmail.BusinessModels.ResponseModels;
using TPEmail.BusinessModels.Enums;
using TPEmail.BusinessModels.Constants;
using TPEmail.Common.Helpers;
using TPEmail.DataAccess.Interface.Repository.v1_0;
using TPEmail.DataAccess.Interface.Service.v1_0;

namespace TPEmail.DataAccess.Service.v1_0
{
    /// <summary>
    /// Auth service with its own authentication logic implementation.
    /// This keeps auth behavior self-contained while preserving existing functionality.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IAppRepository _apprepository;
        private readonly IAuthRepository _authrepository;
        private readonly IAppLookup _applookup;
        private readonly IConfiguration _configuration;
        private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _httpcontextaccessor;
        private readonly NLog.ILogger _logger;

        public AuthService(IAppRepository appRepository, IAuthRepository authRepository, IAppLookup appLookup, IConfiguration configuration, Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor)
        {
            _apprepository = appRepository;
            _authrepository = authRepository;
            _applookup = appLookup;
            _configuration = configuration;
            _httpcontextaccessor = httpContextAccessor;
            _logger = NLog.LogManager.GetCurrentClassLogger();
        }

        public IConfiguration GetConfiguration() => _configuration;

        // ── Registration ──────────────────────────────────────────────
        public async Task<ServiceResult> Registration(AppUser data)
        {
            try
            {
                if (data == null) throw new ArgumentNullException(nameof(data));
                if (string.IsNullOrEmpty(data.Email)) throw new ArgumentNullException(nameof(data.Email), MessageConstants.EmailIsRequired);
                if (string.IsNullOrEmpty(data.AppSecret)) throw new ArgumentNullException(nameof(data.AppSecret), MessageConstants.PasswordIsRequired);

                data.UserId = Guid.NewGuid();
                data.EmailBlindIndex = EncryptionHelper.GenerateBlindIndex(data.Email.Trim());
                data.Email = EncryptionHelper.DataEncryptAsync(data.Email.Trim(), null, "AppService.Registration", data.Username);
                data.Upn = data.Upn?.Trim();

                AppUserCredentials credentials = SecurityHelper.GenerateArgonHash(data.AppSecret);
                data.AppSecret = credentials.Hash;
                data.Salt = credentials.Salt;
                data.EncryptionKey = credentials.EncryptionKey;

                var result = await _apprepository.SaveAppUserAsync(data);
                var userId = result.EntityId ?? Guid.Empty;

                if (userId != Guid.Empty)
                {
                    await _apprepository.SaveUserRoleAsync(new AppUserRole
                    {
                        UserId = userId,
                        RoleId = (data.RoleId == null) ? (int)RoleType.USER : (int)data.RoleId
                    });

                    await _apprepository.SaveAppUserCredentialsLogAsync(new PasswordUpdate
                    {
                        UserId = userId,
                        NoOfUpdate = 0,
                        CreatedBy = userId.ToString(),
                        ModifiedBy = userId.ToString()
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in Registration: {ex.Message}", ex);
            }
        }

        public async Task<ServiceResult> RegisterUser(AppUserPostDto payload)
        {
            var existingUser = await FindUserByEmailAsync(payload.Email);
            if (existingUser != null)
                throw new InvalidOperationException(MessageConstants.UserAlreadyExists);

            if (string.IsNullOrEmpty(payload.Email))
                throw new ArgumentException(MessageConstants.EmailIsRequired);

            string password = string.IsNullOrWhiteSpace(payload.AppSecret) ? SecurityHelper.GenerateRandomString() : payload.AppSecret;

            var user = new AppUser
            {
                Username = payload.Username,
                Email = payload.Email?.Trim(),
                AppSecret = password,
                RoleId = payload.RoleId,
                Upn = payload.Upn
            };

            var result = await Registration(user);
            result.Data = password;
            return result;
        }

        // ── Authentication ────────────────────────────────────────────
        public async Task<AppUserGetDto> Authenticate(AuthenticateRequest data)
        {
            try
            {
                var userData = await FindUserByEmailAsync(data.Email);
                if (userData == null)
                {
                    throw new KeyNotFoundException(string.Format(MessageConstants.NoEmailFoundWithFormat, data.Email));
                }

                if (!VerifyPassword(data.Password, userData.AppSecret, userData.Salt))
                {
                    throw new UnauthorizedAccessException(MessageConstants.InvalidCredentials);
                }

                return userData;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in Authenticate: {ex.Message}", ex);
            }
        }

        public async Task<ApiResponse<AuthTokenResponse>> AuthenticateUserAsync(AuthenticateRequest data)
        {
            try
            {
                var type = await UserStateAsync(data);

                switch (type)
                {
                    case UserDetailsType.USER_NOT_FOUND:
                        return new ApiResponse<AuthTokenResponse>(
                            ResultCodes.ValidationFailure, null,
                            new[] { string.Format(MessageConstants.UserWithEmailNotFoundFormat, data.Email) });

                    case UserDetailsType.INVALID_CREDENTIALS:
                        return new ApiResponse<AuthTokenResponse>(
                            ResultCodes.ValidationFailure, null,
                            new[] { MessageConstants.InvalidEmailOrPassword });

                    case UserDetailsType.AZURE_AD_USER:
                        return new ApiResponse<AuthTokenResponse>(
                            ResultCodes.ValidationFailure, null,
                            new[] { MessageConstants.AzureADAuthenticationRequired });
                }

                var userData = await FindUserByEmailAsync(data.Email);
                if (userData == null)
                {
                    return new ApiResponse<AuthTokenResponse>(
                        ResultCodes.ValidationFailure, null,
                        new[] { string.Format(MessageConstants.NoUserFoundWithEmailFormat, data.Email) });
                }

                string accessToken = GenerateJWTTokenAsync(userData);
                var (refreshToken, _) = await CreateRefreshTokenAsync(Guid.Parse(userData.UserId));

                return new ApiResponse<AuthTokenResponse>(
                    ResultCodes.Success,
                    new[] { new AuthTokenResponse { AccessToken = accessToken, RefreshToken = refreshToken, ExpiresIn = 1800 } },
                    new[] { MessageConstants.AuthenticationSuccessful });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in AuthenticateUserAsync: {ex.Message}", ex);
            }
        }

        public async Task<ApiResponse<AuthTokenResponse>> AuthenticateAzureUserAsync(AzureAuthenticateRequest data)
        {
            try
            {
                string? upn = !string.IsNullOrWhiteSpace(data?.Upn)
                    ? data.Upn.Trim().ToLowerInvariant()
                    : !string.IsNullOrWhiteSpace(data?.Email)
                        ? data.Email.Trim().ToLowerInvariant()
                        : null;

                if (string.IsNullOrWhiteSpace(upn))
                {
                    return new ApiResponse<AuthTokenResponse>(
                        ResultCodes.ValidationFailure, null,
                        new[] { MessageConstants.UPNRequiredForAzureAD });
                }

                var existingUser = await FindUserByUpnAsync(upn);

                if (existingUser == null && !string.IsNullOrWhiteSpace(data?.Email))
                    existingUser = await FindUserByEmailAsync(data.Email.Trim().ToLowerInvariant());

                if (existingUser != null)
                {
                    if (!string.IsNullOrEmpty(existingUser.AppSecret) && !string.IsNullOrEmpty(existingUser.Salt))
                    {
                        return new ApiResponse<AuthTokenResponse>(
                            ResultCodes.ValidationFailure, null,
                            new[] { MessageConstants.PasswordAuthenticationRequired });
                    }

                    string accessToken = GenerateJWTTokenAsync(existingUser);
                    var (refreshToken, _) = await CreateRefreshTokenAsync(Guid.Parse(existingUser.UserId));

                    return new ApiResponse<AuthTokenResponse>(
                        ResultCodes.Success,
                        new[] { new AuthTokenResponse { AccessToken = accessToken, RefreshToken = refreshToken, ExpiresIn = 1800 } },
                        new[] { MessageConstants.AuthenticationSuccessful });
                }

                _logger.Info($"New Azure AD user login attempt. Fetching user details from Graph API for: {upn}");

                ADUser? adUser = await GetADUserAsync(upn);

                string email;
                string userName;

                if (adUser != null)
                {
                    email = !string.IsNullOrWhiteSpace(adUser.Mail)
                        ? adUser.Mail.Trim().ToLowerInvariant()
                        : !string.IsNullOrWhiteSpace(data?.Email)
                            ? data.Email.Trim().ToLowerInvariant()
                            : upn;

                    userName = !string.IsNullOrWhiteSpace(adUser.DisplayName)
                        ? adUser.DisplayName
                        : !string.IsNullOrWhiteSpace(data?.DisplayName)
                            ? data.DisplayName
                            : upn.Split('@')[0];

                    _logger.Info($"Graph API user found: DisplayName={adUser.DisplayName}, Mail={adUser.Mail}, UPN={adUser.UserPrincipalName}");
                }
                else
                {
                    email = !string.IsNullOrWhiteSpace(data?.Email)
                        ? data.Email.Trim().ToLowerInvariant()
                        : upn;

                    userName = !string.IsNullOrWhiteSpace(data?.DisplayName)
                        ? data.DisplayName
                        : !string.IsNullOrWhiteSpace(data?.Username)
                            ? data.Username
                            : upn.Split('@')[0];

                    _logger.Warn($"Graph API user not found for {upn}, using frontend data");
                }

                var newUser = new AppUser
                {
                    UserId = Guid.NewGuid(),
                    Email = email,
                    Upn = upn,
                    Username = userName,
                    AppSecret = null,
                    Salt = null,
                    EncryptionKey = null,
                    Active = 0,
                    RoleId = (int)RoleType.USER
                };

                newUser.EmailBlindIndex = EncryptionHelper.GenerateBlindIndex(email);
                newUser.Email = EncryptionHelper.DataEncryptAsync(email, null, "AppService.AuthenticateAzureUserAsync", newUser.Username);

                var saveResult = await _apprepository.SaveAppUserAsync(newUser);
                var userId = saveResult.EntityId ?? Guid.Empty;

                if (userId != Guid.Empty)
                {
                    await _apprepository.SaveUserRoleAsync(new AppUserRole
                    {
                        UserId = userId,
                        RoleId = (int)RoleType.USER
                    });

                    _logger.Info($"New Azure AD user created: UPN={upn}, Email={email}, UserId={userId}");

                    var createdUser = await FindUserByUpnAsync(upn);
                    if (createdUser != null && createdUser.Roles != null && createdUser.Roles.Count > 0)
                    {
                        string accessToken = GenerateJWTTokenAsync(createdUser);
                        var (refreshToken, _) = await CreateRefreshTokenAsync(Guid.Parse(createdUser.UserId));

                        return new ApiResponse<AuthTokenResponse>(
                            ResultCodes.Success,
                            new[] { new AuthTokenResponse { AccessToken = accessToken, RefreshToken = refreshToken, ExpiresIn = 1800 } },
                            new[] { MessageConstants.AzureADAuthenticationSuccessful });
                    }
                    else
                    {
                        _logger.Warn($"User created but roles not found immediately. Retrying role fetch for userId: {userId}");

                        var userWithRole = new AppUserGetDto
                        {
                            UserId = userId.ToString(),
                            Email = email,
                            Username = userName,
                            Upn = upn,
                            Active = 0,
                            Roles = new List<AppUserRoleDto>
                            {
                                new AppUserRoleDto { RoleId = (int)RoleType.USER, RoleName = "User" }
                            }
                        };

                        string accessToken = GenerateJWTTokenAsync(userWithRole);
                        var (refreshToken, _) = await CreateRefreshTokenAsync(userId);

                        return new ApiResponse<AuthTokenResponse>(
                            ResultCodes.Success,
                            new[] { new AuthTokenResponse { AccessToken = accessToken, RefreshToken = refreshToken, ExpiresIn = 1800 } },
                            new[] { MessageConstants.AzureADAuthenticationSuccessful });
                    }
                }

                return new ApiResponse<AuthTokenResponse>(
                    ResultCodes.SystemError, null,
                    new[] { MessageConstants.FailedToCreateAzureADUserAccount });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in AuthenticateAzureUserAsync: {ex.Message}", ex);
            }
        }

        public async Task<UserDetailsType> UserStateAsync(AuthenticateRequest data)
        {
            try
            {
                var userData = await FindUserByEmailAsync(data.Email);
                if (userData == null) return UserDetailsType.USER_NOT_FOUND;

                if (string.IsNullOrEmpty(userData.AppSecret) || string.IsNullOrEmpty(userData.Salt))
                {
                    return UserDetailsType.AZURE_AD_USER;
                }

                if (!VerifyPassword(data.Password, userData.AppSecret, userData.Salt)) return UserDetailsType.INVALID_CREDENTIALS;

                return UserDetailsType.USER_ALREADY_EXISTS;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in UserStateAsync: {ex.Message}", ex);
            }
        }

        public bool VerifyPassword(string password, string hash, string salt)
            => SecurityHelper.VerifyPassword(password, hash, salt);

        // ── JWT ───────────────────────────────────────────────────────
        public string GenerateJWTTokenAsync(AppUserGetDto source)
            => GenerateUserTokenAsync(source);

        public string GenerateUserTokenAsync(AppUserGetDto data)
        {
            try
            {
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, data.UserId.ToString()),
                    new(ClaimTypes.Name, data.Email),
                    new("username", data.Username ?? data.Email),
                    new("approved", data.Active.ToString())
                };

                if (data.Applications?.Count > 0)
                    claims.Add(new Claim(JwtRegisteredClaimNames.Jti, data.Applications.Count == 1
                        ? data.Applications.First().AppClient.ToString()
                        : string.Join(";", data.Applications.Select(x => x.AppClient.ToString()))));
                else claims.Add(new Claim(JwtRegisteredClaimNames.Jti, string.Empty));

                if (data.Roles?.Count > 0)
                {
                    foreach (var role in data.Roles)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, role.RoleName.ToUpper()));
                        claims.Add(new Claim("roleId", role.RoleId.ToString()));
                    }
                }

                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtKey = Environment.GetEnvironmentVariable("tpjwtkey") ?? _configuration["Jwt:Key"];
                if (string.IsNullOrEmpty(jwtKey)) throw new InvalidOperationException(MessageConstants.JwtKeyNotConfigured);

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme),
                    Issuer = Environment.GetEnvironmentVariable("tpjwtissuer") ?? _configuration["Jwt:Issuer"],
                    Audience = Environment.GetEnvironmentVariable("tpjwtaudience") ?? _configuration["Jwt:Audience"],
                    NotBefore = DateTime.UtcNow,
                    Expires = DateTime.UtcNow.AddMinutes(30),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)), SecurityAlgorithms.HmacSha256Signature)
                };

                var accessToken = tokenHandler.WriteToken(tokenHandler.CreateJwtSecurityToken(tokenDescriptor));

                return accessToken;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in GenerateUserTokenAsync: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Creates a refresh token JWT, persists it to the database, and returns the token string.
        /// This is the SINGLE place where refresh tokens are created.
        /// </summary>
        private async Task<(string refreshToken, Guid refreshTokenId)> CreateRefreshTokenAsync(Guid userId)
        {
            var refreshTokenId = Guid.NewGuid();
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(1);
            var clientIp = _httpcontextaccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

            var jwtKey = Environment.GetEnvironmentVariable("tpjwtkey") ?? _configuration["Jwt:Key"];
            var jwtIssuer = Environment.GetEnvironmentVariable("tpjwtissuer") ?? _configuration["Jwt:Issuer"];
            var jwtAudience = Environment.GetEnvironmentVariable("tpjwtaudience") ?? _configuration["Jwt:Audience"];

            var refreshTokenClaims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(JwtRegisteredClaimNames.Jti, refreshTokenId.ToString())
            };

            var refreshTokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(refreshTokenClaims, "RefreshToken"),
                Issuer = jwtIssuer,
                Audience = jwtAudience,
                NotBefore = DateTime.UtcNow,
                Expires = refreshTokenExpiry,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var refreshToken = tokenHandler.WriteToken(tokenHandler.CreateJwtSecurityToken(refreshTokenDescriptor));

            await _authrepository.InsertRefreshTokenAsync(
                refreshTokenId, userId, DateTime.UtcNow, refreshTokenExpiry, clientIp);

            return (refreshToken, refreshTokenId);
        }

        public Guid? ValidateTokenAsync(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token)) return null;
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtKey = Environment.GetEnvironmentVariable("tpjwtkey") ?? _configuration["Jwt:Key"];
                if (string.IsNullOrEmpty(jwtKey)) throw new InvalidOperationException(MessageConstants.JwtKeyNotConfigured);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidIssuer = Environment.GetEnvironmentVariable("tpjwtissuer") ?? _configuration["Jwt:Issuer"],
                    ValidAudience = Environment.GetEnvironmentVariable("tpjwtaudience") ?? _configuration["Jwt:Audience"]
                }, out SecurityToken validatedToken);

                var userId = new Guid(((JwtSecurityToken)validatedToken).Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value);
                return userId;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in ValidateTokenAsync: {ex.Message}", ex);
            }
        }

        // ── User management ───────────────────────────────────────────
        public async Task<ServiceResult> UpdateAppUserCredentials(AppUser data)
        {
            try
            {
                if (string.IsNullOrEmpty(data.AppSecret))
                    throw new ArgumentException(MessageConstants.PasswordRequiredForCredentialUpdate);

                data.Email = EncryptionHelper.DataEncryptAsync(data.Email, null, "AppService.UpdateAppUserCredentials", data.UserId.ToString());

                AppUserCredentials credentials = SecurityHelper.GenerateArgonHash(data.AppSecret);
                data.AppSecret = credentials.Hash;
                data.Salt = credentials.Salt;
                data.EncryptionKey = credentials.EncryptionKey;

                await _apprepository.AppSecreteUpdateLogAsync(data);
                return await _apprepository.UpdateAppUserCredentialsAsync(data);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in UpdateAppUserCredentials: {ex.Message}", ex);
            }
        }

        public async Task<IList<AppUserGetDto>> FindAllAppUser()
        {
            try
            {
                var userList = await _apprepository.FindAppUsersAsync();
                foreach (var user in userList)
                {
                    user.Email = DecryptOrDefault(user.Email, user.UserId.ToString());
                    user.Upn = DecryptOrDefault(user.Upn, user.UserId.ToString());
                }
                return userList.ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in FindAllAppUser: {ex.Message}", ex);
            }
        }

        public async Task<IList<AppUserGetDto>> FindAllAppUser(int currentPage, int pageSize)
        {
            var data = await _apprepository.FindAppUsersAsync(currentPage, pageSize, null, null);
            foreach (var user in data)
            {
                user.Email = DecryptOrDefault(user.Email, user.UserId.ToString());
                user.Upn = DecryptOrDefault(user.Upn, user.UserId.ToString());
            }
            return data;
        }

        public async Task<IList<AppUserGetDto>> FindAllAppUser(int currentPage, int pageSize, string? searchTerm)
        {
            var data = await _apprepository.FindAppUsersAsync(currentPage, pageSize, searchTerm, null, null, null);
            foreach (var user in data)
            {
                user.Email = DecryptOrDefault(user.Email, user.UserId.ToString());
                user.Upn = DecryptOrDefault(user.Upn, user.UserId.ToString());
            }
            return data;
        }

        public async Task<IList<AppUserGetDto>> FindAllAppUser(int currentPage, int pageSize, string? searchTerm, int? roleId, int? active, string? sortBy)
        {
            bool? activeFilter = active.HasValue ? (active.Value == 1) : null;

            var data = await _apprepository.FindAppUsersAsync(currentPage, pageSize, searchTerm, activeFilter, roleId, sortBy);
            foreach (var user in data)
            {
                user.Email = DecryptOrDefault(user.Email, user.UserId.ToString());
                user.Upn = DecryptOrDefault(user.Upn, user.UserId.ToString());
            }
            return data;
        }

        public async Task<ServiceResult> GetCount()
        {
            return await GetUserCount();
        }

        public async Task<ServiceResult> GetUserCount()
        {
            return await _apprepository.GetAppUserCountAsync();
        }

        public async Task<ServiceResult> GetUserCount(string? searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetUserCount();
            var countResult = await _apprepository.GetUserCountAsync(searchTerm, null, null);
            return ServiceResult.FromCount(countResult.TotalCount);
        }

        public async Task<ServiceResult> GetUserCount(string? searchTerm, int? roleId, int? active)
        {
            bool? activeFilter = active.HasValue ? (active.Value == 1) : null;
            var countResult = await _apprepository.GetUserCountAsync(searchTerm, activeFilter, roleId);
            return ServiceResult.FromCount(countResult.TotalCount);
        }

        public async Task<AppUserGetDto?> FindUserByEmailAsync(string email)
        {
            try
            {
                string emailBlindIndex = EncryptionHelper.GenerateBlindIndex(email);
                AppUserGetDto? result = await _apprepository.FindAppUserByEmailAsync(emailBlindIndex);
                if (result != null)
                {
                    result.Email = DecryptOrDefault(result.Email, result.UserId.ToString());
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in FindUserByEmailAsync: {ex.Message}", ex);
            }
        }

        public async Task<AppUserGetDto?> FindUserByUpnAsync(string upn)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(upn)) return null;

                AppUserGetDto? result = await _apprepository.FindAppUserByUpnAsync(upn.Trim().ToLowerInvariant());
                if (result != null)
                {
                    result.Email = DecryptOrDefault(result.Email, result.UserId.ToString());
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in FindUserByUpnAsync: {ex.Message}", ex);
            }
        }

        public async Task<AppUserGetDto?> FindUserByIdAsync(string userId)
        {
            try
            {
                AppUserGetDto? result = await _apprepository.FindAppUserByIdAsync(userId);
                if (result != null)
                {
                    result.Email = DecryptOrDefault(result.Email, userId);
                    result.Upn = result.Upn;
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in FindUserByIdAsync: {ex.Message}", ex);
            }
        }

        public async Task<ServiceResult> UpdateVerifiedUserAsync(Guid userId, int active = 1)
        {
            return await _apprepository.UpdateToVerifiedUserAsync(userId, active);
        }

        public async Task<ServiceResult> UpdateUserRoleAsync(Guid userId, int newRoleId, string modifiedBy)
        {
            return await _apprepository.UpdateUserRoleAsync(userId, newRoleId, modifiedBy);
        }

        public async Task<IList<AppRole>> FindAppRole()
        {
            return await _apprepository.FindAppRoleAsync();
        }

        // ── Azure AD / Microsoft Graph ────────────────────────────────
        private static readonly HttpClient _graphhttpclient = new();
        private static string? _graphtoken;
        private static DateTime _graphtokenexpiry = DateTime.MinValue;
        private static readonly SemaphoreSlim _tokenlock = new(1, 1);

        private static string GraphUrl => Environment.GetEnvironmentVariable("tpgraphbaseurl") ?? "https://graph.microsoft.com/v1.0";
        private static string UserSelect => Environment.GetEnvironmentVariable("tpgraphuserfields") ?? "id,displayName,givenName,surname,userPrincipalName,mail,jobTitle,department,officeLocation,mobilePhone,companyName";

        private async Task<string> GetGraphTokenAsync()
        {
            await _tokenlock.WaitAsync();
            try
            {
                if (!string.IsNullOrEmpty(_graphtoken) && DateTime.UtcNow < _graphtokenexpiry.AddMinutes(-5))
                    return _graphtoken;

                var clientId = Environment.GetEnvironmentVariable("tpgraphclientid") ?? "";
                var clientSecret = Environment.GetEnvironmentVariable("tpgraphclientsecret") ?? "";
                var tenantId = Environment.GetEnvironmentVariable("tpgraphtenantid") ?? "";

                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = clientId,
                    ["client_secret"] = clientSecret,
                    ["scope"] = Environment.GetEnvironmentVariable("tpgraphoauthscope") ?? "https://graph.microsoft.com/.default",
                    ["grant_type"] = "client_credentials"
                });

                var tokenBaseUrl = Environment.GetEnvironmentVariable("tpgraphtokenendpoint") ?? "https://login.microsoftonline.com";
                var response = await _graphhttpclient.PostAsync($"{tokenBaseUrl}/{tenantId}/oauth2/v2.0/token", content);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                _graphtoken = doc.RootElement.GetProperty("access_token").GetString()!;
                _graphtokenexpiry = DateTime.UtcNow.AddSeconds(doc.RootElement.GetProperty("expires_in").GetInt32());
                return _graphtoken;
            }
            finally { _tokenlock.Release(); }
        }

        public async Task<ADUser?> GetADUserAsync(string upn)
        {
            if (string.IsNullOrWhiteSpace(upn)) return null;
            try
            {
                var token = await GetGraphTokenAsync();
                var request = new HttpRequestMessage(HttpMethod.Get, $"{GraphUrl}/users/{Uri.EscapeDataString(upn)}?$select={UserSelect}");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                var response = await _graphhttpclient.SendAsync(request);
                if (!response.IsSuccessStatusCode) return null;
                var json = await response.Content.ReadAsStringAsync();
                return System.Text.Json.JsonSerializer.Deserialize<ADUser>(json, _jsonoptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in GetADUserAsync: {ex.Message}", ex);
            }
        }

        public async Task<ADUserPhotoResponse> GetADUserPhotoAsync(string upn)
        {
            if (string.IsNullOrWhiteSpace(upn)) return new ADUserPhotoResponse();
            try
            {
                var token = await GetGraphTokenAsync();
                var url = $"{GraphUrl}/users/{Uri.EscapeDataString(upn)}/photo/$value";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                var response = await _graphhttpclient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return new ADUserPhotoResponse { Success = false, StatusCode = (int)response.StatusCode, Error = errorContent };
                }

                var bytes = await response.Content.ReadAsByteArrayAsync();
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
                return new ADUserPhotoResponse { Success = true, Data = $"data:{contentType};base64,{Convert.ToBase64String(bytes)}" };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in GetADUserPhotoAsync: {ex.Message}", ex);
            }
        }

        private static readonly System.Text.Json.JsonSerializerOptions _jsonoptions = new() { PropertyNameCaseInsensitive = true };

        private string BuildADSearchFilter(string term, string? searchType)
        {
            if (string.IsNullOrWhiteSpace(term))
                return string.Empty;

            var escapedTerm = term.Replace("'", "''");
            searchType = (searchType ?? "all").ToLowerInvariant();

            return searchType switch
            {
                "upn" => $"startsWith(userPrincipalName,'{escapedTerm}')",
                "email" => $"startsWith(userPrincipalName,'{escapedTerm}')",
                "displayname" => $"startsWith(displayName,'{escapedTerm}')",
                "firstname" => $"startsWith(givenName,'{escapedTerm}')",
                "lastname" => $"startsWith(surname,'{escapedTerm}')",
                "all" or _ => $"startsWith(userPrincipalName,'{escapedTerm}') or startsWith(displayName,'{escapedTerm}') or startsWith(givenName,'{escapedTerm}') or startsWith(surname,'{escapedTerm}')"
            };
        }

        private async Task<ADUserSearchResponse> SearchADUsersWithFilterAsync(string filter)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filter))
                    return new ADUserSearchResponse();

                var token = await GetGraphTokenAsync();
                var url = $"{GraphUrl}/users?$filter={Uri.EscapeDataString(filter)}&$select={UserSelect}&$count=true";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                request.Headers.Add("ConsistencyLevel", "eventual");
                var response = await _graphhttpclient.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode) return new ADUserSearchResponse();
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var users = new List<ADUser>();
                if (doc.RootElement.TryGetProperty("value", out var valueArray))
                {
                    foreach (var item in valueArray.EnumerateArray())
                        users.Add(System.Text.Json.JsonSerializer.Deserialize<ADUser>(item.GetRawText(), _jsonoptions)!);
                }
                return new ADUserSearchResponse { Users = users, Count = users.Count };
            }
            catch (Exception ex)
            {
                throw new Exception($"Graph API Search Error: {ex.Message}", ex);
            }
        }

        public async Task<ADUserSearchResponse> SearchADUsersAsync(string term, string? searchType = null)
        {
            if (string.IsNullOrWhiteSpace(term))
                return new ADUserSearchResponse();

            var filter = BuildADSearchFilter(term, searchType);
            return await SearchADUsersWithFilterAsync(filter);
        }

        // ── Login audit ───────────────────────────────────────────────
        public async Task<ServiceResult> SignInLog(string userId, string email, string ip, int success)
            => await _apprepository.AppLoginAsync(new AppLogin { UserId = userId, Email = email, IPAddress = ip, Success = success });

        public async Task<ApiResponse<AuthTokenResponse>> RefreshTokensAsync(string refreshToken, string clientIp)
        {
            try
            {
                var jwtKey = Environment.GetEnvironmentVariable("tpjwtkey") ?? _configuration["Jwt:Key"];
                var jwtIssuer = Environment.GetEnvironmentVariable("tpjwtissuer") ?? _configuration["Jwt:Issuer"];
                var jwtAudience = Environment.GetEnvironmentVariable("tpjwtaudience") ?? _configuration["Jwt:Audience"];

                if (string.IsNullOrEmpty(jwtKey))
                    throw new InvalidOperationException(MessageConstants.JwtKeyNotConfigured);

                var tokenHandler = new JwtSecurityTokenHandler();
                ClaimsPrincipal principal;
                SecurityToken validatedToken;
                try
                {
                    principal = tokenHandler.ValidateToken(refreshToken, new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidIssuer = jwtIssuer,
                        ValidAudience = jwtAudience,
                        ClockSkew = TimeSpan.Zero
                    }, out validatedToken);
                }
                catch (SecurityTokenException ex)
                {
                    throw new UnauthorizedAccessException($"Invalid refresh token: {ex.Message}", ex);
                }

                var decodedToken = (JwtSecurityToken)validatedToken;
                var userIdClaim = decodedToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                var tokenJti = decodedToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;

                if (!Guid.TryParse(userIdClaim, out var userId) || !Guid.TryParse(tokenJti, out var oldTokenId))
                    return new ApiResponse<AuthTokenResponse>(ResultCodes.ValidationFailure, null, new[] { "Invalid refresh token" });

                var existingToken = await _authrepository.GetRefreshTokenByIdAsync(oldTokenId);
                if (existingToken == null || existingToken.Revoked || existingToken.ExpiresAt <= DateTime.UtcNow)
                    return new ApiResponse<AuthTokenResponse>(ResultCodes.ValidationFailure, null, new[] { "Refresh token expired or invalid" });

                await _authrepository.RevokeRefreshTokenAsync(oldTokenId, DateTime.UtcNow, clientIp, "Rotation");

                var userData = await FindUserByIdAsync(userId.ToString());
                if (userData == null)
                    return new ApiResponse<AuthTokenResponse>(ResultCodes.ValidationFailure, null, new[] { "User not found" });

                string newAccessToken = GenerateJWTTokenAsync(userData);
                var (newRefreshToken, _) = await CreateRefreshTokenAsync(userId);

                return new ApiResponse<AuthTokenResponse>(
                    ResultCodes.Success,
                    new[] { new AuthTokenResponse { AccessToken = newAccessToken, RefreshToken = newRefreshToken, ExpiresIn = 1800 } },
                    new[] { "Token refreshed successfully" });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in RefreshTokensAsync: {ex.Message}", ex);
            }
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken, string clientIp, string reason)
        {
            try
            {
                var jwtKey = Environment.GetEnvironmentVariable("tpjwtkey") ?? _configuration["Jwt:Key"];
                var jwtIssuer = Environment.GetEnvironmentVariable("tpjwtissuer") ?? _configuration["Jwt:Issuer"];
                var jwtAudience = Environment.GetEnvironmentVariable("tpjwtaudience") ?? _configuration["Jwt:Audience"];

                if (string.IsNullOrEmpty(jwtKey))
                    throw new InvalidOperationException(MessageConstants.JwtKeyNotConfigured);

                var tokenHandler = new JwtSecurityTokenHandler();
                tokenHandler.ValidateToken(refreshToken, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var decodedToken = (JwtSecurityToken)validatedToken;
                var tokenJti = decodedToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;

                if (Guid.TryParse(tokenJti, out var tokenId))
                {
                    await _authrepository.RevokeRefreshTokenAsync(tokenId, DateTime.UtcNow, clientIp, reason);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in RevokeRefreshTokenAsync: {ex.Message}", ex);
            }
        }

        private string DecryptOrDefault(string? cipherText, string? userId = null) =>
            EncryptionHelper.DecryptOrDefault(cipherText, null, "AuthService.DecryptOrDefault", userId);

        // ── Application Authentication ────────────────────────────────
        public async Task<ApiResponse<AuthTokenResponse>> AuthenticateApplicationAsync(string appClientId, string appSecret)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(appClientId) || string.IsNullOrWhiteSpace(appSecret))
                    return new ApiResponse<AuthTokenResponse>(ResultCodes.ValidationFailure, null, new[] { MessageConstants.InvalidApplicationCredentials });

                if (!Guid.TryParse(appClientId, out Guid appClientGuid))
                    return new ApiResponse<AuthTokenResponse>(ResultCodes.ValidationFailure, null, new[] { MessageConstants.NoRegisteredApplicationFoundWithClientId });

                var appData = await _applookup.FindAppLookup(appClientGuid, active: true);

                if (appData == null || appData.Id == 0)
                    return new ApiResponse<AuthTokenResponse>(ResultCodes.ValidationFailure, null, new[] { MessageConstants.NoRegisteredApplicationFoundWithClientId });

                NLog.ScopeContext.PushProperty("ApplicationName", appData.AppName);

                if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(appSecret ?? string.Empty),
                    Encoding.UTF8.GetBytes(appData.AppSecret ?? string.Empty)))
                    return new ApiResponse<AuthTokenResponse>(ResultCodes.ValidationFailure, null, new[] { MessageConstants.InvalidApplicationCredentials });

                var userData = await FindUserByIdAsync(appData.UserId);
                if (userData == null)
                    return new ApiResponse<AuthTokenResponse>(ResultCodes.ValidationFailure, null, new[] { MessageConstants.NoUserFoundForApplication });

                var userApp = userData.Applications?.FirstOrDefault(app => 
                    Guid.TryParse(app.AppClient, out var parsedAppClient) && parsedAppClient.Equals(appClientGuid));
                if (userApp == null)
                    return new ApiResponse<AuthTokenResponse>(ResultCodes.ValidationFailure, null, new[] { MessageConstants.ApplicationNotFoundInUserList });

                userData.Applications = new List<ApplicationGetDto>() { userApp };

                string accessToken = GenerateJWTTokenAsync(userData);
                var (refreshToken, _) = await CreateRefreshTokenAsync(Guid.Parse(userData.UserId));

                return new ApiResponse<AuthTokenResponse>(
                    ResultCodes.Success,
                    new[] { new AuthTokenResponse { AccessToken = accessToken, RefreshToken = refreshToken, ExpiresIn = 1800 } },
                    new[] { MessageConstants.AuthenticationSuccessful });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in AuthenticateApplicationAsync: {ex.Message}", ex);
            }
        }
    }
}
