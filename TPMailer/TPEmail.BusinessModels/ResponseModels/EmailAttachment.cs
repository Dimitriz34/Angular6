using Microsoft.AspNetCore.Http;

namespace TPEmail.BusinessModels.ResponseModels
{

    public class EmailAttachment
    {
        public int? Id { get; set; }
        public string EmailId { get; set; } = string.Empty;
        public string AttachmentName { get; set; } = string.Empty;
        public string AttachmentType { get; set; } = string.Empty;
        public string AttachmentPath { get; set; } = string.Empty;
        public IFormFile Attachment { get; set; } = null!;
        public byte[]? FileBytes { get; set; }
    }
}
