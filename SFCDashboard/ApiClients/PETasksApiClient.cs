using SFCDashboard.Models;
using System.Text.Json;
using System.Text;

namespace SFCDashboard.ApiClients
{
    public class PETasksApiClient : IPETasksApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PETasksApiClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public PETasksApiClient(HttpClient httpClient, ILogger<PETasksApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<IEnumerable<PETask>> GetAllAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/petasksapi");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var tasks = JsonSerializer.Deserialize<IEnumerable<PETask>>(json, _jsonOptions);

                return tasks ?? new List<PETask>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching PE tasks from API");
                return new List<PETask>();
            }
        }

        public async Task<PETask?> GetByIdAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/petasksapi/{id}");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PETask>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching PE task {Id} from API", id);
                return null;
            }
        }

        public async Task<PETask> CreateAsync(PETask peTask)
        {
            try
            {
                var json = JsonSerializer.Serialize(peTask, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/petasksapi", content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PETask>(responseJson, _jsonOptions) ?? peTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PE task via API");
                throw;
            }
        }

        public async Task<PETask> UpdateAsync(PETask peTask)
        {
            try
            {
                var json = JsonSerializer.Serialize(peTask, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"api/petasksapi/{peTask.Id}", content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PETask>(responseJson, _jsonOptions) ?? peTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating PE task {Id} via API", peTask.Id);
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/petasksapi/{id}");
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting PE task {Id} via API", id);
                throw;
            }
        }

        public async Task<IEnumerable<PETask>> GetByPENumberAsync(string peNumber)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/petasksapi/by-pe/{Uri.EscapeDataString(peNumber)}");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(json))
                {
                    return new List<PETask>();
                }

                var tasks = JsonSerializer.Deserialize<IEnumerable<PETask>>(json, _jsonOptions);
                return tasks ?? new List<PETask>();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing JSON response for PE tasks by PE number {PENumber}", peNumber);
                return new List<PETask>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error fetching PE tasks by PE number {PENumber} from API", peNumber);
                return new List<PETask>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching PE tasks by PE number {PENumber} from API", peNumber);
                return new List<PETask>();
            }
        }

        public async Task<IEnumerable<PETask>> GetUrgentRequestsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/petasksapi/urgent-requests");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var tasks = JsonSerializer.Deserialize<IEnumerable<PETask>>(json, _jsonOptions);

                return tasks ?? new List<PETask>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching urgent requests from API");
                return new List<PETask>();
            }
        }

        public async Task<IEnumerable<PETask>> GetOLAViolationsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/petasksapi/ola-violations");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var tasks = JsonSerializer.Deserialize<IEnumerable<PETask>>(json, _jsonOptions);

                return tasks ?? new List<PETask>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching OLA violations from API");
                return new List<PETask>();
            }
        }

        public async Task<IEnumerable<PETask>> GetUrgentTasksAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/petasksapi/urgent-tasks");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var tasks = JsonSerializer.Deserialize<IEnumerable<PETask>>(json, _jsonOptions);

                return tasks ?? new List<PETask>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching urgent tasks from API");
                return new List<PETask>();
            }
        }

        public async Task MarkAsUrgentAsync(int id)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/petasksapi/{id}/mark-urgent", null);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking PE task {Id} as urgent via API", id);
                throw;
            }
        }

        public async Task ProcessUrgentRequestAsync(int id, string urgentReason)
        {
            try
            {
                var requestData = new { urgentReason };
                var json = JsonSerializer.Serialize(requestData, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"api/petasksapi/{id}/process-urgent-request", content);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing urgent request for PE task {Id} via API", id);
                throw;
            }
        }

        public async Task ProcessPEUrgentRequestAsync(string peNumber, string urgentReason)
        {
            try
            {
                var requestData = new { urgentReason };
                var json = JsonSerializer.Serialize(requestData, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"api/petasksapi/pe/{peNumber}/process-urgent-request", content);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PE urgent request for PE {PENumber} via API", peNumber);
                throw;
            }
        }

        public async Task CompleteViolatedTaskAsync(int id)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/petasksapi/{id}/complete-violated", null);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing violated PE task {Id} via API", id);
                throw;
            }
        }

        public async Task RemoveUrgentStatusAsync(int id)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/petasksapi/{id}/remove-urgent", null);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing urgent status from PE task {Id} via API", id);
                throw;
            }
        }

        public async Task UpdateEstimatedTimeAsync(int id, DateTime estimatedTime)
        {
            try
            {
                var requestData = new { EstimatedTime = estimatedTime };
                var json = JsonSerializer.Serialize(requestData, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"api/petasksapi/{id}/update-estimated-time", content);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating estimated time for PE task {Id} via API", id);
                throw;
            }
        }

        public async Task<IEnumerable<PETask>> GetPendingTaskRequestsAsync(int limit = 5)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/petasksapi/pending-task-requests?limit={limit}");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IEnumerable<PETask>>(json, _jsonOptions) ?? new List<PETask>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pending task requests via API");
                return new List<PETask>();
            }
        }

        public async Task<Dictionary<string, IEnumerable<PETask>>> GetTasksByPeNumbersAsync(List<string> peNumbers)
        {
            try
            {
                var requestData = new { peNumbers };
                var json = JsonSerializer.Serialize(requestData, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/petasksapi/tasks-by-pe-numbers", content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Dictionary<string, IEnumerable<PETask>>>(responseJson, _jsonOptions) ??
                       new Dictionary<string, IEnumerable<PETask>>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching tasks by PE numbers via API");
                return new Dictionary<string, IEnumerable<PETask>>();
            }
        }

        public async Task<List<string>> GetOLAViolatingPENumbersAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/petasksapi/ola-violations");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var violatingTasks = JsonSerializer.Deserialize<IEnumerable<PETask>>(json, _jsonOptions) ?? new List<PETask>();
                var peNumbers = violatingTasks
                    .Where(t => !string.IsNullOrEmpty(t.PENumber))
                    .Select(t => t.PENumber)
                    .Distinct()
                    .ToList();
                return peNumbers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching OLA violating PE numbers via API");
                return new List<string>();
            }
        }

        public async Task<IEnumerable<PETask>> GetPETasksByPENumberAsync(string peNumber)
        {
            return await GetByPENumberAsync(peNumber);
        }

        public async Task<IEnumerable<PETask>> GetPETasksByPENumbersAsync(List<string> peNumbers)
        {
            try
            {
                if (peNumbers == null || !peNumbers.Any())
                {
                    return new List<PETask>();
                }

                var requestData = new { peNumbers };
                var json = JsonSerializer.Serialize(requestData, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/petasksapi/pe-tasks-by-pe-numbers", content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(responseJson))
                    return new List<PETask>();

                return JsonSerializer.Deserialize<IEnumerable<PETask>>(responseJson, _jsonOptions) ?? new List<PETask>();
            }
            catch (JsonException)
            {
                return new List<PETask>();
            }
            catch (HttpRequestException)
            {
                return new List<PETask>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching PE tasks by PE numbers via API");
                return new List<PETask>();
            }
        }

        public async Task<PETask> UpdatePETaskAsync(PETask peTask)
        {
            return await UpdateAsync(peTask);
        }

        public async Task<IEnumerable<PETask>> GetPendingUrgentTaskRequestsAsync(int limit = 5)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/petasksapi/pending-urgent-task-requests?limit={limit}");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IEnumerable<PETask>>(json, _jsonOptions) ?? new List<PETask>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pending urgent task requests via API");
                return new List<PETask>();
            }
        }
    }
}
