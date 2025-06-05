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

                // Get all active tasks
                var tasks = await query.ToListAsync();

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
                            priorityScore += (int)TaskPriority.UrgentOpeningCeremony;
                        else if (task.Priority?.Contains("Critical Customer") == true)
                            priorityScore += (int)TaskPriority.UrgentCriticalCustomer;
                        else
                            priorityScore += (int)TaskPriority.UrgentCriticalCustomer; // Default urgent priority
                    }
                    
                    // 2. Check for OLA violation
                    if (task.IsOLAViolate)
                    {
                        priorityScore += (int)TaskPriority.OLAViolation;
                        
                        // Add additional weight based on how overdue the task is relative to its OLA
                        // (1 point per 10% of OLA period overdue, max 5 additional points)
                        if (daysUntilDue < 0)
                        {
                            var daysOverdue = Math.Abs(daysUntilDue);
                            var percentageOverdue = (daysOverdue / (double)olaInDays) * 100;
                            var additionalPoints = Math.Min(5, percentageOverdue / 10.0);
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
                        DaysUntilDue = daysUntilDue,
                        EffectiveDeadline = effectiveDeadline,
                        OLAInDays = olaInDays,
                        OLAPercentRemaining = olaPercentRemaining
                    });
                }

                // Sort by priority score (descending), then by days until due (ascending)
                return result
                    .OrderByDescending(t => t.PriorityScore)
                    .ThenBy(t => t.DaysUntilDue)
                    .Take(take)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating task priorities");
                return new List<TaskQueueItem>();
            }
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

