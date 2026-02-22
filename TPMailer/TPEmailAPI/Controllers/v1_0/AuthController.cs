#nullable disable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TPEmail.Common.Helpers;
using TPEmail.BusinessModels.RequestModels;
using TPEmail.BusinessModels.ResponseModels;
using TPEmail.BusinessModels.Constants;
using TPEmail.DataAccess.Interface.Service.v1_0;
using Asp.Versioning;

namespace TPEmailAPI.Controllers.v_1_0
{
    [ApiVersion("1.0")]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authservice;
        private readonly IEmailService _emailservice;

        public AuthController(IAuthService authService, IEmailService emailService)
        {
            _authservice = authService;
            _emailservice = emailService;
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Register(AppUserPostDto payload)
        {
            var result = await _authservice.RegisterUser(payload);
            var userID = HeaderHelper.GetUserIdFromHeaderOrClaims(HttpContext);
            string decryptedEmail = EncryptionHelper.DecryptOrDefault(payload.Email, null, "AuthController.Register", userID);
            await _emailservice.SendEmail(EmailCompositionHelper.ComposeUserRegistrationEmail(new AppUser { Username = payload.Username, Email = payload.Email }, result.Data, decryptedEmail));
            return Ok(new ApiResponse<Guid>(ResultCodes.Success, new[] { result.EntityId ?? Guid.Empty }, new[] { MessageConstants.RegistrationSuccessfulPendingApproval }));
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(AuthenticateRequest payload)
            => Ok(await _authservice.AuthenticateUserAsync(payload));

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> RefreshToken(RefreshTokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.RefreshToken))
                return Ok(new ApiResponse<object>(ResultCodes.ValidationFailure, null, new[] { MessageConstants.RefreshTokenRequired }));

            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
            return Ok(await _authservice.RefreshTokensAsync(request.RefreshToken, clientIp));
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Logout(RefreshTokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.RefreshToken))
                return Ok(new ApiResponse<object>(ResultCodes.ValidationFailure, null, new[] { MessageConstants.RefreshTokenRequired }));

            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
            await _authservice.RevokeRefreshTokenAsync(request.RefreshToken, clientIp, MessageConstants.LogoutReason);

            return Ok(new ApiResponse<object>(ResultCodes.Success, null, new[] { MessageConstants.LoggedOutSuccessfully }));
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> AzureAuthentication(AzureAuthenticateRequest payload)
            => Ok(await _authservice.AuthenticateAzureUserAsync(payload));

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> ApplicationAuthentication([FromHeader(Name = "x-client-id")] string appClientId, [FromHeader(Name = "x-client-secret")] string appSecret)
            => Ok(await _authservice.AuthenticateApplicationAsync(appClientId, appSecret));
    }
}
