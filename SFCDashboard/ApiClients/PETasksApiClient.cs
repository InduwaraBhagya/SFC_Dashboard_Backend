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
                var response = await _httpClient.GetAsync("api/petasks");
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
                var response = await _httpClient.GetAsync($"api/petasks/{id}");
                
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
                
                var response = await _httpClient.PostAsync("api/petasks", content);
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
                
                var response = await _httpClient.PutAsync($"api/petasks/{peTask.Id}", content);
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
                var response = await _httpClient.DeleteAsync($"api/petasks/{id}");
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
                var response = await _httpClient.GetAsync($"api/petasks/by-pe/{Uri.EscapeDataString(peNumber)}");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var tasks = JsonSerializer.Deserialize<IEnumerable<PETask>>(json, _jsonOptions);
                
                return tasks ?? new List<PETask>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching PE tasks for PE number {PENumber} from API", peNumber);
                return new List<PETask>();
            }
        }
    }
}
