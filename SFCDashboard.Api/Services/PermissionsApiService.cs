using Microsoft.EntityFrameworkCore;
using SFCDashboard.Api.Data;
using SFCDashboard.Api.Models;

namespace SFCDashboard.Api.Services
{
    public class PermissionsApiService : IPermissionsApiService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PermissionsApiService> _logger;

        public PermissionsApiService(ApplicationDbContext context, ILogger<PermissionsApiService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> HasPermissionAsync(string serviceId, string permissionName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(serviceId) || string.IsNullOrWhiteSpace(permissionName))
                    return false;

                // Truncate serviceId to 6 characters if longer (following pattern from other services)
                serviceId = serviceId.Length > 6 ? serviceId.Substring(0, 6) : serviceId;

                // Get user with role and permissions in a single query with proper includes
                var user = await _context.Users
                    .Include(u => u.UserRole)
                        .ThenInclude(r => r != null ? r.RolePermissions : null!)
                            .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(u => u.ServiceId == serviceId);

                if (user?.UserRole == null)
                    return false;

                // Check if user's role has the specific permission (case-insensitive)
                return user.UserRole.RolePermissions
                    .Any(rp => rp.Permission.Name.Equals(permissionName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission {Permission} for user {ServiceId}", permissionName, serviceId);
                return false;
            }
        }

        public async Task<IEnumerable<Permission>> GetAllPermissionsAsync()
        {
            try
            {
                return await _context.Permissions.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all permissions");
                return Enumerable.Empty<Permission>();
            }
        }

        public async Task<Permission?> GetPermissionByIdAsync(int id)
        {
            try
            {
                return await _context.Permissions.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permission {Id}", id);
                return null;
            }
        }

        public async Task<Permission?> CreatePermissionAsync(Permission permission)
        {
            try
            {
                _context.Permissions.Add(permission);
                await _context.SaveChangesAsync();
                return permission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating permission");
                return null;
            }
        }

        public async Task<Permission?> UpdatePermissionAsync(Permission permission)
        {
            try
            {
                _context.Entry(permission).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return permission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating permission {Id}", permission.Id);
                return null;
            }
        }

        public async Task<bool> DeletePermissionAsync(int id)
        {
            try
            {
                var permission = await _context.Permissions.FindAsync(id);
                if (permission == null)
                    return false;

                _context.Permissions.Remove(permission);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting permission {Id}", id);
                return false;
            }
        }

        public async Task<bool> PermissionExistsAsync(int id)
        {
            try
            {
                return await _context.Permissions.AnyAsync(p => p.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if permission {Id} exists", id);
                return false;
            }
        }
    }
}
