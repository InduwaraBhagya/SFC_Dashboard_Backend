using SFCDashboard.Models;
using System.Text.Json;

namespace SFCDashboard.ApiClients
{
    public class AreaNetworkEngineersApiClient : IAreaNetworkEngineersApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public AreaNetworkEngineersApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        }

        public async Task<IEnumerable<AreaNetworkEngineer>> GetAllAsync()
        {
            var response = await _httpClient.GetAsync("api/areanetworkengineers");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<AreaNetworkEngineer>>(json, _jsonOptions) ?? new List<AreaNetworkEngineer>();
        }

        public async Task<AreaNetworkEngineer?> GetByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/areanetworkengineers/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AreaNetworkEngineer>(json, _jsonOptions);
        }

        public async Task<AreaNetworkEngineer> CreateAsync(AreaNetworkEngineer areaNetworkEngineer)
        {
            var json = JsonSerializer.Serialize(areaNetworkEngineer, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/areanetworkengineers", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AreaNetworkEngineer>(responseJson, _jsonOptions)!;
        }

        public async Task<AreaNetworkEngineer> UpdateAsync(AreaNetworkEngineer areaNetworkEngineer)
        {
            var json = JsonSerializer.Serialize(areaNetworkEngineer, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"api/areanetworkengineers/{areaNetworkEngineer.Id}", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AreaNetworkEngineer>(responseJson, _jsonOptions)!;
        }

        public async Task DeleteAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/areanetworkengineers/{id}");
            response.EnsureSuccessStatusCode();
        }

        public async Task<string?> GetEngineerNameByAreaAsync(string area)
        {
            var response = await _httpClient.GetAsync($"api/areanetworkengineers/by-area/{area}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<Dictionary<string, string>>(json, _jsonOptions);
            return result?.GetValueOrDefault("engineerName");
        }
    }
}
