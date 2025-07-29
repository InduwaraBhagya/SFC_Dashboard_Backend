using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Api.Models;
using SFCDashboard.Api.Services;
using Microsoft.AspNetCore.Authorization;

namespace SFCDashboard.Api.Controllers
{
    /// <summary>
    /// API Controller for Users management
    /// </summary>
    [ApiController]
    [Route("api/users")]
    [Produces("application/json")]
    public class UsersApiController : ControllerBase
    {
        private readonly ILogger<UsersApiController> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly IUsersApiService _usersApiService;

        public UsersApiController(
            ILogger<UsersApiController> logger,
            IWebHostEnvironment environment,
            IUsersApiService usersApiService)
        {
            _logger = logger;
            _environment = environment;
            _usersApiService = usersApiService;
        }

        /// <summary>
        /// Get all users
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SystemUser>>> GetUsers()
        {
            // Check authorization in production only
            if (_environment.IsProduction() && !User.Identity?.IsAuthenticated == true)
            {
                return Unauthorized();
            }

            try
            {
                var users = await _usersApiService.GetUsersAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return StatusCode(500, "An error occurred while retrieving users");
            }
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<SystemUser>> GetUser(int id)
        {
            // Check authorization in production only
            if (_environment.IsProduction() && !User.Identity?.IsAuthenticated == true)
            {
                return Unauthorized();
            }

            try
            {
                var user = await _usersApiService.GetUserWithRoleAndWorkGroupsAsync(id);
                if (user == null)
                {
                    return NotFound();
                }
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the user");
            }
        }

        /// <summary>
        /// Get user by service ID
        /// </summary>
        [HttpGet("by-serviceid/{serviceId}")]
        public async Task<ActionResult<object>> GetUserByServiceId(string serviceId)
        {
            // Check authorization in production only
            if (_environment.IsProduction() && !User.Identity?.IsAuthenticated == true)
            {
                return Unauthorized();
            }

            try
            {
                var user = await _usersApiService.GetUserByServiceIdAsync(serviceId);
                if (user == null)
                {
                    return NotFound($"User with ServiceId '{serviceId}' not found");
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by ServiceId {ServiceId}", serviceId);
                return StatusCode(500, "An error occurred while retrieving the user");
            }
        }

        /// <summary>
        /// Get user workgroups by service ID
        /// </summary>
        [HttpGet("current-user-workgroups/{serviceId}")]
        public async Task<ActionResult<IEnumerable<int>>> GetCurrentUserWorkgroups(string serviceId)
        {
            try
            {
                _logger.LogInformation("Getting workgroups for user with service ID: {serviceId}", serviceId);
                var workgroupData = await _usersApiService.GetCurrentUserWorkGroupsAsync(serviceId);
                return Ok(workgroupData.userWorkgroupIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workgroups for user with service ID: {serviceId}", serviceId);
                return StatusCode(500, "An error occurred while retrieving user workgroups");
            }
        }

        /// <summary>
        /// Check if user is in sales workgroup by service ID
        /// </summary>
        [HttpGet("is-in-sales-workgroup/{serviceId}")]
        public async Task<ActionResult<bool>> IsInSalesWorkgroup(string serviceId)
        {
            try
            {
                _logger.LogInformation("Checking if user with service ID {serviceId} is in sales workgroup", serviceId);
                var isInSales = await _usersApiService.IsUserInSalesWorkgroupAsync(serviceId);
                return Ok(isInSales);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking sales workgroup for user with service ID: {serviceId}", serviceId);
                return StatusCode(500, "An error occurred while checking sales workgroup");
            }
        }

        /// <summary>
        /// Get user ID by service ID
        /// </summary>
        [HttpGet("current-user-id/{serviceId}")]
        public async Task<ActionResult<int>> GetCurrentUserId(string serviceId)
        {
            try
            {
                _logger.LogInformation("Getting user ID for service ID: {serviceId}", serviceId);
                var userId = await _usersApiService.GetCurrentUserIdAsync(serviceId);
                return Ok(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user ID for service ID: {serviceId}", serviceId);
                return StatusCode(500, "An error occurred while retrieving user ID");
            }
        }

        /// <summary>
        /// Check if user has draw fiber access by ID
        /// </summary>
        [HttpGet("{id}/has-draw-fiber-access")]
        public async Task<ActionResult<bool>> HasDrawFiberAccess(int id)
        {
            try
            {
                _logger.LogInformation("Checking draw fiber access for user ID: {id}", id);
                var hasAccess = await _usersApiService.HasDrawFiberAccessAsync(id);
                return Ok(hasAccess);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking draw fiber access for user ID: {id}", id);
                return StatusCode(500, "An error occurred while checking draw fiber access");
            }
        }

        /// <summary>
        /// Get user workgroup by service ID (single workgroup)
        /// </summary>
        [HttpGet("current-user-workgroup/{serviceId}")]
        public async Task<ActionResult<int?>> GetCurrentUserWorkgroup(string serviceId)
        {
            try
            {
                _logger.LogInformation("Getting primary workgroup for user with service ID: {serviceId}", serviceId);
                var workgroupData = await _usersApiService.GetCurrentUserWorkGroupsAsync(serviceId);
                var primaryWorkgroupId = workgroupData.userWorkgroupIds?.FirstOrDefault();
                return Ok(primaryWorkgroupId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting primary workgroup for user with service ID: {serviceId}", serviceId);
                return StatusCode(500, "An error occurred while retrieving user workgroup");
            }
        }

        /// <summary>
        /// Create a new user
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<SystemUser>> CreateUser(SystemUser user)
        {
            // Check authorization in production only
            if (_environment.IsProduction() && !User.Identity?.IsAuthenticated == true)
            {
                return Unauthorized();
            }

            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var createdUser = await _usersApiService.CreateUserAsync(user);
                if (createdUser == null)
                    return StatusCode(500, "Failed to create user");

                return CreatedAtAction(nameof(GetUser), new { id = createdUser.Id }, createdUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, "An error occurred while creating the user");
            }
        }

        /// <summary>
        /// Update an existing user
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, SystemUser user)
        {
            // Check authorization in production only
            if (_environment.IsProduction() && !User.Identity?.IsAuthenticated == true)
            {
                return Unauthorized();
            }

            if (id != user.Id)
            {
                return BadRequest("ID mismatch");
            }

            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var updatedUser = await _usersApiService.UpdateUserAsync(user);
                if (updatedUser == null)
                    return NotFound();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {Id}", id);
                return StatusCode(500, "An error occurred while updating the user");
            }
        }

        /// <summary>
        /// Edit system user with workgroups and role
        /// </summary>
        [HttpPut("{id}/edit-system-user")]
        public async Task<IActionResult> EditSystemUser(int id, [FromBody] EditSystemUserRequest request)
        {
            // Check authorization in production only
            if (_environment.IsProduction() && !User.Identity?.IsAuthenticated == true)
            {
                return Unauthorized();
            }

            try
            {
                var success = await _usersApiService.EditSystemUserAsync(id, request.Name, request.ServiceId, request.UserRoleId, request.WorkGroupIds);
                
                if (!success)
                {
                    return NotFound($"User with ID {id} not found");
                }

                _logger.LogInformation("Successfully updated user {Id} with {WorkGroupCount} workgroups", 
                    id, request.WorkGroupIds?.Count ?? 0);

                return Ok(new { success = true, message = "User updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating system user {Id}", id);
                return StatusCode(500, "An error occurred while updating the system user");
            }
        }

        /// <summary>
        /// Delete a user
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            // Check authorization in production only
            if (_environment.IsProduction() && !User.Identity?.IsAuthenticated == true)
            {
                return Unauthorized();
            }

            try
            {
                var deleted = await _usersApiService.DeleteUserAsync(id);
                if (!deleted)
                    return NotFound();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {Id}", id);
                return StatusCode(500, "An error occurred while deleting the user");
            }
        }

        /// <summary>
        /// Set user workgroups (replace all assignments)
        /// </summary>
        [HttpPost("{userId}/set-workgroups")]
        public async Task<IActionResult> SetUserWorkGroups(int userId, [FromBody] List<int> workGroupIds)
        {
            if (_environment.IsProduction() && !User.Identity?.IsAuthenticated == true)
            {
                return Unauthorized();
            }
            try
            {
                var success = await _usersApiService.SetUserWorkGroupsAsync(userId, workGroupIds);
                
                if (!success)
                {
                    return NotFound($"User with ID {userId} not found");
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting workgroups for user {UserId}", userId);
                return StatusCode(500, "An error occurred while setting user workgroups");
            }
        }

        /// <summary>
        /// Get user layout data by service ID
        /// </summary>
        [HttpGet("layout-data/{serviceId}")]
        public async Task<ActionResult<UserLayoutDataDto>> GetUserLayoutData(string serviceId)
        {
            // Check authorization in production only
            if (_environment.IsProduction() && !User.Identity?.IsAuthenticated == true)
            {
                return Unauthorized();
            }

            try
            {
                var layoutData = await _usersApiService.GetUserLayoutDataAsync(serviceId);
                if (layoutData == null)
                {
                    return NotFound($"User layout data not found for service ID: {serviceId}");
                }

                return Ok(layoutData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user layout data for service ID {ServiceId}", serviceId);
                return StatusCode(500, "An error occurred while retrieving user layout data");
            }
        }

        /// <summary>
        /// Get project user permissions by service ID
        /// </summary>
        [HttpGet("project-permissions/{serviceId}")]
        public async Task<ActionResult<ProjectUserPermissionsDto>> GetProjectUserPermissions(string serviceId)
        {
            // Check authorization in production only
            if (_environment.IsProduction() && !User.Identity?.IsAuthenticated == true)
            {
                return Unauthorized();
            }

            try
            {
                var permissions = await _usersApiService.GetProjectUserPermissionsAsync(serviceId);
                if (permissions == null)
                {
                    return NotFound($"User permissions not found for service ID: {serviceId}");
                }

                return Ok(permissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving project user permissions for service ID {ServiceId}", serviceId);
                return StatusCode(500, "An error occurred while retrieving project user permissions");
            }
        }

        /// <summary>
        /// Get users in sales workgroup
        /// </summary>
        [HttpGet("sales-users")]
        public async Task<ActionResult<List<SystemUser>>> GetSalesUsers()
        {
            try
            {
                _logger.LogInformation("Getting sales users");
                var salesUsers = await _usersApiService.GetSalesUsersAsync();
                return Ok(salesUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sales users");
                return StatusCode(500, "An error occurred while retrieving sales users");
            }
        }
    }

    /// <summary>
    /// Request model for editing system user
    /// </summary>
    public class EditSystemUserRequest
    {
        public string Name { get; set; } = string.Empty;
        public string ServiceId { get; set; } = string.Empty;
        public int? UserRoleId { get; set; }
        public List<int> WorkGroupIds { get; set; } = new List<int>();
    }
}

