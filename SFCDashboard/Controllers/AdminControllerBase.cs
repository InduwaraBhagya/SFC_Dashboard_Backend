using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SFCDashboard.ApiClients;

namespace SFCDashboard.Controllers
{
    public class AdminControllerBase : BaseController
    {
        protected readonly IPermissionsApiClient _permissionsApi;

        public AdminControllerBase(IPermissionsApiClient permissionsApi, IUsersApiClient usersApiClient) 
            : base(usersApiClient)
        {
            _permissionsApi = permissionsApi;
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

            // Check admin permission via API call
            var isAdmin = await _permissionsApi.IsUserAdminAsync(serviceId);
            
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