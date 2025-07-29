using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDashboard.Models;
using Microsoft.AspNetCore.Authorization;

namespace SFCDashboard.Api.Controllers
{
    /// <summary>
    /// API Controller for Permissions management
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    // [Authorize] // Temporarily disabled for testing
    public class PermissionsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PermissionsApiController> _logger;

        public PermissionsApiController(
            ApplicationDbContext context,
            ILogger<PermissionsApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Check if a user has admin privileges
        /// </summary>
        [HttpGet("is-admin/{serviceId}")]
        public async Task<ActionResult<bool>> IsUserAdmin(string serviceId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRole)
                    .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(u => u.ServiceId == serviceId);

                if (user == null)
                {
                    return NotFound();
                }

                // Check if user has admin role (Level 3) or has admin permission
                var isAdmin = user.UserRole?.Level == 3 || 
                             user.UserRole?.RolePermissions?.Any(rp => 
                                 rp.Permission.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase)) == true;

                return Ok(isAdmin);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking admin status for user {ServiceId}", serviceId);
                return StatusCode(500, "An error occurred while checking admin status");
            }
        }

        /// <summary>
        /// Check if a user has a specific permission
        /// </summary>
        [HttpGet("has-permission/{serviceId}/{permissionName}")]
        public async Task<ActionResult<bool>> HasPermission(string serviceId, string permissionName)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRole)
                    .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(u => u.ServiceId == serviceId);

                if (user == null)
                {
                    return NotFound();
                }

                // Check if user has the specific permission
                var hasPermission = user.UserRole?.RolePermissions?.Any(rp => 
                    rp.Permission.Name.Equals(permissionName, StringComparison.OrdinalIgnoreCase)) == true;

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
                var permissions = await _context.Permissions.ToListAsync();
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
                var permission = await _context.Permissions.FindAsync(id);
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
                _context.Permissions.Add(permission);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetPermissionById), new { id = permission.Id }, permission);
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

                _context.Entry(permission).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return Ok(permission);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Permissions.AnyAsync(p => p.Id == id))
                {
                    return NotFound();
                }
                throw;
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
                var permission = await _context.Permissions.FindAsync(id);
                if (permission == null)
                {
                    return NotFound();
                }

                _context.Permissions.Remove(permission);
                await _context.SaveChangesAsync();
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
                var exists = await _context.Permissions.AnyAsync(p => p.Id == id);
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
