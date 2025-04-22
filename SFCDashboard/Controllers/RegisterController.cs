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

        public IActionResult Index()
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            var serviceId = User.GetNameIdentifierId();
            var name = User.Identity?.Name ?? string.Empty;

            ViewData["UserRoleId"] = new SelectList(_context.UserRole, "Id", "Name");
            ViewData["WorkGroupId"] = new SelectList(_context.WorkGroups, "Id", "Name");

            var model = new SystemUser
            {
                Name = name ?? "",
                ServiceId = serviceId ?? ""
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index([Bind("UserRoleId,WorkGroupId")] SystemUser user)
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            // Set the name and service ID from Azure AD
            user.Name = User.Identity?.Name ?? "";
            user.ServiceId = User.GetNameIdentifierId() ?? "";

            if (ModelState.IsValid)
            {
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Home");
            }

            ViewData["UserRoleId"] = new SelectList(_context.UserRole, "Id", "Name", user.UserRoleId);
            ViewData["WorkGroupId"] = new SelectList(_context.WorkGroups, "Id", "Name", user.WorkGroupId);
            return View(user);
        }
    }
}