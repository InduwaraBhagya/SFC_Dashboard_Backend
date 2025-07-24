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
                var response = await _httpClient.GetAsync("api/plannedeventsapi");
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
                var response = await _httpClient.GetAsync($"api/plannedeventsapi/{id}");
                
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
                
                var response = await _httpClient.PostAsync("api/plannedeventsapi", content);
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
                
                var response = await _httpClient.PutAsync($"api/plannedeventsapi/{plannedEvent.Id}", content);
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
                var response = await _httpClient.DeleteAsync($"api/plannedeventsapi/{id}");
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
                var response = await _httpClient.GetAsync($"api/plannedeventsapi/search?term={Uri.EscapeDataString(searchTerm)}");
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

        public async Task<IEnumerable<PlannedEvent>> GetPendingUrgentRequestsAsync(int limit = 10)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/plannedeventsapi/pending-urgent-requests?limit={limit}");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IEnumerable<PlannedEvent>>(json, _jsonOptions) ?? new List<PlannedEvent>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pending urgent requests via API");
                return new List<PlannedEvent>();
            }
        }

        public async Task<PaginatedList<PlannedEvent>> SearchPlannedEventsAsync(string searchType, string searchString, string? workgroupName, bool hasDrawFiberAccess, int pageIndex, int pageSize)
        {
            try
            {
                var requestData = new 
                {
                    searchType,
                    searchString,
                    workgroupName,
                    hasDrawFiberAccess,
                    pageIndex,
                    pageSize
                };

                var json = JsonSerializer.Serialize(requestData, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("api/plannedeventsapi/search-paginated", content);
                response.EnsureSuccessStatusCode();
                
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PaginatedList<PlannedEvent>>(responseJson, _jsonOptions) ?? 
                       new PaginatedList<PlannedEvent>(new List<PlannedEvent>(), 0, pageIndex, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching planned events with pagination via API");
                return new PaginatedList<PlannedEvent>(new List<PlannedEvent>(), 0, pageIndex, pageSize);
            }
        }

        public async Task<IEnumerable<PlannedEvent>> GetPlannedEventsAsync()
        {
            return await GetAllAsync();
        }

        public async Task<PlannedEvent?> GetPlannedEventByIdAsync(int? id)
        {
            if (!id.HasValue) return null;
            return await GetByIdAsync(id.Value);
        }

        public async Task<PlannedEvent> CreatePlannedEventAsync(PlannedEvent plannedEvent)
        {
            return await CreateAsync(plannedEvent);
        }

        public async Task<PlannedEvent> UpdatePlannedEventAsync(PlannedEvent plannedEvent)
        {
            return await UpdateAsync(plannedEvent);
        }

        public async Task<bool> DeletePlannedEventAsync(int id)
        {
            try
            {
                await DeleteAsync(id);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> PlannedEventExistsAsync(int id)
        {
            try
            {
                var plannedEvent = await GetByIdAsync(id);
                return plannedEvent != null;
            }
            catch
            {
                return false;
            }
        }

        // Implementation for filtered planned events methods
        public async Task<IEnumerable<PlannedEvent>> GetInProgressPlannedEventsAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false, bool canViewAll = false)
        {
            try
            {
                // Get PE numbers that have OLA violating tasks
                var response = await _httpClient.GetAsync("api/petasksapi/ola-violating-pe-numbers");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var violatingPENumbers = JsonSerializer.Deserialize<IEnumerable<string>>(json, _jsonOptions) ?? new List<string>();

                // Get all planned events
                var allEvents = await GetAllAsync();
                
                // Filter to in-progress events that are not OLA violating and not on hold
                var inProgressEvents = allEvents.Where(p =>
                    (p.PEStatus == "ongoing" || p.PEStatus == "PENDING_URGENT_CONFIRMATION") &&
                    !p.IsHold &&
                    (p.PeNumber == null || !violatingPENumbers.Contains(p.PeNumber)));

                // Apply workgroup filtering if specified
                if (!canViewAll && workgroupNames.Any())
                {
                    if (hasDrawFiberAccess)
                    {
                        inProgressEvents = inProgressEvents.Where(p =>
                            p.TaskWg != null && (
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName)) ||
                            (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
                            )
                        );
                    }
                    else
                    {
                        inProgressEvents = inProgressEvents.Where(p => p.TaskWg != null &&
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName)));
                    }
                }
                else if (canViewAll && workgroupNames.Any())
                {
                    // ViewAll users can still filter by specific workgroups if requested
                    if (hasDrawFiberAccess)
                    {
                        inProgressEvents = inProgressEvents.Where(p =>
                            p.TaskWg != null && (
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName)) ||
                            (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
                            )
                        );
                    }
                    else
                    {
                        inProgressEvents = inProgressEvents.Where(p => p.TaskWg != null &&
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName)));
                    }
                }

                return inProgressEvents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching in-progress planned events via API");
                return new List<PlannedEvent>();
            }
        }

        public async Task<IEnumerable<PlannedEvent>> GetUrgentPlannedEventsAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false, bool canViewAll = false)
        {
            try
            {
                // Get PE numbers that have OLA violating tasks
                var response = await _httpClient.GetAsync("api/petasksapi/ola-violating-pe-numbers");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var violatingPENumbers = JsonSerializer.Deserialize<IEnumerable<string>>(json, _jsonOptions) ?? new List<string>();

                // Get all planned events
                var allEvents = await GetAllAsync();
                
                // Filter to urgent events that are not OLA violating and not on hold
                var urgentEvents = allEvents.Where(p =>
                    p.PEStatus == "urgent" &&
                    !p.IsHold &&
                    (p.PeNumber == null || !violatingPENumbers.Contains(p.PeNumber)));

                // Apply workgroup filtering if specified
                if (!canViewAll && workgroupNames.Any())
                {
                    if (hasDrawFiberAccess)
                    {
                        urgentEvents = urgentEvents.Where(p =>
                            p.TaskWg != null && (
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName)) ||
                            (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
                            )
                        );
                    }
                    else
                    {
                        urgentEvents = urgentEvents.Where(p => p.TaskWg != null &&
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName)));
                    }
                }
                else if (canViewAll && workgroupNames.Any())
                {
                    // ViewAll users can still filter by specific workgroups if requested
                    if (hasDrawFiberAccess)
                    {
                        urgentEvents = urgentEvents.Where(p =>
                            p.TaskWg != null && (
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName)) ||
                            (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
                            )
                        );
                    }
                    else
                    {
                        urgentEvents = urgentEvents.Where(p => p.TaskWg != null &&
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName)));
                    }
                }

                return urgentEvents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching urgent planned events via API");
                return new List<PlannedEvent>();
            }
        }

        public async Task<int> GetUrgentCountAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false, bool canViewAll = false)
        {
            try
            {
                var urgentEvents = await GetUrgentPlannedEventsAsync(workgroupNames, hasDrawFiberAccess, canViewAll);
                return urgentEvents.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting urgent count via API");
                return 0;
            }
        }

        public async Task<int> GetInProgressCountAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false, bool canViewAll = false)
        {
            try
            {
                var inProgressEvents = await GetInProgressPlannedEventsAsync(workgroupNames, hasDrawFiberAccess, canViewAll);
                return inProgressEvents.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting in-progress count via API");
                return 0;
            }
        }

        public async Task<IEnumerable<PlannedEvent>> GetOLAViolatingPlannedEventsAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false, bool canViewAll = false)
        {
            try
            {
                // Get PE numbers that have OLA violating tasks
                var response = await _httpClient.GetAsync("api/petasksapi/ola-violating-pe-numbers");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var violatingPENumbers = JsonSerializer.Deserialize<IEnumerable<string>>(json, _jsonOptions) ?? new List<string>();

                // Get all planned events
                var allEvents = await GetAllAsync();
                
                // Filter to events with violating PE numbers that are not on hold
                var violatingEvents = allEvents.Where(p => 
                    p.PeNumber != null && 
                    violatingPENumbers.Contains(p.PeNumber) && 
                    !p.IsHold);

                // Apply workgroup filtering if specified
                if (!canViewAll && workgroupNames.Any())
                {
                    if (hasDrawFiberAccess)
                    {
                        violatingEvents = violatingEvents.Where(p =>
                            p.TaskWg != null && (
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName)) ||
                            (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
                            )
                        );
                    }
                    else
                    {
                        violatingEvents = violatingEvents.Where(p => p.TaskWg != null &&
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName)));
                    }
                }
                else if (canViewAll && workgroupNames.Any())
                {
                    // ViewAll users can still filter by specific workgroups if requested
                    if (hasDrawFiberAccess)
                    {
                        violatingEvents = violatingEvents.Where(p =>
                            p.TaskWg != null && (
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName)) ||
                            (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
                            )
                        );
                    }
                    else
                    {
                        violatingEvents = violatingEvents.Where(p => p.TaskWg != null &&
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName)));
                    }
                }

                return violatingEvents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching OLA violating planned events via API");
                return new List<PlannedEvent>();
            }
        }

        public async Task<IEnumerable<PlannedEvent>> GetHoldPlannedEventsAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false, bool canViewAll = false)
        {
            try
            {
                // Get all planned events
                var allEvents = await GetAllAsync();
                
                // Filter to hold events
                var holdEvents = allEvents.Where(p => p.IsHold);

                // Apply workgroup filtering if specified
                if (!canViewAll && workgroupNames.Any())
                {
                    if (hasDrawFiberAccess)
                    {
                        holdEvents = holdEvents.Where(p =>
                            p.TaskWg != null && (
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName)) ||
                            (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
                            )
                        );
                    }
                    else
                    {
                        holdEvents = holdEvents.Where(p => p.TaskWg != null &&
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName)));
                    }
                }
                else if (canViewAll && workgroupNames.Any())
                {
                    // ViewAll users can still filter by specific workgroups if requested
                    if (hasDrawFiberAccess)
                    {
                        holdEvents = holdEvents.Where(p =>
                            p.TaskWg != null && (
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName)) ||
                            (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
                            )
                        );
                    }
                    else
                    {
                        holdEvents = holdEvents.Where(p => p.TaskWg != null &&
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName)));
                    }
                }

                return holdEvents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching hold planned events via API");
                return new List<PlannedEvent>();
            }
        }

        public async Task<int> GetOLAViolateCountAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false, bool canViewAll = false)
        {
            try
            {
                var olaEvents = await GetOLAViolatingPlannedEventsAsync(workgroupNames, hasDrawFiberAccess, canViewAll);
                return olaEvents.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OLA violate count via API");
                return 0;
            }
        }

        public async Task<int> GetHoldCountAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false, bool canViewAll = false)
        {
            try
            {
                var holdEvents = await GetHoldPlannedEventsAsync(workgroupNames, hasDrawFiberAccess, canViewAll);
                return holdEvents.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hold count via API");
                return 0;
            }
        }

        public async Task<PlannedEvent?> GetPlannedEventAsync(int id)
        {
            return await GetByIdAsync(id);
        }

        public async Task<IEnumerable<PlannedEvent>> GetPlannedEventsByWorkgroupAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false)
        {
            try
            {
                // Get all planned events
                var allEvents = await GetAllAsync();
                
                // Filter by workgroups
                var filteredEvents = allEvents.Where(p =>
                {
                    if (hasDrawFiberAccess)
                    {
                        return p.TaskWg != null && (
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName)) ||
                            (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
                        );
                    }
                    else
                    {
                        return p.TaskWg != null && workgroupNames.Any(wgName => p.TaskWg.Contains(wgName));
                    }
                });

                return filteredEvents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching planned events by workgroup via API");
                return new List<PlannedEvent>();
            }
        }

        public async Task<int> GetUrgentCountForMultiWorkgroupAsync(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds, bool hasDrawFiberAccess)
        {
            try
            {
                var requestData = new 
                {
                    selectedWorkgroupIds,
                    userWorkgroupIds,
                    hasDrawFiberAccess
                };

                var json = JsonSerializer.Serialize(requestData, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("api/plannedeventsapi/urgent-count-multi-workgroup", content);
                response.EnsureSuccessStatusCode();
                
                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<Dictionary<string, int>>(responseJson, _jsonOptions);
                return result?.GetValueOrDefault("count", 0) ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting urgent count for multi workgroup via API");
                return 0;
            }
        }

        public async Task<int> GetInProgressCountForMultiWorkgroupAsync(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds, bool hasDrawFiberAccess)
        {
            try
            {
                var requestData = new 
                {
                    selectedWorkgroupIds,
                    userWorkgroupIds,
                    hasDrawFiberAccess
                };

                var json = JsonSerializer.Serialize(requestData, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("api/plannedeventsapi/inprogress-count-multi-workgroup", content);
                response.EnsureSuccessStatusCode();
                
                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<Dictionary<string, int>>(responseJson, _jsonOptions);
                return result?.GetValueOrDefault("count", 0) ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting in-progress count for multi workgroup via API");
                return 0;
            }
        }

        public async Task<int> GetOLAViolateCountForMultiWorkgroupAsync(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds, bool hasDrawFiberAccess)
        {
            try
            {
                var requestData = new 
                {
                    selectedWorkgroupIds,
                    userWorkgroupIds,
                    hasDrawFiberAccess
                };

                var json = JsonSerializer.Serialize(requestData, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("api/plannedeventsapi/ola-violate-count-multi-workgroup", content);
                response.EnsureSuccessStatusCode();
                
                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<Dictionary<string, int>>(responseJson, _jsonOptions);
                return result?.GetValueOrDefault("count", 0) ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OLA violate count for multi workgroup via API");
                return 0;
            }
        }

        public async Task<int> GetHoldCountForMultiWorkgroupAsync(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds, bool hasDrawFiberAccess)
        {
            try
            {
                var requestData = new 
                {
                    selectedWorkgroupIds,
                    userWorkgroupIds,
                    hasDrawFiberAccess
                };

                var json = JsonSerializer.Serialize(requestData, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("api/plannedeventsapi/hold-count-multi-workgroup", content);
                response.EnsureSuccessStatusCode();
                
                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<Dictionary<string, int>>(responseJson, _jsonOptions);
                return result?.GetValueOrDefault("count", 0) ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hold count for multi workgroup via API");
                return 0;
            }
        }
    }
}
