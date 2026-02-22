#nullable disable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TPEmail.Common.Helpers;
using TPEmail.BusinessModels.RequestModels;
using TPEmail.BusinessModels.ResponseModels;
using TPEmail.BusinessModels.Enums;
using TPEmail.DataAccess.Interface.Service.v1_0;
using TPEmail.DataAccess.Service.v1_0;
using Asp.Versioning;
using TPEmail.BusinessModels.Constants;

namespace TPEmailAPI.Controllers.v_1_0
{
    //[Authorize]
    //[ApiVersion("1.0")]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class EmailServiceController : ControllerBase
    {
        private readonly IEmailService _emailservice;
        private readonly IAppService _appservice;

        public EmailServiceController(IEmailService emailService, IAppService appService)
        {
            _emailservice = emailService;
            _appservice = appService;
        }

        [Authorize(Roles = "ADMIN,USER")]
        [HttpPost]
        public async Task<IActionResult> GetAll(EmailListRequest request)
        {
            var data = await _emailservice.FindAllEmails(request.PageNumber, request.PageSize, request.SearchTerm, request.AppName, request.StartDate, request.EndDate);
            int totalRecords = (await _emailservice.CountEmailAsync(request.SearchTerm, request.AppName, request.StartDate, request.EndDate)).Value;
            var listRequest = new ListDataRequest { PageNumber = request.PageNumber, PageSize = request.PageSize, SearchTerm = request.SearchTerm };
            return Ok(new ApiResponse<DataPageResult<List<EmailGetDto>>>(ResultCodes.Success, new[] { DataPageResult.Create(data, listRequest, totalRecords) }, new[] { MessageConstants.EmailListRetrievedSuccessfully }));
        }

        [Authorize(Roles = "ADMIN,USER")]
        [HttpGet("{emailId}")]
        public async Task<IActionResult> GetEmailById([FromRoute] string emailId)
        {
            if (!Guid.TryParse(emailId, out Guid emailIdGuid) || emailIdGuid == Guid.Empty)
                return Ok(new ApiResponse<EmailGetDto>(ResultCodes.ValidationFailure, null, new[] { MessageConstants.NoValidEmailId }));

            var data = await _emailservice.FindAllEmails(emailId);
            return data.Count == 0
                ? Ok(new ApiResponse<EmailGetDto>(ResultCodes.ValidationFailure, null, new[] { string.Format(MessageConstants.NoEmailFoundWithIdFormat, emailId) }))
                : Ok(new ApiResponse<EmailGetDto>(ResultCodes.Success, data.ToArray(), new[] { MessageConstants.EmailRetrievedSuccessfully }));
        }

        [Authorize(Roles = "ADMIN,USER")]
        [HttpGet("{emailId}")]
        public async Task<IActionResult> GetEmailDetail([FromRoute] string emailId)
        {
            if (!Guid.TryParse(emailId, out Guid emailIdGuid) || emailIdGuid == Guid.Empty)
                return Ok(new ApiResponse<EmailDetailDto>(ResultCodes.ValidationFailure, null, new[] { MessageConstants.NoValidEmailId }));

            var emailDetail = await _emailservice.GetEmailDetailAsync(emailIdGuid);
            return emailDetail == null
                ? Ok(new ApiResponse<EmailDetailDto>(ResultCodes.ValidationFailure, null, new[] { string.Format(MessageConstants.NoEmailFoundWithIdFormat, emailId) }))
                : Ok(new ApiResponse<EmailDetailDto>(ResultCodes.Success, new[] { emailDetail }, new[] { MessageConstants.EmailRetrievedSuccessfully }));
        }

        [Authorize(Roles = "ADMIN,USER")]
        [HttpPost]
        public async Task<IActionResult> GetTPAssist(TPAssistRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Body))
                return Ok(new ApiResponse<TPAssistResult>(ResultCodes.ValidationFailure, null, new[] { MessageConstants.EmailBodyRequiredForTPAssist }));

            var result = await TPAssistService.EnhanceEmailAsync(request.Body, request.Subject ?? string.Empty, request.IsHtml);
            return Ok(new ApiResponse<TPAssistResult>(result.Success ? ResultCodes.Success : ResultCodes.SystemError, new[] { result }, new[] { result.Success ? MessageConstants.EmailBodyEnhancedSuccessfully : (result.ErrorMessage ?? MessageConstants.TPAssistEnhancementFailedFormat) }));
        }

        [HttpPost]
        public async Task<IActionResult> SendEmailAsync([FromForm] SendEmailRequest request, [FromForm(Name = "attachment")] IList<IFormFile> files)
        {
            var maxSizeMB = int.TryParse(Environment.GetEnvironmentVariable("tpmaxattachmentsizemb"), out var mb) ? mb : 25;
            var totalBytes = files.Sum(f => f.Length);
            if (totalBytes > maxSizeMB * 1024L * 1024L)
                return Ok(new ApiResponse<string>(ResultCodes.ValidationFailure, null, new[] { string.Format(MessageConstants.AttachmentSizeExceedsLimitFormat, maxSizeMB) }));

            var userID = HeaderHelper.GetUserIdFromHeaderOrClaims(HttpContext);
            var (email, appInfo) = await _emailservice.PrepareEmailFromRequest(request, userID, files.ToArray(), User);
            NLog.ScopeContext.PushProperty("ApplicationName", appInfo.AppName);
            var sendResult = await _emailservice.SendEmail(email);
            if (!sendResult.Success)
                throw new Exception(MessageConstants.EmailSendingFailed);

            return Ok(new ApiResponse<string>(ResultCodes.Success, new[] { MessageConstants.EmailSentSuccessfully }, new[] { MessageConstants.EmailSentSuccessfully }));
        }

        [HttpPost]
        public async Task<IActionResult> SendMailTest2([FromForm] string host, [FromForm] int port, [FromForm] string username, [FromForm] string password, [FromForm] string fromEmail, [FromForm] string toEmail)
        {
            var client = new System.Net.Mail.SmtpClient(host, port)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new System.Net.NetworkCredential(username, password),
                DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network,
                Timeout = 15000
            };

            var message = new System.Net.Mail.MailMessage();
            message.From = new System.Net.Mail.MailAddress(fromEmail);
            message.To.Add(toEmail);
            message.Subject = MessageConstants.SmtpTestEmailSubject;
            message.Body = MessageConstants.ManualSmtpTestEmailBody;
            await System.Threading.Tasks.Task.Run(() => client.Send(message));
            return Ok(new ApiResponse<string>(ResultCodes.Success, new[] { MessageConstants.EmailSentSuccessfullyManualTestEndpoint }, new[] { MessageConstants.EmailSentSuccessfully }));
        }
    }
}

