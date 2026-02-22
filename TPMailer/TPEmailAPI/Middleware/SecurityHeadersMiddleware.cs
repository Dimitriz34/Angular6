namespace TPEmailAPI.Middleware
{
    /// <summary>
    /// Security headers middleware — OWASP 2025 recommendations.
    /// Server header suppressed via Kestrel config (AddServerHeader = false) in Program.cs.
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _environment;

        public SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment)
        {
            _next = next;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            AddSecurityHeaders(context);
            await _next(context);
        }

        private void AddSecurityHeaders(HttpContext context)
        {
            var headers = context.Response.Headers;
            var request = context.Request;
            bool isSwagger = request.Path.StartsWithSegments("/swagger");
            bool isApi = request.Path.StartsWithSegments("/api");

            // Clickjacking protection
            headers.Append("X-Frame-Options", "DENY");

            // Prevent MIME-sniffing
            headers.Append("X-Content-Type-Options", "nosniff");

            // X-XSS-Protection intentionally omitted — deprecated, CSP replaces it.

            // Content Security Policy — relaxed only for Swagger in Development
            bool isDev = _environment.IsDevelopment();
            bool relaxForSwagger = isDev && (isSwagger || request.Path.Value == "/");

            var cspApiUrl = Environment.GetEnvironmentVariable("tpcspconnectsrc") ?? "https://localhost:7187";
            var connectSrc = relaxForSwagger
                ? "connect-src 'self' https://localhost:* http://localhost:*; "
                : $"connect-src 'self' {cspApiUrl}; ";

            var csp = relaxForSwagger
                ? "default-src 'self'; " +
                  "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                  "style-src 'self' 'unsafe-inline'; " +
                  "img-src 'self' data: https: blob:; " +
                  "font-src 'self' data:; " +
                  connectSrc +
                  "frame-ancestors 'none'; base-uri 'self'; form-action 'self'; " +
                  "object-src 'none';"
                : "default-src 'self'; " +
                  "script-src 'self'; " +
                  "style-src 'self' 'unsafe-inline'; " +  // Angular needs inline styles
                  "img-src 'self' data: https:; " +
                  "font-src 'self'; " +
                  connectSrc +
                  "frame-ancestors 'none'; base-uri 'self'; form-action 'self'; " +
                  "object-src 'none';" +
                  (isDev ? "" : " upgrade-insecure-requests;");  // only in production

            headers.Append("Content-Security-Policy", csp);

            // HSTS
            if (request.IsHttps)
                headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");

            // Referrer control
            headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

            // Disable unnecessary browser features
            headers.Append("Permissions-Policy",
                "geolocation=(), camera=(), microphone=(), payment=()");

            headers.Append("Cross-Origin-Opener-Policy", "same-origin");
            headers.Append("Cross-Origin-Resource-Policy", "same-origin");

            // Prevent Flash/PDF cross-domain
            headers.Append("X-Permitted-Cross-Domain-Policies", "none");

            // DNS prefetch control
            headers.Append("X-DNS-Prefetch-Control", "off");

            // Cache control — no-store for API, no-cache for other non-Swagger content
            if (isApi)
            {
                headers.Append("Cache-Control", "no-store, no-cache, must-revalidate, proxy-revalidate, max-age=0");
                headers.Append("Pragma", "no-cache");
                headers.Append("Expires", "0");
            }
            else if (!isSwagger)
            {
                headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            }

            // Clear-Site-Data on logout
            if (request.Path.Value?.Contains("logout", StringComparison.OrdinalIgnoreCase) == true)
                headers.Append("Clear-Site-Data", "\"cache\", \"cookies\", \"storage\"");
        }
    }

    public static class SecurityHeadersMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
            => builder.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
