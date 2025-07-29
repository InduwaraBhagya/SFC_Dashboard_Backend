using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Api.Models;
using SFCDashboard.Api.Services;
using Microsoft.AspNetCore.Authorization;

namespace SFCDashboard.Api.Controllers
{
    /// <summary>
    /// API Controller for Permissions management
    /// </summary>
    [ApiController]
    [Route("api/permissions")]
    [Produces("application/json")]
    // [Authorize] // Temporarily disabled for testing
    public class PermissionsApiController : ControllerBase
    {
        private readonly IPermissionsApiService _permissionsService;
        private readonly ILogger<PermissionsApiController> _logger;

        public PermissionsApiController(
            IPermissionsApiService permissionsService,
            ILogger<PermissionsApiController> logger)
        {
            _permissionsService = permissionsService;
            _logger = logger;
        }

        /// <summary>
        /// Check if a user has a specific permission
        /// </summary>
        [HttpGet("has-permission/{serviceId}/{permissionName}")]
        public async Task<ActionResult<bool>> HasPermission(string serviceId, string permissionName)
        {
            if (string.IsNullOrWhiteSpace(serviceId))
            {
                return BadRequest("ServiceId cannot be null or empty");
            }

            if (string.IsNullOrWhiteSpace(permissionName))
            {
                return BadRequest("PermissionName cannot be null or empty");
            }

            try
            {
                var hasPermission = await _permissionsService.HasPermissionAsync(serviceId, permissionName);
                return Ok(hasPermission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission {Permission} for user {ServiceId}", permissionName, serviceId);
                return StatusCode(500, "An error occurred while checking permission");
            }
        }

        /// <summary>
        /// Get all permissions
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<Permission>>> GetAllPermissions()
        {
            try
            {
                var permissions = await _permissionsService.GetAllPermissionsAsync();
                return Ok(permissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all permissions");
                return StatusCode(500, "An error occurred while getting permissions");
            }
        }

        /// <summary>
        /// Get permission by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Permission>> GetPermissionById(int id)
        {
            try
            {
                var permission = await _permissionsService.GetPermissionByIdAsync(id);
                if (permission == null)
                {
                    return NotFound();
                }
                return Ok(permission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permission {Id}", id);
                return StatusCode(500, "An error occurred while getting permission");
            }
        }

        /// <summary>
        /// Create a new permission
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Permission>> CreatePermission([FromBody] Permission permission)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var createdPermission = await _permissionsService.CreatePermissionAsync(permission);
                if (createdPermission == null)
                    return StatusCode(500, "Failed to create permission");

                return CreatedAtAction(nameof(GetPermissionById), new { id = createdPermission.Id }, createdPermission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating permission");
                return StatusCode(500, "An error occurred while creating permission");
            }
        }

        /// <summary>
        /// Update an existing permission
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<Permission>> UpdatePermission(int id, [FromBody] Permission permission)
        {
            try
            {
                if (id != permission.Id)
                {
                    return BadRequest("ID mismatch");
                }

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Check if permission exists
                if (!await _permissionsService.PermissionExistsAsync(id))
                    return NotFound();

                var updatedPermission = await _permissionsService.UpdatePermissionAsync(permission);
                if (updatedPermission == null)
                    return StatusCode(500, "Failed to update permission");

                return Ok(updatedPermission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating permission {Id}", id);
                return StatusCode(500, "An error occurred while updating permission");
            }
        }

        /// <summary>
        /// Delete a permission
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeletePermission(int id)
        {
            try
            {
                var deleted = await _permissionsService.DeletePermissionAsync(id);
                if (!deleted)
                    return NotFound();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting permission {Id}", id);
                return StatusCode(500, "An error occurred while deleting permission");
            }
        }

        /// <summary>
        /// Check if permission exists
        /// </summary>
        [HttpGet("{id}/exists")]
        public async Task<ActionResult<bool>> PermissionExists(int id)
        {
            try
            {
                var exists = await _permissionsService.PermissionExistsAsync(id);
                return Ok(exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if permission {Id} exists", id);
                return StatusCode(500, "An error occurred while checking permission existence");
            }
        }
    }
}

