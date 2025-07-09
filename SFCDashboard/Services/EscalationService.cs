using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDashboard.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace SFCDashboard.Services
{
    public class EscalationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<EscalationService> _logger;
        private const string ESCALATION_ENABLED_KEY = "EscalationServiceEnabled";
        private const string ESCALATION_CONFIG_CACHE_KEY = "EscalationConfig";
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

        public EscalationService(ApplicationDbContext context, IMemoryCache cache, ILogger<EscalationService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
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
                    Console.WriteLine("Escalation service is disabled. Skipping escalation check.");
                    return;
                }

                // Get all OLA violated tasks that don't have escalations disabled
                // Include PlannedEvent to access customer name
                var violatedTasks = await _context.PETasks
                    .Include(t => t.PlannedEvent)
                    .Where(t => t.TaskStatus != "Completed" && t.IsOLAViolate && !t.EscalationsDisabled)
                    .ToListAsync();

                foreach (var task in violatedTasks)
                {
                    // Calculate violation duration - use ViolationStartTime if available, 
                    // otherwise OLADateTime if set, otherwise fall back to task start time
                    DateTime violationStart = task.ViolationStartTime ?? 
                                             task.OLADateTime ?? 
                                             task.ActualTaskCreatedDate ?? 
                                             task.TaskCreatedDate;
                    var violationDuration = DateTime.Now - violationStart;
                    
                    // Ensure we have a positive violation duration (safety check)
                    if (violationDuration.TotalMinutes < 0)
                    {
                        Console.WriteLine($"Warning: Negative violation duration for task {task.PENumber}, skipping escalation creation");
                        continue;
                    }
                    
                    // Determine escalation level based on violation duration
                    int escalationLevel = DetermineEscalationLevel(violationDuration);
                    
                    // Log the violation details for debugging
                    Console.WriteLine($"Processing task {task.PENumber}: OLA violated for {violationDuration.TotalHours:F1} hours, escalation level: {escalationLevel}");
                    
                    // Check if escalation already exists for this task at this level
                    var existingEscalation = await _context.Escalations
                        .FirstOrDefaultAsync(e => e.TaskId == task.Id && e.Level == escalationLevel);
                    
                    if (existingEscalation == null)
                    {
                        
                        // Create new escalation
                        var escalation = new Escalation
                        {
                            TaskId = task.Id,
                            Level = escalationLevel, // This will be implicitly converted to int?
                            Title = CreateEscalationTitle(task, escalationLevel),
                            Message = CreateEscalationMessage(task, escalationLevel, violationDuration),
                            CreatedAt = DateTime.Now,
                            IsRead = false
                        };

                        _context.Escalations.Add(escalation);
                        await _context.SaveChangesAsync();

                        // Log the escalation creation with specific details for Level 1 under 24 hours
                        if (escalationLevel == 1 && violationDuration.TotalHours < 24)
                        {
                            Console.WriteLine($"Created Level 1 escalation for task {task.PENumber} - OLA violated {violationDuration.TotalHours:F1} hours ago (under 24 hours)");
                        }
                        else
                        {
                            Console.WriteLine($"Created Level {escalationLevel} escalation for task {task.PENumber} - OLA violated {violationDuration.TotalDays:F1} days ago");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error in CheckAndCreateEscalationsAsync: {ex.Message}");
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
    }
}