using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDashboard.Models;
using SFCDashboard.ApiClients;

namespace SFCDashboard.Controllers
{
    public class PermissionsController : AdminControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PermissionsController> _logger;

        public PermissionsController(ApplicationDbContext context, ILogger<PermissionsController> logger, IPermissionsApiClient permissionsApi, IUsersApiClient usersApiClient) : base(permissionsApi, usersApiClient)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Permissions
        public async Task<IActionResult> Index()
        {
            return View(await _context.Permissions.ToListAsync());
        }

        // GET: Permissions/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description")] Permission permission)
        {
            if (ModelState.IsValid)
            {
                _context.Add(permission);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Permission created successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(permission);
        }

        // GET: Permissions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var permission = await _context.Permissions.FindAsync(id);
            if (permission == null)
            {
                return NotFound();
            }
            return View(permission);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description")] Permission permission)
        {
            if (id != permission.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(permission);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Permission updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PermissionExists(permission.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(permission);
        }

        private bool PermissionExists(int id)
        {
            return _context.Permissions.Any(e => e.Id == id);
        }
    }
}