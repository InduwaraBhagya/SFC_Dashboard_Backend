using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDashboard.Models;

namespace SFCDashboard.Services
{
    public class EscalationService
    {
        private readonly ApplicationDbContext _context;
        private const string ESCALATION_ENABLED_KEY = "EscalationServiceEnabled";

        public EscalationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> IsEscalationEnabledAsync()
        {
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
                return true;
            }
            
            return bool.TryParse(config.ConfigValue, out bool isEnabled) && isEnabled;
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
                    
                    // Determine escalation level based on violation duration
                    int escalationLevel = DetermineEscalationLevel(violationDuration);
                    
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
            else
                return 1; // Level 1 immediately when OLA violated
        }

        private string CreateEscalationMessage(PETask task, int level, TimeSpan duration)
        {
            var levelText = level switch
            {
                1 => "immediate attention",
                2 => "urgent attention (1+ days overdue)",
                3 => "critical attention (3+ days overdue)",
                _ => "attention"
            };

            // Use the actual task start date - prefer ActualTaskCreatedDate if available, otherwise TaskCreatedDate
            var taskStartDate = task.ActualTaskCreatedDate ?? task.TaskCreatedDate;

            return $"Task {task.PENumber} requires {levelText}. " +
                   $"OLA violation duration: {duration.TotalDays:F1} days. " +
                   $"Task started: {taskStartDate:g}";
        }

        private string CreateEscalationTitle(PETask task, int level)
        {
            var customerName = task.PlannedEvent?.Customer ?? "Unknown Customer";
            return $"Task {task.PENumber} OLA Violation - Level {level} - Customer: {customerName}";
        }

        // Get escalations by role level for display
        public async Task<List<Escalation>> GetEscalationsByUserRoleAsync(int userRoleLevel)
        {
            // Users with role level 0 (normal users) see level 1 escalations
            // Users with higher role levels see escalations >= their role level
            int minEscalationLevel = userRoleLevel == 0 ? 1 : userRoleLevel;
            
            return await _context.Escalations
                .Include(e => e.PETask)
                .Where(e => e.Level.HasValue && e.Level.Value >= minEscalationLevel)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        // Mark escalation as read
        public async Task MarkAsReadAsync(int escalationId)
        {
            var escalation = await _context.Escalations.FindAsync(escalationId);
            if (escalation != null)
            {
                escalation.IsRead = true;
                await _context.SaveChangesAsync();
            }
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
    }
}