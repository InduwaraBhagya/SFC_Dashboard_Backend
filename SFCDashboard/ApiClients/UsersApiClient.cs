using SFCDashboard.Models;
using System.Text.Json;
using System.Text;

namespace SFCDashboard.ApiClients
{
    public class UsersApiClient : IUsersApiClient
    {
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
                var response = await _httpClient.GetAsync($"api/users/by-service-id/{Uri.EscapeDataString(serviceId)}");
                
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
                var json = JsonSerializer.Serialize(user, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PutAsync($"api/users/{user.Id}", content);
                response.EnsureSuccessStatusCode();
                
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<SystemUser>(responseJson, _jsonOptions) ?? user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {Id} via API", user.Id);
                throw;
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
    }
}
