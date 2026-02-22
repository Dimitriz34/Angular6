using TPEmail.BusinessModels.RequestModels;
using TPEmail.BusinessModels.ResponseModels;
using TPEmail.BusinessModels.Enums;

namespace TPEmail.DataAccess.Interface.Service.v1_0
{
    public interface IUserService
    {
        Task<ServiceResult> Registration(AppUser data);
        Task<ServiceResult> RegisterUser(AppUserPostDto payload);
        Task<AppUserGetDto> Authenticate(AuthenticateRequest data);
        Task<ApiResponse<AuthTokenResponse>> AuthenticateUserAsync(AuthenticateRequest data);
        Task<ApiResponse<AuthTokenResponse>> AuthenticateAzureUserAsync(AzureAuthenticateRequest data);
        Task<ServiceResult> UpdateAppUserCredentials(AppUser data);
        bool VerifyPassword(string password, string hash, string salt);
        string GenerateJWTTokenAsync(AppUserGetDto source);
        Task<UserDetailsType> UserStateAsync(AuthenticateRequest data);
        Task<IList<AppUserGetDto>> FindAllAppUser();
        Task<IList<UserShortInfo>> FindAllAppUserDDL();
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
        Task<ADUser?> GetADUserAsync(string upn);
        Task<ADUserPhotoResponse> GetADUserPhotoAsync(string upn);
        Task<ADUserSearchResponse> SearchADUsersAsync(string term, string? searchType = null);
    }
}
