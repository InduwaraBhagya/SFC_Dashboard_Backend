using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SFCDashboard.Data;
using SFCDashboard.Enums;
using SFCDashboard.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SFCDashboard.Services
{
    public class TaskQueueingService : ITaskQueueService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TaskQueueingService> _logger;
        private readonly IMemoryCache _cache;
        private const string WORKGROUP_CACHE_KEY = "Workgroup_{0}";
        private readonly TimeSpan _workgroupCacheTime = TimeSpan.FromMinutes(30);

        public TaskQueueingService(ApplicationDbContext context, ILogger<TaskQueueingService> logger, IMemoryCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }        public async Task<List<TaskQueueItem>> GetPrioritizedTasksAsync(int? workgroupId = null, int take = 20, int? year = null)

        public async Task<List<TaskQueueItem>> GetPrioritizedTasksAsync(int? workgroupId = null, int take = 20)
        {
            var stopwatch = Stopwatch.StartNew();
            var today = DateTime.Today;
            var result = new List<TaskQueueItem>();
            
            try
            {
                // Get all active tasks (not completed, not on hold)
                var query = _context.PETasks
                    .Include(t => t.PlannedEvent)
                    .AsNoTracking() // Don't track entities since we're just reading
                    .Where(t => t.TaskStatus != "COMPLETED" && 
                               (t.PlannedEvent == null || t.PlannedEvent.IsHold == false));                // Apply year filter based on PE number if specified
                if (year.HasValue)
                {
                    query = query.Where(t => t.PlannedEvent != null && 
                                           !string.IsNullOrEmpty(t.PlannedEvent.PeNumber) &&
                                        t.PlannedEvent.PeNumber.Length >= 6 &&
                                           t.PlannedEvent.PeNumber.Substring(2, 4) == year.Value.ToString());
                }
        
                // Apply workgroup filter if specified
                if (workgroupId.HasValue)
                {
                    var workgroup = await GetWorkgroupAsync(workgroupId.Value);
                    if (workgroup != null)
                    {
                        query = query.Where(t => t.TaskWorkGroup != null && 
                                               t.TaskWorkGroup.Contains(workgroup.Name));
                    }
                }

                // Use projection to select only needed data
                var tasksData = await query
                    .Select(t => new {
                        Task = t,
                        StartDate = t.ActualTaskCreatedDate ?? t.TaskCreatedDate,
                        EffectiveDeadline = t.EstimatedTime ?? t.PlannedEvent.ServiceRequiredDate ?? t.TaskCompleteDate,
                        TaskOLA = t.OLA
                    })
                    .ToListAsync();

                // Process tasks in parallel for better performance with large datasets
                var taskItems = tasksData.AsParallel().Select(data => {
                    var task = data.Task;
                    
                    // Calculate days until due and OLA metrics
                    int daysUntilDue = 0;
                    if (data.EffectiveDeadline != null)
                    {
                        // Remove .Value since EffectiveDeadline is not nullable
                        daysUntilDue = (data.EffectiveDeadline.Date - today).Days;
                    }

                    // Parse OLA in days
                    int olaInDays = 1; // Default
                    if (!string.IsNullOrEmpty(data.TaskOLA) && int.TryParse(data.TaskOLA, out int parsedOla))
                    {
                        olaInDays = parsedOla > 0 ? parsedOla : 1;
                    }

                    // Calculate OLA percentage remaining
                    double olaPercentRemaining = 100.0;
                    if (data.EffectiveDeadline != null)
                    {
                        // Remove .Value since EffectiveDeadline is not nullable
                        var totalOlaDuration = (data.EffectiveDeadline.Date - data.StartDate.Date).TotalDays;
                        var daysElapsed = (today - data.StartDate.Date).TotalDays;
                        
                        if (totalOlaDuration > 0)
                        {
                            olaPercentRemaining = Math.Max(0, 100 - ((daysElapsed / totalOlaDuration) * 100));
                        }
                    }

                    // Calculate priority score using helper method
                    double priorityScore = CalculateTaskPriority(task, today, daysUntilDue, olaInDays, olaPercentRemaining);
                    
                    return new TaskQueueItem
                    {
                        Task = task,
                        PriorityScore = priorityScore,
                        DaysUntilDue = daysUntilDue,
                        EffectiveDeadline = data.EffectiveDeadline,
                        OLAInDays = olaInDays,
                        OLAPercentRemaining = olaPercentRemaining
                    };
                }).ToList();

                // Sort the final list by priority score (descending) and take the requested number
                result = taskItems
                    .OrderByDescending(t => t.PriorityScore)
                    .Take(take)
                    .ToList();

                stopwatch.Stop();
                _logger.LogInformation("Task prioritization completed in {ElapsedMs}ms for {Count} tasks, returning {TakeCount}", 
                    stopwatch.ElapsedMilliseconds, taskItems.Count, result.Count);
                
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error calculating task priorities after {ElapsedMs}ms", 
                    stopwatch.ElapsedMilliseconds);
                return new List<TaskQueueItem>();
            }
        }

        public async Task<TaskQueueItem> GetNextTaskAsync(int? workgroupId = null)
        {
            var prioritizedTasks = await GetPrioritizedTasksAsync(workgroupId, take: 1);
            return prioritizedTasks.Count > 0 ? prioritizedTasks[0] : null;
        }

        private async Task<WorkGroup> GetWorkgroupAsync(int workgroupId)
        {
            string cacheKey = string.Format(WORKGROUP_CACHE_KEY, workgroupId);
            
            if (!_cache.TryGetValue(cacheKey, out WorkGroup workgroup))
            {
                workgroup = await _context.WorkGroups.FindAsync(workgroupId);
                
                if (workgroup != null)
                {
                    _cache.Set(cacheKey, workgroup, _workgroupCacheTime);
                }
            }
            
            return workgroup;
        }

        private double CalculateTaskPriority(PETask task, DateTime today, int daysUntilDue, int olaInDays, double olaPercentRemaining)
        {
            // Initialize with base score for regular tasks
            double priorityScore = (int)TaskPriority.Regular;

            // 1. Check for urgent status with different priorities
            if (task.IsUrgent)
            {
                priorityScore = CalculateUrgentTaskPriority(task, today);
            }
            // 2. Check for OLA violation
            else if (task.IsOLAViolate)
            {
                priorityScore = CalculateOlaViolationPriority(task, daysUntilDue, olaInDays);
            }
            // 3. Check for approaching deadline within OLA-based window
            else if (daysUntilDue >= 0)
            {
                priorityScore = CalculateApproachingDeadlinePriority(daysUntilDue, olaInDays, olaPercentRemaining);
            }

            // Ensure final score is never below 1.0 for active tasks
            if (priorityScore < 1.0 && task.TaskStatus != "COMPLETED")
            {
                priorityScore = 1.0;
                _logger.LogWarning("Task {id} had a score of 0, corrected to 1.0", task.Id);
            }

            return priorityScore;
        }

        private double CalculateUrgentTaskPriority(PETask task, DateTime today)
        {
            double priorityScore;
            
            if (task.Priority?.Contains("Opening Ceremony") == true)
            {
                // P1 - Opening Ceremony - Highest priority
                priorityScore = 1000 + (int)TaskPriority.UrgentOpeningCeremony;
                
                // Add urgency based on when it was marked
                if (task.UrgentMarkedDate.HasValue)
                {
                    var daysSinceMarked = (today - task.UrgentMarkedDate.Value.Date).Days;
                    priorityScore += Math.Max(0, 5 - daysSinceMarked);
                }
            }
            else if (task.Priority?.Contains("Critical Customer") == true)
            {
                // P2 - Critical Customer - Second highest priority
                priorityScore = 800 + (int)TaskPriority.UrgentCriticalCustomer + 1.5;
                
                // Add urgency based on when it was marked
                if (task.UrgentMarkedDate.HasValue)
                {
                    var daysSinceMarked = (today - task.UrgentMarkedDate.Value.Date).Days;
                    priorityScore += Math.Max(0, 5 - daysSinceMarked);
                }
            }
            else
            {
                // Regular urgent tasks
                priorityScore = 500 + (int)TaskPriority.UrgentCriticalCustomer;
            }
            
            return priorityScore;
        }

        private double CalculateOlaViolationPriority(PETask task, int daysUntilDue, int olaInDays)
        {
            // Start with reduced base points for OLA violation
            double priorityScore = (int)TaskPriority.OLAViolation * 0.75;
            
            // Add additional points for overdue tasks
            if (daysUntilDue < 0)
            {
                var daysOverdue = Math.Abs(daysUntilDue);
                var percentageOverdue = (daysOverdue / (double)olaInDays) * 100;
                var additionalPoints = Math.Min(3, percentageOverdue / 15.0);
                priorityScore += additionalPoints;
            }
            
            return priorityScore;
        }

        private double CalculateApproachingDeadlinePriority(int daysUntilDue, int olaInDays, double olaPercentRemaining)
        {
            double priorityScore = (int)TaskPriority.Regular;
            
            // Calculate OLA-based warning threshold
            var warningThreshold = Math.Min(2, Math.Ceiling(olaInDays * 0.3));
            
            if (daysUntilDue <= warningThreshold)
            {
                priorityScore = (int)TaskPriority.ApproachingDeadline;
                
                // Add weight for more imminent deadlines relative to their OLA
                var urgencyFactor = 1.0 - (daysUntilDue / (double)warningThreshold);
                priorityScore += 2 * urgencyFactor; // Up to 2 additional points
            }
            else
            {
                // Tasks with less than 50% of OLA time remaining get boosted priority
                if (olaPercentRemaining < 50)
                {
                    var urgencyBoost = Math.Max(0, (50 - olaPercentRemaining) / 40);
                    priorityScore += urgencyBoost;
                }
            }
            
            return priorityScore;
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

        /// <summary>
        /// Extracts the year from a PE number string (e.g., "PE2023120507554" -> 2023)
        /// </summary>
        /// <param name="peNumber">The PE number string</param>
        /// <returns>The year if extraction is successful, null otherwise</returns>
        private static int? ExtractYearFromPeNumber(string peNumber)
        {
            if (string.IsNullOrEmpty(peNumber) || peNumber.Length < 6)
                return null;

            var yearStr = peNumber.Substring(2, 4);
            return int.TryParse(yearStr, out var year) ? year : null;
        }        public async Task<List<int>> GetAvailableYearsAsync()
        {
            try
            {                var peNumbers = await _context.PlannedEvents
                    .AsNoTracking()
                    .Where(pe => !string.IsNullOrEmpty(pe.PeNumber) && pe.PeNumber.Length >= 6)
                    .Select(pe => pe.PeNumber!)
                    .Distinct()
                    .ToListAsync();
                  var validYears = peNumbers
                    .Where(peNumber => !string.IsNullOrEmpty(peNumber))
                    .Select(peNumber => ExtractYearFromPeNumber(peNumber))
                    .Where(year => year.HasValue)
                    .Select(year => year!.Value)
                    .Distinct()
                    .OrderByDescending(year => year)
                    .ToList();
                
                return validYears.Any() ? validYears : new List<int> { DateTime.Now.Year };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available years from PE numbers");
                return new List<int> { DateTime.Now.Year };
            }
        }public async Task<Dictionary<int, int>> GetTaskCountByYearAsync(int? workgroupId = null)
        {
            try
            {
                var query = _context.PETasks
                    .Include(t => t.PlannedEvent)
                    .AsNoTracking()
                    .Where(t => t.TaskStatus != "COMPLETED" && 
                               (t.PlannedEvent == null || t.PlannedEvent.IsHold == false) &&
                               t.PlannedEvent != null && 
                               !string.IsNullOrEmpty(t.PlannedEvent.PeNumber) &&
                               t.PlannedEvent.PeNumber.Length >= 6);

                // Apply workgroup filter if specified
                if (workgroupId.HasValue)
                {
                    var workgroup = await GetWorkgroupAsync(workgroupId.Value);
                    if (workgroup != null)
                    {
                        query = query.Where(t => t.TaskWorkGroup != null && 
                                               t.TaskWorkGroup.Contains(workgroup.Name));
                    }
                }                var tasksWithPeNumbers = await query
                    .Select(t => new { 
                        Task = t, 
                        PeNumber = t.PlannedEvent != null ? t.PlannedEvent.PeNumber : null
                    })
                    .ToListAsync();

                var yearCounts = tasksWithPeNumbers
                    .Where(t => !string.IsNullOrEmpty(t.PeNumber))
                    .Select(t => new { Task = t.Task, Year = ExtractYearFromPeNumber(t.PeNumber!) })
                    .Where(t => t.Year.HasValue)
                    .GroupBy(t => t.Year!.Value)
                    .ToDictionary(g => g.Key, g => g.Count())
                    .OrderByDescending(kvp => kvp.Key)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                return yearCounts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task count by year from PE numbers");
                return new Dictionary<int, int>();
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