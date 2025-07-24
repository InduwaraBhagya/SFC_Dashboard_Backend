using SFCDashboard.Models;
using System.Text.Json;

namespace SFCDashboard.ApiClients
{
    public class PEIssueResolutionsApiClient : IPEIssueResolutionsApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public PEIssueResolutionsApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        }

        public async Task<IEnumerable<PEIssueResolution>> GetAllAsync()
        {
            var response = await _httpClient.GetAsync("api/peissueresolutions");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<PEIssueResolution>>(json, _jsonOptions) ?? new List<PEIssueResolution>();
        }

        public async Task<PEIssueResolution?> GetByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/peissueresolutions/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PEIssueResolution>(json, _jsonOptions);
        }

        public async Task<PEIssueResolution> CreateAsync(PEIssueResolution peIssueResolution)
        {
            var json = JsonSerializer.Serialize(peIssueResolution, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/peissueresolutions", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PEIssueResolution>(responseJson, _jsonOptions)!;
        }

        public async Task<PEIssueResolution> UpdateAsync(PEIssueResolution peIssueResolution)
        {
            var json = JsonSerializer.Serialize(peIssueResolution, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"api/peissueresolutions/{peIssueResolution.Id}", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PEIssueResolution>(responseJson, _jsonOptions)!;
        }

        public async Task DeleteAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/peissueresolutions/{id}");
            response.EnsureSuccessStatusCode();
        }

        public async Task<PEIssueResolution?> GetPendingResolutionAsync(int issueId)
        {
            var response = await _httpClient.GetAsync($"api/peissueresolutions/pending/{issueId}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PEIssueResolution>(json, _jsonOptions);
        }

        public async Task<Dictionary<int, PEIssueResolution>> GetResolutionsByIssueIdsAsync(List<int> issueIds)
        {
            var idsJson = JsonSerializer.Serialize(issueIds, _jsonOptions);
            var content = new StringContent(idsJson, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/peissueresolutions/by-issue-ids", content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Dictionary<int, PEIssueResolution>>(json, _jsonOptions) ?? new Dictionary<int, PEIssueResolution>();
        }
    }
}
