using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Identity.Web;
using SFCDashboard.Data;
using SFCDashboard.Models;

namespace SFCDashboard.Controllers
{
    public class RegisterController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RegisterController(ApplicationDbContext context)
        {
            _context = context;
        }

        private static string ExtractServiceId(string email)
        {
            if (string.IsNullOrEmpty(email)) 
                return string.Empty;
            
            return email[..Math.Min(email.Length, 6)];
        }

        public IActionResult Index()
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            var email = User.Identity?.Name ?? string.Empty;
            var serviceId = ExtractServiceId(email);
            var name = User.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? string.Empty;

            ViewData["WorkGroupId"] = new SelectList(_context.WorkGroups, "Id", "Name");

            var model = new SystemUser
            {
                Name = name,
                ServiceId = serviceId
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(SystemUser user)
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            // Get name from Azure AD claims and service ID from email
            var email = User.Identity?.Name ?? string.Empty;
            user.ServiceId = ExtractServiceId(email);
            user.Name = User.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? string.Empty;

            if (ModelState.IsValid)
            {
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Home");
            }

            ViewData["WorkGroupId"] = new SelectList(_context.WorkGroups, "Id", "Name", user.WorkGroupId);
            return View(user);
        }
    }
}