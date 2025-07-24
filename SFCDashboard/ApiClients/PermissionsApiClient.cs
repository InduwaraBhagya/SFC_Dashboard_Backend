using SFCDashboard.Models;
using System.Text;
using System.Text.Json;

namespace SFCDashboard.ApiClients
{
    public class PermissionsApiClient : IPermissionsApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PermissionsApiClient> _logger;

        public PermissionsApiClient(HttpClient httpClient, ILogger<PermissionsApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<bool> IsUserAdminAsync(string serviceId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/permissions/is-admin/{Uri.EscapeDataString(serviceId)}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return false;
                    
                response.EnsureSuccessStatusCode();
                
                var result = await response.Content.ReadAsStringAsync();
                return bool.TryParse(result, out var isAdmin) && isAdmin;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking admin status for user {ServiceId}", serviceId);
                return false; // Default to not admin on error for security
            }
        }

        public async Task<bool> HasPermissionAsync(string serviceId, string permissionName)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/permissions/has-permission/{Uri.EscapeDataString(serviceId)}/{Uri.EscapeDataString(permissionName)}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return false;
                    
                response.EnsureSuccessStatusCode();
                
                var result = await response.Content.ReadAsStringAsync();
                return bool.TryParse(result, out var hasPermission) && hasPermission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission {Permission} for user {ServiceId}", permissionName, serviceId);
                return false; // Default to no permission on error for security
            }
        }

        public async Task<List<Permission>> GetAllPermissionsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/permissions");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var permissions = JsonSerializer.Deserialize<List<Permission>>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
                
                return permissions ?? new List<Permission>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all permissions");
                return new List<Permission>();
            }
        }

        public async Task<Permission?> GetPermissionByIdAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/permissions/{id}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;
                    
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var permission = JsonSerializer.Deserialize<Permission>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
                
                return permission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permission {Id}", id);
                return null;
            }
        }

        public async Task<Permission> CreatePermissionAsync(Permission permission)
        {
            try
            {
                var json = JsonSerializer.Serialize(permission);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("api/permissions", content);
                response.EnsureSuccessStatusCode();
                
                var responseJson = await response.Content.ReadAsStringAsync();
                var createdPermission = JsonSerializer.Deserialize<Permission>(responseJson, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
                
                return createdPermission ?? permission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating permission");
                throw;
            }
        }

        public async Task<Permission> UpdatePermissionAsync(Permission permission)
        {
            try
            {
                var json = JsonSerializer.Serialize(permission);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PutAsync($"api/permissions/{permission.Id}", content);
                response.EnsureSuccessStatusCode();
                
                var responseJson = await response.Content.ReadAsStringAsync();
                var updatedPermission = JsonSerializer.Deserialize<Permission>(responseJson, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
                
                return updatedPermission ?? permission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating permission {Id}", permission.Id);
                throw;
            }
        }

        public async Task<bool> DeletePermissionAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/permissions/{id}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return false;
                    
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting permission {Id}", id);
                return false;
            }
        }

        public async Task<bool> PermissionExistsAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/permissions/{id}/exists");
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return false;
                    
                response.EnsureSuccessStatusCode();
                
                var result = await response.Content.ReadAsStringAsync();
                return bool.TryParse(result, out var exists) && exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if permission {Id} exists", id);
                return false;
            }
        }
    }
}
