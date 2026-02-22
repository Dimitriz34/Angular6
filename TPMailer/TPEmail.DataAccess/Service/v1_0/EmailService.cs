using Microsoft.Exchange.WebServices.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using MimeKit;
using MailKit.Net.Smtp;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Collections.Concurrent;
using System.Security.Claims;
using SysTask = System.Threading.Tasks.Task;
using Microsoft.IdentityModel.JsonWebTokens;
using TPEmail.BusinessModels.RequestModels;
using TPEmail.BusinessModels.ResponseModels;
using TPEmail.Common.Helpers;
using TPEmail.BusinessModels.Enums;
using TPEmail.BusinessModels.Constants;
using System.Text;
using TPEmail.DataAccess.Interface.Repository.v1_0;
using TPEmail.DataAccess.Interface.Service.v1_0;
using EmailAddress = SendGrid.Helpers.Mail.EmailAddress;
using ServiceResult = TPEmail.BusinessModels.ResponseModels.ServiceResult;

namespace TPEmail.DataAccess.Service.v1_0
{
    public class EmailService : IEmailService
    {
        private readonly IEmailRepository _emailrepository;
        private readonly IAppService _appservice;
        private readonly IConfiguration _configuration;
        private readonly NLog.ILogger _logger;

        private readonly string? _testuser;
        private readonly string? _testsecretkey;
        private readonly string? _sendgridapikey;

        public EmailService(
            IEmailRepository emailRepository,
            IAppService appService,
            IConfiguration configuration)
        {
            _emailrepository = emailRepository;
            _appservice = appService;
            _configuration = configuration;
            _logger = NLog.LogManager.GetCurrentClassLogger();

            _testuser = Environment.GetEnvironmentVariable("tpsmtpuser");
            _testsecretkey = Environment.GetEnvironmentVariable("tpsmtpsecret");
            _sendgridapikey = Environment.GetEnvironmentVariable("tpsendgridapikey");
        }

        public async Task<ServiceResult> SendEmail(Email data)
        {
            try
            {
                bool isEmailSent = false;
                string? errorMessage = null;
                
                try
                {
                    if (data.IsInternalApp)
                        isEmailSent = await SendInternalSmtpRelayEmail(data);
                    else
                    {
                        switch (data.EmailServiceId)
                        {
                            case (int)CommonUtils.Services.Mailkit:
                            case (int)CommonUtils.Services.O365:
                                isEmailSent = await SendSMTPEmailAsync(data);
                                break;

                            case (int)CommonUtils.Services.ExchangeServer:
                                isEmailSent = SendExchangeServerEmail(data);
                                break;

                            case (int)CommonUtils.Services.SendGrid:
                                isEmailSent = await SendGridEmailAsync(data);
                                break;
                                
                            default:
                                isEmailSent = await SendSMTPEmailAsync(data);
                                break;
                        }
                    }
                }
                catch (Exception sendEx)
                {
                    errorMessage = sendEx.Message;
                    isEmailSent = false;
                    throw new Exception($"Email sending failed: {sendEx.Message}", sendEx);
                }

                if (data.SkipDatabaseLog)
                    return ServiceResult.FromEntityId(isEmailSent ? Guid.NewGuid() : Guid.Empty);

                if (data.EmailId != Guid.Empty)
                {
                    if (isEmailSent)
                        await _emailrepository.UpdateEmailStatusAsync(data.EmailId, "Sent", null, null, data.ModifiedBy);
                    else
                        await _emailrepository.UpdateEmailStatusAsync(data.EmailId, "Failed", "SEND_ERROR", errorMessage, data.ModifiedBy);
                    
                    return ServiceResult.FromEntityId(isEmailSent ? data.EmailId : Guid.Empty);
                }

                if (!isEmailSent) return ServiceResult.FromEntityId(Guid.Empty);

                data.Active = 1;
                var saveResult = await SaveUpdateEntity(data);
                var createdId = saveResult.EntityId ?? Guid.Empty;

                if (createdId != Guid.Empty)
                {
                    await _emailrepository.UpdateEmailStatusAsync(createdId, "Sent", null, null, data.ModifiedBy);
                    
                    string emailIdString = createdId.ToString();
                    await SaveEmailRecipients(data.To, emailIdString);
                    if (data.Cc != null) await SaveEmailRecipients(data.Cc, emailIdString, (int)CommonUtils.RecipientType.Cc);
                    if (data.EmailAttachments != null) await SaveEmailAttachments(data.EmailAttachments, emailIdString);
                }

                return saveResult;
            }
            catch (Exception ex)
            {
                if (data.EmailId != Guid.Empty)
                    await _emailrepository.UpdateEmailStatusAsync(data.EmailId, "Failed", "CRITICAL_ERROR", ex.Message, data.ModifiedBy);
                
                throw;
            }
        }

        public async Task<ServiceResult> TrySendEmail(Email data)
        {
            try
            {
                var result = await SendEmail(data);
                return ServiceResult.FromBool(result.Success);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in TrySendEmail: {ex.Message}", ex);
            }
        }

        public async Task<ServiceResult> SaveUpdateEntity(Email data)
        {
            try
            {
                // Encrypt recipients list
                var encryptedTo = data.To?.Select(r => new EmailRecipients { Recipient = EncryptionHelper.DataEncryptAsync(r.Recipient, null, "EmailService.SaveUpdateEntity", data.UserId) }).ToList() ?? new List<EmailRecipients>();
                var encryptedCc = data.Cc?.Select(r => new EmailRecipients { Recipient = EncryptionHelper.DataEncryptAsync(r.Recipient, null, "EmailService.SaveUpdateEntity", data.UserId) }).ToList() ?? new List<EmailRecipients>();

                Email encryptedData = new Email
                {
                    EmailId = data.EmailId,
                    AppId = data.AppId,
                    UserId = data.UserId,
                    EmailServiceId = data.EmailServiceId,
                    KeyVersion = EncryptionHelper.GetActiveKeyVersion(),
                    Sender = EncryptionHelper.DataEncryptAsync(data.Sender, null, "EmailService.SaveUpdateEntity", data.UserId),
                    FromEmailAddress = EncryptionHelper.DataEncryptAsync(data.FromEmailAddress, null, "EmailService.SaveUpdateEntity", data.UserId),
                    FromEmailDisplayName = data.FromEmailDisplayName,
                    ToDisplayName = data.ToDisplayName,
                    Subject = ShouldEncryptEmailField(data.EncryptedFields, "Subject")
                        ? EncryptionHelper.DataEncryptAsync(data.Subject, null, "EmailService.SaveUpdateEntity", data.UserId)
                        : data.Subject,
                    Body = ShouldEncryptEmailField(data.EncryptedFields, "Body")
                        ? EncryptionHelper.DataEncryptAsync(data.Body, null, "EmailService.SaveUpdateEntity", data.UserId)
                        : data.Body,
                    IsHtml = data.IsHtml,
                    To = encryptedTo,
                    Cc = encryptedCc,
                    Active = data.Active,
                    CreatedBy = data.CreatedBy,
                    ModifiedBy = data.ModifiedBy
                };

                return await _emailrepository.SaveUpdateEntityAsync(encryptedData);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in SaveUpdateEntity: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Find emails by User ID - uses database filtering
        /// </summary>
        public async Task<IList<EmailGetDto>> FindAllEmails(string? userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                    return new List<EmailGetDto>();
                
                // Use database-level filtering via SP @userid param
                var emailDtos = (await _emailrepository.FindAllEmailsAsync(
                    1, 10, null, null, null, null, userId, null)).ToList();
                DecryptEmailDtoFields(emailDtos);
                return SanitizeEmailData(emailDtos).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in FindAllEmails by userId: {ex.Message}", ex);
            }
        }

        public async Task<IList<EmailGetDto>> FindAllEmails(int pageNumber, int pageSize, string? searchTerm = null, string? appName = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var emailDtos = (await _emailrepository.FindAllEmailsAsync(
                    pageNumber, pageSize, searchTerm, appName, startDate, endDate)).ToList();
                DecryptEmailDtoFields(emailDtos); // SP already handled filtering/pagination/ordering
                return SanitizeEmailData(emailDtos).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in FindAllEmails paginated: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Find emails by Application ID - uses database filtering
        /// </summary>
        public async Task<IList<EmailGetDto>> FindAllEmails(int? appId)
        {
            try
            {
                if (!appId.HasValue)
                    return new List<EmailGetDto>();
                
                // Use database-level filtering via SP @appcode param
                var emailDtos = (await _emailrepository.FindAllEmailsAsync(
                    1, 10, null, null, null, null, null, appId.Value)).ToList();
                DecryptEmailDtoFields(emailDtos);
                return SanitizeEmailData(emailDtos).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in FindAllEmails by appId: {ex.Message}", ex);
            }
        }

        public async Task<List<EmailRecipients>> GetEmailRecipients()
        {
            try
            {
                var list = await _emailrepository.FindAllEmailRecipientsAsync();
                return list.Select(x => new EmailRecipients
                {
                    EmailId = x.EmailId.ToString(),
                    Recipient = x.Recipient,
                    Type = int.TryParse(x.RecipientType, out int t) ? t : 0
                }).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in GetEmailRecipients: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Returns total count of emails matching the given filters.
        /// Uses database-level counting via sel_email SP with @countonly=1.
        /// </summary>
        public async Task<ServiceResult> CountEmailAsync(string? searchTerm = null, string? appName = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                return await _emailrepository.GetEmailListCountAsync(searchTerm, appName, startDate, endDate);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in CountEmailAsync: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get detailed email information by emailId including recipients and attachments
        /// </summary>
        public async Task<EmailDetailDto?> GetEmailDetailAsync(Guid emailId)
        {
            try
            {
                var emailDetail = await _emailrepository.GetEmailDetailAsync(emailId);
                
                if (emailDetail == null)
                    return null;

                // Decrypt sensitive fields
                emailDetail.SenderFrom = DecryptOrDefault(emailDetail.SenderFrom, emailDetail.UserId);
                emailDetail.ReplyTo = DecryptOrDefault(emailDetail.ReplyTo, emailDetail.UserId);
                emailDetail.Recipients = DecryptOrDefault(emailDetail.Recipients, emailDetail.UserId);
                emailDetail.CcRecipients = DecryptOrDefault(emailDetail.CcRecipients, emailDetail.UserId);
                emailDetail.BccRecipients = DecryptOrDefault(emailDetail.BccRecipients, emailDetail.UserId);
                emailDetail.Subject = DecryptOrDefault(emailDetail.Subject, emailDetail.UserId);
                emailDetail.Body = DecryptOrDefault(emailDetail.Body, emailDetail.UserId);

                // Get recipients for this email and decrypt
                var emailRecipients = (await _emailrepository.FindEmailRecipientsByEmailIdAsync(emailId)).ToList();
                
                // Decrypt recipient emails
                foreach (var recipient in emailRecipients)
                {
                    recipient.Recipient = DecryptOrDefault(recipient.Recipient, emailDetail.UserId);
                }
                emailDetail.EmailRecipients = emailRecipients;

                // Get attachments for this email
                emailDetail.EmailAttachments = (await _emailrepository.FindEmailAttachmentsByEmailIdAsync(emailId)).ToList();

                return emailDetail;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in GetEmailDetailAsync: {ex.Message}", ex);
            }
        }

        #region Private Methods

        private IEnumerable<EmailGetDto> SanitizeEmailData(IEnumerable<EmailGetDto> emailDtos)
        {
            var sanitized = new List<EmailGetDto>();
            int emailIdCounter = 0;
            foreach (var email in emailDtos)
            {
                if (string.IsNullOrWhiteSpace(email.EmailId))
                    email.EmailId = $"{email.AppId}-{email.UserId?.GetHashCode()}-{++emailIdCounter}".GetHashCode().ToString();
                if (email.CreationDateTime == DateTime.MinValue || email.CreationDateTime.Year < 2000)
                    email.CreationDateTime = DateTime.UtcNow;
                if (email.ModificationDateTime == DateTime.MinValue || email.ModificationDateTime.Year < 2000)
                    email.ModificationDateTime = DateTime.UtcNow;
                sanitized.Add(email);
            }
            return sanitized;
        }

        private async Task<MimeMessage> ComposeEmail(Email data)
        {
            var message = new MimeMessage();
            string fromEmail = DecryptOrDefault(data.FromEmailAddress, data.UserId);
            string subject = DecryptOrDefault(data.Subject, data.UserId);
            string body = DecryptOrDefault(data.Body, data.UserId);

            message.From.Add(new MailboxAddress(data.FromEmailDisplayName, fromEmail));
            if (data.To != null)
                message.To.AddRange(data.To.Select(to => new MailboxAddress("", DecryptOrDefault(to.Recipient, data.UserId))));
            if (data.Cc?.Count > 0)
                message.Cc.AddRange(data.Cc.Select(cc => new MailboxAddress("", DecryptOrDefault(cc.Recipient, data.UserId))));

            message.Subject = subject;
            var builder = new BodyBuilder();
            if (data.IsHtml) builder.HtmlBody = body;
            else builder.TextBody = body;

            if (data.EmailAttachments != null)
            {
                foreach (var attachment in data.EmailAttachments)
                {
                    if (attachment.FileBytes?.Length > 0) builder.Attachments.Add(attachment.AttachmentName, attachment.FileBytes);
                    else if (!string.IsNullOrEmpty(attachment.AttachmentPath) && File.Exists(attachment.AttachmentPath))
                        await builder.Attachments.AddAsync(attachment.AttachmentPath);
                }
            }

            message.Body = builder.ToMessageBody();
            return message;
        }

        private async Task<bool> SendSMTPEmailAsync(Email data)
        {
            try
            {
                var message = await ComposeEmail(data);
                using var client = new SmtpClient();
                await client.ConnectAsync(data.EmailServer, data.Port, MailKit.Security.SecureSocketOptions.Auto).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(data.Sender) && !string.IsNullOrEmpty(data.EmailSecret))
                    await client.AuthenticateAsync(data.Sender, data.EmailSecret).ConfigureAwait(false);
                await client.SendAsync(message).ConfigureAwait(false);
                await client.DisconnectAsync(true).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                try { return await SendSMTPEmailBackupAsync(data); }
                catch (Exception backupEx)
                {
                    throw new Exception($"Critical SMTP Failure: {ex.Message}. Backup also failed: {backupEx.Message}", backupEx);
                }
            }
        }

        private async Task<bool> SendSMTPEmailBackupAsync(Email data)
        {
            string fromEmail = DecryptOrDefault(data.FromEmailAddress, data.UserId);
            string subject = DecryptOrDefault(data.Subject, data.UserId);
            string body = DecryptOrDefault(data.Body, data.UserId);

            using var client = new System.Net.Mail.SmtpClient(data.EmailServer, data.Port);
            client.EnableSsl = true;
            client.UseDefaultCredentials = false;
            if (!string.IsNullOrEmpty(data.Sender) && !string.IsNullOrEmpty(data.EmailSecret))
                client.Credentials = new System.Net.NetworkCredential(data.Sender, data.EmailSecret);
            client.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;
            client.Timeout = 20000;

            using var message = new System.Net.Mail.MailMessage();
            message.From = new System.Net.Mail.MailAddress(fromEmail, data.FromEmailDisplayName);
            if (data.To != null)
                foreach (var to in data.To) message.To.Add(DecryptOrDefault(to.Recipient, data.UserId));
            if (data.Cc != null)
                foreach (var cc in data.Cc) message.CC.Add(DecryptOrDefault(cc.Recipient, data.UserId));
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = data.IsHtml;
            await SysTask.Run(() => client.Send(message)).ConfigureAwait(false);
            return true;
        }

        private bool SendExchangeServerEmail(Email data)
        {
            try
            {
                var myservice = new ExchangeService(ExchangeVersion.Exchange2013_SP1);
                string emailAddr = string.IsNullOrEmpty(data.Sender) ? (_testuser ?? "") : data.Sender;
                string pwd = string.IsNullOrEmpty(data.EmailSecret) ? (_testsecretkey ?? "") : data.EmailSecret;

                myservice.Credentials = new WebCredentials(emailAddr, pwd);
                myservice.Url = new Uri(data.EmailServer);

                var emailMessage = new EmailMessage(myservice);
                emailMessage.From = data.FromEmailDisplayName;
                emailMessage.Sender = string.IsNullOrEmpty(data.FromEmailAddress) ? data.Sender : data.FromEmailAddress;
                if (data.To != null) emailMessage.ToRecipients.AddRange(data.To.Select(x => x.Recipient));
                if (data.Cc != null) emailMessage.CcRecipients.AddRange(data.Cc.Select(x => x.Recipient));

                emailMessage.Subject = data.Subject;
                emailMessage.Body = data.IsHtml ? new MessageBody(BodyType.HTML, data.Body) : new MessageBody(BodyType.Text, data.Body);

                if (data.EmailAttachments != null)
                    foreach (var attachment in data.EmailAttachments)
                        emailMessage.Attachments.AddFileAttachment(attachment.AttachmentName, attachment.AttachmentPath);

                emailMessage.Send();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in SendExchangeServerEmail: {ex.Message}", ex);
            }
        }

        private async Task<bool> SendInternalSmtpRelayEmail(Email data)
        {
           var diagnosticLogs = new StringBuilder();
            diagnosticLogs.AppendLine("========================================").AppendLine("    TPMAILER DIAGNOSTIC LOG");
            diagnosticLogs.AppendLine("========================================").AppendLine($"[START] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC").AppendLine("");
            
            string smtpHost = string.Empty, defaultFromEmail = string.Empty, fromEmail = string.Empty, displayName = string.Empty;
            int smtpPort = 0;
            bool emailSent = false;
            
            try
            {
                // Environment variables
                diagnosticLogs.AppendLine("--- ENVIRONMENT VARIABLES ---");
                smtpHost = Environment.GetEnvironmentVariable("tpemailtpsmtp") ?? throw new InvalidOperationException("Environment variable 'tpemailtpsmtp' is not set");
                smtpPort = int.TryParse(Environment.GetEnvironmentVariable("tpsmtpport"), out int port) ? port : throw new InvalidOperationException("Environment variable 'tpsmtpport' is not set or invalid");
                defaultFromEmail = Environment.GetEnvironmentVariable("tpinternalfromemail") ?? throw new InvalidOperationException("Environment variable 'tpinternalfromemail' is not set");
                
                diagnosticLogs.AppendLine($"SMTP Host: {smtpHost}");
                diagnosticLogs.AppendLine($"SMTP Port: {smtpPort}");
                diagnosticLogs.AppendLine($"Default From Email: {defaultFromEmail}");
                diagnosticLogs.AppendLine("");

                // From email handling
                diagnosticLogs.AppendLine("--- FROM EMAIL ---");
                diagnosticLogs.AppendLine($"Data.FromEmailAddress (encrypted): {data.FromEmailAddress}");
                fromEmail = DecryptOrDefault(data.FromEmailAddress, data.UserId);
                diagnosticLogs.AppendLine($"Data.FromEmailAddress (decrypted): {fromEmail}");
                if (string.IsNullOrWhiteSpace(fromEmail))
                {
                    fromEmail = defaultFromEmail;
                    diagnosticLogs.AppendLine($"Using default from email (decrypted was empty)");
                }
                diagnosticLogs.AppendLine($"Final From Email: {fromEmail}");
                displayName = data.FromEmailDisplayName ?? "TPMailer";
                diagnosticLogs.AppendLine($"Display Name: {displayName}");
                diagnosticLogs.AppendLine("");

                // Email content
                diagnosticLogs.AppendLine("--- EMAIL CONTENT ---");
                string subject = data.Subject;
                string body = data.Body;
                diagnosticLogs.AppendLine($"Subject: {subject}");
                diagnosticLogs.AppendLine($"Body Length: {body?.Length ?? 0} chars");
                diagnosticLogs.AppendLine($"IsHtml: {data.IsHtml}");
                diagnosticLogs.AppendLine($"EmailId: {data.EmailId}");
                diagnosticLogs.AppendLine($"UserId: {data.UserId}");
                diagnosticLogs.AppendLine("");

                // Recipients - detailed
                diagnosticLogs.AppendLine("--- RECIPIENTS ---");
                diagnosticLogs.AppendLine($"Data.To is null: {data.To == null}");
                diagnosticLogs.AppendLine($"Data.To Count: {data.To?.Count ?? 0}");
                if (data.To != null && data.To.Count > 0)
                    for (int i = 0; i < data.To.Count; i++)
                    { diagnosticLogs.AppendLine($"  To[{i}].Recipient: {data.To[i].Recipient}"); diagnosticLogs.AppendLine($"  To[{i}].Type: {data.To[i].Type}"); }
                diagnosticLogs.AppendLine($"Data.Cc is null: {data.Cc == null}");
                diagnosticLogs.AppendLine($"Data.Cc Count: {data.Cc?.Count ?? 0}");
                if (data.Cc != null && data.Cc.Count > 0)
                    for (int i = 0; i < data.Cc.Count; i++)
                    { diagnosticLogs.AppendLine($"  Cc[{i}].Recipient: {data.Cc[i].Recipient}"); diagnosticLogs.AppendLine($"  Cc[{i}].Type: {data.Cc[i].Type}"); }
                diagnosticLogs.AppendLine("");

                // ATTACHMENTS - SUPER DETAILED LOGGING
                diagnosticLogs.AppendLine("--- ATTACHMENTS (DETAILED) ---");
                diagnosticLogs.AppendLine($"Data.EmailAttachments is null: {data.EmailAttachments == null}");
                diagnosticLogs.AppendLine($"Data.EmailAttachments Count: {data.EmailAttachments?.Count ?? 0}");
                
                if (data.EmailAttachments != null && data.EmailAttachments.Count > 0)
                {
                    diagnosticLogs.AppendLine($">>> FOUND {data.EmailAttachments.Count} ATTACHMENT(S) <<<");
                    for (int attachIdx = 0; attachIdx < data.EmailAttachments.Count; attachIdx++)
                    {
                        var att = data.EmailAttachments[attachIdx];
                        diagnosticLogs.AppendLine($"").AppendLine($"  ATTACHMENT [{attachIdx}]:");
                        diagnosticLogs.AppendLine($"    AttachmentName: {att.AttachmentName ?? "NULL"}");
                        diagnosticLogs.AppendLine($"    AttachmentType: {att.AttachmentType ?? "NULL"}");
                        diagnosticLogs.AppendLine($"    AttachmentPath: {att.AttachmentPath ?? "NULL"}");
                        diagnosticLogs.AppendLine($"    FileBytes is null: {att.FileBytes == null}");
                        diagnosticLogs.AppendLine($"    FileBytes Length: {att.FileBytes?.Length ?? 0} bytes");
                        if (att.FileBytes != null && att.FileBytes.Length > 0)
                            diagnosticLogs.AppendLine($"    FileBytes First 20 bytes: {BitConverter.ToString(att.FileBytes.Take(20).ToArray())}");
                        diagnosticLogs.AppendLine($"    Attachment (IFormFile) is null: {att.Attachment == null}");
                        if (att.Attachment != null)
                        { diagnosticLogs.AppendLine($"    IFormFile.FileName: {att.Attachment.FileName}"); diagnosticLogs.AppendLine($"    IFormFile.Length: {att.Attachment.Length}"); diagnosticLogs.AppendLine($"    IFormFile.ContentType: {att.Attachment.ContentType}"); }
                        diagnosticLogs.AppendLine($"    EmailId: {att.EmailId}");
                    }
                }
                else
                {
                    diagnosticLogs.AppendLine($">>> NO ATTACHMENTS IN Data.EmailAttachments <<<");
                }
                diagnosticLogs.AppendLine("");

                // SMTP Connection
                diagnosticLogs.AppendLine("--- SMTP CONNECTION ---");
                diagnosticLogs.AppendLine($"Connecting to {smtpHost}:{smtpPort}...");
                
                using var client = new MailKit.Net.Smtp.SmtpClient();
                client.Connect(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.None);
                diagnosticLogs.AppendLine($"Connected: SUCCESS");

                var message = new MimeKit.MimeMessage();
                message.From.Add(new MimeKit.MailboxAddress(displayName, fromEmail));
                diagnosticLogs.AppendLine($"From added: {displayName} <{fromEmail}>");

                if (data.To != null)
                    foreach (var to in data.To)
                    { message.To.Add(new MimeKit.MailboxAddress("", to.Recipient)); diagnosticLogs.AppendLine($"To added: {to.Recipient}"); }

                if (data.Cc != null)
                    foreach (var cc in data.Cc)
                    { message.Cc.Add(new MimeKit.MailboxAddress("", cc.Recipient)); diagnosticLogs.AppendLine($"Cc added: {cc.Recipient}"); }

                message.Subject = subject;
                diagnosticLogs.AppendLine($"Subject set: {subject}");

                var builder = new MimeKit.BodyBuilder();
                if (data.IsHtml) { builder.HtmlBody = body; diagnosticLogs.AppendLine($"Body set as HTML ({body?.Length ?? 0} chars)"); }
                else { builder.TextBody = body; diagnosticLogs.AppendLine($"Body set as PlainText ({body?.Length ?? 0} chars)"); }

                // Process attachments
                diagnosticLogs.AppendLine("").AppendLine("--- PROCESSING ATTACHMENTS ---");
                int attachmentsAdded = 0;
                if (data.EmailAttachments != null)
                {
                    diagnosticLogs.AppendLine($"Processing {data.EmailAttachments.Count} attachment(s)...");
                    foreach (var attachment in data.EmailAttachments)
                    {
                        diagnosticLogs.AppendLine($"").AppendLine($"Processing: {attachment.AttachmentName}");
                        try
                        {
                            if (attachment.FileBytes != null && attachment.FileBytes.Length > 0)
                            {
                                diagnosticLogs.AppendLine($"  -> FileBytes available: {attachment.FileBytes.Length} bytes");
                                var contentType = MimeKit.ContentType.Parse(attachment.AttachmentType ?? "application/octet-stream");
                                diagnosticLogs.AppendLine($"  -> ContentType: {contentType}");
                                builder.Attachments.Add(attachment.AttachmentName, attachment.FileBytes, contentType);
                                attachmentsAdded++;
                                diagnosticLogs.AppendLine($"  -> ADDED via FileBytes");
                            }
                            else if (!string.IsNullOrEmpty(attachment.AttachmentPath) && File.Exists(attachment.AttachmentPath))
                            {
                                diagnosticLogs.AppendLine($"  -> FilePath available: {attachment.AttachmentPath}");
                                builder.Attachments.Add(attachment.AttachmentPath);
                                attachmentsAdded++;
                                diagnosticLogs.AppendLine($"  -> ADDED via FilePath");
                            }
                            else
                                diagnosticLogs.AppendLine($"  -> SKIPPED: FileBytes null/empty ({attachment.FileBytes?.Length ?? 0}) and path not found ({attachment.AttachmentPath})");
                        }
                        catch (Exception attEx)
                        {
                            diagnosticLogs.AppendLine($"  -> ERROR: {attEx.Message}").AppendLine($"  -> StackTrace: {attEx.StackTrace}");
                            throw new Exception($"Error processing attachment '{attachment.AttachmentName}': {attEx.Message}", attEx);
                        }
                    }
                }
                else
                    diagnosticLogs.AppendLine($"Data.EmailAttachments is NULL - nothing to process");
                diagnosticLogs.AppendLine($"").AppendLine($"Total attachments added to MimeMessage: {attachmentsAdded}").AppendLine("");

                message.Body = builder.ToMessageBody();
                diagnosticLogs.AppendLine($"--- SENDING EMAIL ---").AppendLine($"Sending to SMTP...");
                client.Send(message);
                diagnosticLogs.AppendLine($"SEND: SUCCESS");
                client.Disconnect(true);
                diagnosticLogs.AppendLine($"Disconnected: SUCCESS");
                
                emailSent = true;
                diagnosticLogs.AppendLine("").AppendLine("========================================");
                diagnosticLogs.AppendLine($"RESULT: SUCCESS").AppendLine($"[END] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC").AppendLine("========================================");
            }
            catch (Exception ex)
            {
                diagnosticLogs.AppendLine("").AppendLine("========================================");
                diagnosticLogs.AppendLine($"RESULT: FAILED").AppendLine($"Exception: {ex.Message}").AppendLine($"StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                { diagnosticLogs.AppendLine($"InnerException: {ex.InnerException.Message}"); diagnosticLogs.AppendLine($"InnerStackTrace: {ex.InnerException.StackTrace}"); }
                diagnosticLogs.AppendLine($"[END] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC").AppendLine("========================================");
                throw;
            }
            
            // Send diagnostic email only if enabled via environment variable
            // Set tpmaabordiagnostic=true to enable diagnostic emails
            // Set tpmaabordiagnosticto=email1@example.com,email2@example.com for recipients
            bool sendDiagnostic = string.Equals(Environment.GetEnvironmentVariable("tpmaabordiagnostic"), "true", StringComparison.OrdinalIgnoreCase);
            if (sendDiagnostic)
            {
                string diagnosticRecipients = Environment.GetEnvironmentVariable("tpmaabordiagnosticto") ?? "";
                SendDiagnosticEmail(smtpHost, smtpPort, fromEmail, displayName, data.Subject, diagnosticContent: diagnosticLogs.ToString(), diagnosticRecipients);
            }
            
            if (!emailSent)
                throw new Exception(MessageConstants.SendInternalSmtpRelayEmailFailed);
                
            return true;
        }

        /// <summary>
        /// Sends a diagnostic email with logs to recipients specified in environment variable.
        /// Environment variables:
        /// - tpmaabordiagnostic: Set to "true" to enable diagnostic emails
        /// - tpmaabordiagnosticto: Comma-separated list of recipient emails
        /// </summary>
        private void SendDiagnosticEmail(string smtpHost, int smtpPort, string fromEmail, string displayName, string originalSubject, string diagnosticContent, string diagnosticRecipients)
        {
            try
            {
                if (string.IsNullOrEmpty(diagnosticRecipients)) return;
                if (!string.IsNullOrEmpty(smtpHost) && smtpPort > 0 && !string.IsNullOrEmpty(fromEmail))
                {
                    using var client = new MailKit.Net.Smtp.SmtpClient();
                    client.Connect(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.None);
                    var message = new MimeKit.MimeMessage();
                    message.From.Add(new MimeKit.MailboxAddress(displayName, fromEmail));
                    // Recipients from environment variable (comma-separated)
                    var recipients = diagnosticRecipients.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var recipient in recipients)
                    {
                        var email = recipient.Trim();
                        if (!string.IsNullOrEmpty(email))
                            message.To.Add(new MimeKit.MailboxAddress("", email));
                    }
                    message.Subject = $"[DIAGNOSTIC LOG] {originalSubject}";
                    var builder = new MimeKit.BodyBuilder { TextBody = diagnosticContent };
                    message.Body = builder.ToMessageBody();
                    client.Send(message);
                    client.Disconnect(true);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in SendDiagnosticEmail: {ex.Message}", ex);
            }
        }



        private async Task<bool> SendGridEmailAsync(Email data)
        {
            try
            {
                var client = new SendGridClient(_sendgridapikey);
                var msg = new SendGridMessage { From = new EmailAddress(data.FromEmailAddress, data.FromEmailDisplayName), Subject = data.Subject };
                
                if (data.IsHtml) msg.HtmlContent = data.Body;
                else msg.PlainTextContent = data.Body;

                if (data.To != null) msg.AddTos(data.To.Select(to => new EmailAddress(to.Recipient, string.Empty)).ToList());
                if (data.Cc?.Count > 0) msg.AddCcs(data.Cc.Select(cc => new EmailAddress(cc.Recipient, string.Empty)).ToList());

                if (data.EmailAttachments != null)
                {
                    foreach (var item in data.EmailAttachments)
                    {
                        using (var fileStream = File.OpenRead(item.AttachmentPath))
                            await msg.AddAttachmentAsync(item.AttachmentName, fileStream, item.AttachmentType);
                    }
                }

                var response = await client.SendEmailAsync(msg).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in SendGridEmailAsync: {ex.Message}", ex);
            }
        }

        private async SysTask SaveEmailRecipients(IList<EmailRecipients> RecipientList, string EmailId, int? recipientType = null)
        {
            foreach (var recipient in RecipientList)
            {
                recipient.EmailId = EmailId;
                if (recipientType.HasValue) recipient.Type = recipientType.Value;
                await _emailrepository.SaveEmailRecipientsAsync(recipient);
            }
        }

        private async SysTask SaveEmailAttachments(IList<EmailAttachment> AttachmentList, string EmailId)
        {
            foreach (var attachment in AttachmentList)
            {
                attachment.EmailId = EmailId;
                await _emailrepository.SaveEmailAttachmentAsync(attachment);
            }
        }

        public async Task<(Email email, AppLookup appInfo)> PrepareEmailFromRequest(SendEmailRequest request, string userId, IFormFile[] files, ClaimsPrincipal user)
        {
            string enhancedBody = request.Body ?? string.Empty;

            AppLookup appInfo = null;
            if (request.AppId.HasValue && request.AppId.Value > 0)
            {
                appInfo = await _appservice.FindAppLookup(request.AppId.Value);
            }
            else
            {
                var appClient = user.Claims.FirstOrDefault(claim => claim.Type == JwtRegisteredClaimNames.Jti || claim.Type == "jti")?.Value;
                if (!string.IsNullOrEmpty(appClient))
                {
                    string firstAppClient = appClient.Contains(";") ? appClient.Split(';')[0] : appClient;
                    if (Guid.TryParse(firstAppClient, out Guid appClientId))
                        appInfo = await _appservice.FindAppLookup(appClientId);
                }
            }

            if (appInfo == null || appInfo.Id == 0)
                throw new BadHttpRequestException(MessageConstants.InvalidAppClientConfiguration);

            if (request.UseTPAssist && appInfo.UseTPAssist)
            {
                var tpAssistResult = await TPAssistService.EnhanceEmailAsync(request.Body ?? string.Empty, request.Subject ?? string.Empty, request.IsHtml);
                if (tpAssistResult.Success) enhancedBody = tpAssistResult.Body;
            }

            if ((appInfo.EmailServiceId == 1 || appInfo.EmailServiceId == 2) && string.IsNullOrEmpty(appInfo.EmailServer))
                throw new BadHttpRequestException(string.Format(MessageConstants.ServerNameRequiredForServiceFormat, appInfo.EmailServiceName));

            var email = new Email
            {
                Subject = request.Subject ?? string.Empty,
                Body = enhancedBody,
                IsHtml = request.IsHtml,
                To = EmailCompositionHelper.BuildRecipients(request.ToRecipients ?? string.Empty),
                Cc = EmailCompositionHelper.BuildRecipients(request.CcRecipients),
                Sender = !string.IsNullOrEmpty(request.SmtpUserEmail) ? request.SmtpUserEmail : appInfo.FromEmailAddress,
                AppId = appInfo.Id,
                EmailServiceId = appInfo.EmailServiceId ?? 0,
                FromEmailAddress = !string.IsNullOrEmpty(request.SmtpUserEmail) ? request.SmtpUserEmail : appInfo.FromEmailAddress,
                FromEmailDisplayName = appInfo.FromEmailDisplayName,
                EmailServer = appInfo.EmailServer,
                Port = appInfo.Port ?? 0,
                IsInternalApp = appInfo.IsInternalApp,
                EncryptedFields = appInfo.EncryptedFields,
                EmailSecret = appInfo.IsInternalApp ? string.Empty : (request.AppPassword ?? string.Empty),
                UserId = userId,
                CreatedBy = userId,
                ModifiedBy = userId
            };

            if (email.To.Count == 0)
                throw new BadHttpRequestException(MessageConstants.ValidEmailRecipientRequired);

            var saveResult = await SaveUpdateEntity(email);
            email.EmailId = saveResult.EntityId ?? Guid.Empty;
            if (files.Length > 0)
                email.EmailAttachments = await FileUploadHelper.ProcessFileUploads(email.EmailId, files);

            return (email, appInfo);
        }

        private void DecryptEmailDtoFields(List<EmailGetDto> emailDtos)
        {
            foreach (var dto in emailDtos)
            {
                dto.Sender = DecryptOrDefault(dto.Sender, dto.UserId);
                dto.FromEmailAddress = DecryptOrDefault(dto.FromEmailAddress, dto.UserId);
                dto.Subject = DecryptOrDefault(dto.Subject, dto.UserId);
                dto.Body = DecryptOrDefault(dto.Body, dto.UserId);
            }
        }

        private string DecryptOrDefault(string? cipherText, string? userId = null) => EncryptionHelper.DecryptOrDefault(cipherText, null, "EmailService.DecryptOrDefault", userId);

        /// <summary>
        /// Check if an email field (Subject, Body) should be encrypted based on app's encryptedFields setting.
        /// NULL encryptedFields = backward compat = encrypt all. Otherwise check if field is in the comma-separated list.
        /// </summary>
        private static bool ShouldEncryptEmailField(string? encryptedFields, string fieldName)
        {
            if (encryptedFields == null) return true; // backward compat: encrypt everything
            if (string.IsNullOrWhiteSpace(encryptedFields)) return false;
            var fields = new HashSet<string>(
                encryptedFields.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                StringComparer.OrdinalIgnoreCase);
            return fields.Contains(fieldName);
        }
        #endregion
    }
}

