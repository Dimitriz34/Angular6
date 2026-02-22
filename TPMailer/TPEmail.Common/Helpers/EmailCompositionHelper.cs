#nullable disable

using TPEmail.BusinessModels.ResponseModels;
using TPEmail.BusinessModels.RequestModels;

namespace TPEmail.Common.Helpers
{
    public static class EmailCompositionHelper
    {
        public static Email ComposeApplicationGuidanceEmail(
            AppLookup application,
            string appSecret,
            string appPassword,
            string baseUrl)
        {
            string baseUri = !string.IsNullOrWhiteSpace(baseUrl)
                ? baseUrl
                : Environment.GetEnvironmentVariable("tpjwtissuer") ?? string.Empty;

            bool isInternalApp = application.IsInternalApp;

            // For TP Internal guidance emails, always use default (null) so it falls back to tpinternalfromemail
            // For external apps, use the database FromEmailAddress
            string fromEmail = isInternalApp ? null : application.FromEmailAddress;

            Email emailData = new Email()
            {
                SkipDatabaseLog = true, // Do not log guidance emails in database
                Sender = fromEmail,
                FromEmailAddress = fromEmail,
                EmailSecret = appPassword, // Empty for internal apps, user-provided for external
                EmailServiceId = application.EmailServiceId ?? 0,
                EmailServer = application.EmailServer,
                Port = application.Port ?? 0,
                IsInternalApp = isInternalApp, // Set flag for routing decision

                To = new List<EmailRecipients>()
                {
                    new EmailRecipients()
                    {
                        Recipient = application.OwnerEmail
                    }
                },
                IsHtml = true,
                FromEmailDisplayName = Environment.GetEnvironmentVariable("tpemailsendername") ?? "TPMailer",
                Subject = $"{Environment.GetEnvironmentVariable("tpemailsendername") ?? "TPMailer"} - {application.AppName} Integration Credentials",
                Body = TPEmail.Common.EmailTemplates.EmailTemplateFactory.CreateApplicationGuidanceEmail(
                    application.AppName,
                    application.AppOwner,
                    application.OwnerEmail,
                    application.AppClient.ToString(),
                    appSecret,
                    application.FromEmailAddress,
                    baseUri,
                    isInternalApp
                )
            };

            return emailData;
        }

        public static Email ComposeUserRegistrationEmail(
            AppUser user,
            string appSecret,
            string decryptedEmail)
        {
            Email emailData = new Email()
            {
                Sender = Environment.GetEnvironmentVariable("tpsmtpuser"),
                FromEmailAddress = Environment.GetEnvironmentVariable("tpsmtpuser"),
                EmailSecret = Environment.GetEnvironmentVariable("tpsmtpsecret"),
                EmailServiceId = int.TryParse(Environment.GetEnvironmentVariable("tpdefaultemailserviceid"), out var sid) ? sid : 3,
                EmailServer = Environment.GetEnvironmentVariable("tpemailtestes"),
                To = new List<EmailRecipients>()
                {
                    new EmailRecipients()
                    {
                        Recipient = decryptedEmail
                    }
                },
                IsHtml = true,
                FromEmailDisplayName = Environment.GetEnvironmentVariable("tpemailsendername") ?? "TPMailer",
                Subject = $"Welcome to {Environment.GetEnvironmentVariable("tpemailsendername") ?? "TPMailer"} - Your Account Credentials",
                Body = TPEmail.Common.EmailTemplates.EmailTemplateFactory.CreateWelcomeEmail(
                    decryptedEmail,
                    appSecret,
                    user.Username ?? decryptedEmail
                )
            };

            return emailData;
        }

        public static bool IsValidEmailAddress(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try { var _ = new System.Net.Mail.MailAddress(email); return true; }
            catch { return false; }
        }

        public static List<EmailRecipients> BuildRecipients(string recipients)
        {
            if (string.IsNullOrWhiteSpace(recipients)) return new List<EmailRecipients>();
            return recipients.Split(';').Where(r => !string.IsNullOrWhiteSpace(r) && IsValidEmailAddress(r))
                .Select(r => new EmailRecipients { Recipient = r.Trim() }).ToList();
        }
    }
}
