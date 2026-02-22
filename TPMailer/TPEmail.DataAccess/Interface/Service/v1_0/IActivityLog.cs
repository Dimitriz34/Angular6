using TPEmail.BusinessModels.ResponseModels;

namespace TPEmail.DataAccess.Interface.Service.v1_0
{
    public interface IActivityLog
    {
        Task<ServiceResult> Log(ActivityLog data);
        Task<ServiceResult> Log(int logType, string description, string path);
        Task<ServiceResult> Log(int logType, string description, string path, string user);
        Task<IEnumerable<ActivityLog>> GetAllActivityLogs();
        Task<IList<ActivityLog>> GetAllActivityLogs(int pageNumber, int pageSize);
        Task<ServiceResult> SignInLog(string userId, string email, string ip, int success);
        Task<ServiceResult> ActivityLogCount();
        Task<IEnumerable<ActivityLog>> GetAll();
        Task<IList<ActivityLog>> GetAll(int pageNumber, int pageSize);
        Task<ServiceResult> Count();
    }
}
