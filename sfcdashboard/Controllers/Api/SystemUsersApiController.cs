using Microsoft.AspNetCore.Mvc;
using SFCDashboard.ApiClients;
using SFCDashboard.Models;

namespace SFCDashboard.Controllers.Api
{
    [Route("api/system-users")]
    public class SystemUsersApiController : ApiBaseController
    {
        private readonly IUsersApiClient _usersApi;
        private readonly IUserRolesApiClient _userRolesApi;
        private readonly IAreaNetworkEngineersApiClient _aneApi;
        private readonly ILogger<SystemUsersApiController> _logger;

        public SystemUsersApiController(
            IUsersApiClient usersApi,
            IUserRolesApiClient userRolesApi,
            IAreaNetworkEngineersApiClient aneApi,
            ILogger<SystemUsersApiController> logger)
        {
            _usersApi = usersApi;
            _userRolesApi = userRolesApi;
            _aneApi = aneApi;
            _logger = logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            try
            {
                var serviceId = GetCurrentServiceId();
                if (string.IsNullOrEmpty(serviceId))
                {
                    return ApiError("User not authenticated", 401);
                }

                // Create the user model
                var user = new SystemUser
                {
                    ServiceId = request.ServiceId,
                    Name = request.Name,
                    UserRoleId = request.UserRoleId
                };

                var createdUser = await _usersApi.CreateAsync(user);
                return ApiResponse(createdUser, "User created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return ApiException(ex);
            }
        }

        [HttpPut("{userId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUser(int userId, [FromBody] UpdateUserRequest request)
        {
            try
            {
                var serviceId = GetCurrentServiceId();
                if (string.IsNullOrEmpty(serviceId))
                {
                    return ApiError("User not authenticated", 401);
                }

                var existingUser = await _usersApi.GetByIdAsync(userId);
                if (existingUser == null)
                {
                    return ApiError("User not found", 404);
                }

                existingUser.Name = request.Name ?? existingUser.Name;
                if (request.UserRoleId.HasValue)
                    existingUser.UserRoleId = request.UserRoleId.Value;

                var updatedUser = await _usersApi.UpdateAsync(existingUser);
                return ApiResponse(updatedUser, "User updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", userId);
                return ApiException(ex);
            }
        }

        [HttpDelete("{userId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            try
            {
                var serviceId = GetCurrentServiceId();
                if (string.IsNullOrEmpty(serviceId))
                {
                    return ApiError("User not authenticated", 401);
                }

                await _usersApi.DeleteAsync(userId);
                return ApiResponse(new { }, "User deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", userId);
                return ApiException(ex);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _usersApi.GetAllAsync();
                return ApiResponse(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users");
                return ApiException(ex);
            }
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUser(int userId)
        {
            try
            {
                var user = await _usersApi.GetByIdAsync(userId);
                if (user == null)
                {
                    return ApiError("User not found", 404);
                }

                return ApiResponse(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {UserId}", userId);
                return ApiException(ex);
            }
        }

        [HttpGet("by-service/{serviceId}")]
        public async Task<IActionResult> GetUserByServiceId(string serviceId)
        {
            try
            {
                var user = await _usersApi.GetByServiceIdAsync(serviceId);
                if (user == null)
                {
                    return ApiError("User not found", 404);
                }

                return ApiResponse(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by service ID {ServiceId}", serviceId);
                return ApiException(ex);
            }
        }

        [HttpGet("anes")]
        public async Task<IActionResult> GetAreaNetworkEngineers()
        {
            try
            {
                var anes = await _aneApi.GetAllAsync();
                return ApiResponse(anes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting area network engineers");
                return ApiException(ex);
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string query, [FromQuery] int limit = 50)
        {
            try
            {
                if (string.IsNullOrEmpty(query))
                {
                    return ApiError("Search query is required", 400);
                }

                // This would need to be implemented in the API client
                // For now, get all users and filter (not efficient for large datasets)
                var allUsers = await _usersApi.GetAllAsync();
                var filteredUsers = allUsers
                    .Where(u => u.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                               u.ServiceId.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .Take(limit)
                    .ToList();

                return ApiResponse(filteredUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users with query {Query}", query);
                return ApiException(ex);
            }
        }

        [HttpGet("{userId}/roles")]
        public async Task<IActionResult> GetUserRoles(int userId)
        {
            try
            {
                // Get user to check if it exists and get the role
                var user = await _usersApi.GetByIdAsync(userId);
                if (user == null)
                {
                    return ApiError("User not found", 404);
                }

                if (user.UserRoleId.HasValue)
                {
                    var userRole = await _userRolesApi.GetByIdAsync(user.UserRoleId.Value);
                    return ApiResponse(userRole);
                }
                else
                {
                    return ApiResponse(new { });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roles for user {UserId}", userId);
                return ApiException(ex);
            }
        }
    }

    public class CreateUserRequest
    {
        public string ServiceId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int? UserRoleId { get; set; }
    }

    public class UpdateUserRequest
    {
        public string? Name { get; set; }
        public int? UserRoleId { get; set; }
    }
}
