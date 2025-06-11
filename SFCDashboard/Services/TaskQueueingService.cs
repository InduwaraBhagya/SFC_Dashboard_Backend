using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDashboard.Enums;
using SFCDashboard.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SFCDashboard.Services
{
    public class TaskQueueingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TaskQueueingService> _logger;

        public TaskQueueingService(ApplicationDbContext context, ILogger<TaskQueueingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<TaskQueueItem>> GetPrioritizedTasksAsync(int? workgroupId = null, int take = 20)
        {
            var today = DateTime.Today;
            var result = new List<TaskQueueItem>();
            
            try
            {
                // Get all active tasks (not completed, not on hold)
                var query = _context.PETasks
                    .Include(t => t.PlannedEvent)
                    .Where(t => t.TaskStatus != "COMPLETED" && 
                               (t.PlannedEvent == null || t.PlannedEvent.IsHold == false));
        
                // Filter PEs - use simpler condition that EF Core can translate
                query = query.Where(t => t.PlannedEvent == null || 
                                       (t.PlannedEvent.PeNumber != null && 
                                        t.PlannedEvent.PeNumber.StartsWith("PE") &&
                                        t.PlannedEvent.PeNumber.Length >= 6));
        
                // Apply workgroup filter if specified
                if (workgroupId.HasValue)
                {
                    var workgroup = await _context.WorkGroups.FindAsync(workgroupId);
                    if (workgroup != null)
                    {
                        query = query.Where(t => t.TaskWorkGroup != null && 
                                               t.TaskWorkGroup.Contains(workgroup.Name));
                    }
                }

                // Get all tasks that match our basic filters
                var tasks = await query.ToListAsync();
                
                // Apply the year filter in memory after fetching from database
                tasks = tasks.Where(t => 
                    t.PlannedEvent == null || 
                    (t.PlannedEvent.PeNumber != null &&
                     t.PlannedEvent.PeNumber.StartsWith("PE") &&
                     t.PlannedEvent.PeNumber.Length >= 6 &&
                     int.TryParse(t.PlannedEvent.PeNumber.Substring(2, 4), out int year) &&
                     year >= 2024)
                ).ToList();

                // Calculate priority score for each task
                foreach (var task in tasks)
                {
                    // Initialize with base score for regular tasks
                    double priorityScore = (int)TaskPriority.Regular; // Start with regular priority by default
                    int daysUntilDue = 0;
                    var serviceRequiredDate = task.PlannedEvent?.ServiceRequiredDate;
                    var estimatedDate = task.EstimatedTime;
                    DateTime? effectiveDeadline = estimatedDate ?? serviceRequiredDate ?? task.TaskCompleteDate;

                    // Get the task's OLA in days (default to 1 if parsing fails)
                    int olaInDays = 1;
                    if (!string.IsNullOrEmpty(task.OLA) && int.TryParse(task.OLA, out int parsedOla))
                    {
                        olaInDays = parsedOla > 0 ? parsedOla : 1;  // Ensure minimum OLA of 1 day
                    }

                    // Calculate days until due (negative means overdue)
                    if (effectiveDeadline.HasValue)
                    {
                        daysUntilDue = (effectiveDeadline.Value.Date - today).Days;
                    }

                    // Calculate OLA percentage remaining (how much of the OLA time is left)
                    var taskStartDate = task.ActualTaskCreatedDate ?? task.TaskCreatedDate;
                    double olaPercentRemaining = 100.0;
                    
                    if (effectiveDeadline.HasValue)
                    {
                        var totalOlaDuration = (effectiveDeadline.Value.Date - taskStartDate.Date).TotalDays;
                        var daysElapsed = (today - taskStartDate.Date).TotalDays;
                        
                        // Calculate percentage of OLA time consumed
                        if (totalOlaDuration > 0)
                        {
                            olaPercentRemaining = Math.Max(0, 100 - ((daysElapsed / totalOlaDuration) * 100));
                        }
                    }

                    // 1. Check for urgent status with different priorities
                    if (task.IsUrgent)
                    {
                        if (task.Priority?.Contains("Opening Ceremony") == true)
                        {
                            // P1 - Opening Ceremony - Highest priority with a large base value
                            priorityScore = 1000 + (int)TaskPriority.UrgentOpeningCeremony;
                            
                            // Add urgency based on when it was marked (more recent = higher priority)
                            if (task.UrgentMarkedDate.HasValue)
                            {
                                var daysSinceMarked = (today - task.UrgentMarkedDate.Value.Date).Days;
                                priorityScore += Math.Max(0, 5 - daysSinceMarked); // More points if more recently marked
                            }
                        }
                        else if (task.Priority?.Contains("Critical Customer") == true)
                        {
                            // P2 - Critical Customer - Second highest priority with a large base value
                            priorityScore = 800 + (int)TaskPriority.UrgentCriticalCustomer + 1.5;
                            
                            // Add urgency based on when it was marked (more recent = higher priority)
                            if (task.UrgentMarkedDate.HasValue)
                            {
                                var daysSinceMarked = (today - task.UrgentMarkedDate.Value.Date).Days;
                                priorityScore += Math.Max(0, 5 - daysSinceMarked); // More points if more recently marked
                            }
                        }
                        else
                        {
                            // Regular urgent tasks - base value ensures they're high priority
                            priorityScore = 500 + (int)TaskPriority.UrgentCriticalCustomer;
                        }
                    }
                    
                    // 2. Check for OLA violation
                    if (task.IsOLAViolate)
                    {
                        // Reduce the base points for OLA violation (currently using TaskPriority.OLAViolation enum value)
                        priorityScore += (int)TaskPriority.OLAViolation * 0.75; // Reduce to 75% of original value
                        
                        // Reduce the additional points for overdue tasks
                        if (daysUntilDue < 0)
                        {
                            var daysOverdue = Math.Abs(daysUntilDue);
                            var percentageOverdue = (daysOverdue / (double)olaInDays) * 100;
                            // Reduce max additional points from 5 to 3 and reduce the rate of accumulation
                            var additionalPoints = Math.Min(3, percentageOverdue / 15.0); // Changed from 10.0 to 15.0
                            priorityScore += additionalPoints;
                        }
                    }
                    
                    // 3. Check for approaching deadline within OLA-based window
                    // Tasks with shorter OLAs get earlier warnings
                    else if (daysUntilDue >= 0)
                    {
                        // Calculate OLA-based warning threshold (earlier of: 2 days or 30% of OLA duration)
                        var warningThreshold = Math.Min(2, Math.Ceiling(olaInDays * 0.3));
                        
                        if (daysUntilDue <= warningThreshold)
                        {
                            priorityScore += (int)TaskPriority.ApproachingDeadline;
                            
                            // Add weight for more imminent deadlines relative to their OLA
                            // For OLA of 1-2 days: max priority when due today
                            // For longer OLAs: gradually increase priority as deadline approaches
                            var urgencyFactor = 1.0 - (daysUntilDue / (double)warningThreshold);
                            priorityScore += 2 * urgencyFactor; // Up to 2 additional points
                        }
                        // 4. Regular tasks - prioritize by OLA time remaining
                        else
                        {
                            priorityScore += (int)TaskPriority.Regular;
                            
                            // Tasks with less than 50% of OLA time remaining get boosted priority
                            // Maximum boost of 1 point when only 10% of OLA time remains
                            if (olaPercentRemaining < 50)
                            {
                                var urgencyBoost = Math.Max(0, (50 - olaPercentRemaining) / 40);
                                priorityScore += urgencyBoost;
                            }
                        }
                    }

                    // Ensure final score is never below 1.0 for active tasks
                    if (priorityScore < 1.0 && task.TaskStatus != "COMPLETED")
                    {
                        priorityScore = 1.0;
                        _logger.LogWarning("Task {id} had a score of 0, corrected to 1.0", task.Id);
                    }
                    
                    // Create queue item with calculated priority
                    result.Add(new TaskQueueItem
                    {
                        Task = task,
                        PriorityScore = priorityScore,
                        DaysUntilDue = daysUntilDue
                    });
                }

                // Sort the final list by priority score (descending)
                return result.OrderByDescending(t => t.PriorityScore).Take(take).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating task priorities");
                return new List<TaskQueueItem>();
            }
        }

        public async Task<TaskQueueItem> GetNextTaskAsync(int? workgroupId = null)
        {
            var prioritizedTasks = await GetPrioritizedTasksAsync(workgroupId, take: 1);
            return prioritizedTasks.Count > 0 ? prioritizedTasks[0] : null;
        }

        private string GetPriorityLevelName(double priorityScore)
        {
            // Updated priority level determination with new score ranges
            if (priorityScore >= 1000)
                return "URGENT P1";
            else if (priorityScore >= 800)
                return "URGENT P2"; 
            else if (priorityScore >= 500)
                return "URGENT";
            else if (priorityScore >= 5)
                return "Medium";
            else
                return "Low";
        }

        private string GetDueStatusText(int daysUntilDue)
        {
            if (daysUntilDue < 0)
                return $"Overdue by {Math.Abs(daysUntilDue)} days";
            else if (daysUntilDue == 0)
                return "Due today";
            else if (daysUntilDue == 1)
                return "Due tomorrow";
            else
                return $"Due in {daysUntilDue} days";
        }
    }

    public class TaskQueueItem
    {
        public PETask Task { get; set; }
        public double PriorityScore { get; set; }
        public int DaysUntilDue { get; set; }
        public DateTime? EffectiveDeadline { get; set; }
        public int OLAInDays { get; set; }
        public double OLAPercentRemaining { get; set; }
        public DateTime? UrgentMarkedDate => Task?.UrgentMarkedDate;

        // Helper properties for UI display
        public bool IsOverdue => DaysUntilDue < 0;
        
        public string DueStatus 
        {
            get
            {
                if (IsOverdue)
                    return $"Overdue by {Math.Abs(DaysUntilDue)} days";
                else if (DaysUntilDue == 0)
                    return "Due today";
                else if (DaysUntilDue == 1)
                    return "Due tomorrow";
                else
                    return $"Due in {DaysUntilDue} days";
            }
        }
        
        public string OLAStatus
        {
            get
            {
                if (Task.IsOLAViolate)
                    return "OLA Violated";
                else if (OLAPercentRemaining <= 10)
                    return "Critical (≤10% OLA left)";
                else if (OLAPercentRemaining <= 30)
                    return "Warning (≤30% OLA left)";
                else
                    return $"{OLAPercentRemaining:F0}% of OLA remains";
            }
        }
        
        public string PriorityLevel 
        {
            get 
            {
                if (Task.IsUrgent)
                {
                    if (Task.Priority?.Contains("Opening Ceremony") == true)
                        return "URGENT P1";
                    else if (Task.Priority?.Contains("Critical Customer") == true)
                        return "URGENT P2";
                    else
                        return "URGENT";
                }
                else if (Task.IsOLAViolate)
                    return "OLA VIOLATION";
                else if (DaysUntilDue >= 0 && DaysUntilDue <= Math.Min(2, Math.Ceiling(OLAInDays * 0.3)))
                    return "APPROACHING DEADLINE";
                else
                    return "REGULAR";
            }
        }
    }
}

