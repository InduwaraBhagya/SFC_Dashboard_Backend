using SFCDashboard.Models;
using System.Text.Json;

namespace SFCDashboard.ApiClients
{
    public class TaskQueueApiClient : ITaskQueueApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TaskQueueApiClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public TaskQueueApiClient(HttpClient httpClient, ILogger<TaskQueueApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<List<TaskQueueItem>> GetPrioritizedTasksAsync(int? workgroupId = null, int? year = null, int take = 20)
        {
            try
            {
                var queryParams = new List<string>();
                if (workgroupId.HasValue)
                    queryParams.Add($"workgroupId={workgroupId}");
                if (year.HasValue)
                    queryParams.Add($"year={year}");
                queryParams.Add($"take={take}");

                var query = string.Join("&", queryParams);
                var response = await _httpClient.GetAsync($"api/taskqueue/prioritized?{query}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<TaskQueueItem>>(content, _jsonOptions) ?? new List<TaskQueueItem>();
                }
                else
                {
                    _logger.LogWarning("Failed to get prioritized tasks. Status: {StatusCode}", response.StatusCode);
                    return new List<TaskQueueItem>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting prioritized tasks from API");
                return new List<TaskQueueItem>();
            }
        }

        public async Task<TaskQueueItem?> GetNextTaskAsync(int? workgroupId = null, int? year = null)
        {
            try
            {
                var queryParams = new List<string>();
                if (workgroupId.HasValue)
                    queryParams.Add($"workgroupId={workgroupId}");
                if (year.HasValue)
                    queryParams.Add($"year={year}");

                var query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
                var response = await _httpClient.GetAsync($"api/taskqueue/next{query}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<TaskQueueItem>(content, _jsonOptions);
                }
                else
                {
                    _logger.LogWarning("Failed to get next task. Status: {StatusCode}", response.StatusCode);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting next task from API");
                return null;
            }
        }

        public async Task<List<int>> GetAvailableYearsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/taskqueue/years");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<int>>(content, _jsonOptions) ?? new List<int> { DateTime.Now.Year };
                }
                else
                {
                    _logger.LogWarning("Failed to get available years. Status: {StatusCode}", response.StatusCode);
                    return new List<int> { DateTime.Now.Year };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available years from API");
                return new List<int> { DateTime.Now.Year };
            }
        }

        public async Task<List<TaskQueueItem>> RefreshTaskQueueAsync(int? workgroupId = null, int? year = null, int take = 20)
        {
            try
            {
                var queryParams = new List<string>();
                if (workgroupId.HasValue)
                    queryParams.Add($"workgroupId={workgroupId}");
                if (year.HasValue)
                    queryParams.Add($"year={year}");
                queryParams.Add($"take={take}");

                var query = string.Join("&", queryParams);
                var response = await _httpClient.PostAsync($"api/taskqueue/refresh?{query}", null);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<TaskQueueItem>>(content, _jsonOptions) ?? new List<TaskQueueItem>();
                }
                else
                {
                    _logger.LogWarning("Failed to refresh task queue. Status: {StatusCode}", response.StatusCode);
                    return new List<TaskQueueItem>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing task queue from API");
                return new List<TaskQueueItem>();
            }
        }
    }
}
