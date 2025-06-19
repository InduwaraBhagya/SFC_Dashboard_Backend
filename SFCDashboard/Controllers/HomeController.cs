using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Models;

namespace SFCDashboard.Controllers
{
    public class HomeController : Controller
    {
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

        public IActionResult Unauthorized()
        {
            var serviceId = ExtractServiceId(User.Identity?.Name ?? string.Empty);
            ViewData["ServiceId"] = serviceId;
            return View();
        }

        private static string ExtractServiceId(string email)
        {
            if (string.IsNullOrEmpty(email))
                return string.Empty;

            return email[..Math.Min(email.Length, 6)];
        }
    }
}
