using TPEmail.BusinessModels.RequestModels;

namespace TPEmail.DataAccess.Interface.Service.v1_0
{
    public interface IJWTToken
    {
        string GenerateUserTokenAsync(AppUserGetDto data);
        Guid? ValidateTokenAsync(string token);
    }
}
