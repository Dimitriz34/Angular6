using TPEmail.BusinessModels.RequestModels;
using TPEmail.BusinessModels.ResponseModels;
using TPEmail.BusinessModels.Enums;
using Microsoft.Extensions.Configuration;

namespace TPEmail.DataAccess.Interface.Service.v1_0
{
    /// <summary>
    /// Service interface for authentication and authorization operations.
    /// Covers user registration, password/Azure AD authentication, JWT token management,
    /// user CRUD, role management, and Microsoft Graph API lookups.
    /// </summary>
    public interface IAuthService
    {
        IConfiguration GetConfiguration();

        // Registration
        Task<ServiceResult> Registration(AppUser data);
        Task<ServiceResult> RegisterUser(AppUserPostDto payload);

        // Authentication
        Task<AppUserGetDto> Authenticate(AuthenticateRequest data);
        Task<ApiResponse<AuthTokenResponse>> AuthenticateUserAsync(AuthenticateRequest data);
        Task<ApiResponse<AuthTokenResponse>> AuthenticateAzureUserAsync(AzureAuthenticateRequest data);
        Task<UserDetailsType> UserStateAsync(AuthenticateRequest data);
        bool VerifyPassword(string password, string hash, string salt);

        // JWT
        string GenerateJWTTokenAsync(AppUserGetDto source);
        string GenerateUserTokenAsync(AppUserGetDto data);
        Guid? ValidateTokenAsync(string token);

        // User management
        Task<ServiceResult> UpdateAppUserCredentials(AppUser data);
        Task<IList<AppUserGetDto>> FindAllAppUser();
        Task<IList<AppUserGetDto>> FindAllAppUser(int currentPage, int pageSize);
        Task<IList<AppUserGetDto>> FindAllAppUser(int currentPage, int pageSize, string? searchTerm);
        Task<IList<AppUserGetDto>> FindAllAppUser(int currentPage, int pageSize, string? searchTerm, int? roleId, int? active, string? sortBy);
        Task<ServiceResult> GetCount();
        Task<ServiceResult> GetUserCount();
        Task<ServiceResult> GetUserCount(string? searchTerm);
        Task<ServiceResult> GetUserCount(string? searchTerm, int? roleId, int? active);
        Task<AppUserGetDto?> FindUserByEmailAsync(string email);
        Task<AppUserGetDto?> FindUserByUpnAsync(string upn);
        Task<AppUserGetDto?> FindUserByIdAsync(string userId);
        Task<ServiceResult> UpdateVerifiedUserAsync(Guid userId, int active = 1);
        Task<ServiceResult> UpdateUserRoleAsync(Guid userId, int newRoleId, string modifiedBy);
        Task<IList<AppRole>> FindAppRole();

        // Azure AD / Microsoft Graph
        Task<ADUser?> GetADUserAsync(string upn);
        Task<ADUserPhotoResponse> GetADUserPhotoAsync(string upn);
        Task<ADUserSearchResponse> SearchADUsersAsync(string term, string? searchType = null);

        // Login audit
        Task<ServiceResult> SignInLog(string userId, string email, string ip, int success);

        // Refresh tokens
        Task<ApiResponse<AuthTokenResponse>> RefreshTokensAsync(string refreshToken, string clientIp);
        Task RevokeRefreshTokenAsync(string refreshToken, string clientIp, string reason);

        // Application authentication
        Task<ApiResponse<AuthTokenResponse>> AuthenticateApplicationAsync(string appClientId, string appSecret);
    }
}
