using TPEmail.BusinessModels.RequestModels;
using TPEmail.BusinessModels.ResponseModels;
using Microsoft.AspNetCore.Http;

namespace TPEmail.DataAccess.Interface.Service.v1_0
{
    public interface IEmailService : IEmailRecipients
    {
        Task<ServiceResult> SendEmail(Email email);
        Task<ServiceResult> TrySendEmail(Email email);
        Task<(Email email, AppLookup appInfo)> PrepareEmailFromRequest(SendEmailRequest request, string userId, IFormFile[] files, System.Security.Claims.ClaimsPrincipal user);
        Task<ServiceResult> SaveUpdateEntity(Email data);
        Task<IList<EmailGetDto>> FindAllEmails(string? emailId);
        Task<IList<EmailGetDto>> FindAllEmails(int pageNumber, int pageSize, string? searchTerm = null, string? appName = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<IList<EmailGetDto>> FindAllEmails(int? appId);
        Task<ServiceResult> CountEmailAsync(string? searchTerm = null, string? appName = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<EmailRecipients>> GetEmailRecipients();
        Task<EmailDetailDto?> GetEmailDetailAsync(Guid emailId);
    }
}
