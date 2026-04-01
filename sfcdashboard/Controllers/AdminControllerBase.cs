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
                if (IsAjaxRequest(context.HttpContext.Request))
                {
                    context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                }
                else
                {
                    context.Result = RedirectToAction("Index", "PlannedEvents");
                }
                return;
            }

            // Extract first 6 characters of service ID (consistent with other controllers)
            var serviceIdShort = serviceId.Length > 6 ? serviceId.Substring(0, 6) : serviceId;

            try
            {
                // Check admin permission via API call
                var isAdmin = await _permissionsApi.IsUserAdminAsync(serviceIdShort);
                
                if (!isAdmin)
                {
                    if (IsAjaxRequest(context.HttpContext.Request))
                    {
                        context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                    }
                    else
                    {
                        context.Result = RedirectToAction("Index", "PlannedEvents");
                    }
                    return;
                }
                
                // If admin check passes, proceed with base controller logic (setting user data)
                await base.OnActionExecutionAsync(context, next);
            }
            catch (Exception)
            {
                // On error, default to no access for security
                if (IsAjaxRequest(context.HttpContext.Request))
                {
                    context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                }
                else
                {
                    context.Result = RedirectToAction("Index", "PlannedEvents");
                }
                return;
            }
        }

        private static bool IsAjaxRequest(HttpRequest request)
        {
            if (request == null) return false;
            if (request.Headers.TryGetValue("X-Requested-With", out var value))
            {
                return string.Equals(value, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
            }
            // Also treat JSON Accept header as AJAX-like
            if (request.Headers.TryGetValue("Accept", out var acceptValues))
            {
                foreach (var a in acceptValues)
                {
                    if (!string.IsNullOrEmpty(a) && a.Contains("application/json", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}