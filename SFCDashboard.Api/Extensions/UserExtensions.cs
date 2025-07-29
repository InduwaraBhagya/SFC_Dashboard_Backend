using Microsoft.AspNetCore.Http;
using SFCDashboard.Api.Data;
using Microsoft.EntityFrameworkCore;

public static class UserExtensions
{
    public static async Task<bool> HasAdminPermissionAsync(this HttpContext context, ApplicationDbContext dbContext)
    {
        var serviceId = context.User?.Identity?.Name;
        if (string.IsNullOrEmpty(serviceId))
            return false;

        // Extract first 6 chars of service ID
        serviceId = serviceId.Length > 6 ? serviceId.Substring(0, 6) : serviceId;

        var user = await dbContext.Users
            .Include(u => u.UserRole)
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.ServiceId == serviceId);

        return user?.UserRole?.RolePermissions
            .Any(rp => rp.Permission.Name == "Admin") ?? false;
    }
}
