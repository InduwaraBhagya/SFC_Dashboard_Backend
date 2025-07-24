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
            var response = await _httpClient.GetAsync("api/petasklists");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<PETaskList>>(json, _jsonOptions) ?? new List<PETaskList>();
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
            var response = await _httpClient.GetAsync($"api/petasklists/by-name/{Uri.EscapeDataString(taskName)}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PETaskList>(json, _jsonOptions);
        }

        public async Task<IEnumerable<PETaskList>> GetPETaskListsAsync()
        {
            return await GetAllAsync();
        }
    }
}
