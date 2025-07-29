using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Api.Data;
using SFCDashboard.Api.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SFCDashboard.Api.Controllers
{
    [ApiController]
    [Route("api/userroles")]
    [Produces("application/json")]
    public class UserRolesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserRolesApiController> _logger;

        public UserRolesApiController(ApplicationDbContext context, ILogger<UserRolesApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/userroles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserRole>>> GetUserRoles()
        {
            try
            {
                var roles = await _context.UserRoles.Include(r => r.RolePermissions).ToListAsync();
                return Ok(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all user roles");
                return StatusCode(500, "An error occurred while retrieving user roles");
            }
        }

        // GET: api/userroles/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<UserRole>> GetUserRole(int id)
        {
            try
            {
                var role = await _context.UserRoles.Include(r => r.RolePermissions).FirstOrDefaultAsync(r => r.Id == id);
                if (role == null)
                    return NotFound();
                return Ok(role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user role {Id}", id);
                return StatusCode(500, "An error occurred while retrieving user role");
            }
        }

        // POST: api/userroles
        [HttpPost]
        public async Task<ActionResult<UserRole>> CreateUserRole([FromBody] UserRole userRole)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);
                    
                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetUserRole), new { id = userRole.Id }, userRole);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user role");
                return StatusCode(500, "An error occurred while creating user role");
            }
        }

        // PUT: api/userroles/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UserRole userRole)
        {
            try
            {
                if (id != userRole.Id)
                    return BadRequest("ID mismatch");
                    
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);
                    
                _context.Entry(userRole).State = EntityState.Modified;
                
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserRoleExists(id))
                    return NotFound();
                else
                    throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user role {Id}", id);
                return StatusCode(500, "An error occurred while updating user role");
            }
        }

        // DELETE: api/userroles/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUserRole(int id)
        {
            try
            {
                var role = await _context.UserRoles.FindAsync(id);
                if (role == null)
                    return NotFound();
                    
                _context.UserRoles.Remove(role);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user role {Id}", id);
                return StatusCode(500, "An error occurred while deleting user role");
            }
        }

        // GET: api/userroles/{id}/exists
        [HttpGet("{id}/exists")]
        public async Task<ActionResult<bool>> UserRoleExistsApi(int id)
        {
            try
            {
                return await _context.UserRoles.AnyAsync(e => e.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user role {Id} exists", id);
                return StatusCode(500, "An error occurred while checking user role existence");
            }
        }

        // GET: api/userroles/{id}/with-permissions
        [HttpGet("{id}/with-permissions")]
        public async Task<ActionResult<UserRole>> GetUserRoleWithPermissions(int id)
        {
            try
            {
                var role = await _context.UserRoles.Include(r => r.RolePermissions)
                                                   .ThenInclude(rp => rp.Permission)
                                                   .FirstOrDefaultAsync(r => r.Id == id);
                if (role == null)
                    return NotFound();
                return Ok(role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user role {Id} with permissions", id);
                return StatusCode(500, "An error occurred while retrieving user role with permissions");
            }
        }

        // GET: api/userroles/is-admin/{serviceId}
        [HttpGet("is-admin/{serviceId}")]
        public async Task<ActionResult<bool>> IsUserAdmin(string serviceId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(serviceId))
                    return BadRequest("ServiceId cannot be null or empty");

                // Truncate serviceId to 6 characters if longer (following pattern from other controllers)
                serviceId = serviceId.Length > 6 ? serviceId.Substring(0, 6) : serviceId;

                var user = await _context.Users
                    .Include(u => u.UserRole)
                    .ThenInclude(r => r != null ? r.RolePermissions : null!)
                    .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(u => u.ServiceId == serviceId);

                if (user?.UserRole == null)
                    return Ok(false);

                // Check if user has Admin permission
                var hasAdminPermission = user.UserRole.RolePermissions
                    .Any(rp => rp.Permission.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase));

                return Ok(hasAdminPermission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking admin status for user {ServiceId}", serviceId);
                return StatusCode(500, "An error occurred while checking admin status");
            }
        }

        private bool UserRoleExists(int id)
        {
            return _context.UserRoles.Any(e => e.Id == id);
        }
    }
}

