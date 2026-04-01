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

        public async Task<PlannedEvent?> GetPlannedEventByPENumberAsync(string peNumber)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/plannedeventsapi/penumber/{Uri.EscapeDataString(peNumber)}");
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PlannedEvent>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching planned event by PE number {PENumber} from API", peNumber);
                return null;
            }
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
                // Since the API doesn't have a search endpoint, we'll fetch all planned events and filter client-side
                var allPlannedEvents = await GetPlannedEventsAsync();
                var query = allPlannedEvents.AsQueryable();

                // Apply search filters
                if (!string.IsNullOrEmpty(searchString))
                {
                    switch (searchType?.ToLower())
                    {
                        case "customer":
                            query = query.Where(p => p.Customer != null &&
                                p.Customer.Contains(searchString, StringComparison.OrdinalIgnoreCase));
                            break;
                        case "jobreference":
                            query = query.Where(p => p.JobReference != null &&
                                p.JobReference.Contains(searchString, StringComparison.OrdinalIgnoreCase));
                            break;
                        case "sonumber":
                            query = query.Where(p => p.SoNumber != null &&
                                p.SoNumber.Contains(searchString, StringComparison.OrdinalIgnoreCase));
                            break;
                        default: // peNumber
                            query = query.Where(p => p.PeNumber != null &&
                                p.PeNumber.Contains(searchString, StringComparison.OrdinalIgnoreCase));
                            break;
                    }
                }

                // Apply workgroup filter if specified
                if (!string.IsNullOrEmpty(workgroupName))
                {
                    query = query.Where(p => p.TaskWg != null &&
                        p.TaskWg.Equals(workgroupName, StringComparison.OrdinalIgnoreCase));
                }

                // Order and paginate results
                var orderedQuery = query.OrderByDescending(x => x.PeNumber);
                var totalCount = orderedQuery.Count();
                var items = orderedQuery.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
                
                return new PaginatedList<PlannedEvent>(items, totalCount, pageIndex, pageSize);
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

        /// <summary>
        /// Gets in-progress planned events for a specific user by userId (filtered in backend)
        /// </summary>
        public async Task<IEnumerable<PlannedEvent>> GetInProgressPlannedEventsByUserIdAsync(int userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/plannedeventsapi/inprogress/user/{userId}");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var events = JsonSerializer.Deserialize<IEnumerable<PlannedEvent>>(json, _jsonOptions);
                return events ?? new List<PlannedEvent>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching in-progress planned events for user {UserId} via API", userId);
                return new List<PlannedEvent>();
            }
        }

        /// <summary>
        /// Gets OLA violating planned events for a specific user by userId (filtered in backend)
        /// </summary>
        public async Task<IEnumerable<PlannedEvent>> GetOLAViolatingPlannedEventsByUserIdAsync(int userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/plannedeventsapi/ola-violating/user/{userId}");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var events = JsonSerializer.Deserialize<IEnumerable<PlannedEvent>>(json, _jsonOptions);
                return events ?? new List<PlannedEvent>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching OLA violating planned events for user {UserId} via API", userId);
                return new List<PlannedEvent>();
            }
        }

        /// <summary>
        /// Gets urgent planned events for a specific user by userId (filtered in backend)
        /// </summary>
        public async Task<IEnumerable<PlannedEvent>> GetUrgentPlannedEventsByUserIdAsync(int userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/plannedeventsapi/urgent/user/{userId}");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var events = JsonSerializer.Deserialize<IEnumerable<PlannedEvent>>(json, _jsonOptions);
                return events ?? new List<PlannedEvent>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching urgent planned events for user {UserId} via API", userId);
                return new List<PlannedEvent>();
            }
        }

        /// <summary>
        /// Gets hold planned events for a specific user by userId (filtered in backend)
        /// </summary>
        public async Task<IEnumerable<PlannedEvent>> GetHoldPlannedEventsByUserIdAsync(int userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/plannedeventsapi/hold/user/{userId}");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var events = JsonSerializer.Deserialize<IEnumerable<PlannedEvent>>(json, _jsonOptions);
                return events ?? new List<PlannedEvent>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching hold planned events for user {UserId} via API", userId);
                return new List<PlannedEvent>();
            }
        }

        /// <summary>
        /// Gets in-progress count for a specific user by userId (filtered in backend)
        /// </summary>
        public async Task<int> GetInProgressCountByUserIdAsync(int userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/plannedeventsapi/inprogress/user/{userId}/count");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var count = JsonSerializer.Deserialize<int>(json, _jsonOptions);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching in-progress count for user {UserId} via API", userId);
                return 0;
            }
        }

        /// <summary>
        /// Gets OLA violating count for a specific user by userId (filtered in backend)
        /// </summary>
        public async Task<int> GetOLAViolatingCountByUserIdAsync(int userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/plannedeventsapi/ola-violating/user/{userId}/count");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var count = JsonSerializer.Deserialize<int>(json, _jsonOptions);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching OLA violating count for user {UserId} via API", userId);
                return 0;
            }
        }

        /// <summary>
        /// Gets urgent count for a specific user by userId (filtered in backend)
        /// </summary>
        public async Task<int> GetUrgentCountByUserIdAsync(int userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/plannedeventsapi/urgent/user/{userId}/count");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var count = JsonSerializer.Deserialize<int>(json, _jsonOptions);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching urgent count for user {UserId} via API", userId);
                return 0;
            }
        }

        /// <summary>
        /// Gets hold count for a specific user by userId (filtered in backend)
        /// </summary>
        public async Task<int> GetHoldCountByUserIdAsync(int userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/plannedeventsapi/hold/user/{userId}/count");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var count = JsonSerializer.Deserialize<int>(json, _jsonOptions);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching hold count for user {UserId} via API", userId);
                return 0;
            }
        }

        public async Task<IEnumerable<PlannedEvent>> GetInProgressPlannedEventsAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false, bool canViewAll = false)
        {
            try
            {
                // Get PE numbers that have OLA violating tasks
                var response = await _httpClient.GetAsync("api/petasksapi/ola-violations");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var violatingTasks = JsonSerializer.Deserialize<IEnumerable<PETask>>(json, _jsonOptions) ?? new List<PETask>();
                var violatingPENumbers = violatingTasks
                    .Where(t => !string.IsNullOrEmpty(t.PENumber))
                    .Select(t => t.PENumber)
                    .Distinct()
                    .ToList();

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
                var response = await _httpClient.GetAsync("api/petasksapi/ola-violations");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var violatingTasks = JsonSerializer.Deserialize<IEnumerable<PETask>>(json, _jsonOptions) ?? new List<PETask>();
                var violatingPENumbers = violatingTasks
                    .Where(t => !string.IsNullOrEmpty(t.PENumber))
                    .Select(t => t.PENumber)
                    .Distinct()
                    .ToList();

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
                var response = await _httpClient.GetAsync("api/petasksapi/ola-violations");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var violatingTasks = JsonSerializer.Deserialize<IEnumerable<PETask>>(json, _jsonOptions) ?? new List<PETask>();
                var violatingPENumbers = violatingTasks
                    .Where(t => !string.IsNullOrEmpty(t.PENumber))
                    .Select(t => t.PENumber)
                    .Distinct()
                    .ToList();

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

        public async Task<IEnumerable<PlannedEvent>> GetInProgressPlannedEventsForMultiWorkgroupAsync(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds, bool hasDrawFiberAccess)
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
                
                var response = await _httpClient.PostAsync("api/plannedeventsapi/inprogress-records-multi-workgroup", content);
                response.EnsureSuccessStatusCode();
                
                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<IEnumerable<PlannedEvent>>(responseJson, _jsonOptions);
                return result ?? new List<PlannedEvent>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting in-progress records for multi workgroup via API");
                return new List<PlannedEvent>();
            }
        }

        public async Task<IEnumerable<PlannedEvent>> GetUrgentPlannedEventsForMultiWorkgroupAsync(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds, bool hasDrawFiberAccess)
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
                
                var response = await _httpClient.PostAsync("api/plannedeventsapi/urgent-records-multi-workgroup", content);
                response.EnsureSuccessStatusCode();
                
                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<IEnumerable<PlannedEvent>>(responseJson, _jsonOptions);
                return result ?? new List<PlannedEvent>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting urgent records for multi workgroup via API");
                return new List<PlannedEvent>();
            }
        }

        public async Task<IEnumerable<PlannedEvent>> GetHoldPlannedEventsForMultiWorkgroupAsync(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds, bool hasDrawFiberAccess)
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
                
                var response = await _httpClient.PostAsync("api/plannedeventsapi/hold-records-multi-workgroup", content);
                response.EnsureSuccessStatusCode();
                
                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<IEnumerable<PlannedEvent>>(responseJson, _jsonOptions);
                return result ?? new List<PlannedEvent>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hold records for multi workgroup via API");
                return new List<PlannedEvent>();
            }
        }

        public async Task<IEnumerable<PlannedEvent>> GetOLAViolatingPlannedEventsForMultiWorkgroupAsync(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds, bool hasDrawFiberAccess)
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
                
                var response = await _httpClient.PostAsync("api/plannedeventsapi/ola-violating-records-multi-workgroup", content);
                response.EnsureSuccessStatusCode();
                
                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<IEnumerable<PlannedEvent>>(responseJson, _jsonOptions);
                return result ?? new List<PlannedEvent>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OLA violating records for multi workgroup via API");
                return new List<PlannedEvent>();
            }
        }

        public async Task<List<string>> GetDistinctCustomersAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/plannedeventsapi/distinct-customers");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var customers = JsonSerializer.Deserialize<List<string>>(json, _jsonOptions);
                
                return customers ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching distinct customers from API");
                return new List<string>();
            }
        }

        public async Task<IEnumerable<PlannedEvent>> GetSalesInProgressRecordsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/plannedeventsapi/sales-inprogress-records");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var records = JsonSerializer.Deserialize<IEnumerable<PlannedEvent>>(json, _jsonOptions);
                
                return records ?? new List<PlannedEvent>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching sales in-progress records from API");
                return new List<PlannedEvent>();
            }
        }

        public async Task<IEnumerable<PlannedEvent>> GetSalesHoldRecordsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/plannedeventsapi/sales-hold-records");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var records = JsonSerializer.Deserialize<IEnumerable<PlannedEvent>>(json, _jsonOptions);
                
                return records ?? new List<PlannedEvent>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching sales hold records from API");
                return new List<PlannedEvent>();
            }
        }

        public async Task<IEnumerable<PlannedEvent>> GetSalesUrgentRecordsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/plannedeventsapi/sales-urgent-records");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var records = JsonSerializer.Deserialize<IEnumerable<PlannedEvent>>(json, _jsonOptions);
                
                return records ?? new List<PlannedEvent>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching sales urgent records from API");
                return new List<PlannedEvent>();
            }
        }

        public async Task<IEnumerable<PlannedEvent>> GetSalesOLAViolateRecordsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/plannedeventsapi/sales-ola-violate-records");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var records = JsonSerializer.Deserialize<IEnumerable<PlannedEvent>>(json, _jsonOptions);
                
                return records ?? new List<PlannedEvent>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching sales OLA violate records from API");
                return new List<PlannedEvent>();
            }
        }

        public async Task<int> GetSalesInProgressCountAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/plannedeventsapi/sales-inprogress-count");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var count = JsonSerializer.Deserialize<int>(json, _jsonOptions);
                
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching sales in-progress count from API");
                return 0;
            }
        }

        public async Task<int> GetSalesHoldCountAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/plannedeventsapi/sales-hold-count");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var count = JsonSerializer.Deserialize<int>(json, _jsonOptions);
                
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching sales hold count from API");
                return 0;
            }
        }

        public async Task<int> GetSalesUrgentCountAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/plannedeventsapi/sales-urgent-count");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var count = JsonSerializer.Deserialize<int>(json, _jsonOptions);
                
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching sales urgent count from API");
                return 0;
            }
        }

        public async Task<int> GetSalesOLAViolateCountAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/plannedeventsapi/sales-ola-violate-count");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var count = JsonSerializer.Deserialize<int>(json, _jsonOptions);
                
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching sales OLA violate count from API");
                return 0;
            }
        }

        public async Task<PaginatedList<PlannedEvent>> SearchPlannedEventsForUserAsync(string searchType, string searchString, int userId, int pageIndex, int pageSize)
        {
            try
            {
                var request = new
                {
                    UserId = userId,
                    SearchType = searchType,
                    SearchString = searchString,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                };

                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("api/plannedeventsapi/search-user-paginated", content);
                response.EnsureSuccessStatusCode();
                
                var responseJson = await response.Content.ReadAsStringAsync();
                
                // Deserialize the response object
                using var document = JsonDocument.Parse(responseJson);
                var root = document.RootElement;
                
                // Extract the pagination info
                var pageIdx = root.GetProperty("pageIndex").GetInt32();
                var totalPages = root.GetProperty("totalPages").GetInt32();
                var totalCount = root.GetProperty("totalCount").GetInt32();
                
                // Extract and deserialize the items
                var itemsJson = root.GetProperty("items").GetRawText();
                var items = JsonSerializer.Deserialize<List<PlannedEvent>>(itemsJson, _jsonOptions) ?? new List<PlannedEvent>();
                
                // Create a new PaginatedList with the extracted data
                return new PaginatedList<PlannedEvent>(items, totalCount, pageIdx, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching planned events for user {UserId} via API", userId);
                return new PaginatedList<PlannedEvent>(new List<PlannedEvent>(), 0, pageIndex, pageSize);
            }
        }
    }
}
