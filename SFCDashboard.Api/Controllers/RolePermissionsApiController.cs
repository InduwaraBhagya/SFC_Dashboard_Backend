using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Api.Services;

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
    }
}
