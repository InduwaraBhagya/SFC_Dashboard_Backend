using SFCDashboard.Models;
using System.Text.Json;
using System.Text;

namespace SFCDashboard.ApiClients
{
    public class UsersApiClient : IUsersApiClient
    {
        public async Task SetUserWorkGroupsAsync(int userId, List<int> workGroupIds)
        {
            try
            {
                var json = JsonSerializer.Serialize(workGroupIds, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"api/users/{userId}/set-workgroups", content);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting workgroups for user {UserId}", userId);
                throw;
            }
        }
        private readonly HttpClient _httpClient;
        private readonly ILogger<UsersApiClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public UsersApiClient(HttpClient httpClient, ILogger<UsersApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<IEnumerable<SystemUser>> GetAllAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/users");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var users = JsonSerializer.Deserialize<IEnumerable<SystemUser>>(json, _jsonOptions);
                
                return users ?? new List<SystemUser>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching users from API");
                return new List<SystemUser>();
            }
        }

        public async Task<SystemUser?> GetByIdAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/users/{id}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;
                    
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<SystemUser>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user {Id} from API", id);
                return null;
            }
        }

        public async Task<SystemUser?> GetByServiceIdAsync(string serviceId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/users/by-serviceid/{Uri.EscapeDataString(serviceId)}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;
                    
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<SystemUser>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user by service ID {ServiceId} from API", serviceId);
                return null;
            }
        }

        public async Task<SystemUser> CreateAsync(SystemUser user)
        {
            try
            {
                var json = JsonSerializer.Serialize(user, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("api/users", content);
                response.EnsureSuccessStatusCode();
                
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<SystemUser>(responseJson, _jsonOptions) ?? user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user via API");
                throw;
            }
        }

        public async Task<SystemUser> UpdateAsync(SystemUser user)
        {
            try
            {
                // Create a simplified object with only the properties we want to update
                var updateRequest = new
                {
                    Id = user.Id,
                    Name = user.Name,
                    ServiceId = user.ServiceId,
                    UserRoleId = user.UserRoleId
                };
                
                var json = JsonSerializer.Serialize(updateRequest, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PutAsync($"api/users/{user.Id}", content);
                response.EnsureSuccessStatusCode();
                
                // Return the original user object since the API returns NoContent (204)
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {Id} via API", user.Id);
                throw;
            }
        }

        public async Task<ApiResult> EditSystemUserAsync(int id, EditSystemUserRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PutAsync($"api/users/{id}/edit-system-user", content);
                response.EnsureSuccessStatusCode();
                
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResult>(responseJson, _jsonOptions) ?? new ApiResult { Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing system user {Id} via API", id);
                return new ApiResult { Success = false, Message = ex.Message };
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/users/{id}");
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {Id} via API", id);
                throw;
            }
        }

        public async Task<int> GetCurrentUserIdAsync(string serviceId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/users/current-user-id/{Uri.EscapeDataString(serviceId)}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return 0;
                    
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var userId = JsonSerializer.Deserialize<int>(json, _jsonOptions);
                return userId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching current user ID for service ID {ServiceId} from API", serviceId);
                return 0;
            }
        }

        public async Task<SystemUser?> GetUserWithRoleAndWorkGroupsAsync(int userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/users/{userId}/with-role-and-workgroups");
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;
                    
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<SystemUser>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user with role and workgroups for user ID {UserId} from API", userId);
                return null;
            }
        }

        public async Task<SystemUser?> GetUserByServiceIdAsync(string serviceId)
        {
            return await GetByServiceIdAsync(serviceId);
        }

        public async Task<(List<int> userWorkgroupIds, List<string> userWorkgroupNames, bool canViewAll)> GetCurrentUserWorkGroupsAsync(string serviceId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/users/current-user-workgroups/{Uri.EscapeDataString(serviceId)}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return (new List<int>(), new List<string>(), false);
                    
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var userWorkgroupIds = JsonSerializer.Deserialize<List<int>>(json, _jsonOptions) ?? new List<int>();
                
                // For now, we'll return empty workgroup names and false for canViewAll
                // as the API only returns workgroup IDs
                return (userWorkgroupIds, new List<string>(), false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching current user workgroups for {ServiceId} from API", serviceId);
                return (new List<int>(), new List<string>(), false);
            }
        }

        public async Task<(int userWorkgroupId, string userWorkgroupName)> GetCurrentUserWorkGroupAsync(string serviceId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/users/current-user-workgroup/{Uri.EscapeDataString(serviceId)}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return (0, string.Empty);
                    
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var userWorkgroupId = JsonSerializer.Deserialize<int?>(json, _jsonOptions) ?? 0;
                
                // For now, we'll return empty workgroup name as the API only returns workgroup ID
                return (userWorkgroupId, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching current user workgroup for {ServiceId} from API", serviceId);
                return (0, string.Empty);
            }
        }

        public async Task<bool> HasMultipleWorkgroupsAsync(string serviceId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/users/has-multiple-workgroups/{Uri.EscapeDataString(serviceId)}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return false;
                    
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<Dictionary<string, bool>>(json, _jsonOptions);
                return result?.GetValueOrDefault("hasMultipleWorkgroups", false) ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking multiple workgroups for {ServiceId} from API", serviceId);
                return false;
            }
        }

        public async Task<bool> IsUserInSalesWorkgroupAsync(string serviceId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/users/is-in-sales-workgroup/{Uri.EscapeDataString(serviceId)}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return false;
                    
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<bool>(json, _jsonOptions);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking sales workgroup for {ServiceId} from API", serviceId);
                return false;
            }
        }

        public async Task<bool> HasDrawFiberAccessAsync(int userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/users/{userId}/has-draw-fiber-access");
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return false;
                    
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<bool>(json, _jsonOptions);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking draw fiber access for user {UserId} from API", userId);
                return false;
            }
        }

        public async Task<List<string>> GetUserAssignedCustomersAsync(int userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/users/{userId}/assigned-customers");
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return new List<string>();
                    
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<string>>(json, _jsonOptions) ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching assigned customers for user {UserId} from API", userId);
                return new List<string>();
            }
        }

        public async Task<(List<string> salesWorkgroups, bool canViewAll)> GetUserSalesWorkgroupsAsync(string serviceId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/users/sales-workgroups/{Uri.EscapeDataString(serviceId)}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return (new List<string>(), false);
                    
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _jsonOptions);
                
                if (result != null)
                {
                    var salesWorkgroups = JsonSerializer.Deserialize<List<string>>(result["salesWorkgroups"].ToString() ?? "[]", _jsonOptions) ?? new List<string>();
                    var canViewAll = bool.Parse(result["canViewAll"].ToString() ?? "false");
                    
                    return (salesWorkgroups, canViewAll);
                }
                
                return (new List<string>(), false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching sales workgroups for {ServiceId} from API", serviceId);
                return (new List<string>(), false);
            }
        }

        public async Task<SystemUser?> GetUserAsync(int userId)
        {
            return await GetUserWithRoleAndWorkGroupsAsync(userId);
        }

        public async Task<List<SystemUser>> GetSalesUsersAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/users/sales-users");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var users = JsonSerializer.Deserialize<List<SystemUser>>(json, _jsonOptions);
                
                return users ?? new List<SystemUser>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching sales users from API");
                return new List<SystemUser>();
            }
        }
    }
}
