using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TPEmail.BusinessModels.RequestModels;
using TPEmail.BusinessModels.ResponseModels;
using TPEmail.Common.Helpers;
using TPEmail.BusinessModels.Enums;
using TPEmail.BusinessModels.Constants;
using TPEmail.DataAccess.Interface.Repository.v1_0;
using TPEmail.DataAccess.Interface.Service.v1_0;

namespace TPEmail.DataAccess.Service.v1_0
{
    public class AppService : IAppService
    {
        private readonly IAppRepository _apprepository;
        private readonly IAuthRepository _authrepository;
        private readonly IConfiguration _configuration;
        private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _httpcontextaccessor;
        private readonly NLog.ILogger _logger;

        private static readonly HttpClient _graphhttpclient = new();
        private static string? _graphtoken;
        private static DateTime _graphtokenexpiry = DateTime.MinValue;
        private static readonly SemaphoreSlim _tokenlock = new(1, 1);
        private static readonly System.Text.Json.JsonSerializerOptions _jsonoptions = new() { PropertyNameCaseInsensitive = true };

        private static string GraphUrl => Environment.GetEnvironmentVariable("tpgraphbaseurl") ?? "https://graph.microsoft.com/v1.0";
        private static string UserSelect => Environment.GetEnvironmentVariable("tpgraphuserfields") ?? "id,displayName,givenName,surname,userPrincipalName,mail,jobTitle,department,officeLocation,mobilePhone,companyName";

        private static readonly List<EmailServiceLookup> _staticEmailServices = new()
        {
            new() { Id = 0, ServiceName = "TP Internal", Active = true, CreationDateTime = DateTime.MinValue },
            new() { Id = 1, ServiceName = "O365", Active = true, CreationDateTime = DateTime.MinValue },
            new() { Id = 2, ServiceName = "Mailkit", Active = true, CreationDateTime = DateTime.MinValue },
            new() { Id = 3, ServiceName = "Exchange Server", Active = true, CreationDateTime = DateTime.MinValue },
            new() { Id = 4, ServiceName = "SendGrid", Active = true, CreationDateTime = DateTime.MinValue }
        };

        public AppService(IAppRepository appRepository, IAuthRepository authRepository, IConfiguration configuration, Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor)
        {
            _apprepository = appRepository;
            _authrepository = authRepository;
            _configuration = configuration;
            _httpcontextaccessor = httpContextAccessor;
            _logger = NLog.LogManager.GetCurrentClassLogger();
        }

        public IConfiguration GetConfiguration() => _configuration;

        public Task<ServiceResult> GetCount(string tableName, string condition) => _apprepository.GetCountAsync(tableName, condition);

        public Task<ServiceResult> GetCount(string tableName) => _apprepository.GetCountAsync(tableName, string.Empty);

        public Task<ServiceResult> GetCount() => GetUserCount();

        #region Dashboard & Statistics

        public async Task<List<DashboardEmailDto>> FindAdminDashboardData(string? userId = null)
        {
            var data = await _apprepository.FindAdminDashboardDataAsync(userId);
            return data.ToList();
        }

        public async Task<List<Top10AppsDto>> FindTop10Apps()
        {
            var data = await _apprepository.FindTop10AppsAsync();
            return data.ToList();
        }

        public async Task<List<Top5AppsUtilisationDto>> FindTop5AppsUtilisation()
        {
            var data = await _apprepository.FindTop5AppsUtilisationAsync();
            foreach (var item in data)
            {
                item.AppOwner = EncryptionHelper.DecryptOrDefault(item.AppOwner, null, "AppService.FindTop5AppsUtilisation", item.UserId);
                item.FromEmailAddress = EncryptionHelper.DecryptOrDefault(item.FromEmailAddress, null, "AppService.FindTop5AppsUtilisation", item.UserId);
            }
            return data.ToList();
        }

        #endregion

        #region Key Configuration Methods

        public async Task GenerateKeyConfiguration(string key, byte[] salt)
        {
            await _apprepository.GenerateKeyConfigurationAsync(key, salt);
        }

        public async Task<KeyConfig> GetKeyConfig()
        {
            return await _apprepository.GetKeyConfigAsync();
        }

        #endregion

        #region Application Lookup Management

        public async Task<ServiceResult> SaveUpdateEntity(AppLookup data)
        {
            try
            {
                if (data.AppClient == Guid.Empty && data.Id == 0) data.AppClient = Guid.NewGuid();
                
                void EncryptAppLookupFields()
                {
                    // Parse user-selected fields to encrypt (NULL = all for backward compat)
                    var fields = ParseEncryptedFields(data.EncryptedFields);
                    bool encryptAll = data.EncryptedFields == null;

                    // Always encrypt these three fields regardless of user selection
                    if (!string.IsNullOrEmpty(data.AppSecret))
                        data.AppSecret = EncryptionHelper.DataEncryptAsync(data.AppSecret, null, "AppService.SaveUpdateEntity", data.UserId);
                    if (!string.IsNullOrEmpty(data.OwnerEmail))
                        data.OwnerEmail = EncryptionHelper.DataEncryptAsync(data.OwnerEmail, null, "AppService.SaveUpdateEntity.OwnerEmail", data.UserId);
                    if (!string.IsNullOrEmpty(data.AppOwner))
                        data.AppOwner = EncryptionHelper.DataEncryptAsync(data.AppOwner, null, "AppService.SaveUpdateEntity.AppOwner", data.UserId);
                    if ((encryptAll || fields.Contains("CoOwner")) && !string.IsNullOrEmpty(data.CoOwner))
                        data.CoOwner = EncryptionHelper.DataEncryptAsync(data.CoOwner, null, "AppService.SaveUpdateEntity.CoOwner", data.UserId);
                    if ((encryptAll || fields.Contains("CoOwnerEmail")) && !string.IsNullOrEmpty(data.CoOwnerEmail))
                        data.CoOwnerEmail = EncryptionHelper.DataEncryptAsync(data.CoOwnerEmail, null, "AppService.SaveUpdateEntity.CoOwnerEmail", data.UserId);
                    if ((encryptAll || fields.Contains("EmailServer")) && !string.IsNullOrEmpty(data.EmailServer))
                        data.EmailServer = EncryptionHelper.DataEncryptAsync(data.EmailServer, null, "AppService.SaveUpdateEntity.EmailServer", data.UserId);
                    if ((encryptAll || fields.Contains("Port")) && data.Port.HasValue)
                        data.EncryptedPort = EncryptionHelper.DataEncryptAsync(data.Port.Value.ToString(), null, "AppService.SaveUpdateEntity.Port", data.UserId);
                    if ((encryptAll || fields.Contains("FromEmailAddress")) && !string.IsNullOrEmpty(data.FromEmailAddress))
                        data.FromEmailAddress = EncryptionHelper.DataEncryptAsync(data.FromEmailAddress, null, "AppService.SaveUpdateEntity.FromEmailAddress", data.UserId);

                    data.IsEncrypted = encryptAll || fields.Count > 0;
                    data.KeyVersion = EncryptionHelper.GetActiveKeyVersion();
                }
                
                EncryptAppLookupFields();
                return await _apprepository.SaveUpdateEntityAsync(data);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in SaveUpdateEntity: {ex.Message}", ex);
            }
        }

        private static HashSet<string> ParseEncryptedFields(string? fields)
        {
            if (string.IsNullOrWhiteSpace(fields))
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            return new HashSet<string>(
                fields.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                StringComparer.OrdinalIgnoreCase);
        }

        public async Task<IList<AppLookup>> FindAppLookup() { return DecryptAppLookupList(await _apprepository.FindApplicationLookupAsync()); }

        public async Task<IList<AppLookup>> FindAppLookup(int currentPage, int pageSize) => DecryptAppLookupList(await _apprepository.FindApplicationLookupAsync(currentPage, pageSize, null, null, null));

        public async Task<IList<AppLookup>> FindAppLookup(int currentPage, int pageSize, string? searchTerm) => DecryptAppLookupList(await _apprepository.FindApplicationLookupAsync(currentPage, pageSize, searchTerm, null, null));

        public async Task<IList<AppLookup>> FindAppLookup(string userId, int currentPage, int pageSize) => DecryptAppLookupList(await _apprepository.FindApplicationLookupAsync(currentPage, pageSize, null, userId, null));

        public async Task<IList<AppLookup>> FindAppLookup(string userId, int currentPage, int pageSize, string? searchTerm) => DecryptAppLookupList(await _apprepository.FindApplicationLookupAsync(currentPage, pageSize, searchTerm, userId, null));

        public async Task<AppLookup> FindAppLookup(int id) => DecryptAppLookup(await _apprepository.FindApplicationAsync(id));

        public async Task<AppLookup> FindAppLookup(Guid appClientId, bool? active = null) => DecryptAppLookup(await _apprepository.FindApplicationAsync(appClientId, active));

        public Task<ServiceResult> UpdateApplicationApproval(int appId) => _apprepository.UpdateApplicationApprovalAsync(appId);

        public Task<ServiceResult> GetAppCount() => _apprepository.GetAppCountAsync();

        public async Task<ServiceResult> GetAppCount(string? searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetAppCount();
            }
            var countResult = await _apprepository.GetApplicationCountAsync(searchTerm, null, null);
            return ServiceResult.FromCount(countResult.TotalCount);
        }

        public async Task<ServiceResult> GetAppCount(string userId, string? searchTerm)
        {
            var countResult = await _apprepository.GetApplicationCountAsync(searchTerm, userId, null);
            return ServiceResult.FromCount(countResult.TotalCount);
        }

        public async Task<(bool exists, string appName)> CheckApplicationExists(string appName, int? excludeId = null)
        {
            var apps = await FindAppLookup();
            var existing = apps.FirstOrDefault(a => a.AppName.Equals(appName.Trim(), StringComparison.OrdinalIgnoreCase) && (!excludeId.HasValue || a.Id != excludeId.Value));
            return (existing != null, existing?.AppName ?? string.Empty);
        }

        public async Task<ServiceResult> CreateApplication(AppLookup app)
        {
            if (string.IsNullOrEmpty(app.AppSecret))
                app.AppSecret = SecurityHelper.GenerateRandomString();
            return await SaveUpdateEntity(app);
        }

        public async Task<ServiceResult> UpdateApplication(AppLookup app)
        {
            return await SaveUpdateEntity(app);
        }

        private IList<AppLookup> DecryptAppLookupList(IList<AppLookup> list) { foreach (var item in list) DecryptAppLookupFields(item); return list; }

        private AppLookup DecryptAppLookup(AppLookup? item) 
        { 
            if (item != null) DecryptAppLookupFields(item); 
            return item ?? new AppLookup(); 
        }

        private void DecryptAppLookupFields(AppLookup item)
        {
            // Always attempt decryption via DecryptOrDefault - it handles plaintext gracefully
            // via IsEncryptedFormat check, so no need to gate on IsEncrypted flag
            item.FromEmailAddress = EncryptionHelper.DecryptOrDefault(item.FromEmailAddress, null, "AppService", item.UserId);
            item.AppSecret = EncryptionHelper.DecryptOrDefault(item.AppSecret, null, "AppService", item.UserId);
            item.OwnerEmail = EncryptionHelper.DecryptOrDefault(item.OwnerEmail, null, "AppService", item.UserId);
            item.AppOwner = EncryptionHelper.DecryptOrDefault(item.AppOwner, null, "AppService", item.UserId);
            item.CoOwner = EncryptionHelper.DecryptOrDefault(item.CoOwner, null, "AppService", item.UserId);
            item.CoOwnerEmail = EncryptionHelper.DecryptOrDefault(item.CoOwnerEmail, null, "AppService", item.UserId);
            item.EmailServer = EncryptionHelper.DecryptOrDefault(item.EmailServer, null, "AppService", item.UserId);
            if (!string.IsNullOrEmpty(item.EncryptedPort))
            {
                var decryptedPort = EncryptionHelper.DecryptOrDefault(item.EncryptedPort, null, "AppService", item.UserId);
                if (int.TryParse(decryptedPort, out int portValue))
                    item.Port = portValue;
            }
        }

        #endregion

        #region Email Service Lookup (Static)

        public Task<ServiceResult> SaveUpdateEntity(EmailServiceLookup data) => throw new NotSupportedException(MessageConstants.EmailServiceTypesStatic);

        public Task<IEnumerable<EmailServiceLookup>> GetAllEmailServiceLookups() => Task.FromResult<IEnumerable<EmailServiceLookup>>(_staticEmailServices);

        public Task<IEnumerable<EmailServiceLookup>> GetEmailServiceLookup(int currentPage, int pageSize) =>
            Task.FromResult<IEnumerable<EmailServiceLookup>>(_staticEmailServices.Skip((currentPage - 1) * pageSize).Take(pageSize));

        public Task<ServiceResult> GetEmailServiceLookupCount() => Task.FromResult(ServiceResult.FromCount(_staticEmailServices.Count));

        public Task<EmailServiceLookup> GetEmailServiceLookupById(int id) =>
            Task.FromResult(_staticEmailServices.FirstOrDefault(s => s.Id == id) ?? throw new KeyNotFoundException(string.Format(MessageConstants.EmailServiceLookupNotFoundForIdFormat, id)));

        Task<IEnumerable<EmailServiceLookup>> IEmailServiceLookup.GetAll() => GetAllEmailServiceLookups();
        Task<ServiceResult> IEmailServiceLookup.GetCount() => GetEmailServiceLookupCount();
        Task<EmailServiceLookup> IEmailServiceLookup.GetById(int id) => GetEmailServiceLookupById(id);

        #endregion

        #region Activity & Error Logging

        public Task<ServiceResult> Log(ActivityLog data) => _apprepository.LogAsync(data);

        public Task<ServiceResult> Log(int logType, string description, string path) =>
            _apprepository.LogAsync(new ActivityLog { LogTypeLookupId = logType, Description = description, Url = path, LoggedBy = null });

        public Task<ServiceResult> Log(int logType, string description, string path, string user) =>
            _apprepository.LogAsync(new ActivityLog { LogTypeLookupId = logType, Description = description, Url = path, LoggedBy = user });

        public Task<IEnumerable<ActivityLog>> GetAllActivityLogs() => _apprepository.GetAllAsync();

        public Task<IList<ActivityLog>> GetAllActivityLogs(int pageNumber, int pageSize) => _apprepository.GetAllAsync(pageNumber, pageSize);

        public Task<ServiceResult> SignInLog(string userId, string email, string ip, int success) =>
            _apprepository.AppLoginAsync(new AppLogin { UserId = userId, Email = email, IPAddress = ip, Success = success });

        public Task<ServiceResult> ActivityLogCount() => _apprepository.GetActivityLogCountAsync();

        Task<IEnumerable<ActivityLog>> IActivityLog.GetAll() => GetAllActivityLogs();
        Task<IList<ActivityLog>> IActivityLog.GetAll(int pageNumber, int pageSize) => GetAllActivityLogs(pageNumber, pageSize);
        Task<ServiceResult> IActivityLog.Count() => ActivityLogCount();

        public Task<ServiceResult> SaveErrorLog(string error, string path) =>
            _apprepository.SaveAsync(new ErrorLog { Error = error, ErrorSource = path, LoggedBy = string.Empty });

        public Task<ServiceResult> SaveErrorLog(string error, string path, string user) =>
            _apprepository.SaveAsync(new ErrorLog { Error = error, ErrorSource = path, LoggedBy = user });

        #endregion

        #region Authentication & Token Management

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

        /// <summary>
        /// Creates a refresh token JWT, persists it to the database, and returns the token string.
        /// Single source of truth for refresh token creation in AppService (legacy).
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

        public string GenerateJWTTokenAsync(AppUserGetDto source) => GenerateUserTokenAsync(source);

        #endregion

        #region User Registration & Credentials

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

                var saveResult = await _apprepository.SaveAppUserAsync(data);
                Guid userId = saveResult.EntityId ?? Guid.Empty;

                if (!userId.Equals(Guid.Empty))
                {
                    AppUserRole userRoleData = new AppUserRole
                    {
                        UserId = userId,
                        RoleId = (data.RoleId == null) ? (int)RoleType.USER : (int)data.RoleId
                    };

                    await _apprepository.SaveUserRoleAsync(userRoleData);

                    await _apprepository.SaveAppUserCredentialsLogAsync(new PasswordUpdate
                    {
                        UserId = userId,
                        NoOfUpdate = 0,
                        CreatedBy = userId.ToString(),
                        ModifiedBy = userId.ToString()
                    });
                }

                return ServiceResult.FromEntityId(userId);
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
            {
                throw new InvalidOperationException(MessageConstants.UserAlreadyExists);
            }

            if (string.IsNullOrEmpty(payload.Email))
            {
                throw new ArgumentException(MessageConstants.EmailIsRequired);
            }

            string password = string.IsNullOrWhiteSpace(payload.AppSecret) ? SecurityHelper.GenerateRandomString() : payload.AppSecret;

            var user = new AppUser
            {
                Username = payload.Username,
                Email = payload.Email?.Trim(),
                AppSecret = password,
                RoleId = payload.RoleId,
                Upn = payload.Upn
            };

            var regResult = await Registration(user);
            regResult.Data = password;
            return regResult;
        }

        public async Task<ServiceResult> UpdateAppUserCredentials(AppUser data)
        {
            try
            {
                if (string.IsNullOrEmpty(data.AppSecret))
                {
                    throw new ArgumentException(MessageConstants.PasswordRequiredForCredentialUpdate);
                }

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

        #endregion

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

        public async Task<UserDetailsType> UserStateAsync(AuthenticateRequest data)
        {
            try
            {
                var userData = await FindUserByEmailAsync(data.Email);
                if (userData == null) return UserDetailsType.USER_NOT_FOUND;
                
                if (userData.IsAzureAdUser)
                    return UserDetailsType.AZURE_AD_USER;
                
                if (!VerifyPassword(data.Password, userData.AppSecret, userData.Salt)) return UserDetailsType.INVALID_CREDENTIALS;

                return UserDetailsType.USER_ALREADY_EXISTS;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in UserStateAsync: {ex.Message}", ex);
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
                            ResultCodes.ValidationFailure,
                            null,
                            new[] { string.Format(MessageConstants.UserWithEmailNotFoundFormat, data.Email) }
                        );

                    case UserDetailsType.INVALID_CREDENTIALS:
                        return new ApiResponse<AuthTokenResponse>(
                            ResultCodes.ValidationFailure,
                            null,
                            new[] { MessageConstants.InvalidEmailOrPassword }
                        );

                    case UserDetailsType.AZURE_AD_USER:
                        return new ApiResponse<AuthTokenResponse>(
                            ResultCodes.ValidationFailure,
                            null,
                            new[] { MessageConstants.AzureADAuthenticationRequired }
                        );
                }

                var userData = await FindUserByEmailAsync(data.Email);
                if (userData == null)
                {
                    return new ApiResponse<AuthTokenResponse>(
                        ResultCodes.ValidationFailure,
                        null,
                        new[] { string.Format(MessageConstants.NoUserFoundWithEmailFormat, data.Email) }
                    );
                }

                string accessToken = GenerateJWTTokenAsync(userData);
                var (refreshToken, _) = await CreateRefreshTokenAsync(Guid.Parse(userData.UserId));

                return new ApiResponse<AuthTokenResponse>(
                    ResultCodes.Success,
                    new[] { new AuthTokenResponse { AccessToken = accessToken, RefreshToken = refreshToken, ExpiresIn = 1800 } },
                    new[] { MessageConstants.AuthenticationSuccessful }
                );
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
                        ResultCodes.ValidationFailure,
                        null,
                        new[] { MessageConstants.UPNRequiredForAzureAD }
                    );
                }

                var existingUser = await FindUserByUpnAsync(upn);

                if (existingUser == null && !string.IsNullOrWhiteSpace(data?.Email))
                {
                    existingUser = await FindUserByEmailAsync(data.Email.Trim().ToLowerInvariant());
                }

                if (existingUser != null)
                {
                    if (!existingUser.IsAzureAdUser)
                    {
                        return new ApiResponse<AuthTokenResponse>(
                            ResultCodes.ValidationFailure,
                            null,
                            new[] { MessageConstants.PasswordAuthenticationRequired }
                        );
                    }

                    string accessToken = GenerateJWTTokenAsync(existingUser);
                    var (refreshToken, _) = await CreateRefreshTokenAsync(Guid.Parse(existingUser.UserId));

                    return new ApiResponse<AuthTokenResponse>(
                        ResultCodes.Success,
                        new[] { new AuthTokenResponse { AccessToken = accessToken, RefreshToken = refreshToken, ExpiresIn = 1800 } },
                        new[] { MessageConstants.AuthenticationSuccessful }
                    );
                }

                ADUser? adUser = await GetADUserAsync(upn);

                string email;
                string userName;
                
                if (adUser != null)
                {
                    email = !string.IsNullOrWhiteSpace(adUser.Mail) 
                        ? adUser.Mail.Trim().ToLowerInvariant()
                        : !string.IsNullOrWhiteSpace(data?.Email)
                            ? data.Email.Trim().ToLowerInvariant()
                            : upn; // Fallback to UPN if no email found
                    
                    userName = !string.IsNullOrWhiteSpace(adUser.DisplayName) 
                        ? adUser.DisplayName 
                        : !string.IsNullOrWhiteSpace(data?.DisplayName) 
                            ? data.DisplayName 
                            : upn.Split('@')[0];
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
                Guid userId = saveResult.EntityId ?? Guid.Empty;

                if (!userId.Equals(Guid.Empty))
                {
                    await _apprepository.SaveUserRoleAsync(new AppUserRole
                    {
                        UserId = userId,
                        RoleId = (int)RoleType.USER
                    });

                    var createdUser = await FindUserByUpnAsync(upn);
                    if (createdUser != null && createdUser.Roles != null && createdUser.Roles.Count > 0)
                    {
                        string accessToken = GenerateJWTTokenAsync(createdUser);
                        var (refreshToken, _) = await CreateRefreshTokenAsync(Guid.Parse(createdUser.UserId));

                        return new ApiResponse<AuthTokenResponse>(
                            ResultCodes.Success,
                            new[] { new AuthTokenResponse { AccessToken = accessToken, RefreshToken = refreshToken, ExpiresIn = 1800 } },
                            new[] { MessageConstants.AzureADAuthenticationSuccessful }
                        );
                    }
                    else
                    {
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
                            new[] { MessageConstants.AzureADAuthenticationSuccessful }
                        );
                    }
                }

                return new ApiResponse<AuthTokenResponse>(
                    ResultCodes.SystemError,
                    null,
                    new[] { MessageConstants.FailedToCreateAzureADUserAccount }
                );
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in AuthenticateAzureUserAsync: {ex.Message}", ex);
            }
        }

        #region User Management

        public async Task<AppUserGetDto?> FindUserByUpnAsync(string upn)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(upn)) return null;

                AppUserGetDto? result = await _apprepository.FindAppUserByUpnAsync(upn.Trim().ToLowerInvariant());
                if (result != null)
                {
                    result.Email = EncryptionHelper.DecryptOrDefault(result.Email, null, "AppService", result.UserId.ToString());
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in FindUserByUpnAsync: {ex.Message}", ex);
            }
        }

        public async Task<IList<AppUserGetDto>> FindAllAppUser()
        {
            try
            {
                var userList = await _apprepository.FindAppUsersAsync();
                foreach (var user in userList)
                {
                    user.Email = EncryptionHelper.DecryptOrDefault(user.Email, null, "AppService", user.UserId.ToString());
                    user.Upn = EncryptionHelper.DecryptOrDefault(user.Upn, null, "AppService", user.UserId.ToString());
                }
                return userList.ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in FindAllAppUser: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Lightweight user list for dropdowns - returns only UserId, Email, Username, Upn.
        /// No roles, no sensitive credential data, much faster.
        /// </summary>
        public async Task<IList<UserShortInfo>> FindAllAppUserDDL()
        {
            try
            {
                var userList = await _apprepository.FindAppUsersAsync();
                var ddlList = new List<UserShortInfo>();
                foreach (var user in userList)
                {
                    ddlList.Add(new UserShortInfo
                    {
                        UserId = user.UserId,
                        Email = EncryptionHelper.DecryptOrDefault(user.Email, null, "AppService.DDL", user.UserId.ToString()),
                        Username = user.Username ?? string.Empty
                    });
                }
                return ddlList;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in FindAllAppUserDDL: {ex.Message}", ex);
            }
        }

        public async Task<IList<AppUserGetDto>> FindAllAppUser(int currentPage, int pageSize)
        {
            var data = await _apprepository.FindAppUsersAsync(currentPage, pageSize, null, null);
            foreach (var user in data)
            {
                user.Email = EncryptionHelper.DecryptOrDefault(user.Email, null, "AppService", user.UserId.ToString());
                user.Upn = EncryptionHelper.DecryptOrDefault(user.Upn, null, "AppService", user.UserId.ToString());
            }
            return data;
        }

        public async Task<IList<AppUserGetDto>> FindAllAppUser(int currentPage, int pageSize, string? searchTerm)
        {
            var data = await _apprepository.FindAppUsersAsync(currentPage, pageSize, searchTerm, null, null, null);
            foreach (var user in data)
            {
                user.Email = EncryptionHelper.DecryptOrDefault(user.Email, null, "AppService", user.UserId.ToString());
                user.Upn = EncryptionHelper.DecryptOrDefault(user.Upn, null, "AppService", user.UserId.ToString());
            }
            return data;
        }

        public async Task<IList<AppUserGetDto>> FindAllAppUser(int currentPage, int pageSize, string? searchTerm, int? roleId, int? active, string? sortBy)
        {
            bool? activeFilter = active.HasValue ? (active.Value == 1) : null;
            
            var data = await _apprepository.FindAppUsersAsync(currentPage, pageSize, searchTerm, activeFilter, roleId, sortBy);
            foreach (var user in data)
            {
                user.Email = EncryptionHelper.DecryptOrDefault(user.Email, null, "AppService", user.UserId.ToString());
                user.Upn = EncryptionHelper.DecryptOrDefault(user.Upn, null, "AppService", user.UserId.ToString());
            }
            return data;
        }

        public async Task<AppUserGetDto?> FindUserByEmailAsync(string email)
        {
            try
            {
                string emailBlindIndex = EncryptionHelper.GenerateBlindIndex(email);
                AppUserGetDto? result = await _apprepository.FindAppUserByEmailAsync(emailBlindIndex);
                if (result != null)
                {
                    result.Email = EncryptionHelper.DecryptOrDefault(result.Email, null, "AppService", result.UserId.ToString());
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in FindUserByEmailAsync: {ex.Message}", ex);
            }
        }

        public async Task<AppUserGetDto?> FindUserByIdAsync(string userId)
        {
            try
            {
                AppUserGetDto? result = await _apprepository.FindAppUserByIdAsync(userId);
                if (result != null)
                {
                    result.Email = EncryptionHelper.DecryptOrDefault(result.Email, null, "AppService", userId);
                    result.Upn = result.Upn;
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in FindUserByIdAsync: {ex.Message}", ex);
            }
        }

        public async Task<ServiceResult> GetUserCount()
        {
            return await _apprepository.GetAppUserCountAsync();
        }

        public async Task<ServiceResult> GetUserCount(string? searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetUserCount();
            }
            var countResult = await _apprepository.GetUserCountAsync(searchTerm, null, null);
            return ServiceResult.FromCount(countResult.TotalCount);
        }

        public async Task<ServiceResult> GetUserCount(string? searchTerm, int? roleId, int? active)
        {
            bool? activeFilter = active.HasValue ? (active.Value == 1) : null;
            var countResult = await _apprepository.GetUserCountAsync(searchTerm, activeFilter, roleId);
            return ServiceResult.FromCount(countResult.TotalCount);
        }

        public Task<ServiceResult> UpdateVerifiedUserAsync(Guid userId, int active = 1) => _apprepository.UpdateToVerifiedUserAsync(userId, active);

        public Task<ServiceResult> UpdateUserRoleAsync(Guid userId, int newRoleId, string modifiedBy) => _apprepository.UpdateUserRoleAsync(userId, newRoleId, modifiedBy);

        public Task<IList<AppRole>> FindAppRole() => _apprepository.FindAppRoleAsync();

        #endregion

        #region Azure AD Graph API Integration

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

        #endregion

        public bool VerifyPassword(string password, string hash, string salt)
            => SecurityHelper.VerifyPassword(password, hash, salt);
    }
}

