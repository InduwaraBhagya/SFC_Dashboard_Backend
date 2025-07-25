using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDashboard.Models;
using SFCDashboard.Services;
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
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UsersApiController> _logger;
        private readonly IWebHostEnvironment _environment;

        public UsersApiController(
            ApplicationDbContext context,
            ILogger<UsersApiController> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
        }

        /// <summary>
        /// Get all users
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SystemUser>>> GetUsers()
        {
            // Check authorization in production only
            if (_environment.IsProduction() && !User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            try
            {
                var users = await _context.Users
                    .Include(u => u.UserRole)
                    .Include(u => u.UserWorkGroups)
                        .ThenInclude(uwg => uwg.WorkGroup)
                    .ToListAsync();
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
            if (_environment.IsProduction() && !User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id);

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
            if (_environment.IsProduction() && !User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            try
            {
                // Query and project to DTO immediately to avoid circular references
                var userDto = await _context.Users
                    .Where(u => u.ServiceId == serviceId)
                    .Select(u => new
                    {
                        u.Id,
                        u.Name,
                        u.ServiceId,
                        u.UserRoleId,
                        UserRole = u.UserRole == null ? null : new
                        {
                            u.UserRole.Id,
                            u.UserRole.Name
                        },
                        UserWorkGroups = u.UserWorkGroups.Select(uwg => new
                        {
                            uwg.Id,
                            uwg.WorkGroupId,
                            uwg.SystemUserId,
                            WorkGroup = uwg.WorkGroup == null ? null : new
                            {
                                uwg.WorkGroup.Id,
                                uwg.WorkGroup.Name
                            }
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (userDto == null)
                {
                    _logger.LogWarning("User with ServiceId '{ServiceId}' not found", serviceId);
                    return NotFound($"User with ServiceId '{serviceId}' not found");
                }
                
                return Ok(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by service ID {ServiceId}", serviceId);
                return StatusCode(500, "An error occurred while retrieving the user");
            }
        }

        /// <summary>
        /// Debug endpoint to list all users and their ServiceIds
        /// </summary>
        [HttpGet("debug/all-serviceids")]
        public async Task<ActionResult> GetAllServiceIds()
        {
            try
            {
                var users = await _context.Users
                    .Select(u => new { u.Id, u.ServiceId, u.Name })
                    .ToListAsync();
                
                return Ok(new 
                { 
                    TotalCount = users.Count,
                    Users = users,
                    Message = $"Found {users.Count} users in database"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all users");
                return StatusCode(500, "An error occurred while retrieving users");
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
                var user = await _context.Users
                    .Include(u => u.UserWorkGroups)
                    .FirstOrDefaultAsync(u => u.ServiceId == serviceId);

                if (user == null)
                {
                    return NotFound($"User with service ID {serviceId} not found");
                }

                var workgroupIds = user.UserWorkGroups.Select(uwg => uwg.WorkGroupId).ToList();
                return Ok(workgroupIds);
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
                var user = await _context.Users
                    .Include(u => u.UserWorkGroups)
                    .ThenInclude(uwg => uwg.WorkGroup)
                    .FirstOrDefaultAsync(u => u.ServiceId == serviceId);

                if (user == null)
                {
                    return NotFound($"User with service ID {serviceId} not found");
                }

                var isInSales = user.UserWorkGroups.Any(uwg => 
                    uwg.WorkGroup != null && uwg.WorkGroup.Name.ToLower().Contains("sales"));
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
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.ServiceId == serviceId);

                if (user == null)
                {
                    return NotFound($"User with service ID {serviceId} not found");
                }

                return Ok(user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user ID for service ID: {serviceId}", serviceId);
                return StatusCode(500, "An error occurred while retrieving user ID");
            }
        }

        /// <summary>
        /// Get user with role and workgroups by ID
        /// </summary>
        [HttpGet("{id}/with-role-and-workgroups")]
        public async Task<ActionResult> GetUserWithRoleAndWorkgroups(int id)
        {
            try
            {
                _logger.LogInformation("Getting user with role and workgroups for ID: {id}", id);
               
                // First, let's check what workgroups exist for this user in the database
                var userWorkgroups = await _context.UserWorkGroups
                    .Include(uwg => uwg.WorkGroup)
                    .Where(uwg => uwg.SystemUserId == id)
                    .ToListAsync();
                
                _logger.LogInformation("Direct query: User {id} has {count} workgroup records: {workgroups}", 
                    id, userWorkgroups.Count, 
                    string.Join(", ", userWorkgroups.Select(uwg => $"WG:{uwg.WorkGroupId}({uwg.WorkGroup?.Name})")));

                var user = await _context.Users
                    .Include(u => u.UserRole)
                        .ThenInclude(ur => ur.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                    .Include(u => u.UserWorkGroups)
                        .ThenInclude(uwg => uwg.WorkGroup)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    return NotFound($"User with ID {id} not found");
                }

                _logger.LogInformation("User {id} entity has {workgroupCount} workgroup assignments loaded", 
                    id, user.UserWorkGroups?.Count ?? 0);

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user with role and workgroups for ID: {id}", id);
                return StatusCode(500, "An error occurred while retrieving user details");
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
                var user = await _context.Users
                    .Include(u => u.UserRole)
                    .Include(u => u.UserWorkGroups)
                    .ThenInclude(uwg => uwg.WorkGroup)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    return NotFound($"User with ID {id} not found");
                }

                // Check if user has draw fiber access based on role or workgroup
                // This logic might need to be adjusted based on your business rules
                var hasAccess = user.UserRole?.Name?.ToLower().Contains("admin") == true ||
                               user.UserRole?.Name?.ToLower().Contains("engineer") == true ||
                               user.UserWorkGroups.Any(uwg => uwg.WorkGroup?.Name?.ToLower().Contains("fiber") == true);

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
                var user = await _context.Users
                    .Include(u => u.UserWorkGroups)
                    .FirstOrDefaultAsync(u => u.ServiceId == serviceId);

                if (user == null)
                {
                    return NotFound($"User with service ID {serviceId} not found");
                }

                var primaryWorkgroupId = user.UserWorkGroups.FirstOrDefault()?.WorkGroupId;
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
            if (_environment.IsProduction() && !User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
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
            if (_environment.IsProduction() && !User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            if (id != user.Id)
            {
                return BadRequest();
            }

            try
            {
                _context.Entry(user).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                throw;
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
            if (_environment.IsProduction() && !User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            try
            {
                var user = await _context.Users
                    .Include(u => u.UserWorkGroups)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    return NotFound($"User with ID {id} not found");
                }

                // Update basic user properties
                user.Name = request.Name;
                user.ServiceId = request.ServiceId;
                user.UserRoleId = request.UserRoleId;

                // Update workgroups - remove existing and add new ones
                _context.UserWorkGroups.RemoveRange(user.UserWorkGroups);

                if (request.WorkGroupIds != null && request.WorkGroupIds.Any())
                {
                    foreach (var workGroupId in request.WorkGroupIds.Distinct())
                    {
                        _context.UserWorkGroups.Add(new UserWorkGroup
                        {
                            SystemUserId = id,
                            WorkGroupId = workGroupId
                        });
                    }
                }

                await _context.SaveChangesAsync();

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
            if (_environment.IsProduction() && !User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {Id}", id);
                return StatusCode(500, "An error occurred while deleting the user");
            }
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }

        /// <summary>
        /// Check if the user's role has the 'Admin' permission
        /// </summary>
        [HttpGet("{id}/has-admin-permission")]
        public async Task<ActionResult<bool>> HasAdminPermission(int id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRole)
                        .ThenInclude(ur => ur.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null || user.UserRole == null)
                {
                    return NotFound();
                }

                var hasAdminPermission = user.UserRole.RolePermissions
                    .Any(rp => rp.Permission != null && rp.Permission.Name == "Admin");

                return Ok(hasAdminPermission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking admin permission for user {Id}", id);
                return StatusCode(500, "An error occurred while checking admin permission");
            }
        }

        /// <summary>
        /// Set user workgroups (replace all assignments)
        /// </summary>
        [HttpPost("{userId}/set-workgroups")]
        public async Task<IActionResult> SetUserWorkGroups(int userId, [FromBody] List<int> workGroupIds)
        {
            if (_environment.IsProduction() && !User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserWorkGroups)
                    .FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    return NotFound($"User with ID {userId} not found");
                }

                // Remove all existing workgroup assignments
                _context.UserWorkGroups.RemoveRange(user.UserWorkGroups);

                // Add new assignments
                if (workGroupIds != null && workGroupIds.Any())
                {
                    foreach (var wgId in workGroupIds.Distinct())
                    {
                _context.UserWorkGroups.Add(new UserWorkGroup
                {
                    SystemUserId = userId,
                    WorkGroupId = wgId
                });
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting workgroups for user {UserId}", userId);
                return StatusCode(500, "An error occurred while setting user workgroups");
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
