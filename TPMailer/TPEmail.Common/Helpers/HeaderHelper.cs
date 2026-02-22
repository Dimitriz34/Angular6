using Microsoft.AspNetCore.Http;

namespace TPEmail.Common.Helpers
{
    public static class HeaderHelper
    {
        public static string GetUserId(HttpContext httpContext)
        {
            return httpContext?.User?.Claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        }

        public static string GetUserIdFromHeaderOrClaims(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                return string.Empty;
            }

            var headers = httpContext.Request?.Headers;
            if (headers != null)
            {
                if (headers.TryGetValue("userId", out var values))
                {
                    var headerValue = values.FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(headerValue))
                    {
                        return headerValue;
                    }
                }
            }

            return GetUserId(httpContext);
        }

        public static string GetUserEmail(HttpContext httpContext)
        {
            return httpContext?.User?.Claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Name)?.Value ?? string.Empty;
        }

        public static string GetUserRole(HttpContext httpContext)
        {
            return httpContext?.User?.Claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty;
        }

        public static string GetAppClientId(HttpContext httpContext)
        {
            return httpContext?.User?.Claims?.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value ?? string.Empty;
        }

        public static void ValidateUserIdHeader(HttpContext httpContext)
        {
            var userId = GetUserIdFromHeaderOrClaims(httpContext);
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new BadHttpRequestException("userId is missing from authentication context");
            }
        }
    }
}
