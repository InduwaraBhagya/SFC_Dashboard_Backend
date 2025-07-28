using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDashboard.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace SFCDashboard.Services
{
    public interface IEscalationService
    {
        Task<bool> IsEscalationEnabledAsync();
        Task SetEscalationEnabledAsync(bool enabled);
        Task CheckAndCreateEscalationsAsync();
        Task<List<Escalation>> GetEscalationsByUserRoleAsync(int userRoleLevel, List<string>? userWorkgroupNames = null);
        Task<int> GetEscalationCountByUserRoleAsync(int userRoleLevel, bool unreadOnly = false, List<string>? userWorkgroupNames = null);
        Task<object> GetEscalationStatsAsync();
        Task<string> ManualEscalationCheckAsync();
        Task<object> GetOLAViolatedTasksDebugInfoAsync();
        Task MarkAsReadAsync(int escalationId);
        Task MarkMultipleAsReadAsync(List<int> escalationIds);
        Task<List<Escalation>> GetEscalationsBatchAsync(int userRoleLevel, List<string>? userWorkgroupNames = null, int skip = 0, int take = 50);
    }

    public class OptimizedEscalationService : IEscalationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<OptimizedEscalationService> _logger;
        private const string ESCALATION_ENABLED_KEY = "EscalationServiceEnabled";
        private const string ESCALATION_CONFIG_CACHE_KEY = "EscalationConfig";
        private const string ESCALATION_STATS_CACHE_KEY = "EscalationStats";
        private static readonly TimeSpan ConfigCacheExpiration = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan StatsCacheExpiration = TimeSpan.FromMinutes(1);

        // Escalation level constants for better maintainability
        private const int LEVEL_1_HOURS_THRESHOLD = 24;
        private const int LEVEL_2_DAYS_THRESHOLD = 1;
        private const int LEVEL_3_DAYS_THRESHOLD = 3;

        public OptimizedEscalationService(ApplicationDbContext context, IMemoryCache cache, ILogger<OptimizedEscalationService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<bool> IsEscalationEnabledAsync()
        {
            // Try to get from cache first
            if (_cache.TryGetValue(ESCALATION_CONFIG_CACHE_KEY, out bool cachedValue))
            {
                return cachedValue;
            }

            try
            {
                var config = await _context.SystemConfigurations
                    .AsNoTracking() // Use no tracking for read-only operations
                    .FirstOrDefaultAsync(c => c.ConfigKey == ESCALATION_ENABLED_KEY);
                
                bool isEnabled;
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
                    isEnabled = bool.TryParse(config.ConfigValue, out bool parsed) && parsed;
                }
                
                // Cache the result
                _cache.Set(ESCALATION_CONFIG_CACHE_KEY, isEnabled, ConfigCacheExpiration);
                return isEnabled;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking escalation enabled status");
                return true; // Default to enabled on error
            }
        }

        public async Task SetEscalationEnabledAsync(bool enabled)
        {
            try
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
                
                // Invalidate cache
                _cache.Remove(ESCALATION_CONFIG_CACHE_KEY);
                
                _logger.LogInformation("Escalation service {Status}", enabled ? "enabled" : "disabled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting escalation enabled status to {Enabled}", enabled);
                throw;
            }
        }

        public async Task CheckAndCreateEscalationsAsync()
        {
            try
            {
                // Check if escalation service is enabled
                if (!await IsEscalationEnabledAsync())
                {
                    _logger.LogDebug("Escalation service is disabled. Skipping escalation check.");
                    return;
                }

                // Get all OLA violated tasks in a single optimized query
                var violatedTasks = await GetOLAViolatedTasksAsync();
                
                if (!violatedTasks.Any())
                {
                    _logger.LogDebug("No OLA violated tasks found");
                    return;
                }

                // Get existing escalations for these tasks to avoid duplicates
                var taskIds = violatedTasks.Select(t => t.Id).ToList();
                var existingEscalations = await _context.Escalations
                    .AsNoTracking()
                    .Where(e => taskIds.Contains(e.TaskId))
                    .Select(e => new { e.TaskId, e.Level })
                    .ToListAsync();

                var existingEscalationLookup = existingEscalations
                    .GroupBy(e => e.TaskId)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.Level).ToHashSet());

                var newEscalations = new List<Escalation>();
                var now = DateTime.Now;

                foreach (var task in violatedTasks)
                {
                    try
                    {
                        // Calculate violation duration using the most appropriate date
                        var violationStart = GetViolationStartTime(task);
                        var violationDuration = now - violationStart;
                        
                        // Ensure we have a positive violation duration
                        if (violationDuration.TotalMinutes < 0)
                        {
                            _logger.LogWarning("Negative violation duration for task {PENumber}, skipping", task.PENumber);
                            continue;
                        }
                        
                        // Determine escalation level based on violation duration
                        int escalationLevel = DetermineEscalationLevel(violationDuration);
                        
                        // Check if escalation already exists for this task at this level
                        if (existingEscalationLookup.TryGetValue(task.Id, out var existingLevels) && 
                            existingLevels.Contains(escalationLevel))
                        {
                            continue; // Skip if escalation already exists
                        }
                        
                        // Create new escalation
                        var escalation = new Escalation
                        {
                            TaskId = task.Id,
                            Level = escalationLevel,
                            Title = CreateEscalationTitle(task, escalationLevel),
                            Message = CreateEscalationMessage(task, escalationLevel, violationDuration),
                            CreatedAt = now,
                            IsRead = false
                        };

                        newEscalations.Add(escalation);
                        
                        _logger.LogInformation("Created Level {Level} escalation for task {PENumber} - OLA violated {Duration}",
                            escalationLevel, task.PENumber, 
                            violationDuration.TotalDays < 1 ? $"{violationDuration.TotalHours:F1} hours ago" : $"{violationDuration.TotalDays:F1} days ago");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing escalation for task {PENumber}", task.PENumber);
                    }
                }

                // Bulk insert new escalations
                if (newEscalations.Any())
                {
                    _context.Escalations.AddRange(newEscalations);
                    await _context.SaveChangesAsync();
                    
                    // Invalidate stats cache
                    _cache.Remove(ESCALATION_STATS_CACHE_KEY);
                    
                    _logger.LogInformation("Created {Count} new escalations", newEscalations.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CheckAndCreateEscalationsAsync");
            }
        }

        private async Task<List<PETask>> GetOLAViolatedTasksAsync()
        {
            return await _context.PETasks
                .AsNoTracking()
                .Include(t => t.PlannedEvent)
                .Where(t => t.TaskStatus != "Completed" && 
                           t.IsOLAViolate && 
                           !t.EscalationsDisabled)
                .ToListAsync();
        }

        private static DateTime GetViolationStartTime(PETask task)
        {
            return task.ViolationStartTime ?? 
                   task.OLADateTime ?? 
                   task.ActualTaskCreatedDate ?? 
                   task.TaskCreatedDate;
        }

        private static double CalculateViolationDurationHours(
            DateTime now,
            DateTime? violationStartTime,
            DateTime? olaDateTime,
            DateTime? actualTaskCreatedDate,
            DateTime taskCreatedDate)
        {
            var violationStart = violationStartTime ?? 
                                olaDateTime ?? 
                                actualTaskCreatedDate ?? 
                                taskCreatedDate;
            
            return (now - violationStart).TotalHours;
        }

        private static int DetermineEscalationLevel(TimeSpan violationDuration)
        {
            return violationDuration.TotalDays switch
            {
                >= LEVEL_3_DAYS_THRESHOLD => 3, // Level 3 after 3+ days
                >= LEVEL_2_DAYS_THRESHOLD => 2, // Level 2 after 1+ days
                _ => 1 // Level 1 for violations less than 1 day
            };
        }

        private static string CreateEscalationMessage(PETask task, int level, TimeSpan duration)
        {
            var levelText = level switch
            {
                1 when duration.TotalHours < LEVEL_1_HOURS_THRESHOLD => $"immediate attention (violated {duration.TotalHours:F1} hours ago)",
                1 => "immediate attention",
                2 => "urgent attention (1+ days overdue)",
                3 => "critical attention (3+ days overdue)",
                _ => "attention"
            };

            var taskStartDate = task.ActualTaskCreatedDate ?? task.TaskCreatedDate;
            var durationText = duration.TotalDays < 1 
                ? $"{duration.TotalHours:F1} hours" 
                : $"{duration.TotalDays:F1} days";

            return $"Task {task.PENumber} requires {levelText}. " +
                   $"OLA violation duration: {durationText}. " +
                   $"Task started: {taskStartDate:g}";
        }

        private static string CreateEscalationTitle(PETask task, int level)
        {
            var customerName = task.PlannedEvent?.Customer ?? "Unknown Customer";
            return $"Task {task.PENumber} OLA Violation - Level {level} - Customer: {customerName}";
        }

        public async Task<List<Escalation>> GetEscalationsByUserRoleAsync(int userRoleLevel, List<string>? userWorkgroupNames = null)
        {
            return await GetEscalationsBatchAsync(userRoleLevel, userWorkgroupNames, 0, int.MaxValue);
        }

        public async Task<List<Escalation>> GetEscalationsBatchAsync(int userRoleLevel, List<string>? userWorkgroupNames = null, int skip = 0, int take = 50)
        {
            try
            {
                int targetEscalationLevel = userRoleLevel == 0 ? 1 : userRoleLevel;
                
                var query = _context.Escalations
                    .AsNoTracking()
                    .Include(e => e.PETask)
                        .ThenInclude(t => t.PlannedEvent)
                    .Where(e => e.Level.HasValue && e.Level.Value == targetEscalationLevel);
                
                // Apply workgroup filtering if provided
                if (userWorkgroupNames?.Any() == true)
                {
                    query = query.Where(e => e.PETask != null && 
                        e.PETask.TaskWorkGroup != null && 
                        userWorkgroupNames.Contains(e.PETask.TaskWorkGroup));
                }
                
                return await query
                    .OrderByDescending(e => e.CreatedAt)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting escalations for user role {UserRoleLevel}", userRoleLevel);
                return new List<Escalation>();
            }
        }

        public async Task<int> GetEscalationCountByUserRoleAsync(int userRoleLevel, bool unreadOnly = false, List<string>? userWorkgroupNames = null)
        {
            try
            {
                int targetEscalationLevel = userRoleLevel == 0 ? 1 : userRoleLevel;
                
                var query = _context.Escalations
                    .AsNoTracking()
                    .Include(e => e.PETask)
                    .Where(e => e.Level.HasValue && e.Level.Value == targetEscalationLevel);

                // Apply workgroup filtering if provided
                if (userWorkgroupNames?.Any() == true)
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting escalation count for user role {UserRoleLevel}", userRoleLevel);
                return 0;
            }
        }

        public async Task<object> GetEscalationStatsAsync()
        {
            // Try to get from cache first
            if (_cache.TryGetValue(ESCALATION_STATS_CACHE_KEY, out object? cachedStats))
            {
                return cachedStats!;
            }

            try
            {
                var stats = await _context.Escalations
                    .AsNoTracking()
                    .Where(e => e.Level.HasValue)
                    .GroupBy(e => e.Level!.Value)
                    .Select(g => new {
                        Level = g.Key,
                        Total = g.Count(),
                        Unread = g.Count(e => !e.IsRead)
                    })
                    .ToListAsync();

                // Cache the result
                _cache.Set(ESCALATION_STATS_CACHE_KEY, stats, StatsCacheExpiration);
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting escalation statistics");
                return new object();
            }
        }

        public async Task<string> ManualEscalationCheckAsync()
        {
            try
            {
                await CheckAndCreateEscalationsAsync();
                return "Manual escalation check completed successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Manual escalation check failed");
                return $"Manual escalation check failed: {ex.Message}";
            }
        }

        public async Task<object> GetOLAViolatedTasksDebugInfoAsync()
        {
            try
            {
                var now = DateTime.Now;
                var violatedTasks = await _context.PETasks
                    .AsNoTracking()
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
                        CustomerName = t.PlannedEvent != null ? t.PlannedEvent.Customer : "Unknown"
                    })
                    .ToListAsync();

                return violatedTasks.Select(t => new {
                    t.Id,
                    t.PENumber,
                    t.TaskStatus,
                    t.IsOLAViolate,
                    t.ViolationStartTime,
                    t.OLADateTime,
                    t.ActualTaskCreatedDate,
                    t.TaskCreatedDate,
                    ViolationDurationHours = CalculateViolationDurationHours(
                        now, 
                        t.ViolationStartTime, 
                        t.OLADateTime, 
                        t.ActualTaskCreatedDate, 
                        t.TaskCreatedDate),
                    t.CustomerName
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OLA violated tasks debug info");
                return new { Error = ex.Message };
            }
        }

        public async Task MarkAsReadAsync(int escalationId)
        {
            try
            {
                var escalation = await _context.Escalations.FindAsync(escalationId);
                if (escalation != null && !escalation.IsRead)
                {
                    escalation.IsRead = true;
                    await _context.SaveChangesAsync();
                    
                    // Invalidate stats cache
                    _cache.Remove(ESCALATION_STATS_CACHE_KEY);
                    
                    _logger.LogDebug("Marked escalation {EscalationId} as read", escalationId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking escalation {EscalationId} as read", escalationId);
                throw;
            }
        }

        public async Task MarkMultipleAsReadAsync(List<int> escalationIds)
        {
            if (!escalationIds.Any()) return;

            try
            {
                var escalations = await _context.Escalations
                    .Where(e => escalationIds.Contains(e.Id) && !e.IsRead)
                    .ToListAsync();

                if (escalations.Any())
                {
                    foreach (var escalation in escalations)
                    {
                        escalation.IsRead = true;
                    }
                    
                    await _context.SaveChangesAsync();
                    
                    // Invalidate stats cache
                    _cache.Remove(ESCALATION_STATS_CACHE_KEY);
                    
                    _logger.LogInformation("Marked {Count} escalations as read", escalations.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking multiple escalations as read");
                throw;
            }
        }
    }
}
