using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SFCDashboard.Data;
using SFCDashboard.Models;
using SFCDashboard.ApiClients;

namespace SFCDashboard.Controllers
{
    public class UserRolesController : AdminControllerBase
    {
        private readonly ILogger<UserRolesController> _logger;
        private readonly IUserRolesApiClient _userRolesApiClient;
        private readonly IRolePermissionsApiClient _rolePermissionsApiClient;

        public UserRolesController(
            ILogger<UserRolesController> logger,
            IPermissionsApiClient permissionsApi,
            IUsersApiClient usersApiClient,
            IUserRolesApiClient userRolesApiClient,
            IRolePermissionsApiClient rolePermissionsApiClient)
            : base(permissionsApi, usersApiClient)
        {
            _logger = logger;
            _userRolesApiClient = userRolesApiClient;
            _rolePermissionsApiClient = rolePermissionsApiClient;
        }

        // GET: UserRoles
        public async Task<IActionResult> Index()
        {
            var roles = await _userRolesApiClient.GetAllAsync();
            return View(roles);
        }

        // GET: UserRoles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var userRole = await _userRolesApiClient.GetWithRolePermissionsAsync(id.Value);
            if (userRole == null)
            {
                return NotFound();
            }
            return View(userRole);
        }

        // GET: UserRoles/Create
        public async Task<IActionResult> Create()
        {
            ViewData["Permissions"] = await _permissionsApi.GetAllPermissionsAsync();
            return View();
        }

        // POST: UserRoles/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Level")] UserRole userRole, int[] selectedPermissions)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Create role
                    var createdRole = await _userRolesApiClient.CreateAsync(userRole);
                    _logger.LogInformation($"Role created with ID: {createdRole.Id}");

                    if (selectedPermissions != null && selectedPermissions.Any())
                    {
                        var rolePermissions = selectedPermissions.Select(permissionId => new RolePermission
                        {
                            RoleId = createdRole.Id,
                            PermissionId = permissionId
                        });
                        await _rolePermissionsApiClient.CreateMultipleAsync(rolePermissions);
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

            ViewData["Permissions"] = await _permissionsApi.GetAllPermissionsAsync();
            return View(userRole);
        }

        // GET: UserRoles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var userRole = await _userRolesApiClient.GetWithRolePermissionsAsync(id.Value);
            if (userRole == null)
            {
                return NotFound();
            }
            ViewData["Permissions"] = await _permissionsApi.GetAllPermissionsAsync();
            ViewData["SelectedPermissions"] = userRole.RolePermissions.Select(rp => rp.PermissionId).ToList();
            return View(userRole);
        }

        // POST: UserRoles/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Level")] UserRole userRole, int[] selectedPermissions)
        {
            if (id != userRole.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update role
                    await _userRolesApiClient.UpdateAsync(userRole);
                    // Remove existing permissions
                    await _rolePermissionsApiClient.DeleteByRoleIdAsync(id);
                    // Add new permissions
                    if (selectedPermissions != null && selectedPermissions.Any())
                    {
                        var newRolePermissions = selectedPermissions.Select(pid => new RolePermission
                        {
                            RoleId = id,
                            PermissionId = pid
                        });
                        await _rolePermissionsApiClient.CreateMultipleAsync(newRolePermissions);
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    if (!await _userRolesApiClient.ExistsAsync(userRole.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewData["Permissions"] = await _permissionsApi.GetAllPermissionsAsync();
            return View(userRole);
        }

        // GET: UserRoles/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var userRole = await _userRolesApiClient.GetByIdAsync(id.Value);
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
            await _userRolesApiClient.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> UserRoleExists(int id)
        {
            return await _userRolesApiClient.ExistsAsync(id);
        }

    }
}
