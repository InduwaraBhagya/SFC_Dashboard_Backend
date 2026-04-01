using Microsoft.AspNetCore.Authentication;

namespace SFCDashboard.Middleware
{
    /// <summary>
    /// Middleware to clear cookies before authentication challenges
    /// </summary>
    public class CookieClearingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CookieClearingMiddleware> _logger;

        public CookieClearingMiddleware(RequestDelegate next, ILogger<CookieClearingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Check if this is a login or logout request
            if (ShouldClearCookies(context))
            {
                var action = GetRequestAction(context);
                _logger.LogInformation("Clearing cookies for {Action} request from {UserAgent}", 
                    action, context.Request.Headers.UserAgent.ToString());
                
                // Clear all existing cookies before authentication or after sign-out
                ClearAllCookies(context, action);
            }

            await _next(context);
        }

        private static bool ShouldClearCookies(HttpContext context)
        {
            // Clear cookies for Microsoft Identity sign-in requests
            if (context.Request.Path.StartsWithSegments("/MicrosoftIdentity/Account/SignIn"))
                return true;

            // Clear cookies for Microsoft Identity sign-out requests
            if (context.Request.Path.StartsWithSegments("/MicrosoftIdentity/Account/SignOut"))
                return true;

            // Clear cookies for authentication challenges when user is not authenticated
            if (context.Request.Query.ContainsKey("authscheme") && 
                !context.User.Identity?.IsAuthenticated == true)
                return true;

            // Clear cookies if this is a fresh login attempt (no authentication cookies present)
            if (context.Request.Path.StartsWithSegments("/") && 
                !context.User.Identity?.IsAuthenticated == true &&
                context.Request.Cookies.Any(c => c.Key.Contains("AspNetCore") || c.Key.Contains("Identity")))
                return true;

            return false;
        }

        private static string GetRequestAction(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/MicrosoftIdentity/Account/SignIn"))
                return "sign-in";
            if (context.Request.Path.StartsWithSegments("/MicrosoftIdentity/Account/SignOut"))
                return "sign-out";
            return "authentication";
        }

        private void ClearAllCookies(HttpContext context, string action)
        {
            var cookiesCleared = 0;
            
            foreach (var cookie in context.Request.Cookies)
            {
                // Clear each cookie by setting it to expire in the past
                context.Response.Cookies.Append(cookie.Key, "", new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(-1),
                    Path = "/",
                    HttpOnly = true,
                    Secure = context.Request.IsHttps,
                    SameSite = SameSiteMode.Lax
                });

                // Also try to clear with domain variations
                var domain = context.Request.Host.Host;
                if (!string.IsNullOrEmpty(domain))
                {
                    context.Response.Cookies.Append(cookie.Key, "", new CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.AddDays(-1),
                        Path = "/",
                        Domain = domain,
                        HttpOnly = true,
                        Secure = context.Request.IsHttps,
                        SameSite = SameSiteMode.Lax
                    });
                }
                
                cookiesCleared++;
            }
            
            if (cookiesCleared > 0)
            {
                _logger.LogInformation("Cleared {CookieCount} cookies during {Action}", cookiesCleared, action);
            }
        }
    }
}
