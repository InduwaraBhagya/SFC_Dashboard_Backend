using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using SFCDashboard.Data;

namespace SFCDashboard.Middleware
{
    public class UserRegistrationMiddleware
    {
        private readonly RequestDelegate _next;

        public UserRegistrationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var serviceId = ExtractServiceId(context.User.Identity.Name ?? string.Empty);

                // First check if user exists in database
                var userExists = await dbContext.Users.AnyAsync(u => u.ServiceId == serviceId);

                if (!userExists)
                {
                    // If user doesn't exist and trying to access Register page, redirect to unauthorized
                    if (!context.Request.Path.StartsWithSegments("/Home/Unauthorized"))
                    {
                        context.Response.Redirect("/Home/Unauthorized");
                        return;
                    }
                }
                else if (!context.Request.Path.StartsWithSegments("/Register"))
                {
                    // If user exists but hasn't completed registration (no workgroup assigned)
                    var user = await dbContext.Users.FirstOrDefaultAsync(u => u.ServiceId == serviceId);
                    if (user?.WorkGroupId == null)
                    {
                        context.Response.Redirect("/Register");
                        return;
                    }
                }
            }

            await _next(context);
        }

        private static string ExtractServiceId(string email)
        {
            if (string.IsNullOrEmpty(email)) 
                return string.Empty;
            
            return email[..Math.Min(email.Length, 6)];
        }
    }
}