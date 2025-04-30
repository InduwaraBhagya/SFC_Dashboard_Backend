using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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

        public async Task<IActionResult> Index()
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "PlannedEvents");
            }

            var email = User.Identity?.Name ?? string.Empty;
            var serviceId = ExtractServiceId(email);

            // Get existing user details from database
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.ServiceId == serviceId);

            if (existingUser == null)
            {
                // If user doesn't exist, create new user object
                var name = User.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? string.Empty;
                existingUser = new SystemUser
                {
                    Name = name,
                    ServiceId = serviceId
                };
            }

            ViewData["WorkGroupId"] = new SelectList(_context.WorkGroups, "Id", "Name");
            return View(existingUser);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(SystemUser user)
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "PlannedEvents");
            }

            var email = User.Identity?.Name ?? string.Empty;
            var serviceId = ExtractServiceId(email);

            // Get existing user
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.ServiceId == serviceId);

            if (existingUser != null)
            {
                // Update existing user's WorkGroup
                existingUser.WorkGroupId = user.WorkGroupId;
                _context.Update(existingUser);
            }
            else
            {
                // Create new user if doesn't exist
                user.ServiceId = serviceId;
                user.Name = User.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? string.Empty;
                _context.Add(user);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Index", "PlannedEvents");
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Unable to save changes. Please try again.");
                }
            }

            ViewData["WorkGroupId"] = new SelectList(_context.WorkGroups, "Id", "Name", user.WorkGroupId);
            return View(user);
        }
    }
}