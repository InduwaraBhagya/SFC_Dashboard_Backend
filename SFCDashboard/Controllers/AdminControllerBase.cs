using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SFCDashboard.ApiClients;

namespace SFCDashboard.Controllers
{
    public class AdminControllerBase : BaseController
    {
        protected readonly IPermissionsApiClient _permissionsApi;
        protected readonly IRolePermissionsApiClient _rolePermissionsApi;

        public AdminControllerBase(IPermissionsApiClient permissionsApi, IUsersApiClient usersApiClient, IRolePermissionsApiClient rolePermissionsApi)
            : base(usersApiClient)
        {
            _permissionsApi = permissionsApi;
            _rolePermissionsApi = rolePermissionsApi;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Get the current user's service ID
            var serviceId = User.Identity?.Name;

            if (string.IsNullOrEmpty(serviceId))
            {
                context.Result = RedirectToAction("Index", "PlannedEvents");
                return;
            }

            // Check admin permission via the working RolePermissions API
            var isAdmin = await _rolePermissionsApi.HasPermissionAsync(serviceId, "Admin");

            if (!isAdmin)
            {
                context.Result = RedirectToAction("Index", "PlannedEvents");
                return;
            }

            // If admin check passes, proceed with base controller logic (setting user data)
            await base.OnActionExecutionAsync(context, next);
        }
    }
}