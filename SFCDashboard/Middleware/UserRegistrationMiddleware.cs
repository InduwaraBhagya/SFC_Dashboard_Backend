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
                var azureAdName = context.User.Claims.FirstOrDefault(c => c.Type == "name")?.Value;

                // First check if user exists in database
                var user = await dbContext.Users
                    .Include(u => u.UserWorkGroups)
                    .FirstOrDefaultAsync(u => u.ServiceId == serviceId);

                if (user == null)
                {
                    // If user doesn't exist and trying to access Register page, redirect to unauthorized
                    if (!context.Request.Path.StartsWithSegments("/Home/Unauthorized"))
                    {
                        context.Response.Redirect("/Home/Unauthorized");
                        return;
                    }
                }
                else 
                {
                    // Update user's name from Azure AD if it has changed
                    if (!string.IsNullOrEmpty(azureAdName) && user.Name != azureAdName)
                    {
                        user.Name = azureAdName;
                        await dbContext.SaveChangesAsync();
                    }

                    // Check if registration is complete (must have at least one workgroup)
                    if (!context.Request.Path.StartsWithSegments("/Register") &&
                        (user.UserWorkGroups == null || !user.UserWorkGroups.Any()))
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