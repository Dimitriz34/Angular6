using System.ComponentModel.DataAnnotations;

namespace TPEmail.BusinessModels.ResponseModels
{
    public class EmailRecipients
    {
        public int? Id { get; set; }
        public string EmailId { get; set; } = string.Empty;
        public string ToDisplayName { get; set; } = string.Empty;

        [DataType(DataType.EmailAddress)]
        [EmailAddress]
        public string Recipient { get; set; } = string.Empty;
        public int Type { get; set; }
        public int Status { get; set; }
        public DateTime? CreateDateTime { get; set; }
    }
}
