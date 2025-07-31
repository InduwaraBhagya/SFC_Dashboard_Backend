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
            try
            {
                var response = await _httpClient.GetAsync("api/workgroups");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Failed to get workgroups. Status: {response.StatusCode}, Content: {errorContent}");
                }
                
                var json = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrWhiteSpace(json))
                {
                    throw new InvalidOperationException("API returned empty response for GetAllAsync");
                }
                
                return JsonSerializer.Deserialize<IEnumerable<WorkGroup>>(json, _jsonOptions) ?? new List<WorkGroup>();
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to deserialize workgroups response. Response might not be valid JSON.", ex);
            }
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
            try
            {
                var json = JsonSerializer.Serialize(workGroup, _jsonOptions);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("api/workgroups", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Failed to create workgroup. Status: {response.StatusCode}, Content: {errorContent}, Request: {json}");
                }
                
                var responseJson = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrWhiteSpace(responseJson))
                {
                    throw new InvalidOperationException("API returned empty response for CreateAsync");
                }
                
                return JsonSerializer.Deserialize<WorkGroup>(responseJson, _jsonOptions)!;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to deserialize create workgroup response. Response might not be valid JSON.", ex);
            }
        }

        public async Task<WorkGroup> UpdateAsync(WorkGroup workGroup)
        {
            try
            {
                var json = JsonSerializer.Serialize(workGroup, _jsonOptions);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"api/workgroups/{workGroup.Id}", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    // Try to parse error as JSON, fall back to raw content if it fails
                    try
                    {
                        var errorObj = JsonSerializer.Deserialize<Dictionary<string, object>>(errorContent, _jsonOptions);
                        var errorMessage = errorObj?.GetValueOrDefault("error")?.ToString() ?? errorContent;
                        throw new HttpRequestException($"Failed to update workgroup. Status: {response.StatusCode}, Error: {errorMessage}");
                    }
                    catch (JsonException)
                    {
                        throw new HttpRequestException($"Failed to update workgroup. Status: {response.StatusCode}, Content: {errorContent}");
                    }
                }
                
                var responseJson = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrWhiteSpace(responseJson))
                {
                    throw new InvalidOperationException("API returned empty response for UpdateAsync");
                }
                
                return JsonSerializer.Deserialize<WorkGroup>(responseJson, _jsonOptions)!;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to deserialize update workgroup response. Response might not be valid JSON.", ex);
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/workgroups/{id}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Failed to delete workgroup. Status: {response.StatusCode}, Content: {errorContent}");
                }
            }
            catch (HttpRequestException)
            {
                throw; // Re-throw HTTP exceptions with more context
            }
            catch (Exception ex)
            {
                throw new HttpRequestException($"Error calling DeleteAsync: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<WorkGroup>> GetWorkGroupsForUserAsync(List<int> userWorkgroupIds, bool canViewAll)
        {
            try
            {
                var requestData = new { userWorkgroupIds, canViewAll };
                var idsJson = JsonSerializer.Serialize(requestData, _jsonOptions);
                var content = new StringContent(idsJson, System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("api/workgroups/for-user", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Failed to get workgroups for user. Status: {response.StatusCode}, Content: {errorContent}, Request: {idsJson}");
                }
                
                var json = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrWhiteSpace(json))
                {
                    throw new InvalidOperationException("API returned empty response for GetWorkGroupsForUserAsync");
                }
                
                return JsonSerializer.Deserialize<IEnumerable<WorkGroup>>(json, _jsonOptions) ?? new List<WorkGroup>();
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to deserialize workgroups for user response. Response might not be valid JSON.", ex);
            }
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
