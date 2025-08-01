using Microsoft.EntityFrameworkCore;
using SFCDashboard.Api.Data;
using SFCDashboard.Api.Models;

namespace SFCDashboard.Api.Services
{
    public class PlannedEventsApiService : IPlannedEventsApiService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PlannedEventsApiService> _logger;
        private readonly IUsersApiService _usersApiService;

        public PlannedEventsApiService(ApplicationDbContext context, ILogger<PlannedEventsApiService> logger, IUsersApiService usersApiService)
        {
            _context = context;
            _logger = logger;
            _usersApiService = usersApiService;
        }
        /// <summary>
        /// Gets in-progress planned events for a specific user (filters by workgroups and permissions)
        /// </summary>
        public async Task<IEnumerable<PlannedEvent>> GetInProgressPlannedEventsByUserIdAsync(int userId)
        {
            // Get user with workgroups and role/permissions
            var user = await _usersApiService.GetUserWithRoleAndWorkGroupsAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for in-progress planned events: {UserId}", userId);
                return new List<PlannedEvent>();
            }
            var workgroupNames = user.UserWorkGroups?.Select(uwg => uwg.WorkGroup.Name).ToList() ?? new List<string>();
            bool canViewAll = user.UserRole?.RolePermissions.Any(rp => rp.Permission.Name == "ViewAll") == true;
            bool hasDrawFiberAccess = user.UserWorkGroups?.Any(uwg => uwg.WorkGroup.Name.Equals("NET-PROJ-ACC-CABLE", StringComparison.OrdinalIgnoreCase)) == true;

            // Use the same logic as GetInProgressPlannedEventsAsync
            var violatingPENumbers = await _context.PETasks
                .Where(t => t.IsOLAViolate)
                .Select(t => t.PENumber)
                .Distinct()
                .ToListAsync();

            var query = _context.PlannedEvents
                .Where(p =>
                    (p.PEStatus == "ongoing" || p.PEStatus == "PENDING_URGENT_CONFIRMATION") &&
                    !p.IsHold &&
                    (p.PeNumber == null || !violatingPENumbers.Contains(p.PeNumber)))
                .AsNoTracking();

            if (!canViewAll && workgroupNames.Any())
            {
                if (hasDrawFiberAccess)
                {
                    query = query.Where(p =>
                        p.TaskWg != null && (
                        workgroupNames.Any(wgName => p.TaskWg.Contains(wgName))
                        || (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
                        )
                    );
                }
                else
                {
                    query = query.Where(p => p.TaskWg != null &&
                        workgroupNames.Any(wgName => p.TaskWg.Contains(wgName)));
                }
            }
            // If user has ViewAll permission, don't apply any workgroup filtering - return all records

            return await query.ToListAsync();
        }

        /// <summary>
        /// Gets OLA violating planned events for a specific user (filters by workgroups and permissions)
        /// </summary>
        public async Task<IEnumerable<PlannedEvent>> GetOLAViolatingPlannedEventsByUserIdAsync(int userId)
        {
            // Get user with workgroups and role/permissions
            var user = await _usersApiService.GetUserWithRoleAndWorkGroupsAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for OLA violating planned events: {UserId}", userId);
                return new List<PlannedEvent>();
            }
            var workgroupNames = user.UserWorkGroups?.Select(uwg => uwg.WorkGroup.Name).ToList() ?? new List<string>();
            bool canViewAll = user.UserRole?.RolePermissions.Any(rp => rp.Permission.Name == "ViewAll") == true;
            bool hasDrawFiberAccess = user.UserWorkGroups?.Any(uwg => uwg.WorkGroup.Name.Equals("NET-PROJ-ACC-CABLE", StringComparison.OrdinalIgnoreCase)) == true;

            // Use the same logic as GetOLAViolatingPlannedEventsAsync
            var violatingPENumbers = await _context.PETasks
                .Where(t => t.IsOLAViolate)
                .Select(t => t.PENumber)
                .Distinct()
                .ToListAsync();

            var query = _context.PlannedEvents
                .Where(p => p.PeNumber != null && violatingPENumbers.Contains(p.PeNumber) && !p.IsHold)
                .AsNoTracking();

            if (!canViewAll && workgroupNames.Any())
            {
                if (hasDrawFiberAccess)
                {
                    query = query.Where(p =>
                        p.TaskWg != null && (
                        workgroupNames.Any(wgName => p.TaskWg.Contains(wgName))
                        || (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
                        )
                    );
                }
                else
                {
                    query = query.Where(p => p.TaskWg != null &&
                        workgroupNames.Any(wgName => p.TaskWg.Contains(wgName)));
                }
            }
            // If user has ViewAll permission, don't apply any workgroup filtering - return all records

            return await query.ToListAsync();
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

        public async Task<IEnumerable<PlannedEvent>> GetPlannedEventsByWorkgroupAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false, bool canViewAll = false)
        {
            try
            {
                var query = _context.PlannedEvents.AsQueryable();

                // If user has ViewAll permission, don't filter by workgroup unless specifically requested
                if (!canViewAll && workgroupNames.Any())
                {
                    if (hasDrawFiberAccess)
                    {
                        query = query.Where(p =>
                            p.TaskWg != null && (
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName))
                            || (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
                            )
                        );
                    }
                    else
                    {
                        query = query.Where(p => p.TaskWg != null &&
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName)));
                    }
                }
                else if (canViewAll && workgroupNames.Any())
                {
                    // ViewAll users can still filter by specific workgroups if requested
                    if (hasDrawFiberAccess)
                    {
                        query = query.Where(p =>
                            p.TaskWg != null && (
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName))
                            || (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
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

        public async Task<IEnumerable<PlannedEvent>> GetPlannedEventsByWorkgroupIdsAsync(List<int> workgroupIds, bool hasDrawFiberAccess = false, bool canViewAll = false)
        {
            try
            {
                var workgroupNames = await _context.WorkGroups
                    .Where(w => workgroupIds.Contains(w.Id))
                    .Select(w => w.Name)
                    .ToListAsync();

                return await GetPlannedEventsByWorkgroupAsync(workgroupNames, hasDrawFiberAccess, canViewAll);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting planned events by workgroup IDs");
                return new List<PlannedEvent>();
            }
        }

        public async Task<IEnumerable<PlannedEvent>> GetInProgressPlannedEventsAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false, bool canViewAll = false)
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
                        (p.PeNumber == null || !violatingPENumbers.Contains(p.PeNumber)))
                    .AsNoTracking();

                // If user has ViewAll permission, don't filter by workgroup unless specifically requested
                if (!canViewAll && workgroupNames.Any())
                {
                    if (hasDrawFiberAccess)
                    {
                        query = query.Where(p =>
                            p.TaskWg != null && (
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName))
                            || (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
                            )
                        );
                    }
                    else
                    {
                        query = query.Where(p => p.TaskWg != null &&
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName)));
                    }
                }
                else if (canViewAll && workgroupNames.Any())
                {
                    // ViewAll users can still filter by specific workgroups if requested
                    if (hasDrawFiberAccess)
                    {
                        query = query.Where(p =>
                            p.TaskWg != null && (
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName))
                            || (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
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

        public async Task<IEnumerable<PlannedEvent>> GetUrgentPlannedEventsAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false, bool canViewAll = false)
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
                        (p.PeNumber == null || !violatingPENumbers.Contains(p.PeNumber)))
                    .AsNoTracking();

                // If user has ViewAll permission, don't filter by workgroup unless specifically requested
                if (!canViewAll && workgroupNames.Any())
                {
                    if (hasDrawFiberAccess)
                    {
                        query = query.Where(p =>
                            p.TaskWg != null && (
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName))
                            || (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
                            )
                        );
                    }
                    else
                    {
                        query = query.Where(p => p.TaskWg != null &&
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName)));
                    }
                }
                else if (canViewAll && workgroupNames.Any())
                {
                    // ViewAll users can still filter by specific workgroups if requested
                    if (hasDrawFiberAccess)
                    {
                        query = query.Where(p =>
                            p.TaskWg != null && (
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName))
                            || (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
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

        public async Task<IEnumerable<PlannedEvent>> GetUrgentPlannedEventsByUserIdAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Getting urgent planned events for user {userId}", userId);

                // Get user with workgroups and permissions
                var user = await _usersApiService.GetUserWithRoleAndWorkGroupsAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User {userId} not found", userId);
                    return new List<PlannedEvent>();
                }

                // Check if user has ViewAll permission
                bool canViewAll = user.UserRole?.RolePermissions
                    ?.Any(rp => rp.Permission?.Name == "ViewAll") == true;

                // Get user's workgroup names
                var userWorkgroupNames = user.UserWorkGroups?.Select(uw => uw.WorkGroup?.Name)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Cast<string>()
                    .ToList() ?? new List<string>();

                // Check if user has Draw Fiber access
                bool hasDrawFiberAccess = userWorkgroupNames.Any(wg =>
                    wg.Equals("NET-PROJ-ACC-CABLE", StringComparison.OrdinalIgnoreCase));

                _logger.LogInformation("User {userId}: CanViewAll={canViewAll}, Workgroups={workgroups}, DrawFiberAccess={drawFiberAccess}",
                    userId, canViewAll, string.Join(", ", userWorkgroupNames), hasDrawFiberAccess);

                // Get OLA violating PE numbers to exclude
                var violatingPENumbers = await _context.PETasks
                    .Where(t => t.IsOLAViolate)
                    .Select(t => t.PENumber)
                    .Distinct()
                    .ToListAsync();

                var query = _context.PlannedEvents
                    .Where(p =>
                        p.PEStatus == "urgent" &&
                        !p.IsHold &&
                        (p.PeNumber == null || !violatingPENumbers.Contains(p.PeNumber)))
                    .AsNoTracking();

                // Apply workgroup filtering based on user permissions
                if (!canViewAll && userWorkgroupNames.Any())
                {
                    if (hasDrawFiberAccess)
                    {
                        query = query.Where(p =>
                            p.TaskWg != null && (
                            userWorkgroupNames.Any(wgName => p.TaskWg.Contains(wgName))
                            || (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
                            )
                        );
                    }
                    else
                    {
                        query = query.Where(p => p.TaskWg != null &&
                            userWorkgroupNames.Any(wgName => p.TaskWg.Contains(wgName)));
                    }
                }
                // If user has ViewAll permission, return all urgent records without workgroup filtering

                var result = await query.ToListAsync();
                _logger.LogInformation("Retrieved {count} urgent planned events for user {userId}", result.Count, userId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting urgent planned events for user {userId}", userId);
                return new List<PlannedEvent>();
            }
        }

        public async Task<IEnumerable<PlannedEvent>> GetHoldPlannedEventsAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false, bool canViewAll = false)
        {
            try
            {
                var query = _context.PlannedEvents
                    .Where(p => p.IsHold)
                    .AsNoTracking();

                // If user has ViewAll permission, don't filter by workgroup unless specifically requested
                if (!canViewAll && workgroupNames.Any())
                {
                    if (hasDrawFiberAccess)
                    {
                        query = query.Where(p =>
                            p.TaskWg != null && (
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName))
                            || (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
                            )
                        );
                    }
                    else
                    {
                        query = query.Where(p => p.TaskWg != null &&
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName)));
                    }
                }
                else if (canViewAll && workgroupNames.Any())
                {
                    // ViewAll users can still filter by specific workgroups if requested
                    if (hasDrawFiberAccess)
                    {
                        query = query.Where(p =>
                            p.TaskWg != null && (
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName))
                            || (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
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

        public async Task<IEnumerable<PlannedEvent>> GetHoldPlannedEventsByUserIdAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Getting hold planned events for user {userId}", userId);

                // Get user with workgroups and permissions
                var user = await _usersApiService.GetUserWithRoleAndWorkGroupsAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User {userId} not found", userId);
                    return new List<PlannedEvent>();
                }

                // Check if user has ViewAll permission
                bool canViewAll = user.UserRole?.RolePermissions
                    ?.Any(rp => rp.Permission?.Name == "ViewAll") == true;

                // Get user's workgroup names
                var userWorkgroupNames = user.UserWorkGroups?.Select(uw => uw.WorkGroup?.Name)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Cast<string>()
                    .ToList() ?? new List<string>();

                // Check if user has Draw Fiber access
                bool hasDrawFiberAccess = userWorkgroupNames.Any(wg =>
                    wg.Equals("NET-PROJ-ACC-CABLE", StringComparison.OrdinalIgnoreCase));

                _logger.LogInformation("User {userId}: CanViewAll={canViewAll}, Workgroups={workgroups}, DrawFiberAccess={drawFiberAccess}",
                    userId, canViewAll, string.Join(", ", userWorkgroupNames), hasDrawFiberAccess);

                var query = _context.PlannedEvents
                    .Where(p => p.IsHold)
                    .AsNoTracking();

                // Apply workgroup filtering based on user permissions
                if (!canViewAll && userWorkgroupNames.Any())
                {
                    if (hasDrawFiberAccess)
                    {
                        query = query.Where(p =>
                            p.TaskWg != null && (
                            userWorkgroupNames.Any(wgName => p.TaskWg.Contains(wgName))
                            || (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
                            )
                        );
                    }
                    else
                    {
                        query = query.Where(p => p.TaskWg != null &&
                            userWorkgroupNames.Any(wgName => p.TaskWg.Contains(wgName)));
                    }
                }
                // If user has ViewAll permission, return all hold records without workgroup filtering

                var result = await query.ToListAsync();
                _logger.LogInformation("Retrieved {count} hold planned events for user {userId}", result.Count, userId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hold planned events for user {userId}", userId);
                return new List<PlannedEvent>();
            }
        }

        public async Task<IEnumerable<PlannedEvent>> GetOLAViolatingPlannedEventsAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false, bool canViewAll = false)
        {
            try
            {
                var violatingPENumbers = await _context.PETasks
                    .Where(t => t.IsOLAViolate)
                    .Select(t => t.PENumber)
                    .Distinct()
                    .ToListAsync();

                var query = _context.PlannedEvents
                    .Where(p => p.PeNumber != null && violatingPENumbers.Contains(p.PeNumber) && !p.IsHold)
                    .AsNoTracking();

                // If user has ViewAll permission, don't filter by workgroup unless specifically requested
                if (!canViewAll && workgroupNames.Any())
                {
                    if (hasDrawFiberAccess)
                    {
                        query = query.Where(p =>
                            p.TaskWg != null && (
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName))
                            || (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
                            )
                        );
                    }
                    else
                    {
                        query = query.Where(p => p.TaskWg != null &&
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName)));
                    }
                }
                else if (canViewAll && workgroupNames.Any())
                {
                    // ViewAll users can still filter by specific workgroups if requested
                    if (hasDrawFiberAccess)
                    {
                        query = query.Where(p =>
                            p.TaskWg != null && (
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName))
                            || (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
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

        public async Task<IEnumerable<PlannedEvent>> SearchPlannedEventsAsync(string searchType, string searchValue, List<string> workgroupNames, bool hasDrawFiberAccess = false, bool canViewAll = false)
        {
            try
            {
                var query = _context.PlannedEvents.AsQueryable();

                // Apply workgroup filtering - If user has ViewAll permission, don't filter by workgroup unless specifically requested
                if (!canViewAll && workgroupNames.Any())
                {
                    if (hasDrawFiberAccess)
                    {
                        query = query.Where(p =>
                            p.TaskWg != null && (
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName))
                            || (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
                            )
                        );
                    }
                    else
                    {
                        query = query.Where(p => p.TaskWg != null &&
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName)));
                    }
                }
                else if (canViewAll && workgroupNames.Any())
                {
                    // ViewAll users can still filter by specific workgroups if requested
                    if (hasDrawFiberAccess)
                    {
                        query = query.Where(p =>
                            p.TaskWg != null && (
                            workgroupNames.Any(wgName => p.TaskWg.Contains(wgName))
                            || (p.TaskName != null && p.TaskName.Trim().ToLower() == "draw fiber")
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

        public async Task<int> GetUrgentCountAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false, bool canViewAll = false)
        {
            try
            {
                var events = await GetUrgentPlannedEventsAsync(workgroupNames, hasDrawFiberAccess, canViewAll);
                return events.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting urgent count");
                return 0;
            }
        }

        public async Task<int> GetInProgressCountAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false, bool canViewAll = false)
        {
            try
            {
                var events = await GetInProgressPlannedEventsAsync(workgroupNames, hasDrawFiberAccess, canViewAll);
                return events.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting in-progress count");
                return 0;
            }
        }

        public async Task<int> GetOLAViolateCountAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false, bool canViewAll = false)
        {
            try
            {
                var events = await GetOLAViolatingPlannedEventsAsync(workgroupNames, hasDrawFiberAccess, canViewAll);
                return events.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OLA violate count");
                return 0;
            }
        }

        public async Task<int> GetHoldCountAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false, bool canViewAll = false)
        {
            try
            {
                var events = await GetHoldPlannedEventsAsync(workgroupNames, hasDrawFiberAccess, canViewAll);
                return events.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hold count");
                return 0;
            }
        }

        public async Task<int> GetUrgentCountForMultiWorkgroupAsync(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds, bool hasDrawFiberAccess = false)
        {
            try
            {
                var events = await GetUrgentPlannedEventsForMultiWorkgroupAsync(selectedWorkgroupIds, userWorkgroupIds, hasDrawFiberAccess);
                return events.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting urgent count for multi workgroup");
                return 0;
            }
        }

        public async Task<int> GetInProgressCountForMultiWorkgroupAsync(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds, bool hasDrawFiberAccess = false)
        {
            try
            {
                var events = await GetInProgressPlannedEventsForMultiWorkgroupAsync(selectedWorkgroupIds, userWorkgroupIds, hasDrawFiberAccess);
                return events.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting in-progress count for multi workgroup");
                return 0;
            }
        }

        public async Task<int> GetOLAViolateCountForMultiWorkgroupAsync(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds, bool hasDrawFiberAccess = false)
        {
            try
            {
                var events = await GetOLAViolatingPlannedEventsForMultiWorkgroupAsync(selectedWorkgroupIds, userWorkgroupIds, hasDrawFiberAccess);
                return events.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OLA violate count for multi workgroup");
                return 0;
            }
        }

        public async Task<int> GetHoldCountForMultiWorkgroupAsync(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds, bool hasDrawFiberAccess = false)
        {
            try
            {
                var events = await GetHoldPlannedEventsForMultiWorkgroupAsync(selectedWorkgroupIds, userWorkgroupIds, hasDrawFiberAccess);
                return events.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hold count for multi workgroup");
                return 0;
            }
        }

        public async Task<IEnumerable<PlannedEvent>> GetInProgressPlannedEventsForMultiWorkgroupAsync(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds, bool hasDrawFiberAccess = false)
        {
            try
            {
                var workgroupIdsToUse = (selectedWorkgroupIds != null && selectedWorkgroupIds.Count > 0) ? selectedWorkgroupIds : userWorkgroupIds;
                var workgroupNames = await _context.WorkGroups
                    .Where(w => workgroupIdsToUse.Contains(w.Id))
                    .Select(w => w.Name)
                    .ToListAsync();

                // Multi-workgroup methods should never use ViewAll permission
                return await GetInProgressPlannedEventsAsync(workgroupNames, hasDrawFiberAccess, canViewAll: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting in-progress planned events for multi workgroup");
                return new List<PlannedEvent>();
            }
        }

        public async Task<IEnumerable<PlannedEvent>> GetUrgentPlannedEventsForMultiWorkgroupAsync(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds, bool hasDrawFiberAccess = false)
        {
            try
            {
                var workgroupIdsToUse = (selectedWorkgroupIds != null && selectedWorkgroupIds.Count > 0) ? selectedWorkgroupIds : userWorkgroupIds;
                var workgroupNames = await _context.WorkGroups
                    .Where(w => workgroupIdsToUse.Contains(w.Id))
                    .Select(w => w.Name)
                    .ToListAsync();

                // Multi-workgroup methods should never use ViewAll permission
                return await GetUrgentPlannedEventsAsync(workgroupNames, hasDrawFiberAccess, canViewAll: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting urgent planned events for multi workgroup");
                return new List<PlannedEvent>();
            }
        }

        public async Task<IEnumerable<PlannedEvent>> GetHoldPlannedEventsForMultiWorkgroupAsync(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds, bool hasDrawFiberAccess = false)
        {
            try
            {
                var workgroupIdsToUse = (selectedWorkgroupIds != null && selectedWorkgroupIds.Count > 0) ? selectedWorkgroupIds : userWorkgroupIds;
                var workgroupNames = await _context.WorkGroups
                    .Where(w => workgroupIdsToUse.Contains(w.Id))
                    .Select(w => w.Name)
                    .ToListAsync();

                // Multi-workgroup methods should never use ViewAll permission
                return await GetHoldPlannedEventsAsync(workgroupNames, hasDrawFiberAccess, canViewAll: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hold planned events for multi workgroup");
                return new List<PlannedEvent>();
            }
        }

        public async Task<IEnumerable<PlannedEvent>> GetOLAViolatingPlannedEventsForMultiWorkgroupAsync(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds, bool hasDrawFiberAccess = false)
        {
            try
            {
                var workgroupIdsToUse = (selectedWorkgroupIds != null && selectedWorkgroupIds.Count > 0) ? selectedWorkgroupIds : userWorkgroupIds;
                var workgroupNames = await _context.WorkGroups
                    .Where(w => workgroupIdsToUse.Contains(w.Id))
                    .Select(w => w.Name)
                    .ToListAsync();

                // Multi-workgroup methods should never use ViewAll permission
                return await GetOLAViolatingPlannedEventsAsync(workgroupNames, hasDrawFiberAccess, canViewAll: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OLA violating planned events for multi workgroup");
                return new List<PlannedEvent>();
            }
        }

        // User-based count methods
        public async Task<int> GetInProgressCountByUserIdAsync(int userId)
        {
            try
            {
                var events = await GetInProgressPlannedEventsByUserIdAsync(userId);
                return events.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting in-progress count for user {userId}", userId);
                return 0;
            }
        }

        public async Task<int> GetOLAViolatingCountByUserIdAsync(int userId)
        {
            try
            {
                var events = await GetOLAViolatingPlannedEventsByUserIdAsync(userId);
                return events.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OLA violating count for user {userId}", userId);
                return 0;
            }
        }

        public async Task<int> GetUrgentCountByUserIdAsync(int userId)
        {
            try
            {
                var events = await GetUrgentPlannedEventsByUserIdAsync(userId);
                return events.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting urgent count for user {userId}", userId);
                return 0;
            }
        }

        public async Task<int> GetHoldCountByUserIdAsync(int userId)
        {
            try
            {
                var events = await GetHoldPlannedEventsByUserIdAsync(userId);
                return events.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hold count for user {userId}", userId);
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

        public async Task<PaginatedList<PlannedEvent>> SearchPlannedEventsAsync(string searchType, string searchValue, List<string> salesWorkgroups, bool hasDrawFiberAccess, int pageIndex, int pageSize)
        {
            try
            {
                _logger.LogInformation("Searching planned events for sales: type={type}, value={value}, workgroups={workgroups}, drawFiber={drawFiber}", 
                    searchType, searchValue, string.Join(",", salesWorkgroups), hasDrawFiberAccess);

                var query = _context.PlannedEvents.AsQueryable();

                // Apply sales workgroup filter - check both TaskWg and SectionHandledBy
                if (salesWorkgroups.Any())
                {
                    query = query.Where(p => salesWorkgroups.Any(wg =>
                        (p.TaskWg != null && p.TaskWg.Contains(wg)) ||
                        (p.SectionHandledBy != null && p.SectionHandledBy.Contains(wg))
                    ));
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
                _logger.LogError(ex, "Error searching planned events for sales workgroups");
                return new PaginatedList<PlannedEvent>(new List<PlannedEvent>(), 0, pageIndex, pageSize);
            }
        }

        /// <summary>
        /// Get sales in-progress records for a user (applies sales workgroup filtering)
        /// </summary>
        public async Task<IEnumerable<PlannedEvent>> GetSalesInProgressRecordsAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Getting sales in-progress records for user {userId}", userId);

                // Get user with workgroups and permissions
                var user = await _usersApiService.GetUserWithRoleAndWorkGroupsAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User {userId} not found", userId);
                    return new List<PlannedEvent>();
                }

                // Check if user has ViewAll permission
                bool canViewAll = user.UserRole?.RolePermissions
                    ?.Any(rp => rp.Permission?.Name == "ViewAll") == true;

                // Get user's sales workgroups (workgroups containing "SALES")
                var salesWorkgroups = user.UserWorkGroups?
                    .Where(uwg => uwg.WorkGroup?.Name?.Contains("SALES", StringComparison.OrdinalIgnoreCase) == true)
                    .Select(uwg => uwg.WorkGroup.Name)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Cast<string>()
                    .ToList() ?? new List<string>();

                // Get user's assigned customers
                var assignedCustomers = await _usersApiService.GetUserAssignedCustomersAsync(userId);

                _logger.LogInformation("User {userId}: CanViewAll={canViewAll}, SalesWorkgroups={salesWorkgroups}, AssignedCustomers={assignedCustomers}",
                    userId, canViewAll, string.Join(", ", salesWorkgroups), assignedCustomers.Count);

                // Get OLA violating PE numbers
                var violatingPENumbers = await _context.PETasks
                    .Where(t => t.IsOLAViolate)
                    .Select(t => t.PENumber)
                    .Distinct()
                    .ToListAsync();

                var query = _context.PlannedEvents
                    .Where(p =>
                        (p.PEStatus == "ongoing" || p.PEStatus == "PENDING_URGENT_CONFIRMATION") &&
                        !p.IsHold &&
                        p.PeNumber != null &&
                        !violatingPENumbers.Contains(p.PeNumber))
                    .AsQueryable();

                // Apply sales workgroup filtering
                query = await ApplySalesWorkgroupFilteringAsync(query, salesWorkgroups, assignedCustomers, canViewAll);

                var result = await query
                    .OrderByDescending(p => p.ServiceRequiredDate)
                    .AsNoTracking()
                    .ToListAsync();

                _logger.LogInformation("Retrieved {count} sales in-progress records for user {userId}", result.Count, userId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales in-progress records for user {userId}", userId);
                return new List<PlannedEvent>();
            }
        }

        /// <summary>
        /// Get sales hold records for a user (applies sales workgroup filtering)
        /// </summary>
        public async Task<IEnumerable<PlannedEvent>> GetSalesHoldRecordsAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Getting sales hold records for user {userId}", userId);

                // Get user with workgroups and permissions
                var user = await _usersApiService.GetUserWithRoleAndWorkGroupsAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User {userId} not found", userId);
                    return new List<PlannedEvent>();
                }

                // Check if user has ViewAll permission
                bool canViewAll = user.UserRole?.RolePermissions
                    ?.Any(rp => rp.Permission?.Name == "ViewAll") == true;

                // Get user's sales workgroups (workgroups containing "SALES")
                var salesWorkgroups = user.UserWorkGroups?
                    .Where(uwg => uwg.WorkGroup?.Name?.Contains("SALES", StringComparison.OrdinalIgnoreCase) == true)
                    .Select(uwg => uwg.WorkGroup.Name)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Cast<string>()
                    .ToList() ?? new List<string>();

                // Get user's assigned customers
                var assignedCustomers = await _usersApiService.GetUserAssignedCustomersAsync(userId);

                _logger.LogInformation("User {userId}: CanViewAll={canViewAll}, SalesWorkgroups={salesWorkgroups}, AssignedCustomers={assignedCustomers}",
                    userId, canViewAll, string.Join(", ", salesWorkgroups), assignedCustomers.Count);

                var query = _context.PlannedEvents
                    .Where(p => p.IsHold)
                    .AsQueryable();

                // Apply sales workgroup filtering
                query = await ApplySalesWorkgroupFilteringAsync(query, salesWorkgroups, assignedCustomers, canViewAll);

                var result = await query
                    .OrderByDescending(p => p.ServiceRequiredDate)
                    .AsNoTracking()
                    .ToListAsync();

                _logger.LogInformation("Retrieved {count} sales hold records for user {userId}", result.Count, userId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales hold records for user {userId}", userId);
                return new List<PlannedEvent>();
            }
        }

        /// <summary>
        /// Get sales urgent records for a user (applies sales workgroup filtering)
        /// </summary>
        public async Task<IEnumerable<PlannedEvent>> GetSalesUrgentRecordsAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Getting sales urgent records for user {userId}", userId);

                // Get user with workgroups and permissions
                var user = await _usersApiService.GetUserWithRoleAndWorkGroupsAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User {userId} not found", userId);
                    return new List<PlannedEvent>();
                }

                // Check if user has ViewAll permission
                bool canViewAll = user.UserRole?.RolePermissions
                    ?.Any(rp => rp.Permission?.Name == "ViewAll") == true;

                // Get user's sales workgroups (workgroups containing "SALES")
                var salesWorkgroups = user.UserWorkGroups?
                    .Where(uwg => uwg.WorkGroup?.Name?.Contains("SALES", StringComparison.OrdinalIgnoreCase) == true)
                    .Select(uwg => uwg.WorkGroup.Name)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Cast<string>()
                    .ToList() ?? new List<string>();

                // Get user's assigned customers
                var assignedCustomers = await _usersApiService.GetUserAssignedCustomersAsync(userId);

                _logger.LogInformation("User {userId}: CanViewAll={canViewAll}, SalesWorkgroups={salesWorkgroups}, AssignedCustomers={assignedCustomers}",
                    userId, canViewAll, string.Join(", ", salesWorkgroups), assignedCustomers.Count);

                // Get OLA violating PE numbers
                var violatingPENumbers = await _context.PETasks
                    .Where(t => t.IsOLAViolate)
                    .Select(t => t.PENumber)
                    .Distinct()
                    .ToListAsync();

                var query = _context.PlannedEvents
                    .Where(p => p.PEStatus == "urgent" &&
                           !p.IsHold &&
                           p.PeNumber != null &&
                           !violatingPENumbers.Contains(p.PeNumber))
                    .AsQueryable();

                // Apply sales workgroup filtering
                query = await ApplySalesWorkgroupFilteringAsync(query, salesWorkgroups, assignedCustomers, canViewAll);

                var result = await query
                    .OrderByDescending(p => p.ServiceRequiredDate)
                    .AsNoTracking()
                    .ToListAsync();

                _logger.LogInformation("Retrieved {count} sales urgent records for user {userId}", result.Count, userId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales urgent records for user {userId}", userId);
                return new List<PlannedEvent>();
            }
        }

        /// <summary>
        /// Get sales OLA violate records for a user (applies sales workgroup filtering)
        /// </summary>
        public async Task<IEnumerable<PlannedEvent>> GetSalesOLAViolateRecordsAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Getting sales OLA violate records for user {userId}", userId);

                // Get user with workgroups and permissions
                var user = await _usersApiService.GetUserWithRoleAndWorkGroupsAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User {userId} not found", userId);
                    return new List<PlannedEvent>();
                }

                // Check if user has ViewAll permission
                bool canViewAll = user.UserRole?.RolePermissions
                    ?.Any(rp => rp.Permission?.Name == "ViewAll") == true;

                // Get user's sales workgroups (workgroups containing "SALES")
                var salesWorkgroups = user.UserWorkGroups?
                    .Where(uwg => uwg.WorkGroup?.Name?.Contains("SALES", StringComparison.OrdinalIgnoreCase) == true)
                    .Select(uwg => uwg.WorkGroup.Name)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Cast<string>()
                    .ToList() ?? new List<string>();

                // Get user's assigned customers
                var assignedCustomers = await _usersApiService.GetUserAssignedCustomersAsync(userId);

                _logger.LogInformation("User {userId}: CanViewAll={canViewAll}, SalesWorkgroups={salesWorkgroups}, AssignedCustomers={assignedCustomers}",
                    userId, canViewAll, string.Join(", ", salesWorkgroups), assignedCustomers.Count);

                // Get OLA violating PE numbers
                var violatingPENumbers = await _context.PETasks
                    .Where(t => t.IsOLAViolate)
                    .Select(t => t.PENumber)
                    .Distinct()
                    .ToListAsync();

                var query = _context.PlannedEvents
                    .Where(p => p.PeNumber != null && violatingPENumbers.Contains(p.PeNumber) && !p.IsHold)
                    .AsQueryable();

                // Apply sales workgroup filtering
                query = await ApplySalesWorkgroupFilteringAsync(query, salesWorkgroups, assignedCustomers, canViewAll);

                var result = await query
                    .OrderByDescending(p => p.ServiceRequiredDate)
                    .AsNoTracking()
                    .ToListAsync();

                _logger.LogInformation("Retrieved {count} sales OLA violate records for user {userId}", result.Count, userId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales OLA violate records for user {userId}", userId);
                return new List<PlannedEvent>();
            }
        }

        /// <summary>
        /// Apply sales workgroup filtering to a query
        /// </summary>
        private Task<IQueryable<PlannedEvent>> ApplySalesWorkgroupFilteringAsync(IQueryable<PlannedEvent> query, List<string> salesWorkgroups, List<string> assignedCustomers, bool canViewAll)
        {
            if (!canViewAll)
            {
                // Apply workgroup filtering first with case-insensitive matching
                // Convert workgroups to lowercase for comparison
                var lowerSalesWorkgroups = salesWorkgroups.Select(wg => wg.ToLower()).ToList();

                query = query.Where(p => lowerSalesWorkgroups.Any(wg =>
                    (p.TaskWg != null && (
                        p.TaskWg.ToLower() == wg ||
                        p.TaskWg.ToLower().Contains(wg)
                    )) ||
                    (p.SectionHandledBy != null && (
                        p.SectionHandledBy.ToLower() == wg ||
                        p.SectionHandledBy.ToLower().Contains(wg)
                    ))
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

            return Task.FromResult(query);
        }

        /// <summary>
        /// Get sales in-progress count for a user
        /// </summary>
        public async Task<int> GetSalesInProgressCountAsync(int userId)
        {
            try
            {
                var records = await GetSalesInProgressRecordsAsync(userId);
                return records.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales in-progress count for user {userId}", userId);
                return 0;
            }
        }

        /// <summary>
        /// Get sales hold count for a user
        /// </summary>
        public async Task<int> GetSalesHoldCountAsync(int userId)
        {
            try
            {
                var records = await GetSalesHoldRecordsAsync(userId);
                return records.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales hold count for user {userId}", userId);
                return 0;
            }
        }

        /// <summary>
        /// Get sales urgent count for a user
        /// </summary>
        public async Task<int> GetSalesUrgentCountAsync(int userId)
        {
            try
            {
                var records = await GetSalesUrgentRecordsAsync(userId);
                return records.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales urgent count for user {userId}", userId);
                return 0;
            }
        }

        /// <summary>
        /// Get sales OLA violate count for a user
        /// </summary>
        public async Task<int> GetSalesOLAViolateCountAsync(int userId)
        {
            try
            {
                var records = await GetSalesOLAViolateRecordsAsync(userId);
                return records.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales OLA violate count for user {userId}", userId);
                return 0;
            }
        }

    }
}


