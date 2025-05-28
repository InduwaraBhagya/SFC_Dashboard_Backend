using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SFCDashboard.Data;
using SFCDashboard.Models;

namespace SFCDashboard.Controllers
{
    public class UserRolesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserRolesController> _logger;

        public UserRolesController(ApplicationDbContext context, ILogger<UserRolesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: UserRoles
        public async Task<IActionResult> Index()
        {
            return View(await _context.UserRoles.ToListAsync());
        }

        // GET: UserRoles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(m => m.Id == id);
            if (userRole == null)
            {
                return NotFound();
            }

            return View(userRole);
        }

        // GET: UserRoles/Create
        public async Task<IActionResult> Create()
        {
            ViewData["Permissions"] = await _context.Permissions.ToListAsync();
            return View();
        }

        // POST: UserRoles/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name")] UserRole userRole, int[] selectedPermissions)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.UserRoles.Add(userRole);  // Changed from UserRole to UserRoles
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation($"Role created with ID: {userRole.Id}");

                    if (selectedPermissions != null && selectedPermissions.Any())
                    {
                        foreach (var permissionId in selectedPermissions)
                        {
                            var rolePermission = new RolePermission
                            {
                                RoleId = userRole.Id,
                                PermissionId = permissionId
                            };
                            _context.RolePermissions.Add(rolePermission);
                        }
                        await _context.SaveChangesAsync();
                    }

                    TempData["SuccessMessage"] = "Role created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error creating role: {ex.Message}");
                    ModelState.AddModelError("", "Error creating role: " + ex.Message);
                }
            }

            ViewData["Permissions"] = await _context.Permissions.ToListAsync();
            return View(userRole);
        }

        // GET: UserRoles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userRole = await _context.UserRoles
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (userRole == null)
            {
                return NotFound();
            }

            ViewData["Permissions"] = await _context.Permissions.ToListAsync();
            ViewData["SelectedPermissions"] = userRole.RolePermissions.Select(rp => rp.PermissionId).ToList();
            return View(userRole);
        }

        // POST: UserRoles/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] UserRole userRole, int[] selectedPermissions)
        {
            if (id != userRole.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Get existing role with permissions
                    var existingRole = await _context.UserRoles
                        .Include(r => r.RolePermissions)
                        .FirstOrDefaultAsync(r => r.Id == id);

                    if (existingRole == null)
                    {
                        return NotFound();
                    }

                    // Update basic properties
                    existingRole.Name = userRole.Name;

                    // Remove existing permissions
                    _context.RolePermissions.RemoveRange(existingRole.RolePermissions);

                    // Add new permissions
                    if (selectedPermissions != null)
                    {
                        foreach (var permissionId in selectedPermissions)
                        {
                            existingRole.RolePermissions.Add(new RolePermission
                            {
                                RoleId = id,
                                PermissionId = permissionId
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserRoleExists(userRole.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewData["Permissions"] = await _context.Permissions.ToListAsync();
            return View(userRole);
        }

        // GET: UserRoles/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(m => m.Id == id);
            if (userRole == null)
            {
                return NotFound();
            }

            return View(userRole);
        }

        // POST: UserRoles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userRole = await _context.UserRoles.FindAsync(id);
            if (userRole != null)
            {
                _context.UserRoles.Remove(userRole);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserRoleExists(int id)
        {
            return _context.UserRoles.Any(e => e.Id == id);
        }

    }
}
