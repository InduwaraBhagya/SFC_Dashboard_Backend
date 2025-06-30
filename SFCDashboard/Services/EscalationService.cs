using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDashboard.Models;

namespace SFCDashboard.Services
{
    public class EscalationService
    {
        private readonly ApplicationDbContext _context;

        public EscalationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CheckAndCreateEscalationsAsync()
        {
            try
            {
                // Get all OLA violated tasks that don't have escalations disabled
                var violatedTasks = await _context.PETasks
                    .Where(t => t.TaskStatus != "Completed" && t.IsOLAViolate && !t.EscalationsDisabled)
                    .ToListAsync();

                foreach (var task in violatedTasks)
                {
                    // Calculate violation duration
                    var violationDuration = DateTime.Now - task.OLADateTime;
                    
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
                            Level = escalationLevel,
                            Title = $"Task {task.PENumber} OLA Violation - Level {escalationLevel}",
                            Message = CreateEscalationMessage(task, escalationLevel, violationDuration),
                            CreatedAt = DateTime.Now,
                            IsRead = false,
                            IsResolved = false
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

            return $"Task {task.PENumber} requires {levelText}. " +
                   $"OLA violation duration: {duration.TotalDays:F1} days. " +
                   $"Task started: {task.OLADateTime:g}";
        }

        // Get escalations by role level for display
        public async Task<List<Escalation>> GetEscalationsByUserRoleAsync(int userRoleLevel)
        {
            // Users with role level 0 (normal users) see level 1 escalations
            // Users with higher role levels see escalations >= their role level
            int minEscalationLevel = userRoleLevel == 0 ? 1 : userRoleLevel;            return await _context.Escalations
                .Include(e => e.PETask)
                .Where(e => !e.IsResolved && e.Level >= minEscalationLevel)
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

        // Resolve escalation
        public async Task ResolveEscalationAsync(int escalationId)
        {
            var escalation = await _context.Escalations.FindAsync(escalationId);
            if (escalation != null)
            {
                escalation.IsResolved = true;
                escalation.IsRead = true;
                
                // Disable future escalations for this task
                var task = await _context.PETasks.FindAsync(escalation.TaskId);
                if (task != null)
                {
                    task.EscalationsDisabled = true;
                }
                
                await _context.SaveChangesAsync();
            }
        }

        // Get escalation statistics
        public async Task<object> GetEscalationStatsAsync()
        {
            var stats = await _context.Escalations
                .GroupBy(e => e.Level)
                .Select(g => new {
                    Level = g.Key,
                    Total = g.Count(),
                    Unread = g.Count(e => !e.IsRead),
                    Unresolved = g.Count(e => !e.IsResolved)
                })
                .ToListAsync();

            return stats;
        }
    }
}