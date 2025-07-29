using SFCDashboard.Models;
using System.Text.Json;

namespace SFCDashboard.ApiClients
{
    public class RolePermissionsApiClient : IRolePermissionsApiClient
    {
        public async Task<bool> HasPermissionAsync(string serviceId, string permissionName)
        {
            var response = await _httpClient.GetAsync($"api/rolepermissions/has-permission/{Uri.EscapeDataString(serviceId)}/{Uri.EscapeDataString(permissionName)}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<bool>(json, _jsonOptions);
        }

        public async Task<UserPermissionResult> GetUserPermissionsAsync(int userId)
        {
            var response = await _httpClient.GetAsync($"api/rolepermissions/user-permissions/{userId}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserPermissionResult>(json, _jsonOptions)!;
        }

        public async Task<ApiResult> UpdateUserPermissionsAsync(int userId, bool manageProjects, bool canManageEstimatedTime)
        {
            var payload = new
            {
                UserId = userId,
                ManageProjects = manageProjects,
                CanManageEstimatedTime = canManageEstimatedTime
            };
            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/rolepermissions/update-user-permissions", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResult>(responseJson, _jsonOptions)!;
        }
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public RolePermissionsApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        }

        public async Task<IEnumerable<RolePermission>> GetAllAsync()
        {
            var response = await _httpClient.GetAsync("api/rolepermissions");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<RolePermission>>(json, _jsonOptions) ?? new List<RolePermission>();
        }

        public async Task<RolePermission?> GetByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/rolepermissions/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RolePermission>(json, _jsonOptions);
        }

        public async Task<RolePermission> CreateAsync(RolePermission rolePermission)
        {
            var json = JsonSerializer.Serialize(rolePermission, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/rolepermissions", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RolePermission>(responseJson, _jsonOptions)!;
        }

        public async Task<RolePermission> UpdateAsync(RolePermission rolePermission)
        {
            var json = JsonSerializer.Serialize(rolePermission, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"api/rolepermissions/{rolePermission.Id}", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RolePermission>(responseJson, _jsonOptions)!;
        }

        public async Task DeleteAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/rolepermissions/{id}");
            response.EnsureSuccessStatusCode();
        }

        public async Task<IEnumerable<RolePermission>> GetByRoleIdAsync(int roleId)
        {
            var response = await _httpClient.GetAsync($"api/rolepermissions/by-role/{roleId}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<RolePermission>>(json, _jsonOptions) ?? new List<RolePermission>();
        }

        public async Task DeleteByRoleIdAsync(int roleId)
        {
            var response = await _httpClient.DeleteAsync($"api/rolepermissions/by-role/{roleId}");
            response.EnsureSuccessStatusCode();
        }

        public async Task CreateMultipleAsync(IEnumerable<RolePermission> rolePermissions)
        {
            var json = JsonSerializer.Serialize(rolePermissions, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/rolepermissions/multiple", content);
            response.EnsureSuccessStatusCode();
        }
    }
}
