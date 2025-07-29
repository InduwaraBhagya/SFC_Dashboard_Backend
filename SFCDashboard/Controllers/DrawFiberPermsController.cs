using Microsoft.AspNetCore.Mvc;

using SFCDashboard.ApiClients;

namespace SFCDashboard.Controllers
{
    public class DrawFiberPermsController : BaseController
    {
        // Remove the redundant IUsersApiClient since it's inherited from BaseController
        private readonly IUserRolesApiClient _userRolesApiClient;
        private readonly IWorkGroupsApiClient _workGroupsApiClient;
        private readonly IPermissionsApiClient _permissionsApiClient;
        private readonly IRolePermissionsApiClient _rolePermissionsApiClient;
        private readonly ILogger<DrawFiberPermsController> _logger;

        public DrawFiberPermsController(
            IUsersApiClient usersApiClient,
            IUserRolesApiClient userRolesApiClient,
            IWorkGroupsApiClient workGroupsApiClient,
            IPermissionsApiClient permissionsApiClient,
            IRolePermissionsApiClient rolePermissionsApiClient,
            ILogger<DrawFiberPermsController> logger)
            : base(usersApiClient)
        {
            _userRolesApiClient = userRolesApiClient;
            _workGroupsApiClient = workGroupsApiClient;
            _permissionsApiClient = permissionsApiClient;
            _rolePermissionsApiClient = rolePermissionsApiClient;
            _logger = logger;
        }

        // GET: DrawFiberPerms
        public async Task<IActionResult> Index()
        {
            // Call backend API to check permission
            var serviceId = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrEmpty(serviceId))
                return RedirectToAction("Index", "PlannedEvents");

            serviceId = serviceId.Length > 6 ? serviceId.Substring(0, 6) : serviceId;

            // Call backend API endpoint for permission check
            var hasPerm = await _rolePermissionsApiClient.HasPermissionAsync(serviceId, "ManageDrawFiberPerms");
            if (!hasPerm)
            {
                return RedirectToAction("Index", "PlannedEvents");
            }

            // Get all users and filter for NET-PROJ-ACC-CABLE workgroup
            var allUsers = await _usersApiClient.GetAllAsync();
            var netProjAccCableUsers = allUsers
                .Where(u => u.UserWorkGroups != null && u.UserWorkGroups.Any(uwg => uwg.WorkGroup != null && uwg.WorkGroup.Name == "NET-PROJ-ACC-CABLE"))
                .OrderBy(u => u.Name)
                .ToList();

            // For each user, get permission info from backend API
            var viewModel = new List<DrawFiberPermsViewModel>();
            foreach (var user in netProjAccCableUsers)
            {
                var permResult = await _rolePermissionsApiClient.GetUserPermissionsAsync(user.Id);
                viewModel.Add(new DrawFiberPermsViewModel
                {
                    UserId = user.Id,
                    UserName = user.Name,
                    ServiceId = user.ServiceId,
                    RoleName = user.UserRole?.Name ?? "No Role",
                    HasManageProjects = permResult.HasManageProjects,
                    HasCanManageEstimatedTime = permResult.HasCanManageEstimatedTime
                });
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePermissions(int userId, bool manageProjects, bool canManageEstimatedTime)
        {
            // Call backend API endpoint to update permissions
            var serviceId = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrEmpty(serviceId))
                return Json(new { success = false, message = "Unauthorized access" });

            serviceId = serviceId.Length > 6 ? serviceId.Substring(0, 6) : serviceId;

            var hasPerm = await _rolePermissionsApiClient.HasPermissionAsync(serviceId, "ManageDrawFiberPerms");
            if (!hasPerm)
            {
                return Json(new { success = false, message = "Unauthorized access" });
            }

            // Call backend API to update permissions
            var result = await _rolePermissionsApiClient.UpdateUserPermissionsAsync(userId, manageProjects, canManageEstimatedTime);
            return Json(result);
        }

        // Permission checks are now handled by backend API endpoints
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
