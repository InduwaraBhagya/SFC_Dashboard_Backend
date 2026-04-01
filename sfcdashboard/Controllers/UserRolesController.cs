using Microsoft.AspNetCore.Mvc;
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
            : base(permissionsApi, usersApiClient, rolePermissionsApiClient)
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
            var userRole = await _userRolesApiClient.GetWithPermissionsAsync(id.Value);
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
            var userRole = await _userRolesApiClient.GetWithPermissionsAsync(id.Value);
            if (userRole == null)
            {
                return NotFound();
            }
            ViewData["Permissions"] = await _permissionsApi.GetAllPermissionsAsync();
            ViewData["SelectedPermissions"] = await _userRolesApiClient.GetRolePermissionIdsAsync(id.Value);
            return View(userRole);
        }

        // POST: UserRoles/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Level")] UserRole userRole, int[] selectedPermissions)
        {
            _logger.LogInformation("Edit POST called with ID: {Id}, UserRole: {@UserRole}", id, userRole);
            
            if (id != userRole.Id)
            {
                _logger.LogWarning("ID mismatch: URL ID {UrlId} != UserRole ID {UserRoleId}", id, userRole.Id);
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _logger.LogInformation("Attempting to update role {Id} with name '{Name}' and level {Level}", 
                        userRole.Id, userRole.Name, userRole.Level);
                    
                    // Update role
                    var updatedRole = await _userRolesApiClient.UpdateAsync(userRole);
                    _logger.LogInformation("Successfully updated role {Id}", userRole.Id);
                    
                    // Remove existing permissions
                    _logger.LogInformation("Removing existing permissions for role {Id}", id);
                    await _rolePermissionsApiClient.DeleteByRoleIdAsync(id);
                    
                    // Add new permissions
                    if (selectedPermissions != null && selectedPermissions.Any())
                    {
                        _logger.LogInformation("Adding {Count} new permissions for role {Id}: [{Permissions}]", 
                            selectedPermissions.Length, id, string.Join(", ", selectedPermissions));
                        
                        var newRolePermissions = selectedPermissions.Select(pid => new RolePermission
                        {
                            RoleId = id,
                            PermissionId = pid
                        });
                        await _rolePermissionsApiClient.CreateMultipleAsync(newRolePermissions);
                    }
                    else
                    {
                        _logger.LogInformation("No permissions selected for role {Id}", id);
                    }
                    
                    TempData["SuccessMessage"] = "Role updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating role {Id}: {Message}", id, ex.Message);
                    
                    // Check if role still exists
                    try
                    {
                        var exists = await _userRolesApiClient.ExistsAsync(userRole.Id);
                        _logger.LogInformation("Role {Id} exists check: {Exists}", userRole.Id, exists);
                        
                        if (!exists)
                        {
                            return NotFound();
                        }
                    }
                    catch (Exception checkEx)
                    {
                        _logger.LogError(checkEx, "Error checking if role {Id} exists", userRole.Id);
                    }
                    
                    ModelState.AddModelError("", "Error updating role: " + ex.Message);
                }
            }
            else
            {
                _logger.LogWarning("ModelState is invalid for role {Id}. Errors: {Errors}", 
                    userRole.Id, 
                    string.Join("; ", ModelState.SelectMany(x => x.Value.Errors).Select(e => e.ErrorMessage)));
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
            try
            {
                // Check if role exists before attempting deletion
                if (!await _userRolesApiClient.ExistsAsync(id))
                {
                    TempData["ErrorMessage"] = "Role not found.";
                    return RedirectToAction(nameof(Index));
                }

                await _userRolesApiClient.DeleteAsync(id);
                TempData["SuccessMessage"] = "Role deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting role: {ex.Message}");
                TempData["ErrorMessage"] = "Error deleting role: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

    }
}
