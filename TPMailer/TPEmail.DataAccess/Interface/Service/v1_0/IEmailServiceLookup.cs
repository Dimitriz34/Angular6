using TPEmail.BusinessModels.ResponseModels;

namespace TPEmail.DataAccess.Interface.Service.v1_0
{
    public interface IEmailServiceLookup
    {
        Task<ServiceResult> SaveUpdateEntity(EmailServiceLookup data);
        Task<IEnumerable<EmailServiceLookup>> GetAllEmailServiceLookups();
        Task<IEnumerable<EmailServiceLookup>> GetEmailServiceLookup(int currentPage, int pageSize);
        Task<ServiceResult> GetEmailServiceLookupCount();
        Task<EmailServiceLookup> GetEmailServiceLookupById(int id);
        Task<IEnumerable<EmailServiceLookup>> GetAll();
        Task<ServiceResult> GetCount();
        Task<EmailServiceLookup> GetById(int id);
    }
}
