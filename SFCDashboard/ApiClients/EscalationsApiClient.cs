using SFCDashboard.Models;
using System.Text.Json;

namespace SFCDashboard.ApiClients
{
    public class EscalationsApiClient : IEscalationsApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public EscalationsApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        }

        public async Task<IEnumerable<Escalation>> GetAllAsync()
        {
            var response = await _httpClient.GetAsync("api/escalations");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<Escalation>>(json, _jsonOptions) ?? new List<Escalation>();
        }

        public async Task<Escalation?> GetByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/escalations/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Escalation>(json, _jsonOptions);
        }

        public async Task<Escalation> CreateAsync(Escalation escalation)
        {
            var json = JsonSerializer.Serialize(escalation, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/escalations", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Escalation>(responseJson, _jsonOptions)!;
        }

        public async Task<Escalation> UpdateAsync(Escalation escalation)
        {
            var json = JsonSerializer.Serialize(escalation, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"api/escalations/{escalation.Id}", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Escalation>(responseJson, _jsonOptions)!;
        }

        public async Task DeleteAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/escalations/{id}");
            response.EnsureSuccessStatusCode();
        }

        public async Task<IEnumerable<Escalation>> GetEscalationsByTaskIdsAsync(List<int> peTaskIds)
        {
            var idsJson = JsonSerializer.Serialize(peTaskIds, _jsonOptions);
            var content = new StringContent(idsJson, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/escalations/by-task-ids", content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<Escalation>>(json, _jsonOptions) ?? new List<Escalation>();
        }

        public async Task<List<Escalation>> GetEscalationsByUserRoleAsync(int userRoleLevel, List<string>? userWorkgroupNames = null)
        {
            var requestData = new { userRoleLevel, userWorkgroupNames };
            var json = JsonSerializer.Serialize(requestData, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/escalations/by-user-role", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Escalation>>(responseJson, _jsonOptions) ?? new List<Escalation>();
        }

        public async Task MarkAsReadAsync(int escalationId)
        {
            var response = await _httpClient.PostAsync($"api/escalations/{escalationId}/mark-read", null);
            response.EnsureSuccessStatusCode();
        }

        public async Task<string> ManualEscalationCheckAsync()
        {
            var response = await _httpClient.PostAsync("api/escalations/manual-check", null);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<Dictionary<string, string>>(json, _jsonOptions);
            return result?.GetValueOrDefault("message", "Manual escalation check completed") ?? "Manual escalation check completed";
        }

        public async Task<object> GetOLAViolatedTasksDebugInfoAsync()
        {
            var response = await _httpClient.GetAsync("api/escalations/ola-violated-tasks-debug");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<object>(json, _jsonOptions) ?? new object();
        }

        public async Task<bool> IsEscalationEnabledAsync()
        {
            var response = await _httpClient.GetAsync("api/escalations/service-status");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<Dictionary<string, bool>>(json, _jsonOptions);
            return result?.GetValueOrDefault("enabled", true) ?? true;
        }

        public async Task SetEscalationEnabledAsync(bool enabled)
        {
            var requestData = new { enabled };
            var json = JsonSerializer.Serialize(requestData, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/escalations/toggle-service", content);
            response.EnsureSuccessStatusCode();
        }
    }
}
