using TPEmail.BusinessModels.ResponseModels;

namespace TPEmail.DataAccess.Interface.Service.v1_0
{
    public interface IAppLookup
    {
        Task<ServiceResult> SaveUpdateEntity(AppLookup data);
        Task<IList<AppLookup>> FindAppLookup();
        Task<IList<AppLookup>> FindAppLookup(int currentPage, int pageSize);
        Task<IList<AppLookup>> FindAppLookup(int currentPage, int pageSize, string? searchTerm);
        Task<IList<AppLookup>> FindAppLookup(string userId, int currentPage, int pageSize);
        Task<IList<AppLookup>> FindAppLookup(string userId, int currentPage, int pageSize, string? searchTerm);
        Task<AppLookup> FindAppLookup(int id);
        Task<AppLookup> FindAppLookup(Guid appClientId, bool? active = null);
        Task<ServiceResult> UpdateApplicationApproval(int appId);
        Task<ServiceResult> GetAppCount();
        Task<ServiceResult> GetAppCount(string? searchTerm);
        Task<ServiceResult> GetAppCount(string userId, string? searchTerm);
        Task<(bool exists, string appName)> CheckApplicationExists(string appName, int? excludeId = null);
        Task<ServiceResult> CreateApplication(AppLookup app);
        Task<ServiceResult> UpdateApplication(AppLookup app);
    }
}
