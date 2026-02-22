using System.ComponentModel.DataAnnotations;

namespace TPEmail.BusinessModels.ResponseModels
{
    public class AppLookup
    {
        public int Id { get; set; }
        public Guid AppClient { get; set; }
        public string AppSecret { get; set; } = string.Empty;
        
        /// <summary>
        /// Flag indicating whether sensitive fields (AppSecret, OwnerEmail, AppOwner, EmailServer, Port) are encrypted.
        /// </summary>
        public bool IsEncrypted { get; set; }

        [Display(Name = "Application Name")]
        public string AppName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string AppOwner { get; set; } = string.Empty;

        public string CoOwner { get; set; } = string.Empty;
        public string CoOwnerEmail { get; set; } = string.Empty;

        [EmailAddress]
        public string OwnerEmail { get; set; } = string.Empty;
        public int Active { get; set; }
        public int? EmailServiceId { get; set; }
        public string EmailServiceName { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime CreatedDateTime { get; set; }
        public DateTime ModifiedDateTime { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string ModifiedBy { get; set; } = string.Empty;

        public string FromEmailAddress { get; set; } = string.Empty;
        public string FromEmailDisplayName { get; set; } = string.Empty;
        public string EmailServer { get; set; } = string.Empty;

        [Range(0, 65535)]
        public int? Port { get; set; }
        
        /// <summary>
        /// Encrypted port value stored in database. Used for encryption/decryption since Port is int.
        /// </summary>
        public string EncryptedPort { get; set; } = string.Empty;
        
        public bool IsVerified { get; set; }
        public bool IsInternalApp { get; set; }

        /// <summary>
        /// Enables TP Data Assist (AI enhancement) for this application.
        /// </summary>
        public bool UseTPAssist { get; set; }

        /// <summary>
        /// Comma-separated list of field names that should be encrypted.
        /// NULL = backward compatible (all fields encrypted when IsEncrypted=true).
        /// Empty = no encryption. Values: AppSecret,AppOwner,OwnerEmail,CoOwner,CoOwnerEmail,EmailServer,Port,FromEmailAddress
        /// </summary>
        public string? EncryptedFields { get; set; }

        /// <summary>
        /// Tracks which encryption key version was used for this application's encrypted fields.
        /// </summary>
        public int? KeyVersion { get; set; }
    }
}
