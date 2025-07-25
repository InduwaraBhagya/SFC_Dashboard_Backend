using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDashboard.Models;

namespace SFCDashboard.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RolePermissionsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RolePermissionsApiController> _logger;

        public RolePermissionsApiController(ApplicationDbContext context, ILogger<RolePermissionsApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/rolepermissions/has-permission/{permissionName}
        [HttpGet("has-permission/{permissionName}")]
        public async Task<IActionResult> HasPermission(string permissionName)
        {
            var serviceId = User.Identity?.Name;
            if (string.IsNullOrEmpty(serviceId))
                return Unauthorized();

            serviceId = serviceId.Length > 6 ? serviceId.Substring(0, 6) : serviceId;

            var user = await _context.Users
                .Include(u => u.UserRole)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.ServiceId == serviceId);

            var hasPermission = user?.UserRole?.RolePermissions
                .Any(rp => rp.Permission.Name == permissionName) ?? false;

            return Ok(hasPermission);
        }

        // POST: api/rolepermissions/update-user-permissions
        [HttpPost("update-user-permissions")]
        public async Task<IActionResult> UpdateUserPermissions([FromBody] UpdateUserPermissionsRequest request)
        {
            var serviceId = User.Identity?.Name;
            if (string.IsNullOrEmpty(serviceId))
                return Unauthorized();

            serviceId = serviceId.Length > 6 ? serviceId.Substring(0, 6) : serviceId;

            var currentUser = await _context.Users
                .Include(u => u.UserRole)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.ServiceId == serviceId);

            var canManage = currentUser?.UserRole?.RolePermissions
                .Any(rp => rp.Permission.Name == "ManageDrawFiberPerms") ?? false;

            if (!canManage)
                return Forbid();

            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRole)
                        .ThenInclude(r => r.RolePermissions)
                    .FirstOrDefaultAsync(u => u.Id == request.UserId);

                if (user?.UserRole == null)
                    return NotFound(new { success = false, message = "User or user role not found" });

                // Get the permissions
                var manageProjectsPermission = await _context.Permissions.FirstOrDefaultAsync(p => p.Name == "ManageProjects");
                var canManageEstimatedTimePermission = await _context.Permissions.FirstOrDefaultAsync(p => p.Name == "CanManageEstimatedTime");

                if (manageProjectsPermission == null || canManageEstimatedTimePermission == null)
                    return BadRequest(new { success = false, message = "Required permissions not found in database" });

                // Handle ManageProjects permission
                var existingManageProjects = user.UserRole.RolePermissions
                    .FirstOrDefault(rp => rp.PermissionId == manageProjectsPermission.Id);

                if (request.ManageProjects && existingManageProjects == null)
                {
                    _context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = user.UserRole.Id,
                        PermissionId = manageProjectsPermission.Id
                    });
                }
                else if (!request.ManageProjects && existingManageProjects != null)
                {
                    _context.RolePermissions.Remove(existingManageProjects);
                }

                // Handle CanManageEstimatedTime permission
                var existingCanManageEstimatedTime = user.UserRole.RolePermissions
                    .FirstOrDefault(rp => rp.PermissionId == canManageEstimatedTimePermission.Id);

                if (request.CanManageEstimatedTime && existingCanManageEstimatedTime == null)
                {
                    _context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = user.UserRole.Id,
                        PermissionId = canManageEstimatedTimePermission.Id
                    });
                }
                else if (!request.CanManageEstimatedTime && existingCanManageEstimatedTime != null)
                {
                    _context.RolePermissions.Remove(existingCanManageEstimatedTime);
                }

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Permissions updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating permissions for user {UserId}", request.UserId);
                return StatusCode(500, new { success = false, message = "An error occurred while updating permissions" });
            }
        }
    }

    public class UpdateUserPermissionsRequest
    {
        public int UserId { get; set; }
        public bool ManageProjects { get; set; }
        public bool CanManageEstimatedTime { get; set; }
    }
}
