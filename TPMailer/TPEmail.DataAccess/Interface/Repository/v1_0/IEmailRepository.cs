using TPEmail.BusinessModels.RequestModels;
using TPEmail.BusinessModels.ResponseModels;

namespace TPEmail.DataAccess.Interface.Repository.v1_0
{
    public interface IEmailRepository
    {
        Task<ServiceResult> SaveUpdateEntityAsync(Email data);
        Task<ServiceResult> SaveEmailRecipientsAsync(EmailRecipients data);
        Task<ServiceResult> SaveEmailAttachmentAsync(EmailAttachment data);
        Task<ServiceResult> UpdateEmailStatusAsync(Guid emailId, string status, string? errorCode = null, string? errorMessage = null, string? modifiedBy = null);

        Task<IList<EmailGetDto>> FindAllEmailsAsync();
        /// <summary>
        /// Retrieves emails with database-level pagination and filtering.
        /// All filtering, searching, and pagination is performed by the stored procedure.
        /// </summary>
        Task<IList<EmailGetDto>> FindAllEmailsAsync(int pageIndex, int pageSize, string? searchTerm = null, string? appName = null, DateTime? startDate = null, DateTime? endDate = null, string? userId = null, int? appCode = null);
        /// <summary>
        /// Returns total count of emails matching the given filters (for pagination metadata).
        /// </summary>
        Task<ServiceResult> GetEmailListCountAsync(string? searchTerm = null, string? appName = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<EmailDetailDto?> GetEmailDetailAsync(Guid emailId);
        Task<IList<EmailRecipientsGetDto>> FindAllEmailRecipientsAsync();
        Task<IList<EmailRecipientsGetDto>> FindEmailRecipientsByEmailIdAsync(Guid emailId);
        Task<IList<EmailAttachment>> FindAllEmailAttachmentsAsync();
        Task<IList<EmailAttachment>> FindEmailAttachmentsByEmailIdAsync(Guid emailId);
        // EmailServiceLookup removed - now static in AppService (CommonUtils.Services enum)

        Task<ServiceResult> GetNumberOfAppUserEmailCountAsync(string? userId = null);
        Task<ServiceResult> GetNumberOfMonthlyAppUserEmailCountAsync(string? userId = null);
        Task<ServiceResult> GetNumberOfTodayAppUserEmailCountAsync(string? userId = null);
    }
}
