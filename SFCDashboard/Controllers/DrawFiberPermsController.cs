using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDashboard.Models;

namespace SFCDashboard.Controllers
{
    public class DrawFiberPermsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DrawFiberPermsController> _logger;

        public DrawFiberPermsController(ApplicationDbContext context, ILogger<DrawFiberPermsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: DrawFiberPerms
        public async Task<IActionResult> Index()
        {
            // Check if user has ManageDrawFiberPerms permission
            if (!await HasPermissionAsync("ManageDrawFiberPerms"))
            {
                return RedirectToAction("Index", "PlannedEvents");
            }

            // Get all users in NET-PROJ-ACC-CABLE workgroup
            var netProjAccCableUsers = await _context.Users
                .Include(u => u.UserRole)
                    .ThenInclude(r => r!.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                .Include(u => u.UserWorkGroups)
                    .ThenInclude(uwg => uwg.WorkGroup)
                .Where(u => u.UserWorkGroups.Any(uwg => uwg.WorkGroup.Name == "NET-PROJ-ACC-CABLE"))
                .OrderBy(u => u.Name)
                .ToListAsync();

            var viewModel = netProjAccCableUsers.Select(user => new DrawFiberPermsViewModel
            {
                UserId = user.Id,
                UserName = user.Name,
                ServiceId = user.ServiceId,
                RoleName = user.UserRole?.Name ?? "No Role",
                HasManageProjects = user.UserRole?.HasPermission("ManageProjects") ?? false,
                HasCanManageEstimatedTime = user.UserRole?.HasPermission("CanManageEstimatedTime") ?? false
            }).ToList();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePermissions(int userId, bool manageProjects, bool canManageEstimatedTime)
        {
            // Check if user has ManageDrawFiberPerms permission
            if (!await HasPermissionAsync("ManageDrawFiberPerms"))
            {
                return Json(new { success = false, message = "Unauthorized access" });
            }

            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRole)
                        .ThenInclude(r => r!.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user?.UserRole == null)
                {
                    return Json(new { success = false, message = "User or user role not found" });
                }

                // Get the permissions
                var manageProjectsPermission = await _context.Permissions
                    .FirstOrDefaultAsync(p => p.Name == "ManageProjects");
                var canManageEstimatedTimePermission = await _context.Permissions
                    .FirstOrDefaultAsync(p => p.Name == "CanManageEstimatedTime");

                if (manageProjectsPermission == null || canManageEstimatedTimePermission == null)
                {
                    return Json(new { success = false, message = "Required permissions not found in database" });
                }

                // Handle ManageProjects permission
                var existingManageProjects = user.UserRole.RolePermissions
                    .FirstOrDefault(rp => rp.PermissionId == manageProjectsPermission.Id);

                if (manageProjects && existingManageProjects == null)
                {
                    // Add permission
                    _context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = user.UserRole.Id,
                        PermissionId = manageProjectsPermission.Id
                    });
                }
                else if (!manageProjects && existingManageProjects != null)
                {
                    // Remove permission
                    _context.RolePermissions.Remove(existingManageProjects);
                }

                // Handle CanManageEstimatedTime permission
                var existingCanManageEstimatedTime = user.UserRole.RolePermissions
                    .FirstOrDefault(rp => rp.PermissionId == canManageEstimatedTimePermission.Id);

                if (canManageEstimatedTime && existingCanManageEstimatedTime == null)
                {
                    // Add permission
                    _context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = user.UserRole.Id,
                        PermissionId = canManageEstimatedTimePermission.Id
                    });
                }
                else if (!canManageEstimatedTime && existingCanManageEstimatedTime != null)
                {
                    // Remove permission
                    _context.RolePermissions.Remove(existingCanManageEstimatedTime);
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Permissions updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating permissions for user {UserId}", userId);
                return Json(new { success = false, message = "An error occurred while updating permissions" });
            }
        }

        private async Task<bool> HasPermissionAsync(string permissionName)
        {
            var serviceId = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrEmpty(serviceId))
                return false;

            // Extract first 6 chars of service ID
            serviceId = serviceId.Length > 6 ? serviceId.Substring(0, 6) : serviceId;

            var user = await _context.Users
                .Include(u => u.UserRole)
                    .ThenInclude(r => r!.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.ServiceId == serviceId);

            return user?.UserRole?.RolePermissions
                .Any(rp => rp.Permission.Name == permissionName) ?? false;
        }
    }

    public class DrawFiberPermsViewModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string ServiceId { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public bool HasManageProjects { get; set; }
        public bool HasCanManageEstimatedTime { get; set; }
    }
}
