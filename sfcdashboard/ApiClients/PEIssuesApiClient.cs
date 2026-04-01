using SFCDashboard.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SFCDashboard.ApiClients
{
    // Response model for mark all reminders as read operation
    public class MarkAllRemindersResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        
        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    public class PEIssuesApiClient : IPEIssuesApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        public async Task<IEnumerable<PEIssue>> GetByTaskIdAsync(int taskId)
        {
            var response = await _httpClient.GetAsync($"api/peissues/bytask/{taskId}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<PEIssue>>(json, _jsonOptions) ?? new List<PEIssue>();
        }

        public async Task<IEnumerable<PEIssue>> GetByPlannedEventIdAsync(int plannedEventId)
        {
            var response = await _httpClient.GetAsync($"api/peissues/plannedevent/{plannedEventId}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<PEIssue>>(json, _jsonOptions) ?? new List<PEIssue>();
        }

        public PEIssuesApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        }

        public async Task<IEnumerable<PEIssue>> GetAllAsync()
        {
            var response = await _httpClient.GetAsync("api/peissues");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<PEIssue>>(json, _jsonOptions) ?? new List<PEIssue>();
        }

        public async Task<PEIssue?> GetByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/peissues/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PEIssue>(json, _jsonOptions);
        }

        public async Task<PEIssue> CreateAsync(PEIssue peIssue)
        {
            var json = JsonSerializer.Serialize(peIssue, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/peissues", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PEIssue>(responseJson, _jsonOptions)!;
        }

        public async Task<PEIssue> UpdateAsync(PEIssue peIssue)
        {
            var json = JsonSerializer.Serialize(peIssue, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"api/peissues/{peIssue.Id}", content);
            response.EnsureSuccessStatusCode();
            
            // API returns NoContent (204), so we return the original object
            // since the update was successful
            return peIssue;
        }

        public async Task DeleteAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/peissues/{id}");
            response.EnsureSuccessStatusCode();
        }

        public async Task<IEnumerable<PEIssue>> GetInboxIssuesAsync(int userId, int limit = 10)
        {
            var response = await _httpClient.GetAsync($"api/peissues/inbox/{userId}?limit={limit}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<PEIssue>>(json, _jsonOptions) ?? new List<PEIssue>();
        }

        public async Task<IEnumerable<PEIssue>> GetRemindersAsync(int userId, bool showAll = true)
        {
            var response = await _httpClient.GetAsync($"api/peissues/reminders/{userId}?showAll={showAll}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<PEIssue>>(json, _jsonOptions) ?? new List<PEIssue>();
        }

        public async Task<int> GetReminderCountAsync(int userId)
        {
            var response = await _httpClient.GetAsync($"api/peissues/reminders/{userId}/count");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<Dictionary<string, int>>(json, _jsonOptions);
            return result?.GetValueOrDefault("count", 0) ?? 0;
        }

        public async Task<bool> MarkAllRemindersAsReadAsync(int userId)
        {
            var response = await _httpClient.PostAsync($"api/peissues/reminders/{userId}/markallread", null);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<MarkAllRemindersResponse>(json, _jsonOptions);
            return result?.Success ?? false;
        }

        public async Task<bool> MarkReminderAsReadAsync(int reminderId)
        {
            var response = await _httpClient.PostAsync($"api/peissues/{reminderId}/markread", null);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<Dictionary<string, bool>>(json, _jsonOptions);
            return result?.GetValueOrDefault("success", false) ?? false;
        }

        public async Task<IEnumerable<PEIssue>> GetPEIssuesByPlannedEventAsync(int plannedEventId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/peissues/plannedevent/{plannedEventId}");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(json))
                {
                    return new List<PEIssue>();
                }

                var result = JsonSerializer.Deserialize<IEnumerable<PEIssue>>(json, _jsonOptions);
                return result ?? new List<PEIssue>();
            }
            catch (JsonException ex)
            {
                // Log JSON parsing error but return empty list to prevent crashes
                return new List<PEIssue>();
            }
            catch (HttpRequestException ex)
            {
                // Log HTTP error but return empty list for graceful degradation
                return new List<PEIssue>();
            }
            catch (Exception ex)
            {
                // Log unexpected error but return empty list
                return new List<PEIssue>();
            }
        }

        public async Task<IEnumerable<PEIssueViewModel>> GetPEIssueViewModelsByPlannedEventAsync(int plannedEventId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/peissues/plannedevent/{plannedEventId}/viewmodels");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(json))
                {
                    return new List<PEIssueViewModel>();
                }

                var result = JsonSerializer.Deserialize<IEnumerable<PEIssueViewModel>>(json, _jsonOptions);
                return result ?? new List<PEIssueViewModel>();
            }
            catch (JsonException ex)
            {
                // Log JSON parsing error but return empty list to prevent crashes
                return new List<PEIssueViewModel>();
            }
            catch (HttpRequestException ex)
            {
                // Log HTTP error but return empty list for graceful degradation
                return new List<PEIssueViewModel>();
            }
            catch (Exception ex)
            {
                // Log unexpected error but return empty list
                return new List<PEIssueViewModel>();
            }
        }

        public async Task<Dictionary<int, IEnumerable<PEIssue>>> GetIssuesByPlannedEventIdsAsync(List<int> peIds)
        {
            try
            {
                if (peIds == null || !peIds.Any())
                {
                    return new Dictionary<int, IEnumerable<PEIssue>>();
                }

                var idsJson = JsonSerializer.Serialize(peIds, _jsonOptions);
                var content = new StringContent(idsJson, System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("api/peissues/by-plannedevent-ids", content);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(json))
                    return new Dictionary<int, IEnumerable<PEIssue>>();

                return JsonSerializer.Deserialize<Dictionary<int, IEnumerable<PEIssue>>>(json, _jsonOptions) ?? new Dictionary<int, IEnumerable<PEIssue>>();
            }
            catch (JsonException)
            {
                return new Dictionary<int, IEnumerable<PEIssue>>();
            }
            catch (HttpRequestException)
            {
                return new Dictionary<int, IEnumerable<PEIssue>>();
            }
        }

        public async Task<Dictionary<int, IEnumerable<PEIssueViewModel>>> GetPEIssueViewModelsByPlannedEventIdsAsync(List<int> peIds)
        {
            try
            {
                if (peIds == null || !peIds.Any())
                {
                    return new Dictionary<int, IEnumerable<PEIssueViewModel>>();
                }

                var idsJson = JsonSerializer.Serialize(peIds, _jsonOptions);
                var content = new StringContent(idsJson, System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("api/peissues/viewmodels-by-plannedevent-ids", content);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(json))
                    return new Dictionary<int, IEnumerable<PEIssueViewModel>>();

                return JsonSerializer.Deserialize<Dictionary<int, IEnumerable<PEIssueViewModel>>>(json, _jsonOptions) ?? new Dictionary<int, IEnumerable<PEIssueViewModel>>();
            }
            catch (JsonException)
            {
                return new Dictionary<int, IEnumerable<PEIssueViewModel>>();
            }
            catch (HttpRequestException)
            {
                return new Dictionary<int, IEnumerable<PEIssueViewModel>>();
            }
        }

        public async Task<PEIssue?> GetPEIssueAsync(int id)
        {
            return await GetByIdAsync(id);
        }

        public async Task<PEIssue> UpdatePEIssueAsync(PEIssue peIssue)
        {
            return await UpdateAsync(peIssue);
        }

        public async Task<IEnumerable<PEIssue>> GetReceivedIssuesAsync(int userId)
        {
            var response = await _httpClient.GetAsync($"api/peissues/received/{userId}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<PEIssue>>(json, _jsonOptions) ?? new List<PEIssue>();
        }

        public async Task<IEnumerable<PEIssue>> GetSentIssuesAsync(int userId)
        {
            var response = await _httpClient.GetAsync($"api/peissues/sent/{userId}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<PEIssue>>(json, _jsonOptions) ?? new List<PEIssue>();
        }

        public async Task<IEnumerable<PEIssue>> GetUnreadInboxIssuesAsync(int userId)
        {
            var response = await _httpClient.GetAsync($"api/peissues/unread-inbox/{userId}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<PEIssue>>(json, _jsonOptions) ?? new List<PEIssue>();
        }

        public async Task<IEnumerable<PEIssueViewModel>> GetInboxViewModelsAsync(int userId)
        {
            var response = await _httpClient.GetAsync($"api/peissues/inbox-viewmodels/{userId}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<PEIssueViewModel>>(json, _jsonOptions) ?? new List<PEIssueViewModel>();
        }

        public async Task<IEnumerable<PEIssueViewModel>> GetInboxViewModelsAsync(int userId, int limit)
        {
            var response = await _httpClient.GetAsync($"api/peissues/inbox-viewmodels/{userId}?limit={limit}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<PEIssueViewModel>>(json, _jsonOptions) ?? new List<PEIssueViewModel>();
        }

        public async Task<IEnumerable<PEIssueViewModel>> GetSentViewModelsAsync(int userId)
        {
            var response = await _httpClient.GetAsync($"api/peissues/sent-viewmodels/{userId}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<PEIssueViewModel>>(json, _jsonOptions) ?? new List<PEIssueViewModel>();
        }

        public async Task<bool> MarkIssueAsReadAsync(int issueId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/peissues/{issueId}/markread", null);
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (HttpRequestException)
            {
                return false;
            }
        }
    }
}
