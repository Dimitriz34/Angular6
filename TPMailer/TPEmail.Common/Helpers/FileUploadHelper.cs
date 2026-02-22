#nullable disable

using Microsoft.AspNetCore.Http;
using TPEmail.BusinessModels.ResponseModels;
using TPEmail.BusinessModels.Constants;

namespace TPEmail.Common.Helpers
{
    public static class FileUploadHelper
    {
        private static readonly string[] _supportedfileextensions =
        {
            ".jpg", ".png", ".jpeg", ".gif", ".tiff", ".bmp", ".pdf", ".csv", ".doc",
            ".docx", ".txt", ".rtf", ".xls", ".xlsx", ".ppt", ".pptx", ".psd"
        };

        private static readonly HashSet<string> _supportedfileextensionsset = new(
            _supportedfileextensions,
            StringComparer.OrdinalIgnoreCase);

        public static bool IsSupportedFileType(string extension)
        {
            return _supportedfileextensionsset.Contains(extension);
        }

        public static IList<string> GetSupportedFileTypes()
        {
            return _supportedfileextensions.ToList();
        }

        public static async Task<IList<EmailAttachment>> ProcessFileUploads(Guid emailId, IList<IFormFile> files)
        {
            IList<EmailAttachment> emailAttachments = new List<EmailAttachment>();

            foreach (IFormFile file in files)
            {
                string extension = Path.GetExtension(file.FileName);
                if (!IsSupportedFileType(extension))
                {
                    throw new InvalidDataException(MessageConstants.InvalidFileExtension);
                }

                if (file.Length > 0)
                {
                    byte[] fileBytes;
                    using (var ms = new MemoryStream())
                    {
                        await file.CopyToAsync(ms);
                        fileBytes = ms.ToArray();
                    }

                    emailAttachments.Add(
                        new EmailAttachment()
                        {
                            Attachment = file,
                            AttachmentName = file.FileName,
                            AttachmentPath = string.Empty,
                            AttachmentType = file.ContentType,
                            FileBytes = fileBytes
                        }
                    );
                }
            }

            return emailAttachments;
        }
    }
}
