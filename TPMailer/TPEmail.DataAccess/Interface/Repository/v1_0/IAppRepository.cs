using TPEmail.BusinessModels.RequestModels;
using TPEmail.BusinessModels.ResponseModels;

namespace TPEmail.DataAccess.Interface.Repository.v1_0
{
    public interface IAppRepository
    {
        // Common
        Task<ServiceResult> GetCountAsync(string tableName, string condition);
        Task<IList<DashboardEmailDto>> FindAdminDashboardDataAsync(string? userId = null);
        Task<IList<Top10AppsDto>> FindTop10AppsAsync();
        Task<IList<Top5AppsUtilisationDto>> FindTop5AppsUtilisationAsync();
        Task GenerateKeyConfigurationAsync(string key, byte[] salt);
        Task<KeyConfig> GetKeyConfigAsync();

        // EmailServiceLookup removed - now static in AppService (CommonUtils.Services enum)

        // AppLookup - with database-level pagination and filtering
        Task<ServiceResult> SaveUpdateEntityAsync(AppLookup data);
        Task<IList<AppLookup>> FindApplicationLookupAsync();
        Task<IList<AppLookup>> FindApplicationLookupAsync(int pageIndex, int pageSize, string? searchTerm = null, string? userId = null, bool? active = null);
        Task<AppLookup?> FindApplicationAsync(int id);
        Task<AppLookup?> FindApplicationAsync(Guid id, bool? active = null);
        Task<ServiceResult> UpdateApplicationApprovalAsync(int appId);
        Task<ServiceResult> GetAppCountAsync();
        Task<ApplicationCountDto> GetApplicationCountAsync(string? searchTerm = null, string? userId = null, bool? active = null);

        // Log
        Task<ServiceResult> LogAsync(ActivityLog data);
        Task<ServiceResult> AppLoginAsync(AppLogin data);
        Task<IEnumerable<ActivityLog>> GetAllAsync();
        Task<IList<ActivityLog>> GetAllAsync(int pageIndex, int pageSize);
        Task<ServiceResult> GetActivityLogCountAsync();
        Task<ServiceResult> SaveAsync(ErrorLog data);

        // User - with database-level pagination and filtering
        Task<ServiceResult> SaveAppUserAsync(AppUser data);
        Task<ServiceResult> SaveUserRoleAsync(AppUserRole data);
        Task<ServiceResult> UpdateUserRoleAsync(Guid userId, int newRoleId, string modifiedBy);
        Task<ServiceResult> UpdateToVerifiedUserAsync(Guid userId, int active = 1);
        Task<ServiceResult> UpdateAppUserCredentialsAsync(AppUser data);
        Task<ServiceResult> AppSecreteUpdateLogAsync(AppUser data);
        Task<ServiceResult> SaveAppUserCredentialsLogAsync(PasswordUpdate data);
        Task<IList<AppUserGetDto>> FindAppUsersAsync();
        Task<IList<AppUserGetDto>> FindAppUsersAsync(int pageIndex, int pageSize, string? searchTerm = null, bool? active = null, int? roleId = null, string? sortBy = null);
        Task<AppUserGetDto?> FindAppUserByEmailAsync(string email);
        Task<AppUserGetDto?> FindAppUserByUpnAsync(string upn);
        Task<AppUserGetDto?> FindAppUserByIdAsync(string userId);
        Task<ServiceResult> GetAppUserCountAsync();
        Task<UserCountDto> GetUserCountAsync(string? searchTerm = null, bool? active = null, int? roleId = null);
        Task<IList<AppRole>> FindAppRoleAsync();
    }
}
