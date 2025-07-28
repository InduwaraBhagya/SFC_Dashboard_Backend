using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using Microsoft.AspNetCore.Authorization;

namespace SFCDashboard.Api.Controllers
{
    /// <summary>
    /// API Controller for Permissions management
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
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
    }
}
