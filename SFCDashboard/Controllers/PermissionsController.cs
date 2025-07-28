using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Models;
using SFCDashboard.ApiClients;

namespace SFCDashboard.Controllers
{
    public class PermissionsController : AdminControllerBase
    {
        private readonly ILogger<PermissionsController> _logger;

        public PermissionsController(ILogger<PermissionsController> logger, IPermissionsApiClient permissionsApi, IUsersApiClient usersApiClient, IRolePermissionsApiClient rolePermissionsApi) : base(permissionsApi, usersApiClient, rolePermissionsApi)
        {
            _logger = logger;
        }

        // GET: Permissions
        public async Task<IActionResult> Index()
        {
            var permissions = await _permissionsApi.GetAllPermissionsAsync();
            return View(permissions);
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
                await _permissionsApi.CreatePermissionAsync(permission);
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

            var permission = await _permissionsApi.GetPermissionByIdAsync(id.Value);
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
                    await _permissionsApi.UpdatePermissionAsync(permission);
                    TempData["SuccessMessage"] = "Permission updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    if (!await _permissionsApi.PermissionExistsAsync(permission.Id))
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

        private async Task<bool> PermissionExists(int id)
        {
            return await _permissionsApi.PermissionExistsAsync(id);
        }
    }
}