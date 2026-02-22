using TPEmail.BusinessModels.ResponseModels;

namespace TPEmail.DataAccess.Interface.Service.v1_0
{
    public interface IErrorLog
    {
        Task<ServiceResult> SaveErrorLog(string error, string path);
        Task<ServiceResult> SaveErrorLog(string error, string path, string user);
    }
}
