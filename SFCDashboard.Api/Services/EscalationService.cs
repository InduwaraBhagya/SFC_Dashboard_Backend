using Microsoft.EntityFrameworkCore;
using SFCDashboard.Api.Data;
using SFCDashboard.Api.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace SFCDashboard.Api.Services
{
    public class EscalationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<EscalationService> _logger;
        private const string ESCALATION_ENABLED_KEY = "EscalationServiceEnabled";
        private const string ESCALATION_CONFIG_CACHE_KEY = "EscalationConfig";
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);
        
        // Add timeout configuration
        private static readonly TimeSpan DefaultQueryTimeout = TimeSpan.FromSeconds(30);

        public EscalationService(ApplicationDbContext context, IMemoryCache cache, ILogger<EscalationService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
            
            // Configure command timeout for this context
            if (_context.Database.IsRelational())
            {
                _context.Database.SetCommandTimeout(DefaultQueryTimeout);
            }
        }

        public async Task<bool> IsEscalationEnabledAsync()
        {
            // Check cache first
            if (_cache.TryGetValue(ESCALATION_CONFIG_CACHE_KEY, out bool isEnabled))
            {
                return isEnabled;
            }

            var config = await _context.SystemConfigurations
                .FirstOrDefaultAsync(c => c.ConfigKey == ESCALATION_ENABLED_KEY);
            
            if (config == null)
            {
                // If no configuration exists, create it with default value (enabled)
                config = new SystemConfiguration
                {
                    ConfigKey = ESCALATION_ENABLED_KEY,
                    ConfigValue = "true",
                    Description = "Controls whether the escalation service is enabled"
                };
                _context.SystemConfigurations.Add(config);
                await _context.SaveChangesAsync();
                isEnabled = true;
            }
            else
            {
                isEnabled = bool.TryParse(config.ConfigValue, out bool result) && result;
            }

            // Set cache with expiration
            _cache.Set(ESCALATION_CONFIG_CACHE_KEY, isEnabled, CacheExpiration);

            return isEnabled;
        }

        public async Task SetEscalationEnabledAsync(bool enabled)
        {
            var config = await _context.SystemConfigurations
                .FirstOrDefaultAsync(c => c.ConfigKey == ESCALATION_ENABLED_KEY);
            
            if (config == null)
            {
                config = new SystemConfiguration
                {
                    ConfigKey = ESCALATION_ENABLED_KEY,
                    ConfigValue = enabled.ToString().ToLower(),
                    Description = "Controls whether the escalation service is enabled"
                };
                _context.SystemConfigurations.Add(config);
            }
            else
            {
                config.ConfigValue = enabled.ToString().ToLower();
                config.UpdatedAt = DateTime.Now;
            }
            
            await _context.SaveChangesAsync();

            // Update cache
            _cache.Set(ESCALATION_CONFIG_CACHE_KEY, enabled, CacheExpiration);
        }

        public async Task CheckAndCreateEscalationsAsync()
        {
            try
            {
                // Check if escalation service is enabled
                if (!await IsEscalationEnabledAsync())
                {
                    _logger.LogInformation("Escalation service is disabled. Skipping escalation check.");
                    return;
                }

                // Get all OLA violated tasks that don't have escalations disabled
                // Include PlannedEvent to access customer name
                var violatedTasks = await _context.PETasks
                    .Include(t => t.PlannedEvent)
                    .Where(t => t.TaskStatus != "Completed" && t.IsOLAViolate && !t.EscalationsDisabled)
                    .ToListAsync();

                if (!violatedTasks.Any())
                {
                    _logger.LogInformation("No violated tasks found for escalation processing.");
                    return;
                }

                // Get all task IDs for batch processing
                var taskIds = violatedTasks.Select(t => t.Id).ToList();
                
                // Batch load existing escalations to avoid N+1 queries
                var existingEscalations = await _context.Escalations
                    .Where(e => taskIds.Contains(e.TaskId))
                    .ToListAsync();

                // Create a lookup dictionary for faster access
                var escalationLookup = existingEscalations
                    .GroupBy(e => e.TaskId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var newEscalations = new List<Escalation>();
                var currentTime = DateTime.Now;

                foreach (var task in violatedTasks)
                {
                    // Calculate violation duration - use ViolationStartTime if available, 
                    // otherwise OLADateTime if set, otherwise fall back to task start time
                    DateTime violationStart = task.ViolationStartTime ?? 
                                             task.OLADateTime ?? 
                                             task.ActualTaskCreatedDate ?? 
                                             task.TaskCreatedDate;
                    var violationDuration = currentTime - violationStart;
                    
                    // Ensure we have a positive violation duration (safety check)
                    if (violationDuration.TotalMinutes < 0)
                    {
                        _logger.LogWarning("Negative violation duration for task {PENumber}, skipping escalation creation", task.PENumber);
                        continue;
                    }
                    
                    // Determine escalation level based on violation duration
                    int escalationLevel = DetermineEscalationLevel(violationDuration);
                    
                    // Log the violation details for debugging
                    _logger.LogDebug("Processing task {PENumber}: OLA violated for {ViolationHours:F1} hours, escalation level: {EscalationLevel}", 
                        task.PENumber, violationDuration.TotalHours, escalationLevel);
                    
                    // Check if escalation already exists for this task at this level using our lookup
                    var taskEscalations = escalationLookup.GetValueOrDefault(task.Id, new List<Escalation>());
                    var existingEscalation = taskEscalations.FirstOrDefault(e => e.Level == escalationLevel);
                    
                    if (existingEscalation == null)
                    {
                        // Create new escalation
                        var escalation = new Escalation
                        {
                            TaskId = task.Id,
                            Level = escalationLevel, // This will be implicitly converted to int?
                            Title = CreateEscalationTitle(task, escalationLevel),
                            Message = CreateEscalationMessage(task, escalationLevel, violationDuration),
                            CreatedAt = currentTime,
                            IsRead = false
                        };

                        newEscalations.Add(escalation);

                        // Log the escalation creation with specific details for Level 1 under 24 hours
                        if (escalationLevel == 1 && violationDuration.TotalHours < 24)
                        {
                            _logger.LogInformation("Prepared Level 1 escalation for task {PENumber} - OLA violated {ViolationHours:F1} hours ago (under 24 hours)", 
                                task.PENumber, violationDuration.TotalHours);
                        }
                        else
                        {
                            _logger.LogInformation("Prepared Level {EscalationLevel} escalation for task {PENumber} - OLA violated {ViolationDays:F1} days ago", 
                                escalationLevel, task.PENumber, violationDuration.TotalDays);
                        }
                    }
                }

                // Batch insert all new escalations
                if (newEscalations.Any())
                {
                    _context.Escalations.AddRange(newEscalations);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Created {EscalationCount} new escalations in batch", newEscalations.Count);
                }
                else
                {
                    _logger.LogInformation("No new escalations needed");
                }
            }
            catch (Exception ex)
            {
                // Log error
                _logger.LogError(ex, "Error in CheckAndCreateEscalationsAsync: {ErrorMessage}", ex.Message);
                throw; // Re-throw to allow proper error handling by the caller
            }
        }

        private int DetermineEscalationLevel(TimeSpan violationDuration)
        {
            if (violationDuration.TotalDays >= 3)
                return 3; // Level 3 after 3+ days
            else if (violationDuration.TotalDays >= 1)
                return 2; // Level 2 after 1+ days
            else if (violationDuration.TotalHours < 24)
                return 1; // Level 1 for violations less than 24 hours
            else
                return 1; // Level 1 for any other violation case
        }

        private string CreateEscalationMessage(PETask task, int level, TimeSpan duration)
        {
            var levelText = level switch
            {
                1 when duration.TotalHours < 24 => $"immediate attention (violated {duration.TotalHours:F1} hours ago)",
                1 => "immediate attention",
                2 => "urgent attention (1+ days overdue)",
                3 => "critical attention (3+ days overdue)",
                _ => "attention"
            };

            // Use the actual task start date - prefer ActualTaskCreatedDate if available, otherwise TaskCreatedDate
            var taskStartDate = task.ActualTaskCreatedDate ?? task.TaskCreatedDate;

            var durationText = duration.TotalDays < 1 
                ? $"{duration.TotalHours:F1} hours" 
                : $"{duration.TotalDays:F1} days";

            return $"Task {task.PENumber} requires {levelText}. " +
                   $"OLA violation duration: {durationText}. " +
                   $"Task started: {taskStartDate:g}";
        }

        private string CreateEscalationTitle(PETask task, int level)
        {
            var customerName = task.PlannedEvent?.Customer ?? "Unknown Customer";
            return $"Task {task.PENumber} OLA Violation - Level {level} - Customer: {customerName}";
        }

        /// <summary>
        /// Gets escalations filtered by user role level and workgroup with strict matching criteria.
        /// Filtering Rules:
        /// 1. Role Level Matching: Escalations are shown ONLY when escalation level EQUALS user role level
        ///    - Role Level 0 users: See Level 1 escalations (special case since no Level 0 escalations are created)
        ///    - Role Level 1 users: See ONLY Level 1 escalations
        ///    - Role Level 2 users: See ONLY Level 2 escalations  
        ///    - Role Level 3 users: See ONLY Level 3 escalations
        /// 2. Workgroup Matching: Escalation's task workgroup must EXACTLY match one of user's workgroups
        ///    - Uses exact string equality, not substring matching
        ///    - If user has multiple workgroups, escalation is shown if task workgroup equals ANY of them
        /// </summary>
        /// <param name="userRoleLevel">User's role level (0-3)</param>
        /// <param name="userWorkgroupNames">List of user's workgroup names for filtering (null means no workgroup filtering)</param>
        /// <returns>List of escalations matching the strict filtering criteria</returns>
        public async Task<List<Escalation>> GetEscalationsByUserRoleAsync(int userRoleLevel, List<string>? userWorkgroupNames = null)
        {
            // Show escalations only when escalation level exactly matches user role level
            // Special case: Role Level 0 users see Level 1 escalations (since no Level 0 escalations are created)
            // Role Level 1: See only Level 1 escalations
            // Role Level 2: See only Level 2 escalations
            // Role Level 3: See only Level 3 escalations
            
            int targetEscalationLevel = userRoleLevel == 0 ? 1 : userRoleLevel;
            
            // Set longer timeout for this complex query
            var previousTimeout = _context.Database.GetCommandTimeout();
            _context.Database.SetCommandTimeout(120); // 2 minutes
            
            try
            {
                var query = _context.Escalations
                    .Include(e => e.PETask)
                        .ThenInclude(t => t.PlannedEvent)
                    .Where(e => e.Level.HasValue && e.Level.Value == targetEscalationLevel);
                
                // Filter by user workgroups if provided - using exact match for strict filtering
                if (userWorkgroupNames != null && userWorkgroupNames.Any())
                {
                    query = query.Where(e => e.PETask != null && 
                        e.PETask.TaskWorkGroup != null && 
                        userWorkgroupNames.Contains(e.PETask.TaskWorkGroup));
                }
                
                return await query
                    .OrderByDescending(e => e.CreatedAt)  // Order by creation date
                    .ToListAsync();
            }
            finally
            {
                // Restore previous timeout
                _context.Database.SetCommandTimeout(previousTimeout);
            }
        }

        /// <summary>
        /// Gets escalations with pagination to improve performance for large datasets.
        /// Uses the same filtering logic as GetEscalationsByUserRoleAsync but with pagination support.
        /// </summary>
        /// <param name="userRoleLevel">User's role level (0-3)</param>
        /// <param name="userWorkgroupNames">List of user's workgroup names for filtering</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <returns>Paginated list of escalations</returns>
        public async Task<(List<Escalation> Escalations, int TotalCount)> GetEscalationsByUserRolePaginatedAsync(
            int userRoleLevel, 
            List<string>? userWorkgroupNames = null, 
            int pageNumber = 1, 
            int pageSize = 50)
        {
            int targetEscalationLevel = userRoleLevel == 0 ? 1 : userRoleLevel;
            
            // Set longer timeout for this complex query
            var previousTimeout = _context.Database.GetCommandTimeout();
            _context.Database.SetCommandTimeout(120);
            
            try
            {
                var baseQuery = _context.Escalations
                    .Where(e => e.Level.HasValue && e.Level.Value == targetEscalationLevel);
                
                // Filter by user workgroups if provided
                if (userWorkgroupNames != null && userWorkgroupNames.Any())
                {
                    baseQuery = baseQuery.Where(e => e.PETask != null && 
                        e.PETask.TaskWorkGroup != null && 
                        userWorkgroupNames.Contains(e.PETask.TaskWorkGroup));
                }
                
                // Get total count first (without includes for performance)
                var totalCount = await baseQuery.CountAsync();
                
                // Get paginated results with includes
                var escalations = await baseQuery
                    .Include(e => e.PETask)
                        .ThenInclude(t => t.PlannedEvent)
                    .OrderByDescending(e => e.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                
                return (escalations, totalCount);
            }
            finally
            {
                _context.Database.SetCommandTimeout(previousTimeout);
            }
        }

        /// <summary>
        /// Gets escalations with optimized query using projection to minimize data transfer.
        /// This is the most performant version for large datasets.
        /// </summary>
        /// <param name="userRoleLevel">User's role level (0-3)</param>
        /// <param name="userWorkgroupNames">List of user's workgroup names for filtering</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <returns>Optimized escalations with minimal data</returns>
        public async Task<(List<object> Escalations, int TotalCount)> GetEscalationsOptimizedAsync(
            int userRoleLevel, 
            List<string>? userWorkgroupNames = null, 
            int pageNumber = 1, 
            int pageSize = 50)
        {
            int targetEscalationLevel = userRoleLevel == 0 ? 1 : userRoleLevel;
            
            var previousTimeout = _context.Database.GetCommandTimeout();
            _context.Database.SetCommandTimeout(60); // Shorter timeout for optimized query
            
            try
            {
                var baseQuery = from e in _context.Escalations
                               join p in _context.PETasks on e.TaskId equals p.Id
                               join pe in _context.PlannedEvents on p.PENumber equals pe.PeNumber
                               where e.Level.HasValue && e.Level.Value == targetEscalationLevel
                               select new { e, p, pe };
                
                // Filter by user workgroups if provided
                if (userWorkgroupNames != null && userWorkgroupNames.Any())
                {
                    baseQuery = baseQuery.Where(x => x.p.TaskWorkGroup != null && 
                        userWorkgroupNames.Contains(x.p.TaskWorkGroup));
                }
                
                // Get total count
                var totalCount = await baseQuery.CountAsync();
                
                // Get paginated results with only necessary fields
                var escalations = await baseQuery
                    .OrderByDescending(x => x.e.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(x => new
                    {
                        // Escalation fields
                        Id = x.e.Id,
                        TaskId = x.e.TaskId,
                        Level = x.e.Level,
                        Title = x.e.Title,
                        Message = x.e.Message,
                        CreatedAt = x.e.CreatedAt,
                        IsRead = x.e.IsRead,
                        IsIgnored = x.e.IsIgnored,
                        IgnoreReason = x.e.IgnoreReason,
                        IgnoredAt = x.e.IgnoredAt,
                        IgnoredById = x.e.IgnoredById,
                        
                        // PETask essential fields
                        PETask = new
                        {
                            Id = x.p.Id,
                            PENumber = x.p.PENumber,
                            Task = x.p.Task,
                            TaskWorkGroup = x.p.TaskWorkGroup,
                            TaskStatus = x.p.TaskStatus,
                            Priority = x.p.Priority,
                            IsUrgent = x.p.IsUrgent,
                            OLA = x.p.OLA,
                            OLADateTime = x.p.OLADateTime,
                            IsOLAViolate = x.p.IsOLAViolate
                        },
                        
                        // PlannedEvent essential fields
                        PlannedEvent = new
                        {
                            Id = x.pe.Id,
                            PeNumber = x.pe.PeNumber,
                            PeTitle = x.pe.PeTitle,
                            Customer = x.pe.Customer,
                            PEStatus = x.pe.PEStatus,
                            Priority = x.pe.Priority,
                            ServiceRequiredDate = x.pe.ServiceRequiredDate
                        }
                    })
                    .ToListAsync<object>();
                
                return (escalations, totalCount);
            }
            finally
            {
                _context.Database.SetCommandTimeout(previousTimeout);
            }
        }

        /// <summary>
        /// Gets the count of escalations filtered by user role level and workgroup with strict matching criteria.
        /// Uses the same filtering logic as GetEscalationsByUserRoleAsync:
        /// 1. Role Level: EXACT match between escalation level and user role level
        /// 2. Workgroup: EXACT match between task workgroup and user workgroups
        /// </summary>
        /// <param name="userRoleLevel">User's role level (0-3)</param>
        /// <param name="unreadOnly">If true, count only unread escalations</param>
        /// <param name="userWorkgroupNames">List of user's workgroup names for filtering (null means no workgroup filtering)</param>
        /// <returns>Count of escalations matching the strict filtering criteria</returns>
        // Get escalation count for a specific user role level and workgroups
        public async Task<int> GetEscalationCountByUserRoleAsync(int userRoleLevel, bool unreadOnly = false, List<string>? userWorkgroupNames = null)
        {
            // Count escalations only when escalation level exactly matches user role level
            // Special case: Role Level 0 users see Level 1 escalations
            int targetEscalationLevel = userRoleLevel == 0 ? 1 : userRoleLevel;
            
            var query = _context.Escalations
                .Include(e => e.PETask)
                .Where(e => e.Level.HasValue && e.Level.Value == targetEscalationLevel);

            // Filter by user workgroups if provided - using exact match for strict filtering
            if (userWorkgroupNames != null && userWorkgroupNames.Any())
            {
                query = query.Where(e => e.PETask != null && 
                    e.PETask.TaskWorkGroup != null && 
                    userWorkgroupNames.Contains(e.PETask.TaskWorkGroup));
            }

            if (unreadOnly)
            {
                query = query.Where(e => !e.IsRead);
            }

            return await query.CountAsync();
        }



        // Get escalation statistics
        public async Task<object> GetEscalationStatsAsync()
        {
            var stats = await _context.Escalations
                .Where(e => e.Level.HasValue)
                .GroupBy(e => e.Level!.Value)
                .Select(g => new {
                    Level = g.Key,
                    Total = g.Count(),
                    Unread = g.Count(e => !e.IsRead)
                })
                .ToListAsync();

            return stats;
        }

        // Manual method to trigger escalation check (useful for testing)
        public async Task<string> ManualEscalationCheckAsync()
        {
            try
            {
                await CheckAndCreateEscalationsAsync();
                return "Manual escalation check completed successfully";
            }
            catch (Exception ex)
            {
                return $"Manual escalation check failed: {ex.Message}";
            }
        }

        // Get current OLA violated tasks for debugging
        public async Task<object> GetOLAViolatedTasksDebugInfoAsync()
        {
            var violatedTasks = await _context.PETasks
                .Include(t => t.PlannedEvent)
                .Where(t => t.TaskStatus != "Completed" && t.IsOLAViolate && !t.EscalationsDisabled)
                .Select(t => new {
                    t.Id,
                    t.PENumber,
                    t.TaskStatus,
                    t.IsOLAViolate,
                    t.ViolationStartTime,
                    t.OLADateTime,
                    t.ActualTaskCreatedDate,
                    t.TaskCreatedDate,
                    ViolationDurationHours = t.ViolationStartTime.HasValue ? 
                        (DateTime.Now - t.ViolationStartTime.Value).TotalHours :
                        t.OLADateTime.HasValue ?
                        (DateTime.Now - t.OLADateTime.Value).TotalHours :
                        t.ActualTaskCreatedDate.HasValue ?
                        (DateTime.Now - t.ActualTaskCreatedDate.Value).TotalHours :
                        (DateTime.Now - t.TaskCreatedDate).TotalHours,
                    CustomerName = t.PlannedEvent != null ? t.PlannedEvent.Customer : "Unknown"
                })
                .ToListAsync();

            return violatedTasks;
        }

        /// <summary>
        /// Marks an escalation as read by updating its IsRead property to true.
        /// </summary>
        /// <param name="escalationId">The ID of the escalation to mark as read</param>
        /// <returns>Task representing the async operation</returns>
        public async Task MarkAsReadAsync(int escalationId)
        {
            var escalation = await _context.Escalations.FindAsync(escalationId);
            if (escalation != null)
            {
                escalation.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Processes escalations in batches to handle large datasets efficiently.
        /// This method is particularly useful when dealing with thousands of violated tasks.
        /// </summary>
        /// <param name="batchSize">Number of tasks to process in each batch (default: 100)</param>
        /// <returns>Task representing the async operation</returns>
        public async Task CheckAndCreateEscalationsBatchAsync(int batchSize = 100)
        {
            try
            {
                // Check if escalation service is enabled
                if (!await IsEscalationEnabledAsync())
                {
                    _logger.LogInformation("Escalation service is disabled. Skipping escalation check.");
                    return;
                }

                _logger.LogInformation("Starting batch escalation processing with batch size: {BatchSize}", batchSize);

                // Get count of violated tasks first
                var totalCount = await _context.PETasks
                    .Where(t => t.TaskStatus != "Completed" && t.IsOLAViolate && !t.EscalationsDisabled)
                    .CountAsync();

                if (totalCount == 0)
                {
                    _logger.LogInformation("No violated tasks found for escalation processing.");
                    return;
                }

                _logger.LogInformation("Found {TotalCount} violated tasks to process in batches", totalCount);

                var processedCount = 0;
                var totalNewEscalations = 0;

                // Process in batches
                while (processedCount < totalCount)
                {
                    var batchTasks = await _context.PETasks
                        .Include(t => t.PlannedEvent)
                        .Where(t => t.TaskStatus != "Completed" && t.IsOLAViolate && !t.EscalationsDisabled)
                        .OrderBy(t => t.Id) // Ensure consistent ordering
                        .Skip(processedCount)
                        .Take(batchSize)
                        .ToListAsync();

                    if (!batchTasks.Any())
                        break;

                    var batchResult = await ProcessTaskBatch(batchTasks);
                    totalNewEscalations += batchResult;
                    processedCount += batchTasks.Count;

                    _logger.LogInformation("Processed batch: {ProcessedCount}/{TotalCount} tasks, {BatchEscalations} new escalations in this batch", 
                        processedCount, totalCount, batchResult);
                }

                _logger.LogInformation("Batch escalation processing completed. Processed {ProcessedCount} tasks, created {TotalNewEscalations} new escalations", 
                    processedCount, totalNewEscalations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CheckAndCreateEscalationsBatchAsync: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Processes a batch of tasks and returns the number of new escalations created.
        /// </summary>
        /// <param name="tasks">Batch of tasks to process</param>
        /// <returns>Number of new escalations created</returns>
        private async Task<int> ProcessTaskBatch(List<PETask> tasks)
        {
            var taskIds = tasks.Select(t => t.Id).ToList();
            
            // Batch load existing escalations
            var existingEscalations = await _context.Escalations
                .Where(e => taskIds.Contains(e.TaskId))
                .ToListAsync();

            var escalationLookup = existingEscalations
                .GroupBy(e => e.TaskId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var newEscalations = new List<Escalation>();
            var currentTime = DateTime.Now;

            foreach (var task in tasks)
            {
                var violationStart = task.ViolationStartTime ?? 
                                   task.OLADateTime ?? 
                                   task.ActualTaskCreatedDate ?? 
                                   task.TaskCreatedDate;
                var violationDuration = currentTime - violationStart;
                
                if (violationDuration.TotalMinutes < 0)
                {
                    _logger.LogWarning("Negative violation duration for task {PENumber}, skipping", task.PENumber);
                    continue;
                }
                
                var escalationLevel = DetermineEscalationLevel(violationDuration);
                var taskEscalations = escalationLookup.GetValueOrDefault(task.Id, new List<Escalation>());
                var existingEscalation = taskEscalations.FirstOrDefault(e => e.Level == escalationLevel);
                
                if (existingEscalation == null)
                {
                    var escalation = new Escalation
                    {
                        TaskId = task.Id,
                        Level = escalationLevel,
                        Title = CreateEscalationTitle(task, escalationLevel),
                        Message = CreateEscalationMessage(task, escalationLevel, violationDuration),
                        CreatedAt = currentTime,
                        IsRead = false
                    };

                    newEscalations.Add(escalation);
                }
            }

            // Batch insert new escalations
            if (newEscalations.Any())
            {
                _context.Escalations.AddRange(newEscalations);
                await _context.SaveChangesAsync();
            }

            return newEscalations.Count;
        }
    }
}

