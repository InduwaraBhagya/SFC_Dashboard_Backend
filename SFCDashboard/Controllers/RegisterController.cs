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

        // GET
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
                .Include(u => u.UserWorkGroups)
                .FirstOrDefaultAsync(u => u.ServiceId == serviceId);

            if (existingUser == null)
            {
                // If user doesn't exist, create new user object
                var name = User.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? string.Empty;
                existingUser = new SystemUser
                {
                    Name = name,
                    ServiceId = serviceId,
                    UserWorkGroups = new List<UserWorkGroup>()
                };
            }

            var vm = new SystemUserViewModel
            {
                Name = existingUser?.Name ?? "",
                ServiceId = existingUser?.ServiceId ?? "",
                WorkGroupIds = existingUser?.UserWorkGroups?.Select(uwg => uwg.WorkGroupId).ToList() ?? new List<int>()
            };
            ViewData["WorkGroupIds"] = new MultiSelectList(_context.WorkGroups, "Id", "Name", vm.WorkGroupIds);
            return View(vm);
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(SystemUserViewModel vm)
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "PlannedEvents");
            }

            var email = User.Identity?.Name ?? string.Empty;
            var serviceId = ExtractServiceId(email);

            // Get existing user
            var existingUser = await _context.Users
                .Include(u => u.UserWorkGroups)
                .FirstOrDefaultAsync(u => u.ServiceId == serviceId);

            if (existingUser != null)
            {
                // Update existing user's name
                existingUser.Name = vm.Name;

                // Update user-workgroup relations
                var existingWgIds = existingUser.UserWorkGroups?.Select(uwg => uwg.WorkGroupId).ToList() ?? new List<int>();

                // Remove old relations
                var toRemove = existingUser.UserWorkGroups?.Where(uwg => !vm.WorkGroupIds.Contains(uwg.WorkGroupId)).ToList() ?? new List<UserWorkGroup>();
                _context.UserWorkGroups.RemoveRange(toRemove);

                // Add new relations
                var toAdd = vm.WorkGroupIds.Where(wgId => !existingWgIds.Contains(wgId)).ToList();
                foreach (var wgId in toAdd)
                {
                    _context.UserWorkGroups.Add(new UserWorkGroup
                    {
                        SystemUserId = existingUser.Id,
                        WorkGroupId = wgId
                    });
                }

                _context.Update(existingUser);
            }
            else
            {
                // Create new user if doesn't exist
                var user = new SystemUser
                {
                    ServiceId = serviceId,
                    Name = User.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? string.Empty,
                    UserWorkGroups = new List<UserWorkGroup>()
                };
                foreach (var wgId in vm.WorkGroupIds)
                {
                    user.UserWorkGroups.Add(new UserWorkGroup
                    {
                        WorkGroupId = wgId
                    });
                }
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

            ViewData["WorkGroupIds"] = new MultiSelectList(_context.WorkGroups, "Id", "Name", vm.WorkGroupIds);
            return View(vm);
        }
    }
}