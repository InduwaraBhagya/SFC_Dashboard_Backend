using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SFCDashboard.ApiClients;

namespace SFCDashboard.Controllers
{
    public class BaseController : Controller
    {
        protected readonly IUsersApiClient _usersApiClient;

        public BaseController(IUsersApiClient usersApiClient)
        {
            _usersApiClient = usersApiClient;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Set user data for layout before any action executes
            await SetUserDataForLayout();
            
            // Continue with the action execution
            await base.OnActionExecutionAsync(context, next);
        }

        protected async Task SetUserDataForLayout()
        {
            try
            {
                var serviceId = ExtractServiceId(User.Identity?.Name ?? string.Empty);
                if (!string.IsNullOrEmpty(serviceId))
                {
                    // Get all user layout data in a single API call
                    var layoutData = await _usersApiClient.GetUserLayoutDataAsync(serviceId);
                    if (layoutData != null)
                    {
                        ViewData["CurrentUserName"] = layoutData.UserName;
                        ViewData["IsAdmin"] = layoutData.IsAdmin;
                        ViewData["CanManageCustomerAssignments"] = layoutData.CanManageCustomerAssignments;
                        ViewData["CanManageDrawFiberPerms"] = layoutData.CanManageDrawFiberPerms;
                        ViewData["CanManageEstimatedTime"] = layoutData.CanManageEstimatedTime;
                        ViewData["CanSendUrgentRequests"] = layoutData.CanSendPEUrgentRequests;
                        ViewData["CanAcceptUrgentRequests"] = layoutData.CanAcceptUrgentRequests;
                        ViewData["CanMakeTasksUrgent"] = layoutData.CanMakeTasksUrgent;
                        ViewData["CanViewAll"] = layoutData.CanViewAll;
                        ViewData["CanReportIssues"] = layoutData.CanReportIssues;
                        ViewData["ManageProjects"] = layoutData.ManageProjects;
                        ViewData["CanManageNotices"] = layoutData.CanManageNotices;
                    }
                    else
                    {
                        SetDefaultViewData();
                    }
                }
                else
                {
                    SetDefaultViewData();
                }
            }
            catch
            {
                SetDefaultViewData();
            }
        }

        private void SetDefaultViewData()
        {
            ViewData["CurrentUserName"] = "Guest";
            ViewData["IsAdmin"] = false;
            ViewData["CanManageCustomerAssignments"] = false;
            ViewData["CanManageDrawFiberPerms"] = false;
            ViewData["CanManageEstimatedTime"] = false;
            ViewData["CanSendUrgentRequests"] = false;
            ViewData["CanAcceptUrgentRequests"] = false;
            ViewData["CanMakeTasksUrgent"] = false;
            ViewData["CanViewAll"] = false;
            ViewData["CanReportIssues"] = false;
            ViewData["ManageProjects"] = false;
            ViewData["CanManageNotices"] = false;
        }

        protected static string ExtractServiceId(string email)
        {
            if (string.IsNullOrEmpty(email))
                return string.Empty;

            return email[..Math.Min(email.Length, 6)];
        }
    }
}
