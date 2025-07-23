using SFCDashboard.Models;
using System.Text.Json;
using System.Text;

namespace SFCDashboard.ApiClients
{
    public class PlannedEventsApiClient : IPlannedEventsApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PlannedEventsApiClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public PlannedEventsApiClient(HttpClient httpClient, ILogger<PlannedEventsApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<IEnumerable<PlannedEvent>> GetAllAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/plannedevents");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var events = JsonSerializer.Deserialize<IEnumerable<PlannedEvent>>(json, _jsonOptions);
                
                return events ?? new List<PlannedEvent>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching planned events from API");
                return new List<PlannedEvent>();
            }
        }

        public async Task<PlannedEvent?> GetByIdAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/plannedevents/{id}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;
                    
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PlannedEvent>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching planned event {Id} from API", id);
                return null;
            }
        }

        public async Task<PlannedEvent> CreateAsync(PlannedEvent plannedEvent)
        {
            try
            {
                var json = JsonSerializer.Serialize(plannedEvent, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("api/plannedevents", content);
                response.EnsureSuccessStatusCode();
                
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PlannedEvent>(responseJson, _jsonOptions) ?? plannedEvent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating planned event via API");
                throw;
            }
        }

        public async Task<PlannedEvent> UpdateAsync(PlannedEvent plannedEvent)
        {
            try
            {
                var json = JsonSerializer.Serialize(plannedEvent, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PutAsync($"api/plannedevents/{plannedEvent.Id}", content);
                response.EnsureSuccessStatusCode();
                
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PlannedEvent>(responseJson, _jsonOptions) ?? plannedEvent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating planned event {Id} via API", plannedEvent.Id);
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/plannedevents/{id}");
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting planned event {Id} via API", id);
                throw;
            }
        }

        public async Task<IEnumerable<PlannedEvent>> SearchAsync(string searchTerm)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/plannedevents/search?term={Uri.EscapeDataString(searchTerm)}");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var events = JsonSerializer.Deserialize<IEnumerable<PlannedEvent>>(json, _jsonOptions);
                
                return events ?? new List<PlannedEvent>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching planned events via API");
                return new List<PlannedEvent>();
            }
        }
    }
}
