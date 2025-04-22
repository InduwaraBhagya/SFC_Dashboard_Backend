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
                var serviceId = context.User.GetNameIdentifierId();
                var name = context.User.Identity.Name;

                // Check if user exists in database
                var userExists = await dbContext.Users.AnyAsync(u => u.ServiceId == serviceId);

                if (!userExists && !context.Request.Path.StartsWithSegments("/Register"))
                {
                    context.Response.Redirect("/Register");
                    return;
                }
            }

            await _next(context);
        }
    }
}