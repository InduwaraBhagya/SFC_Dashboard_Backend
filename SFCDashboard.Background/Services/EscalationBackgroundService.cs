using Microsoft.EntityFrameworkCore;
using SFCDashboard.Background.Data;
using SFCDashboard.Background.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace SFCDashboard.Background.Services
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
    }
}
