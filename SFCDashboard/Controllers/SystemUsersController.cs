using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDashboard.Models;

namespace SFCDashboard.Controllers
{
    public class SystemUsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        public SystemUsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: SystemUsers
        public async Task<IActionResult> Index()
        {
            if (!await HttpContext.HasAdminPermissionAsync(_context))
                return RedirectToAction("Index", "PlannedEvents");

            var applicationDbContext = _context.Users
                .Include(s => s.UserRole)
                .Include(s => s.UserWorkGroups)
                    .ThenInclude(uwg => uwg.WorkGroup);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: SystemUsers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var systemUser = await _context.Users
                .Include(s => s.UserRole)
                .Include(s => s.UserWorkGroups)
                    .ThenInclude(uwg => uwg.WorkGroup)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (systemUser == null)
            {
                return NotFound();
            }

            return View(systemUser);
        }

        // GET: SystemUsers/Create
        public async Task<IActionResult> CreateAsync()
        {
            if (!await HttpContext.HasAdminPermissionAsync(_context))
                return RedirectToAction("Index", "PlannedEvents");

            ViewData["UserRoleId"] = new SelectList(_context.UserRoles, "Id", "Name");
            ViewData["WorkGroups"] = new MultiSelectList(_context.WorkGroups, "Id", "Name");
            return View(new SystemUserViewModel());
        }

        // POST: SystemUsers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SystemUserViewModel vm)
        {
            if (!await HttpContext.HasAdminPermissionAsync(_context))
                return RedirectToAction("Index", "PlannedEvents");

            if (ModelState.IsValid)
            {
                var systemUser = new SystemUser
                {
                    Name = vm.Name,
                    ServiceId = vm.ServiceId,
                    UserRoleId = vm.UserRoleId
                };

                _context.Add(systemUser);
                await _context.SaveChangesAsync();

                // Add user-workgroup relations
                if (vm.WorkGroupIds != null && vm.WorkGroupIds.Count > 0)
                {
                    foreach (var wgId in vm.WorkGroupIds)
                    {
                        _context.UserWorkGroups.Add(new UserWorkGroup
                        {
                            SystemUserId = systemUser.Id,
                            WorkGroupId = wgId
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }
            ViewData["UserRoleId"] = new SelectList(_context.UserRoles, "Id", "Name", vm.UserRoleId);
            ViewData["WorkGroups"] = new MultiSelectList(_context.WorkGroups, "Id", "Name", vm.WorkGroupIds);
            return View(vm);
        }

        // GET: SystemUsers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!await HttpContext.HasAdminPermissionAsync(_context))
                return RedirectToAction("Index", "PlannedEvents");

            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.UserWorkGroups)
                    .ThenInclude(uwg => uwg.WorkGroup)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var vm = new SystemUserViewModel
            {
                Name = user.Name,
                ServiceId = user.ServiceId,
                UserRoleId = user.UserRoleId,
                WorkGroupIds = user.UserWorkGroups?.Select(uwg => uwg.WorkGroupId).ToList() ?? new List<int>()
            };

            ViewData["UserRoleId"] = new SelectList(_context.UserRoles, "Id", "Name", user.UserRoleId);
            ViewData["WorkGroups"] = new MultiSelectList(_context.WorkGroups, "Id", "Name", vm.WorkGroupIds);

            return View(vm);
        }

        // POST: SystemUsers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SystemUserViewModel vm)
        {
            if (!await HttpContext.HasAdminPermissionAsync(_context))
                return RedirectToAction("Index", "PlannedEvents");

            var existingUser = await _context.Users
                .Include(u => u.UserWorkGroups)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (existingUser == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update properties
                    existingUser.Name = vm.Name;
                    existingUser.ServiceId = vm.ServiceId;
                    existingUser.UserRoleId = vm.UserRoleId;

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

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SystemUserExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserRoleId"] = new SelectList(_context.UserRoles, "Id", "Name", vm.UserRoleId);
            ViewData["WorkGroups"] = new MultiSelectList(_context.WorkGroups, "Id", "Name", vm.WorkGroupIds);
            return View(vm);
        }

        // GET: SystemUsers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!await HttpContext.HasAdminPermissionAsync(_context))
                return RedirectToAction("Index", "PlannedEvents");

            if (id == null)
            {
                return NotFound();
            }

            var systemUser = await _context.Users
                .Include(s => s.UserRole)
                .Include(s => s.UserWorkGroups)
                    .ThenInclude(uwg => uwg.WorkGroup)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (systemUser == null)
            {
                return NotFound();
            }

            return View(systemUser);
        }

        // POST: SystemUsers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!await HttpContext.HasAdminPermissionAsync(_context))
                return RedirectToAction("Index", "PlannedEvents");

            var systemUser = await _context.Users
                .Include(u => u.UserWorkGroups)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (systemUser != null)
            {
                // Remove user-workgroup relations first
                if (systemUser.UserWorkGroups != null)
                {
                    _context.UserWorkGroups.RemoveRange(systemUser.UserWorkGroups);
                }
                _context.Users.Remove(systemUser);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SystemUserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }

        [HttpGet]
        public JsonResult SearchWorkgroups(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new { });

            var workgroups = _context.WorkGroups
                .Where(w => w.Name.ToLower().Contains(term.ToLower()))
                .Select(w => new { id = w.Id, label = w.Name })
                .Take(20)
                .Distinct()
                .ToList();

            return Json(workgroups);
        }

        [HttpGet]
        public JsonResult GetWorkgroupsByIds(List<int> ids)
        {
            var workgroups = _context.WorkGroups
                .Where(w => ids.Contains(w.Id))
                .Select(w => new { id = w.Id, label = w.Name })
                .ToList();
            return Json(workgroups);
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var users = _context.Users.Select(u => new { id = u.Id, name = u.Name }).ToList();
            return Json(users);
        }

        [HttpGet]
        public async Task<IActionResult> GetCurrentUser()
        {
            var serviceId = User.Identity?.Name;
            if (string.IsNullOrEmpty(serviceId))
                return Json(null);

            var serviceIdShort = serviceId.Length > 6 ? serviceId.Substring(0, 6) : serviceId;

            var user = await _context.Users
                .Where(u => u.ServiceId == serviceIdShort)
                .Select(u => new { id = u.Id, name = u.Name })
                .FirstOrDefaultAsync();

            return Json(user);
        }
    }
}