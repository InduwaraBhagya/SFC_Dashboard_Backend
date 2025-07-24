using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Models;
using SFCDashboard.ApiClients;

namespace SFCDashboard.Controllers
{
    public class HomeController : Controller
    {
        private readonly IUsersApiClient _usersApiClient;
        
        public HomeController(IUsersApiClient usersApiClient)
        {
            _usersApiClient = usersApiClient;
        }

        public async Task<IActionResult> Index()
        {
            await SetUserDataForLayout();
            return View();
        }

        public async Task<IActionResult> Privacy()
        {
            await SetUserDataForLayout();
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public new IActionResult Unauthorized()
        {
            var serviceId = ExtractServiceId(User.Identity?.Name ?? string.Empty);
            ViewData["ServiceId"] = serviceId;
            return View();
        }

        private async Task SetUserDataForLayout()
        {
            try
            {
                var serviceId = ExtractServiceId(User.Identity?.Name ?? string.Empty);
                if (!string.IsNullOrEmpty(serviceId))
                {
                    var currentUser = await _usersApiClient.GetByServiceIdAsync(serviceId);
                    if (currentUser != null)
                    {
                        ViewData["CurrentUserName"] = currentUser.Name ?? "Guest";
                        ViewData["IsAdmin"] = currentUser.UserRole?.HasPermission("Admin") == true;
                        ViewData["CanManageCustomerAssignments"] = currentUser.UserRole?.HasPermission("ManageCustomerAssignments") == true;
                        ViewData["CanManageDrawFiberPerms"] = currentUser.UserRole?.HasPermission("ManageDrawFiberPerms") == true;
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
        }

        private static string ExtractServiceId(string email)
        {
            if (string.IsNullOrEmpty(email))
                return string.Empty;

            return email[..Math.Min(email.Length, 6)];
        }
    }
}
