using SFCDashboard.Models;
using System.Text.Json;

namespace SFCDashboard.ApiClients
{
    public class PETaskListsApiClient : IPETaskListsApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public PETaskListsApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        }

        public async Task<IEnumerable<PETaskList>> GetAllAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/petasklists");
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // Log that the endpoint was not found
                    return new List<PETaskList>();
                }
                
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new List<PETaskList>();
                }
                
                return JsonSerializer.Deserialize<IEnumerable<PETaskList>>(json, _jsonOptions) ?? new List<PETaskList>();
            }
            catch (JsonException ex)
            {
                // Log JSON parsing error but return empty list to prevent crashes
                return new List<PETaskList>();
            }
            catch (HttpRequestException ex)
            {
                // Log HTTP error but return empty list for graceful degradation
                return new List<PETaskList>();
            }
            catch (Exception ex)
            {
                // Log unexpected error but return empty list
                return new List<PETaskList>();
            }
        }

        public async Task<PETaskList?> GetByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/petasklists/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PETaskList>(json, _jsonOptions);
        }

        public async Task<PETaskList> CreateAsync(PETaskList peTaskList)
        {
            var json = JsonSerializer.Serialize(peTaskList, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/petasklists", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PETaskList>(responseJson, _jsonOptions)!;
        }

        public async Task<PETaskList> UpdateAsync(PETaskList peTaskList)
        {
            var json = JsonSerializer.Serialize(peTaskList, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"api/petasklists/{peTaskList.Id}", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PETaskList>(responseJson, _jsonOptions)!;
        }

        public async Task DeleteAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/petasklists/{id}");
            response.EnsureSuccessStatusCode();
        }

        public async Task<PETaskList?> GetPETaskListByNameAsync(string taskName)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/petasklists/by-name/{Uri.EscapeDataString(taskName)}");
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json))
                {
                    return null;
                }
                
                return JsonSerializer.Deserialize<PETaskList>(json, _jsonOptions);
            }
            catch (JsonException ex)
            {
                // Log JSON parsing error but return null
                return null;
            }
            catch (HttpRequestException ex)
            {
                // Log HTTP error but return null for graceful degradation
                return null;
            }
            catch (Exception ex)
            {
                // Log unexpected error but return null
                return null;
            }
        }

        public async Task<IEnumerable<PETaskList>> GetPETaskListsAsync()
        {
            return await GetAllAsync();
        }

        public async Task<bool> ExistsAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/petasklists/{id}/exists");
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return false;
                response.EnsureSuccessStatusCode();
                
                var result = await response.Content.ReadAsStringAsync();
                return bool.TryParse(result, out var exists) && exists;
            }
            catch (JsonException ex)
            {
                // Log JSON parsing error but return false
                return false;
            }
            catch (HttpRequestException ex)
            {
                // Log HTTP error but return false for graceful degradation
                return false;
            }
            catch (Exception ex)
            {
                // Log unexpected error but return false
                return false;
            }
        }
    }
}
