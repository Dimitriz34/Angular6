using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using TPEmail.BusinessModels.Constants;
using TPEmail.DataAccess.Interface.Service.v1_0;

namespace TPEmailAPI.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly NLog.ILogger _nlogger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            _nlogger = NLog.LogManager.GetCurrentClassLogger();
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                var userId = httpContext.User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? "Unknown";
                var userName = httpContext.User?.Claims?.FirstOrDefault(c => c.Type == "username")?.Value
                    ?? httpContext.User?.Identity?.Name
                    ?? "Unknown";
                var requestPath = httpContext.Request.Path;
                var requestMethod = httpContext.Request.Method;

                NLog.ScopeContext.TryGetProperty("ApplicationName", out var appNameObj);
                var applicationName = appNameObj as string;
                if (string.IsNullOrEmpty(applicationName))
                    applicationName = await ResolveApplicationNameAsync(httpContext);

                _nlogger.Error(ex, $"[{requestMethod} {requestPath}] Application: {applicationName} | User: {userName} (Id: {userId}) - Unhandled exception: {ex.Message}");
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private static async Task<string> ResolveApplicationNameAsync(HttpContext httpContext)
        {
            try
            {
                var jtiClaim = httpContext.User?.Claims?.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti || c.Type == "jti")?.Value;
                if (string.IsNullOrEmpty(jtiClaim)) return "Unknown";

                var firstClient = jtiClaim.Contains(';') ? jtiClaim.Split(';')[0] : jtiClaim;
                if (!Guid.TryParse(firstClient, out var appClientGuid)) return "Unknown";

                var appLookup = httpContext.RequestServices.GetService<IAppLookup>();
                if (appLookup == null) return "Unknown";

                var appData = await appLookup.FindAppLookup(appClientGuid);
                return appData is { Id: > 0 } ? appData.AppName : "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Check if response has already started - if so, we can't modify headers
            if (context.Response.HasStarted)
            {
                // Log the fact that we couldn't send a proper error response
                var logger = context.RequestServices.GetRequiredService<ILogger<ExceptionMiddleware>>();
                logger.LogWarning("Response has already started, cannot send error response for exception: {Message}", exception.Message);
                return;
            }

            context.Response.Clear();
            context.Response.ContentType = "application/json";
            
            // Set appropriate status code based on exception type
            if (exception is BadHttpRequestException badRequestEx)
            {
                context.Response.StatusCode = badRequestEx.StatusCode;
            }
            else if (exception is UnauthorizedAccessException)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }

            var response = new
            {
                statusCode = context.Response.StatusCode,
                message = context.Response.StatusCode == (int)HttpStatusCode.InternalServerError 
                    ? MessageConstants.InternalServerError 
                    : exception.Message,
                detail = exception.Message 
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
