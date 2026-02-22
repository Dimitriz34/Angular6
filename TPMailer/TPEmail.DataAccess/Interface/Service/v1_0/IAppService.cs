using TPEmail.BusinessModels.RequestModels;
using TPEmail.BusinessModels.ResponseModels;
using TPEmail.BusinessModels.Enums;
using Microsoft.Extensions.Configuration;

namespace TPEmail.DataAccess.Interface.Service.v1_0
{
    public interface IAppService : IActivityLog, IErrorLog, IAppLookup, IEmailServiceLookup, IJWTToken, ICommonData, IUserService
    {
        IConfiguration GetConfiguration();
    }
}
