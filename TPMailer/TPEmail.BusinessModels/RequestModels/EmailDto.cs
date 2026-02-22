using TPEmail.BusinessModels.ResponseModels;

namespace TPEmail.BusinessModels.RequestModels
{
    public class EmailGetDto
    {
        public string? EmailId { get; set; }
        public int? AppId { get; set; }
        public string? AppName { get; set; }
        public string? UserId { get; set; }
        public string? Username { get; set; }
        public string? Upn { get; set; }
        public int? EmailServiceLookupId { get; set; }
        public string? ServiceName { get; set; } = string.Empty;
        public string? Sender { get; set; }
        public string? FromEmailAddress { get; set; }
        public string? FromDisplayName { get; set; }
        public string ToDisplayName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public int IsHtml { get; set; }
        public string Status { get; set; } = "Pending";
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public int Active { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string ModifiedBy { get; set; } = string.Empty;
        public DateTime CreationDateTime { get; set; }
        public DateTime ModificationDateTime { get; set; }
        public IList<EmailRecipientsGetDto> EmailRecipients { get; set; } = new List<EmailRecipientsGetDto>();
        public IList<EmailAttachment> EmailAttachments { get; set; } = new List<EmailAttachment>();
    }

    public class EmailPostDto
    {
        public string Subject { get; set; } = string.Empty;
        public string? Body { get; set; }
        public bool IsHtml { get; set; }
        public string ToRecipients { get; set; } = string.Empty;
        public string? CcRecipients { get; set; }
    }

    /// <summary>
    /// Request model for SendEmail API with all required fields including TPAssist flag
    /// </summary>
    public class SendEmailRequest
    {
        public string Subject { get; set; } = string.Empty;
        public string? Body { get; set; }
        public bool IsHtml { get; set; }
        public string ToRecipients { get; set; } = string.Empty;
        public string? CcRecipients { get; set; }
        public int? AppId { get; set; }
        public string? AppPassword { get; set; }
        public string? SmtpUserEmail { get; set; }
        /// <summary>
        /// Optional flag to enable AI enhancement of email body. Defaults to false.
        /// </summary>
        public bool UseTPAssist { get; set; } = false;
    }

    /// <summary>
    /// Request model for GetTPAssist API
    /// </summary>
    public class TPAssistRequest
    {
        public string Body { get; set; } = string.Empty;
        public string? Subject { get; set; }
        public bool IsHtml { get; set; }
    }

    public class EmailRecipientsGetDto
    {
        public string Id { get; set; } = string.Empty;
        public string EmailId { get; set; } = string.Empty;
        public string? ToDisplayName { get; set; }
        public string Recipient { get; set; } = string.Empty;
        public string RecipientType { get; set; } = string.Empty;
        public DateTime CreateDateTime { get; set; }
    }

    public class EmailServiceLookupDto
    {
        public int Id { get; set; }
        public string ServiceName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Detailed email DTO for viewing full email information
    /// </summary>
    public class EmailDetailDto
    {
        public string? EmailId { get; set; }
        public string? UserId { get; set; }
        public int? AppCode { get; set; }
        public string? AppName { get; set; }
        public string? SenderFrom { get; set; }
        public string? ReplyTo { get; set; }
        public string? Recipients { get; set; }
        public string? CcRecipients { get; set; }
        public string? BccRecipients { get; set; }
        public string? Subject { get; set; }
        public string? Body { get; set; }
        public bool IsHtmlBody { get; set; }
        public string? Priority { get; set; }
        public string? Status { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; }
        public int MaxRetry { get; set; }
        public DateTime? ScheduledDateTime { get; set; }
        public DateTime? SentDateTime { get; set; }
        public string? TrackingId { get; set; }
        public string? GraphMessageId { get; set; }
        public bool Active { get; set; }
        public DateTime? CreatedDateTime { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedDateTime { get; set; }
        public string? ModifiedBy { get; set; }
        public string? ServiceName { get; set; }
        public string? Upn { get; set; }
        public string? Username { get; set; }
        public IList<EmailRecipientsGetDto> EmailRecipients { get; set; } = new List<EmailRecipientsGetDto>();
        public IList<EmailAttachment> EmailAttachments { get; set; } = new List<EmailAttachment>();
    }
}
