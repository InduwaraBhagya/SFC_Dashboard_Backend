using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Api.Models;
using SFCDashboard.Api.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SFCDashboard.Api.Controllers
{
    [ApiController]
    [Route("api/userroles")]
    [Produces("application/json")]
    public class UserRolesApiController : ControllerBase
    {
        private readonly IUserRolesApiService _userRolesService;
        private readonly ILogger<UserRolesApiController> _logger;

        public UserRolesApiController(IUserRolesApiService userRolesService, ILogger<UserRolesApiController> logger)
        {
            _userRolesService = userRolesService;
            _logger = logger;
        }

        // GET: api/userroles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserRole>>> GetUserRoles()
        {
            try
            {
                var roles = await _userRolesService.GetUserRolesAsync();
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
                var role = await _userRolesService.GetUserRoleAsync(id);
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
                    
                var createdRole = await _userRolesService.CreateUserRoleAsync(userRole);
                if (createdRole == null)
                    return StatusCode(500, "Failed to create user role");
                    
                return CreatedAtAction(nameof(GetUserRole), new { id = createdRole.Id }, createdRole);
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

                // Check if the role exists
                if (!await _userRolesService.UserRoleExistsAsync(id))
                    return NotFound();
                    
                var updatedRole = await _userRolesService.UpdateUserRoleAsync(userRole);
                if (updatedRole == null)
                    return StatusCode(500, "Failed to update user role");
                    
                return NoContent();
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
                var deleted = await _userRolesService.DeleteUserRoleAsync(id);
                if (!deleted)
                    return NotFound();
                    
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
                return await _userRolesService.UserRoleExistsAsync(id);
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
                var role = await _userRolesService.GetUserRoleWithPermissionsAsync(id);
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

                var isAdmin = await _userRolesService.IsUserAdminAsync(serviceId);
                return Ok(isAdmin);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking admin status for user {ServiceId}", serviceId);
                return StatusCode(500, "An error occurred while checking admin status");
            }
        }

    }
}

