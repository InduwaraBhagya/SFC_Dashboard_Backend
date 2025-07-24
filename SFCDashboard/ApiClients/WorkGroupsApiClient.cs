using SFCDashboard.Models;
using System.Text.Json;

namespace SFCDashboard.ApiClients
{
    public class WorkGroupsApiClient : IWorkGroupsApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public WorkGroupsApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        }

        public async Task<IEnumerable<WorkGroup>> GetAllAsync()
        {
            var response = await _httpClient.GetAsync("api/workgroups");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<WorkGroup>>(json, _jsonOptions) ?? new List<WorkGroup>();
        }

        public async Task<WorkGroup?> GetByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/workgroups/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<WorkGroup>(json, _jsonOptions);
        }

        public async Task<WorkGroup> CreateAsync(WorkGroup workGroup)
        {
            var json = JsonSerializer.Serialize(workGroup, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/workgroups", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<WorkGroup>(responseJson, _jsonOptions)!;
        }

        public async Task<WorkGroup> UpdateAsync(WorkGroup workGroup)
        {
            var json = JsonSerializer.Serialize(workGroup, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"api/workgroups/{workGroup.Id}", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<WorkGroup>(responseJson, _jsonOptions)!;
        }

        public async Task DeleteAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/workgroups/{id}");
            response.EnsureSuccessStatusCode();
        }

        public async Task<IEnumerable<WorkGroup>> GetWorkGroupsForUserAsync(List<int> userWorkgroupIds, bool canViewAll)
        {
            var idsJson = JsonSerializer.Serialize(new { userWorkgroupIds, canViewAll }, _jsonOptions);
            var content = new StringContent(idsJson, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/workgroups/for-user", content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<WorkGroup>>(json, _jsonOptions) ?? new List<WorkGroup>();
        }

        public async Task<string?> GetWorkGroupNameAsync(int workgroupId)
        {
            var response = await _httpClient.GetAsync($"api/workgroups/{workgroupId}/name");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<Dictionary<string, string>>(json, _jsonOptions);
            return result?.GetValueOrDefault("name");
        }

        public async Task<IEnumerable<WorkGroup>> GetWorkGroupsByIdsAsync(List<int> workgroupIds)
        {
            try
            {
                if (workgroupIds == null || !workgroupIds.Any())
                {
                    return new List<WorkGroup>();
                }

                var request = new { WorkgroupIds = workgroupIds };
                var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("api/workgroups/by-ids", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Failed to get workgroups by IDs. Status: {response.StatusCode}, Content: {errorContent}, IDs: [{string.Join(", ", workgroupIds)}]");
                }
                
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IEnumerable<WorkGroup>>(json, _jsonOptions) ?? new List<WorkGroup>();
            }
            catch (HttpRequestException)
            {
                throw; // Re-throw HTTP exceptions with more context
            }
            catch (Exception ex)
            {
                throw new HttpRequestException($"Error calling GetWorkGroupsByIdsAsync: {ex.Message}", ex);
            }
        }

        public async Task<WorkGroup?> GetWorkGroupAsync(int workgroupId)
        {
            return await GetByIdAsync(workgroupId);
        }

        public async Task<IEnumerable<WorkGroup>> GetWorkGroupsAsync()
        {
            return await GetAllAsync();
        }
    }
}
