#nullable disable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Net;
using System.Security.Claims;
using TPEmail.Common.Helpers;
using TPEmail.BusinessModels.RequestModels;
using TPEmail.BusinessModels.ResponseModels;
using TPEmail.BusinessModels.Enums;
using TPEmail.DataAccess.Interface.Service.v1_0;
using Asp.Versioning;
using TPEmail.BusinessModels.Constants;

namespace TPEmailAPI.Controllers.v_1_0
{
    //[Authorize]
    //[ApiVersion("1.0")]
    [Route("api/[controller]/[action]")]
    [ApiController]

    public class ApplicationController : ControllerBase
    {
        private readonly IActivityLog _activitylog;
        private readonly IAppLookup _application;
        private readonly IEmailService _emailservice;
        private readonly ICommonData _commondata;
        private readonly IEmailServiceLookup _emailservicelookup;

        public ApplicationController(IActivityLog activityLog, IAppLookup appLookup, IEmailService emailService, ICommonData commonData, IEmailServiceLookup emailServiceLookup)
        {
            _activitylog = activityLog;
            _application = appLookup;
            _emailservice = emailService;
            _commondata = commonData;
            _emailservicelookup = emailServiceLookup;
        }

        [HttpPost]
        public async Task<IActionResult> SaveApplication(AppLookup Payload)
        {
            var (exists, appName) = await _application.CheckApplicationExists(Payload.AppName);
            if (exists)
                return Ok(new ApiResponse<object>(ResultCodes.ValidationFailure, null, new[] { string.Format(MessageConstants.ApplicationAlreadyExistsFormat, appName) }));

            string userAppSecret = SecurityHelper.GenerateRandomString();
            Payload.AppSecret = userAppSecret;
            var createResult = await _application.CreateApplication(Payload);
            int createdAppId = createResult.Value;

            return Ok(new ApiResponse<object>(ResultCodes.Success, new[] { new { id = createdAppId, appSecret = userAppSecret } }, new[] { MessageConstants.ApplicationCreatedSuccessfully }));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateApplication(AppLookup Payload)
        {
            var (exists, appName) = await _application.CheckApplicationExists(Payload.AppName, Payload.Id);
            if (exists)
                return Ok(new ApiResponse<string>(ResultCodes.ValidationFailure, null, new[] { string.Format(MessageConstants.ApplicationAlreadyExistsFormat, appName) }));

            var updateResult = await _application.UpdateApplication(Payload);
            return Ok(new ApiResponse<string>(ResultCodes.Success, new[] { updateResult.Data ?? string.Empty }, new[] { MessageConstants.ApplicationUpdatedSuccessfully }));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateApplicationApproval(UpdateApplicationApprovalRequest request)
        {
            int status = (await _application.UpdateApplicationApproval(request.AppId)).Value;
            return Ok(new ApiResponse<int>(ResultCodes.Success, new[] { status }, new[] { MessageConstants.ApplicationApprovalUpdatedSuccessfully }));
        }

        [HttpPost]
        public async Task<IActionResult> GetUserApplicationList(UserApplicationListRequest request)
        {
            var data = await _application.FindAppLookup(request.UserId, request.PageNumber, request.PageSize, request.SearchTerm);
            int totalRecords = (await _application.GetAppCount(request.UserId, request.SearchTerm)).Value;
            var listRequest = new ListDataRequest { PageNumber = request.PageNumber, PageSize = request.PageSize, SearchTerm = request.SearchTerm };
            return Ok(new ApiResponse<DataPageResult<List<AppLookup>>>(ResultCodes.Success, new[] { DataPageResult.Create(data, listRequest, totalRecords) }, new[] { MessageConstants.UserApplicationListRetrievedSuccessfully }));
        }

        [HttpPost]
        public async Task<IActionResult> GetApplicationList(ListDataRequest request)
        {
            var data = await _application.FindAppLookup(request.PageNumber, request.PageSize, request.SearchTerm);
            int totalRecords = (await _application.GetAppCount(request.SearchTerm)).Value;
            return Ok(new ApiResponse<DataPageResult<List<AppLookup>>>(ResultCodes.Success, new[] { DataPageResult.Create(data, request, totalRecords) }, new[] { MessageConstants.ApplicationListRetrievedSuccessfully }));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> FindApplicationById([FromRoute] int id)
        {
            var data = await _application.FindAppLookup(id);
            return data == null
                ? Ok(new ApiResponse<AppLookup>(ResultCodes.ValidationFailure, null, new[] { MessageConstants.ApplicationNotFoundMessage }))
                : Ok(new ApiResponse<AppLookup>(ResultCodes.Success, new[] { data }, new[] { MessageConstants.ApplicationRetrievedSuccessfully }));
        }

        [HttpGet("{appClientId}")]
        public async Task<IActionResult> DiagnoseApplication([FromRoute] string appClientId)
        {
            var appData = await _application.FindAppLookup(new Guid(appClientId));
            if (appData == null)
                return Ok(new ApiResponse<object>(ResultCodes.ValidationFailure, null, new[] { MessageConstants.ApplicationNotFoundInDatabase }));

            var diagnosticData = new
            {
                id = appData.Id,
                appName = appData.AppName,
                appClientId = appData.AppClient.ToString(),
                active = appData.Active,
                isEncrypted = appData.IsEncrypted,
                appSecretDecrypted = appData.AppSecret,
                fromEmail = appData.FromEmailAddress,
                message = appData.Active == 1 ? MessageConstants.ApplicationIsActive : MessageConstants.ApplicationIsInactiveNeedsApproval
            };
            return Ok(new ApiResponse<object>(ResultCodes.Success, new[] { diagnosticData }, new[] { MessageConstants.ApplicationDiagnosticDataRetrievedSuccessfully }));
        }

        [HttpPost("{id}")]
        public async Task<IActionResult> SendGuidanceEmail([FromRoute] int id, [FromForm] string ownerEmail, [FromForm] string appPassword, [FromForm] string appSecret, [FromForm] string? baseApiUrl, [FromForm] string? coOwnerEmail)
        {
            var appData = await _application.FindAppLookup(id);
            if (appData == null)
                return Ok(new ApiResponse<string>(ResultCodes.ValidationFailure, null, new[] { MessageConstants.ApplicationNotFound }));
            if (string.IsNullOrWhiteSpace(ownerEmail))
                return Ok(new ApiResponse<string>(ResultCodes.ValidationFailure, null, new[] { MessageConstants.OwnerEmailRequired }));
            if (string.IsNullOrWhiteSpace(appSecret))
                return Ok(new ApiResponse<string>(ResultCodes.ValidationFailure, null, new[] { MessageConstants.AppSecretRequired }));

            string actualAppPassword = appData.IsInternalApp ? string.Empty : appPassword;
            if (!appData.IsInternalApp && string.IsNullOrWhiteSpace(appPassword))
                return Ok(new ApiResponse<string>(ResultCodes.ValidationFailure, null, new[] { MessageConstants.SmtpAppPasswordRequired }));

            appData.OwnerEmail = ownerEmail;
            await _emailservice.SendEmail(EmailCompositionHelper.ComposeApplicationGuidanceEmail(appData, appSecret, actualAppPassword, baseApiUrl));
            if (!string.IsNullOrWhiteSpace(coOwnerEmail))
            {
                appData.OwnerEmail = coOwnerEmail;
                await _emailservice.SendEmail(EmailCompositionHelper.ComposeApplicationGuidanceEmail(appData, appSecret, actualAppPassword, baseApiUrl));
            }
            return Ok(new ApiResponse<string>(ResultCodes.Success, new[] { MessageConstants.SuccessValue }, new[] { MessageConstants.GuidanceEmailSentSuccessfully }));
        }

        [Authorize(Roles = "ADMIN,USER")]
        [HttpGet]
        public async Task<IActionResult> GetEmailServicesList()
        {
            var data = (await _emailservicelookup.GetAll()).OrderByDescending(d => d.Id);
            return Ok(new ApiResponse<EmailServiceLookup>(ResultCodes.Success, data.ToArray(), new[] { MessageConstants.EmailServiceListRetrievedSuccessfully }));
        }

        [Authorize(Roles = nameof(RoleType.ADMIN))]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEmailServiceById([FromRoute] int id)
        {
            await _activitylog.Log(new ActivityLog()
            {
                LogTypeLookupId = Convert.ToInt32(Operation.RETRIEVE),
                Description = $"Fetching email service lookup by ID : {id}",
                Url = HttpContext.Request.Path
            });

            var data = await _emailservicelookup.GetById(id);
            return data == null
                ? Ok(new ApiResponse<EmailServiceLookup>(ResultCodes.ValidationFailure, null, new[] { MessageConstants.EmailServiceNotFound }))
                : Ok(new ApiResponse<EmailServiceLookup>(ResultCodes.Success, new[] { data }, new[] { MessageConstants.EmailServiceRetrievedSuccessfully }));
        }

        [Authorize(Roles = nameof(RoleType.ADMIN))]
        [HttpPost]
        public async Task<IActionResult> AddEmailService(EmailServiceLookup payload)
        {
            var userID = HeaderHelper.GetUserIdFromHeaderOrClaims(HttpContext);
            var serviceData = (await _emailservicelookup.GetAll()).SingleOrDefault(name => name.ServiceName.Equals(payload.ServiceName.Trim()));
            if (serviceData != null)
                throw new BadHttpRequestException(string.Format(MessageConstants.AlreadyExistsFormat, payload.ServiceName));

            payload.Active = true;
            payload.CreatedBy = userID;
            payload.ModifiedBy = userID;
            int insertedId = (await _emailservicelookup.SaveUpdateEntity(payload)).Value;
            var data = await _emailservicelookup.GetById(insertedId);
            return CreatedAtAction(nameof(AddEmailService), new ApiResponse<EmailServiceLookup>(ResultCodes.Success, new[] { data }, new[] { MessageConstants.EmailServiceAddedSuccessfully }));
        }

        [Authorize(Roles = nameof(RoleType.ADMIN))]
        [HttpPost]
        public async Task<IActionResult> UpdateEmailService(EmailServiceLookup payload)
        {
            var appInfo = (await _emailservicelookup.GetAll()).SingleOrDefault(name => name.ServiceName.ToLower().Equals(payload.ServiceName.Trim().ToLower()) && name.Id != payload.Id);
            if (appInfo != null)
                throw new BadHttpRequestException(string.Format(MessageConstants.AlreadyExistsFormat, appInfo.ServiceName));

            int updateStatus = (await _emailservicelookup.SaveUpdateEntity(payload)).Value;
            return Ok(new ApiResponse<int>(ResultCodes.Success, new[] { updateStatus }, new[] { MessageConstants.EmailServiceUpdatedSuccessfully }));
        }

        [Authorize(Roles = "ADMIN,USER")]
        [HttpPost]
        public async Task<IActionResult> FindAdminDashboardDataAsync(FindAdminDashboardDataRequest request)
        {
            var data = await _commondata.FindAdminDashboardData(request.UserId);
            return Ok(new ApiResponse<DashboardEmailDto>(ResultCodes.Success, data.ToArray(), new[] { MessageConstants.DashboardDataRetrievedSuccessfully }));
        }

        [Authorize(Roles = "ADMIN,USER")]
        [HttpGet]
        public async Task<IActionResult> FindTop10AppsAsync()
        {
            var data = await _commondata.FindTop10Apps();
            return Ok(new ApiResponse<Top10AppsDto>(ResultCodes.Success, data.ToArray(), new[] { MessageConstants.DashboardDataRetrievedSuccessfully }));
        }

        [Authorize(Roles = "ADMIN,USER")]
        [HttpGet]
        public async Task<IActionResult> FindTop5AppsUtilisationAsync()
        {
            var data = await _commondata.FindTop5AppsUtilisation();
            return Ok(new ApiResponse<Top5AppsUtilisationDto>(ResultCodes.Success, data.ToArray(), new[] { MessageConstants.DashboardDataRetrievedSuccessfully }));
        }

        [Authorize(Roles = nameof(RoleType.ADMIN))]
        [HttpPost]
        public async Task<IActionResult> FindRowsCount(CountDto count)
        {
            int rowCount = string.IsNullOrEmpty(count.Condition)
                ? (await _commondata.GetCount(count.Table.Trim())).Value
                : (await _commondata.GetCount(count.Table.Trim(), count.Condition.Trim())).Value;
            return Ok(new ApiResponse<int>(ResultCodes.Success, new[] { rowCount }, new[] { MessageConstants.RowCountRetrievedSuccessfully }));
        }

        [Authorize(Roles = nameof(RoleType.ADMIN))]
        [HttpPost]
        public async Task<IActionResult> GetActivityLog(ListDataRequest request)
        {
            var data = await _activitylog.GetAll(request.PageNumber, request.PageSize);
            int totalRecords = (await _activitylog.Count()).Value;
            return Ok(new ApiResponse<DataPageResult<List<ActivityLog>>>(ResultCodes.Success, new[] { DataPageResult.Create(data, request, totalRecords) }, new[] { MessageConstants.ActivityLogListRetrievedSuccessfully }));
        }
    }
}
