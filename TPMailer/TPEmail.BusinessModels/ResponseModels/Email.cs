using System.ComponentModel.DataAnnotations;

namespace TPEmail.BusinessModels.ResponseModels
{
    public class Email
    {
        public Guid EmailId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int AppId { get; set; }
        public int EmailServiceId { get; set; }

        [DataType(DataType.EmailAddress)]
        public string Sender { get; set; } = string.Empty;
        public string FromEmailDisplayName { get; set; } = string.Empty;
        public string ToDisplayName { get; set; } = string.Empty;
        public string EmailSecret { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsHtml { get; set; }
        public IList<EmailRecipients> To { get; set; } = new List<EmailRecipients>();
        public IList<EmailRecipients> Cc { get; set; } = new List<EmailRecipients>();
        public IList<EmailAttachment> EmailAttachments { get; set; } = new List<EmailAttachment>();
        public int? Active { get; set; }
        public DateTime? CreationDateTime { get; set; }
        public DateTime? ModificationDateTime { get; set; }
        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }
        public string FromEmailAddress { get; set; } = string.Empty;
        public string EmailServer { get; set; } = string.Empty;
        public int Port { get; set; }
        public bool SkipDatabaseLog { get; set; } = false;
        public bool IsInternalApp { get; set; } = false;
        public string? EncryptedFields { get; set; }

        /// <summary>
        /// Tracks which encryption key version was used. Set by EncryptionHelper.GetActiveKeyVersion().
        /// </summary>
        public int KeyVersion { get; set; }
    }
}
