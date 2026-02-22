#nullable disable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TPEmail.Common.Helpers;
using TPEmail.BusinessModels.RequestModels;
using TPEmail.BusinessModels.ResponseModels;
using TPEmail.BusinessModels.Enums;
using TPEmail.BusinessModels.Constants;
using TPEmail.DataAccess.Interface.Service.v1_0;
using Asp.Versioning;

namespace TPEmailAPI.Controllers.v_1_0
{
    [Authorize]
    [ApiVersion("1.0")]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IAppService _appservice;
        private readonly IEmailService _emailservice;

        public UserController(IAppService appService, IEmailService emailService)
        {
            _appservice = appService;
            _emailservice = emailService;
        }

        [Authorize(Roles = nameof(RoleType.ADMIN))]
        [HttpPost]
        public async Task<IActionResult> FindAppUserListAsync(ListDataRequest request)
        {
            var data = await _appservice.FindAllAppUser(request.PageNumber, request.PageSize, request.SearchTerm, request.RoleId, request.Active, request.SortBy);
            int totalRecords = (await _appservice.GetUserCount(request.SearchTerm, request.RoleId, request.Active)).Value;
            return Ok(new ApiResponse<DataPageResult<List<AppUserGetDto>>>(ResultCodes.Success, new[] { DataPageResult.Create(data, request, totalRecords) }, new[] { MessageConstants.UserListRetrievedSuccessfully }));
        }

        [HttpGet]
        public async Task<IActionResult> FindAppUserListDataAsync()
        {
            var data = await _appservice.FindAllAppUserDDL();
            return Ok(new ApiResponse<UserShortInfo>(ResultCodes.Success, data.ToArray(), new[] { MessageConstants.UserListRetrievedSuccessfully }));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> FindAppUserAsync([FromRoute] string id)
        {
            var data = await _appservice.FindUserByIdAsync(id);
            return data == null
                ? Ok(new ApiResponse<AppUserGetDto>(ResultCodes.ValidationFailure, null, new[] { MessageConstants.UserNotFound }))
                : Ok(new ApiResponse<AppUserGetDto>(ResultCodes.Success, new[] { data }, new[] { MessageConstants.UserRetrievedSuccessfully }));
        }

        [Authorize(Roles = nameof(RoleType.ADMIN))]
        [HttpPost]
        public async Task<IActionResult> UpdateAppUserApproval(UpdateAppUserApprovalRequest request)
        {
            int data = (await _appservice.UpdateVerifiedUserAsync(request.UserId, request.Active)).Value;
            return Ok(new ApiResponse<int>(ResultCodes.Success, new[] { data }, new[] { MessageConstants.UserApprovalUpdatedSuccessfully }));
        }

        [Authorize(Roles = nameof(RoleType.ADMIN))]
        [HttpPost]
        public async Task<IActionResult> UpdateUserRole(UpdateUserRoleRequest request)
        {
            if (!Guid.TryParse(request.UserId, out Guid userIdGuid))
                return Ok(new ApiResponse<int>(ResultCodes.ValidationFailure, null, new[] { MessageConstants.InvalidUserIdFormat }));

            var modifiedBy = HeaderHelper.GetUserIdFromHeaderOrClaims(HttpContext);
            if (string.IsNullOrWhiteSpace(modifiedBy)) modifiedBy = MessageConstants.SystemUser;

            int data = (await _appservice.UpdateUserRoleAsync(userIdGuid, request.RoleId, modifiedBy)).Value;
            return Ok(new ApiResponse<int>(ResultCodes.Success, new[] { data }, new[] { MessageConstants.UserRoleUpdatedSuccessfully }));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAppUserCredentials(AppUser userData)
        {
            int data = (await _appservice.UpdateAppUserCredentials(userData)).Value;
            return Ok(new ApiResponse<int>(ResultCodes.Success, new[] { data }, new[] { MessageConstants.UserCredentialsUpdatedSuccessfully }));
        }

        [HttpGet]
        public async Task<IActionResult> GetUserInfo()
        {
            var userID = HeaderHelper.GetUserIdFromHeaderOrClaims(HttpContext);
            var email = EncryptionHelper.DecryptOrDefault(User.Identity!.Name!, null, "UserController.GetUserInfo", userID);
            var username = User.Claims.FirstOrDefault(x => x.Type.Equals("username"))?.Value ?? email;
            var role = User.Claims.FirstOrDefault(x => x.Type.Equals(ClaimTypes.Role))?.Value ?? string.Empty;
            string roleString = "USER";
            if (!string.IsNullOrEmpty(role) && Enum.TryParse<RoleType>(role, ignoreCase: true, out var parsedRole))
                roleString = parsedRole.ToString();

            UserShortInfo data = new UserShortInfo()
            {
                UserId = userID,
                Email = email,
                Username = username,
                Role = roleString,
            };
            return Ok(new ApiResponse<UserShortInfo>(ResultCodes.Success, new[] { data }, new[] { MessageConstants.UserInfoRetrievedSuccessfully }));
        }

        [HttpGet]
        public async Task<IActionResult> FindAppRole()
        {
            var data = await _appservice.FindAppRole();
            return Ok(new ApiResponse<AppRole>(ResultCodes.Success, data.ToArray(), new[] { MessageConstants.RoleListRetrievedSuccessfully }));
        }

        [HttpPost]
        public async Task<IActionResult> GetADUser(ADUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Upn))
                return Ok(new ApiResponse<ADUser>(ResultCodes.ValidationFailure, null, new string[] { MessageConstants.UpnRequired }));
            var result = await _appservice.GetADUserAsync(request.Upn);
            return result == null
                ? Ok(new ApiResponse<ADUser>(ResultCodes.ValidationFailure, null, new string[] { MessageConstants.UserNotFound }))
                : Ok(new ApiResponse<ADUser>(ResultCodes.Success, new[] { result }, new string[] { MessageConstants.OperationSuccessful }));
        }

        [HttpPost]
        public async Task<IActionResult> GetADUserPhoto(ADUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Upn))
                return Ok(new { success = false, error = MessageConstants.UpnRequired });
            var result = await _appservice.GetADUserPhotoAsync(request.Upn);
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> SearchADUsers(ADUserSearchRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Term))
                return Ok(new ApiResponse<ADUserSearchResponse>(ResultCodes.ValidationFailure, null, new string[] { MessageConstants.SearchTermRequired }));
            var result = await _appservice.SearchADUsersAsync(request.Term, request.SearchType);
            return Ok(new ApiResponse<ADUserSearchResponse>(ResultCodes.Success, new[] { result }, new string[] { MessageConstants.OperationSuccessful }));
        }
    }
}
