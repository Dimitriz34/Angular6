using TPEmail.BusinessModels.RequestModels;
using TPEmail.BusinessModels.ResponseModels;

namespace TPEmail.DataAccess.Interface.Repository.v1_0
{
    public interface IAuthRepository
    {
        Task<ServiceResult> SaveAppUserAsync(AppUser data);
        Task<ServiceResult> SaveUserRoleAsync(AppUserRole data);
        Task<ServiceResult> UpdateUserRoleAsync(Guid userId, int newRoleId, string modifiedBy);
        Task<ServiceResult> UpdateToVerifiedUserAsync(Guid userId, int active = 1);
        Task<ServiceResult> UpdateAppUserCredentialsAsync(AppUser data);
        Task<ServiceResult> AppSecreteUpdateLogAsync(AppUser data);
        Task<ServiceResult> SaveAppUserCredentialsLogAsync(PasswordUpdate data);

        Task<AppUserGetDto?> FindAppUserByEmailAsync(string email);
        Task<AppUserGetDto?> FindAppUserByUpnAsync(string upn);
        Task<AppUserGetDto?> FindAppUserByIdAsync(string userId);
        Task<IList<AppUserGetDto>> FindAppUsersAsync();
        Task<IList<AppUserGetDto>> FindAppUsersAsync(int pageIndex, int pageSize, string? searchTerm = null, bool? active = null, int? roleId = null, string? sortBy = null);
        Task<ServiceResult> GetAppUserCountAsync();
        Task<UserCountDto> GetUserCountAsync(string? searchTerm = null, bool? active = null, int? roleId = null);
        Task<IList<AppRole>> FindAppRoleAsync();
        Task<ServiceResult> AppLoginAsync(AppLogin data);

        Task<RefreshTokenDto?> GetRefreshTokenByIdAsync(Guid tokenId);
        Task InsertRefreshTokenAsync(Guid tokenId, Guid userId, DateTime issuedAt, DateTime expiresAt, string createdByIp);
        Task RevokeRefreshTokenAsync(Guid tokenId, DateTime revokedAt, string revokedByIp, string reasonRevoked);
        Task<ServiceResult> CleanupExpiredTokensAsync();
    }
}
