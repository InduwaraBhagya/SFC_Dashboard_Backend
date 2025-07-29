using Microsoft.EntityFrameworkCore;
using SFCDashboard.Api.Data;
using SFCDashboard.Api.Models;

namespace SFCDashboard.Api.Services
{
    public class UserRolesApiService : IUserRolesApiService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserRolesApiService> _logger;

        public UserRolesApiService(ApplicationDbContext context, ILogger<UserRolesApiService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<UserRole>> GetUserRolesAsync()
        {
            try
            {
                return await _context.UserRoles.Include(r => r.RolePermissions).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all user roles");
                return Enumerable.Empty<UserRole>();
            }
        }

        public async Task<UserRole?> GetUserRoleAsync(int id)
        {
            try
            {
                return await _context.UserRoles
                    .Include(r => r.RolePermissions)
                    .FirstOrDefaultAsync(r => r.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user role {Id}", id);
                return null;
            }
        }

        public async Task<UserRole?> GetUserRoleWithPermissionsAsync(int id)
        {
            try
            {
                return await _context.UserRoles
                    .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(r => r.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user role {Id} with permissions", id);
                return null;
            }
        }

        public async Task<UserRole?> CreateUserRoleAsync(UserRole userRole)
        {
            try
            {
                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();
                return userRole;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user role");
                return null;
            }
        }

        public async Task<UserRole?> UpdateUserRoleAsync(UserRole userRole)
        {
            try
            {
                _context.Entry(userRole).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return userRole;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user role {Id}", userRole.Id);
                return null;
            }
        }

        public async Task<bool> DeleteUserRoleAsync(int id)
        {
            try
            {
                var role = await _context.UserRoles.FindAsync(id);
                if (role == null)
                    return false;

                _context.UserRoles.Remove(role);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user role {Id}", id);
                return false;
            }
        }

        public async Task<bool> UserRoleExistsAsync(int id)
        {
            try
            {
                return await _context.UserRoles.AnyAsync(e => e.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user role {Id} exists", id);
                return false;
            }
        }

        public async Task<bool> IsUserAdminAsync(string serviceId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(serviceId))
                    return false;

                // Truncate serviceId to 6 characters if longer (following pattern from other controllers)
                serviceId = serviceId.Length > 6 ? serviceId.Substring(0, 6) : serviceId;

                var user = await _context.Users
                    .Include(u => u.UserRole)
                    .ThenInclude(r => r != null ? r.RolePermissions : null!)
                    .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(u => u.ServiceId == serviceId);

                if (user?.UserRole == null)
                    return false;

                // Check if user has Admin permission
                var hasAdminPermission = user.UserRole.RolePermissions
                    .Any(rp => rp.Permission.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase));

                return hasAdminPermission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking admin status for user {ServiceId}", serviceId);
                return false;
            }
        }
    }
}
