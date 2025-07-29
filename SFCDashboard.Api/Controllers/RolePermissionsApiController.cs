using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Api.Services;
using SFCDashboard.Api.Models;

namespace SFCDashboard.Api.Controllers
{
    [ApiController]
    [Route("api/rolepermissions")]
    public class RolePermissionsApiController : ControllerBase
    {
        private readonly IRolePermissionsApiService _rolePermissionsService;
        private readonly ILogger<RolePermissionsApiController> _logger;

        public RolePermissionsApiController(
            IRolePermissionsApiService rolePermissionsService, 
            ILogger<RolePermissionsApiController> logger)
        {
            _rolePermissionsService = rolePermissionsService;
            _logger = logger;
        }

        // GET: api/rolepermissions/has-permission/{serviceId}/{permissionName}
        [HttpGet("has-permission/{serviceId}/{permissionName}")]
        public async Task<IActionResult> HasPermission(string serviceId, string permissionName)
        {
            if (string.IsNullOrEmpty(serviceId))
                return BadRequest("ServiceId is required");

            var hasPermission = await _rolePermissionsService.HasPermissionAsync(serviceId, permissionName);
            return Ok(hasPermission);
        }

        // GET: api/rolepermissions/user-permissions/{userId}
        [HttpGet("user-permissions/{userId}")]
        public async Task<IActionResult> GetUserPermissions(int userId)
        {
            var result = await _rolePermissionsService.GetUserPermissionsAsync(userId);
            
            if (result is object obj && obj.GetType().GetProperty("error") != null)
            {
                return NotFound(result);
            }
            
            return Ok(result);
        }

        // POST: api/rolepermissions/update-user-permissions
        [HttpPost("update-user-permissions")]
        public async Task<IActionResult> UpdateUserPermissions([FromBody] UpdateUserPermissionsRequest request)
        {
            var serviceId = User.Identity?.Name;
            if (string.IsNullOrEmpty(serviceId))
                return Unauthorized();

            var canManage = await _rolePermissionsService.CanUserManagePermissions(serviceId);
            if (!canManage)
                return Forbid();

            var success = await _rolePermissionsService.UpdateUserPermissionsAsync(request);
            
            if (success)
                return Ok(new { success = true, message = "Permissions updated successfully" });
            else
                return BadRequest(new { success = false, message = "Failed to update permissions" });
        }

        // GET: api/rolepermissions/by-role/{roleId}
        [HttpGet("by-role/{roleId}")]
        public async Task<IActionResult> GetByRoleId(int roleId)
        {
            try
            {
                var rolePermissions = await _rolePermissionsService.GetByRoleIdAsync(roleId);
                return Ok(rolePermissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting role permissions for role {RoleId}", roleId);
                return StatusCode(500, "An error occurred while retrieving role permissions");
            }
        }

        // DELETE: api/rolepermissions/by-role/{roleId}
        [HttpDelete("by-role/{roleId}")]
        public async Task<IActionResult> DeleteByRoleId(int roleId)
        {
            try
            {
                var success = await _rolePermissionsService.DeleteByRoleIdAsync(roleId);
                if (success)
                    return NoContent();
                else
                    return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting role permissions for role {RoleId}", roleId);
                return StatusCode(500, "An error occurred while deleting role permissions");
            }
        }

        // POST: api/rolepermissions/multiple
        [HttpPost("multiple")]
        public async Task<IActionResult> CreateMultiple([FromBody] IEnumerable<CreateRolePermissionRequest> rolePermissionRequests)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Convert DTOs to entities
                var rolePermissions = rolePermissionRequests.Select(rpr => new RolePermission
                {
                    RoleId = rpr.RoleId,
                    PermissionId = rpr.PermissionId
                });

                var success = await _rolePermissionsService.CreateMultipleAsync(rolePermissions);
                if (success)
                    return Ok();
                else
                    return StatusCode(500, "Failed to create role permissions");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating multiple role permissions");
                return StatusCode(500, "An error occurred while creating role permissions");
            }
        }
    }
}
