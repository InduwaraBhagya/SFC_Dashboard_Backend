using Microsoft.EntityFrameworkCore;
using SFCDashboard.Api.Data;
using SFCDashboard.Api.Models;

namespace SFCDashboard.Api.Services
{
    public class RolePermissionsApiService : IRolePermissionsApiService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RolePermissionsApiService> _logger;

        public RolePermissionsApiService(ApplicationDbContext context, ILogger<RolePermissionsApiService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> HasPermissionAsync(string serviceId, string permissionName)
        {
            try
            {
                if (string.IsNullOrEmpty(serviceId))
                    return false;

                serviceId = serviceId.Length > 6 ? serviceId.Substring(0, 6) : serviceId;

                var user = await _context.Users
                    .Include(u => u.UserRole)
                        .ThenInclude(r => r != null ? r.RolePermissions : null!)
                            .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(u => u.ServiceId == serviceId);

                if (user?.UserRole == null)
                    return false;

                return user.UserRole.RolePermissions
                    .Any(rp => rp.Permission.Name.Equals(permissionName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission {PermissionName} for service ID {ServiceId}", permissionName, serviceId);
                return false;
            }
        }

        public async Task<object> GetUserPermissionsAsync(int userId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRole)
                        .ThenInclude(r => r != null ? r.RolePermissions : null!)
                            .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return new { error = $"User with ID {userId} not found" };

                // Check specific permissions
                var hasManageProjects = user.UserRole?.RolePermissions
                    .Any(rp => rp.Permission.Name == "ManageProjects") ?? false;

                var hasCanManageEstimatedTime = user.UserRole?.RolePermissions
                    .Any(rp => rp.Permission.Name == "CanManageEstimatedTime") ?? false;

                var hasManageCustomerAssignments = user.UserRole?.RolePermissions
                    .Any(rp => rp.Permission.Name == "ManageCustomerAssignments") ?? false;

                var hasCanReportIssues = user.UserRole?.RolePermissions
                    .Any(rp => rp.Permission.Name == "CanReportIssues") ?? false;

                var hasManageDrawFiberPerms = user.UserRole?.RolePermissions
                    .Any(rp => rp.Permission.Name == "ManageDrawFiberPerms") ?? false;

                var hasAdmin = user.UserRole?.RolePermissions
                    .Any(rp => rp.Permission.Name == "Admin") ?? false;

                var hasViewAll = user.UserRole?.RolePermissions
                    .Any(rp => rp.Permission.Name == "ViewAll") ?? false;

                return new
                {
                    userId = user.Id,
                    userName = user.Name,
                    userRole = user.UserRole?.Name ?? "No Role",
                    permissions = new
                    {
                        manageProjects = hasManageProjects,
                        canManageEstimatedTime = hasCanManageEstimatedTime,
                        manageCustomerAssignments = hasManageCustomerAssignments,
                        canReportIssues = hasCanReportIssues,
                        manageDrawFiberPerms = hasManageDrawFiberPerms,
                        admin = hasAdmin,
                        viewAll = hasViewAll
                    },
                    allPermissions = user.UserRole?.RolePermissions
                        .Select(rp => new { id = rp.Permission.Id, name = rp.Permission.Name })
                        .ToList() as object ?? new List<object>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user permissions for user {UserId}", userId);
                return new { error = "Internal server error" };
            }
        }

        public async Task<bool> UpdateUserPermissionsAsync(UpdateUserPermissionsRequest request)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRole)
                        .ThenInclude(r => r != null ? r.RolePermissions : null!)
                    .FirstOrDefaultAsync(u => u.Id == request.UserId);

                if (user?.UserRole == null)
                    return false;

                // Get the permissions
                var manageProjectsPermission = await _context.Permissions
                    .FirstOrDefaultAsync(p => p.Name == "ManageProjects");
                var canManageEstimatedTimePermission = await _context.Permissions
                    .FirstOrDefaultAsync(p => p.Name == "CanManageEstimatedTime");
                var manageCustomerAssignmentsPermission = await _context.Permissions
                    .FirstOrDefaultAsync(p => p.Name == "ManageCustomerAssignments");
                var canReportIssuesPermission = await _context.Permissions
                    .FirstOrDefaultAsync(p => p.Name == "CanReportIssues");
                var manageDrawFiberPermsPermission = await _context.Permissions
                    .FirstOrDefaultAsync(p => p.Name == "ManageDrawFiberPerms");
                var canSendPEUrgentRequestsPermission = await _context.Permissions
                    .FirstOrDefaultAsync(p => p.Name == "CanSendPEUrgentRequests");
                var canAcceptUrgentRequestsPermission = await _context.Permissions
                    .FirstOrDefaultAsync(p => p.Name == "CanAcceptUrgentRequests");
                var canMakeTasksUrgentPermission = await _context.Permissions
                    .FirstOrDefaultAsync(p => p.Name == "CanMakeTasksUrgent");

                // Create a list of permissions to handle
                var permissionsToHandle = new[]
                {
                    new { Permission = manageProjectsPermission, RequestValue = request.ManageProjects },
                    new { Permission = canManageEstimatedTimePermission, RequestValue = request.CanManageEstimatedTime },
                    new { Permission = manageCustomerAssignmentsPermission, RequestValue = request.ManageCustomerAssignments },
                    new { Permission = canReportIssuesPermission, RequestValue = request.CanReportIssues },
                    new { Permission = manageDrawFiberPermsPermission, RequestValue = request.ManageDrawFiberPerms },
                    new { Permission = canSendPEUrgentRequestsPermission, RequestValue = request.CanSendPEUrgentRequests },
                    new { Permission = canAcceptUrgentRequestsPermission, RequestValue = request.CanAcceptUrgentRequests },
                    new { Permission = canMakeTasksUrgentPermission, RequestValue = request.CanMakeTasksUrgent }
                };

                // Process each permission
                foreach (var permissionData in permissionsToHandle)
                {
                    if (permissionData.Permission == null) continue;

                    var existingRolePermission = user.UserRole.RolePermissions
                        .FirstOrDefault(rp => rp.PermissionId == permissionData.Permission.Id);

                    if (permissionData.RequestValue && existingRolePermission == null)
                    {
                        // Add permission
                        _context.RolePermissions.Add(new RolePermission
                        {
                            RoleId = user.UserRole.Id,
                            PermissionId = permissionData.Permission.Id
                        });
                    }
                    else if (!permissionData.RequestValue && existingRolePermission != null)
                    {
                        // Remove permission
                        _context.RolePermissions.Remove(existingRolePermission);
                    }
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user permissions for user {UserId}", request.UserId);
                return false;
            }
        }

        public async Task<bool> CanUserManagePermissions(string serviceId)
        {
            try
            {
                serviceId = serviceId.Length > 6 ? serviceId.Substring(0, 6) : serviceId;

                var currentUser = await _context.Users
                    .Include(u => u.UserRole)
                        .ThenInclude(r => r != null ? r.RolePermissions : null!)
                            .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(u => u.ServiceId == serviceId);

                return currentUser?.UserRole?.RolePermissions
                    .Any(rp => rp.Permission.Name == "ManageDrawFiberPerms") ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking manage permissions for service ID {ServiceId}", serviceId);
                return false;
            }
        }
    }
}
