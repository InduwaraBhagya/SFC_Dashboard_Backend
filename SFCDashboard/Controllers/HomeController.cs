using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Models;
using SFCDashboard.ApiClients;

namespace SFCDashboard.Controllers
{
    public class HomeController : BaseController
    {
        public HomeController(IUsersApiClient usersApiClient) : base(usersApiClient)
        {
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
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
    }
}
