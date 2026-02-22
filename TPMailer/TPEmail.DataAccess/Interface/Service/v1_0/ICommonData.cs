using TPEmail.BusinessModels.RequestModels;
using TPEmail.BusinessModels.ResponseModels;

namespace TPEmail.DataAccess.Interface.Service.v1_0
{
    public interface ICommonData
    {
        Task<ServiceResult> GetCount(string tableName, string condition);
        Task<ServiceResult> GetCount(string tableName);
        Task<List<DashboardEmailDto>> FindAdminDashboardData(string? userId = null);
        Task<List<Top10AppsDto>> FindTop10Apps();
        Task<List<Top5AppsUtilisationDto>> FindTop5AppsUtilisation();
        Task GenerateKeyConfiguration(string key, byte[] salt);
        Task<KeyConfig> GetKeyConfig();
    }
}
