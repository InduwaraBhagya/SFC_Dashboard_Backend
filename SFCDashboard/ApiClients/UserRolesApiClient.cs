using SFCDashboard.Models;
using System.Text.Json;

namespace SFCDashboard.ApiClients
{
    public class UserRolesApiClient : IUserRolesApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public UserRolesApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        }

        public async Task<IEnumerable<UserRole>> GetAllAsync()
        {
            var response = await _httpClient.GetAsync("api/userroles");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<UserRole>>(json, _jsonOptions) ?? new List<UserRole>();
        }

        public async Task<UserRole?> GetByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/userroles/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserRole>(json, _jsonOptions);
        }

        public async Task<UserRole> CreateAsync(UserRole userRole)
        {
            var json = JsonSerializer.Serialize(userRole, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/userroles", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserRole>(responseJson, _jsonOptions)!;
        }

        public async Task<UserRole> UpdateAsync(UserRole userRole)
        {
            var json = JsonSerializer.Serialize(userRole, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"api/userroles/{userRole.Id}", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserRole>(responseJson, _jsonOptions)!;
        }

        public async Task DeleteAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/userroles/{id}");
            response.EnsureSuccessStatusCode();
        }

        public async Task<bool> ExistsAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/userroles/{id}/exists");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<bool>(json, _jsonOptions);
        }

        public async Task<UserRole?> GetWithPermissionsAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/userroles/{id}/with-permissions");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserRole>(json, _jsonOptions);
        }


        public async Task<bool> IsUserAdminAsync(string serviceId)
        {
            var response = await _httpClient.GetAsync($"api/userroles/is-admin/{serviceId}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<bool>(json, _jsonOptions);
        }

        public async Task<IEnumerable<int>> GetRolePermissionIdsAsync(int roleId)
        {
            var response = await _httpClient.GetAsync($"api/userroles/{roleId}/permission-ids");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<int>>(json, _jsonOptions) ?? new List<int>();
        }
    }
}
