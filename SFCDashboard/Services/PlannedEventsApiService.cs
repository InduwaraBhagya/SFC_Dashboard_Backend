using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDashboard.Models;

namespace SFCDashboard.Services
{
    public class PlannedEventsApiService : IPlannedEventsApiService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PlannedEventsApiService> _logger;

        public PlannedEventsApiService(ApplicationDbContext context, ILogger<PlannedEventsApiService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PlannedEvent?> GetPlannedEventAsync(int id)
        {
            try
            {
                return await _context.PlannedEvents.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting planned event with id {Id}", id);
                return null;
            }
        }

        public async Task<PlannedEvent?> GetPlannedEventByIdAsync(int? id)
        {
            if (id == null) return null;
            
            try
            {
                return await _context.PlannedEvents
                    .FirstOrDefaultAsync(m => m.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting planned event with id {Id}", id);
                return null;
            }
        }

        public async Task<IEnumerable<PlannedEvent>> GetPlannedEventsAsync()
        {
            try
            {
                return await _context.PlannedEvents.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all planned events");
                return new List<PlannedEvent>();
            }
        }

        public async Task<IEnumerable<PlannedEvent>> GetPlannedEventsByWorkgroupAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false)
        {
            try
            {
                var query = _context.PlannedEvents.AsQueryable();

                if (workgroupNames.Any())
                {
                    if (hasDrawFiberAccess)
                    {
                        query = query.Where(p =>
                            p.TaskWg != null && (
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName))
                            || (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
                            || _context.PETasks.Any(t => t.PENumber == p.PeNumber && t.Task.Trim().ToLower() == "draw fiber")
                            )
                        );
                    }
                    else
                    {
                        query = query.Where(p => p.TaskWg != null &&
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName)));
                    }
                }

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting planned events by workgroup");
                return new List<PlannedEvent>();
            }
        }

        public async Task<IEnumerable<PlannedEvent>> GetPlannedEventsByWorkgroupIdsAsync(List<int> workgroupIds, bool hasDrawFiberAccess = false)
        {
            try
            {
                var workgroupNames = await _context.WorkGroups
                    .Where(w => workgroupIds.Contains(w.Id))
                    .Select(w => w.Name)
                    .ToListAsync();

                return await GetPlannedEventsByWorkgroupAsync(workgroupNames, hasDrawFiberAccess);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting planned events by workgroup IDs");
                return new List<PlannedEvent>();
            }
        }

        public async Task<IEnumerable<PlannedEvent>> GetInProgressPlannedEventsAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false)
        {
            try
            {
                var violatingPENumbers = await _context.PETasks
                    .Where(t => t.IsOLAViolate)
                    .Select(t => t.PENumber)
                    .Distinct()
                    .ToListAsync();

                var query = _context.PlannedEvents
                    .Where(p =>
                        (p.PEStatus == "ongoing" || p.PEStatus == "PENDING_URGENT_CONFIRMATION") &&
                        !p.IsHold &&
                        !violatingPENumbers.Contains(p.PeNumber))
                    .AsNoTracking();

                if (workgroupNames.Any())
                {
                    if (hasDrawFiberAccess)
                    {
                        query = query.Where(p =>
                            p.TaskWg != null && (
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName))
                            || (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
                            || _context.PETasks.Any(t => t.PENumber == p.PeNumber && t.Task.Trim().ToLower() == "draw fiber")
                            )
                        );
                    }
                    else
                    {
                        query = query.Where(p => p.TaskWg != null &&
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName)));
                    }
                }

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting in-progress planned events");
                return new List<PlannedEvent>();
            }
        }

        public async Task<IEnumerable<PlannedEvent>> GetUrgentPlannedEventsAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false)
        {
            try
            {
                var violatingPENumbers = await _context.PETasks
                    .Where(t => t.IsOLAViolate)
                    .Select(t => t.PENumber)
                    .Distinct()
                    .ToListAsync();

                var query = _context.PlannedEvents
                    .Where(p =>
                        p.PEStatus == "urgent" &&
                        !p.IsHold &&
                        !violatingPENumbers.Contains(p.PeNumber))
                    .AsNoTracking();

                if (workgroupNames.Any())
                {
                    if (hasDrawFiberAccess)
                    {
                        query = query.Where(p =>
                            p.TaskWg != null && (
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName))
                            || (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
                            || _context.PETasks.Any(t => t.PENumber == p.PeNumber && t.Task.Trim().ToLower() == "draw fiber")
                            )
                        );
                    }
                    else
                    {
                        query = query.Where(p => p.TaskWg != null &&
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName)));
                    }
                }

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting urgent planned events");
                return new List<PlannedEvent>();
            }
        }

        public async Task<IEnumerable<PlannedEvent>> GetHoldPlannedEventsAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false)
        {
            try
            {
                var query = _context.PlannedEvents
                    .Where(p => p.IsHold)
                    .AsNoTracking();

                if (workgroupNames.Any())
                {
                    if (hasDrawFiberAccess)
                    {
                        query = query.Where(p =>
                            p.TaskWg != null && (
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName))
                            || (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
                            || _context.PETasks.Any(t => t.PENumber == p.PeNumber && t.Task.Trim().ToLower() == "draw fiber")
                            )
                        );
                    }
                    else
                    {
                        query = query.Where(p => p.TaskWg != null &&
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName)));
                    }
                }

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hold planned events");
                return new List<PlannedEvent>();
            }
        }

        public async Task<IEnumerable<PlannedEvent>> GetOLAViolatingPlannedEventsAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false)
        {
            try
            {
                var violatingPENumbers = await _context.PETasks
                    .Where(t => t.IsOLAViolate)
                    .Select(t => t.PENumber)
                    .Distinct()
                    .ToListAsync();

                var query = _context.PlannedEvents
                    .Where(p => violatingPENumbers.Contains(p.PeNumber) && !p.IsHold)
                    .AsNoTracking();

                if (workgroupNames.Any())
                {
                    if (hasDrawFiberAccess)
                    {
                        query = query.Where(p =>
                            p.TaskWg != null && (
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName))
                            || (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
                            || _context.PETasks.Any(t => t.PENumber == p.PeNumber && t.Task.Trim().ToLower() == "draw fiber")
                            )
                        );
                    }
                    else
                    {
                        query = query.Where(p => p.TaskWg != null &&
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName)));
                    }
                }

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OLA violating planned events");
                return new List<PlannedEvent>();
            }
        }

        public async Task<IEnumerable<PlannedEvent>> GetPendingUrgentRequestsAsync(int take = 10)
        {
            try
            {
                _logger.LogInformation("Getting pending urgent requests, take: {take}", take);
                
                return await _context.PlannedEvents
                    .Where(p => p.PEStatus == "PENDING_URGENT_CONFIRMATION")
                    .OrderByDescending(p => p.PECreatedDate)
                    .Take(take)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending urgent requests");
                return new List<PlannedEvent>();
            }
        }

        public async Task<IEnumerable<PlannedEvent>> SearchPlannedEventsAsync(string searchType, string searchValue, List<string> workgroupNames, bool hasDrawFiberAccess = false)
        {
            try
            {
                var query = _context.PlannedEvents.AsQueryable();

                // Apply workgroup filtering
                if (workgroupNames.Any())
                {
                    if (hasDrawFiberAccess)
                    {
                        query = query.Where(p =>
                            p.TaskWg != null && (
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName))
                            || (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
                            || _context.PETasks.Any(t => t.PENumber == p.PeNumber && t.Task.Trim().ToLower() == "draw fiber")
                            )
                        );
                    }
                    else
                    {
                        query = query.Where(p => p.TaskWg != null &&
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName)));
                    }
                }

                // Apply search filters
                if (!string.IsNullOrEmpty(searchValue))
                {
                    switch (searchType?.ToLower())
                    {
                        case "customer":
                            query = query.Where(p => p.Customer != null &&
                                EF.Functions.Like(p.Customer, $"%{searchValue}%"));
                            break;
                        case "jobreference":
                            query = query.Where(p => p.JobReference != null &&
                                EF.Functions.Like(p.JobReference, $"%{searchValue}%"));
                            break;
                        case "sonumber":
                            query = query.Where(p => p.SoNumber != null &&
                                EF.Functions.Like(p.SoNumber, $"%{searchValue}%"));
                            break;
                        default: // peNumber
                            query = query.Where(p => p.PeNumber != null &&
                                EF.Functions.Like(p.PeNumber, $"%{searchValue}%"));
                            break;
                    }
                }

                return await query
                    .OrderByDescending(p => p.PECreatedDate)
                    .ThenBy(p => p.PeNumber)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching planned events");
                return new List<PlannedEvent>();
            }
        }

        public async Task<IEnumerable<PlannedEvent>> GetPlannedEventsBySalesWorkgroupAsync(List<string> salesWorkgroups, List<string> assignedCustomers, bool canViewAll)
        {
            try
            {
                var query = _context.PlannedEvents.AsQueryable();

                if (!canViewAll)
                {
                    // Apply workgroup filtering first
                    query = query.Where(p => salesWorkgroups.Any(wg =>
                        (p.TaskWg != null && p.TaskWg.Contains(wg)) ||
                        (p.SectionHandledBy != null && p.SectionHandledBy.Contains(wg))
                    ));

                    // If user has assigned customers, further filter by those customers
                    if (assignedCustomers.Any())
                    {
                        query = query.Where(p => p.Customer != null && assignedCustomers.Contains(p.Customer));
                    }
                }
                else
                {
                    // For users with ViewAll permission, still apply customer filtering if they have assigned customers
                    if (assignedCustomers.Any())
                    {
                        query = query.Where(p => p.Customer != null && assignedCustomers.Contains(p.Customer));
                    }
                }

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting planned events by sales workgroup");
                return new List<PlannedEvent>();
            }
        }

        public async Task<PlannedEvent?> CreatePlannedEventAsync(PlannedEvent plannedEvent)
        {
            try
            {
                _context.Add(plannedEvent);
                await _context.SaveChangesAsync();
                return plannedEvent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating planned event");
                return null;
            }
        }

        public async Task<PlannedEvent?> UpdatePlannedEventAsync(PlannedEvent plannedEvent)
        {
            try
            {
                _context.Update(plannedEvent);
                await _context.SaveChangesAsync();
                return plannedEvent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating planned event");
                return null;
            }
        }

        public async Task<bool> DeletePlannedEventAsync(int id)
        {
            try
            {
                var plannedEvent = await _context.PlannedEvents.FindAsync(id);
                if (plannedEvent != null)
                {
                    _context.PlannedEvents.Remove(plannedEvent);
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting planned event with id {Id}", id);
                return false;
            }
        }

        public async Task<bool> PlannedEventExistsAsync(int id)
        {
            try
            {
                return await _context.PlannedEvents.AnyAsync(e => e.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if planned event exists with id {Id}", id);
                return false;
            }
        }

        public async Task<int> GetUrgentCountAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false)
        {
            try
            {
                var events = await GetUrgentPlannedEventsAsync(workgroupNames, hasDrawFiberAccess);
                return events.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting urgent count");
                return 0;
            }
        }

        public async Task<int> GetInProgressCountAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false)
        {
            try
            {
                var events = await GetInProgressPlannedEventsAsync(workgroupNames, hasDrawFiberAccess);
                return events.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting in-progress count");
                return 0;
            }
        }

        public async Task<int> GetOLAViolateCountAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false)
        {
            try
            {
                var events = await GetOLAViolatingPlannedEventsAsync(workgroupNames, hasDrawFiberAccess);
                return events.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OLA violate count");
                return 0;
            }
        }

        public async Task<int> GetHoldCountAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false)
        {
            try
            {
                var events = await GetHoldPlannedEventsAsync(workgroupNames, hasDrawFiberAccess);
                return events.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hold count");
                return 0;
            }
        }

        public async Task<int> GetUrgentCountForMultiWorkgroupAsync(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds)
        {
            try
            {
                var workgroupIdsToUse = (selectedWorkgroupIds != null && selectedWorkgroupIds.Count > 0) ? selectedWorkgroupIds : userWorkgroupIds;
                var workgroupNames = await _context.WorkGroups
                    .Where(w => workgroupIdsToUse.Contains(w.Id))
                    .Select(w => w.Name)
                    .ToListAsync();

                return await GetUrgentCountAsync(workgroupNames);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting urgent count for multi workgroup");
                return 0;
            }
        }

        public async Task<int> GetInProgressCountForMultiWorkgroupAsync(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds)
        {
            try
            {
                var workgroupIdsToUse = (selectedWorkgroupIds != null && selectedWorkgroupIds.Count > 0) ? selectedWorkgroupIds : userWorkgroupIds;
                var workgroupNames = await _context.WorkGroups
                    .Where(w => workgroupIdsToUse.Contains(w.Id))
                    .Select(w => w.Name)
                    .ToListAsync();

                return await GetInProgressCountAsync(workgroupNames);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting in-progress count for multi workgroup");
                return 0;
            }
        }

        public async Task<int> GetOLAViolateCountForMultiWorkgroupAsync(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds)
        {
            try
            {
                var workgroupIdsToUse = (selectedWorkgroupIds != null && selectedWorkgroupIds.Count > 0) ? selectedWorkgroupIds : userWorkgroupIds;
                var workgroupNames = await _context.WorkGroups
                    .Where(w => workgroupIdsToUse.Contains(w.Id))
                    .Select(w => w.Name)
                    .ToListAsync();

                return await GetOLAViolateCountAsync(workgroupNames);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OLA violate count for multi workgroup");
                return 0;
            }
        }

        public async Task<int> GetHoldCountForMultiWorkgroupAsync(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds)
        {
            try
            {
                var workgroupIdsToUse = (selectedWorkgroupIds != null && selectedWorkgroupIds.Count > 0) ? selectedWorkgroupIds : userWorkgroupIds;
                var workgroupNames = await _context.WorkGroups
                    .Where(w => workgroupIdsToUse.Contains(w.Id))
                    .Select(w => w.Name)
                    .ToListAsync();

                return await GetHoldCountAsync(workgroupNames);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hold count for multi workgroup");
                return 0;
            }
        }

        public async Task<PaginatedList<PlannedEvent>> SearchPlannedEventsAsync(string searchType, string searchValue, string? workgroupName, bool hasDrawFiberAccess, int pageIndex, int pageSize)
        {
            try
            {
                _logger.LogInformation("Searching planned events: type={type}, value={value}, workgroup={workgroup}, drawFiber={drawFiber}", 
                    searchType, searchValue, workgroupName, hasDrawFiberAccess);

                var query = _context.PlannedEvents.AsQueryable();

                // Apply workgroup filter if specified
                if (!string.IsNullOrEmpty(workgroupName))
                {
                    if (hasDrawFiberAccess)
                    {
                        query = query.Where(p =>
                            p.TaskWg != null && (
                            p.TaskWg.Contains(workgroupName)
                            || (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
                            || _context.PETasks.Any(t => t.PENumber == p.PeNumber && !string.IsNullOrEmpty(t.Task) && t.Task.Trim().ToLower() == "draw fiber")
                            )
                        );
                    }
                    else
                    {
                        query = query.Where(p => p.TaskWg != null && p.TaskWg.Contains(workgroupName));
                    }
                }

                // Apply search filters based on type
                if (!string.IsNullOrEmpty(searchValue))
                {
                    switch (searchType)
                    {
                        case "customer":
                            query = query.Where(p => p.Customer != null &&
                                EF.Functions.Like(p.Customer, $"%{searchValue}%"));
                            break;
                        case "jobReference":
                            query = query.Where(p => p.JobReference != null &&
                                EF.Functions.Like(p.JobReference, $"%{searchValue}%"));
                            break;
                        case "soNumber":
                            query = query.Where(p => p.SoNumber != null &&
                                EF.Functions.Like(p.SoNumber, $"%{searchValue}%"));
                            break;
                        default: // peNumber
                            query = query.Where(p => p.PeNumber != null &&
                                EF.Functions.Like(p.PeNumber, $"%{searchValue}%"));
                            break;
                    }
                }

                query = query.OrderByDescending(p => p.PECreatedDate)
                            .ThenBy(p => p.PeNumber);

                return await PaginatedList<PlannedEvent>.CreateAsync(query.AsNoTracking(), pageIndex, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching planned events");
                return new PaginatedList<PlannedEvent>(new List<PlannedEvent>(), 0, pageIndex, pageSize);
            }
        }
    }
}
