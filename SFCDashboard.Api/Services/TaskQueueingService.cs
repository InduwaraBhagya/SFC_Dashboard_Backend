using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SFCDashboard.Api.Data;
using SFCDashboard.Enums;
using SFCDashboard.Api.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SFCDashboard.Api.Services
{
    public class TaskQueueingService : ITaskQueueService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TaskQueueingService> _logger;
        private readonly IMemoryCache _cache;
        private const string WORKGROUP_CACHE_KEY = "Workgroup_{0}";
        private const string TASKQUEUE_CACHE_KEY = "TaskQueue_{0}_{1}_{2}"; // workgroupId_year_take
        private readonly TimeSpan _workgroupCacheTime = TimeSpan.FromMinutes(30);
        private readonly TimeSpan _taskQueueCacheTime = TimeSpan.FromMinutes(5);

        public TaskQueueingService(ApplicationDbContext context, ILogger<TaskQueueingService> logger, IMemoryCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }

        public async Task<List<TaskQueueItem>> GetPrioritizedTasksAsync(int? workgroupId = null, int? year = null, int take = 20)
        {
            // Check cache first for better performance
            string cacheKey = string.Format(TASKQUEUE_CACHE_KEY, workgroupId ?? 0, year ?? 0, take);
            
            if (_cache.TryGetValue(cacheKey, out List<TaskQueueItem>? cachedResult))
            {
                _logger.LogDebug("Task queue cache hit for key: {CacheKey}", cacheKey);
                return cachedResult ?? new List<TaskQueueItem>();
            }

            var stopwatch = Stopwatch.StartNew();
            var result = new List<TaskQueueItem>();
            
            try
            {
                _logger.LogDebug("Task queue cache miss for key: {CacheKey}, fetching from database snapshots", cacheKey);
                
                // Use TaskQueueSnapshots for much faster retrieval
                var query = _context.TaskQueueSnapshots
                    .Include(tqs => tqs.Task)
                        .ThenInclude(t => t.PlannedEvent)
                    .Include(tqs => tqs.WorkGroup)
                    .AsNoTracking();

                // Apply year filter if specified
                if (year.HasValue)
                {
                    query = query.Where(tqs => tqs.Year == year.Value);
                }

                // Apply workgroup filter if specified
                if (workgroupId.HasValue)
                {
                    query = query.Where(tqs => tqs.WorkGroupId == workgroupId.Value);
                }

                // Get top results ordered by priority score (descending)
                var snapshots = await query
                    .OrderByDescending(tqs => tqs.PriorityScore)
                    .ToListAsync();

                // If year is null (getting all years), we need to deduplicate by TaskId
                if (!year.HasValue)
                {
                    snapshots = snapshots
                        .GroupBy(tqs => tqs.TaskId)
                        .Select(group => group.OrderByDescending(tqs => tqs.PriorityScore).First())
                        .OrderByDescending(tqs => tqs.PriorityScore)
                        .Take(take)
                        .ToList();
                }
                else
                {
                    snapshots = snapshots.Take(take).ToList();
                }

                // Convert snapshots to TaskQueueItems
                var taskQueueItems = snapshots.Select(snapshot => new TaskQueueItem
                {
                    Task = snapshot.Task,
                    PriorityScore = snapshot.PriorityScore,
                    DaysUntilDue = snapshot.DaysUntilDue,
                    EffectiveDeadline = snapshot.EffectiveDeadline,
                    OLAInDays = snapshot.OLAInDays,
                    OLAPercentRemaining = snapshot.OLAPercentRemaining
                }).ToList();

                // Apply "current task only" filtering - one task per PlannedEvent
                var currentTasksOnly = taskQueueItems
                    .Where(tqi => tqi.Task.PlannedEvent != null)
                    .GroupBy(tqi => tqi.Task.PlannedEvent!.Id)
                    .Select(group => group.OrderByDescending(tqi => tqi.PriorityScore).First())
                    .ToList();

                // Add tasks without PlannedEvent (standalone tasks)
                var standaloneTasks = taskQueueItems
                    .Where(tqi => tqi.Task.PlannedEvent == null)
                    .ToList();

                currentTasksOnly.AddRange(standaloneTasks);

                // Sort by priority score and return final result
                result = currentTasksOnly.OrderByDescending(tqi => tqi.PriorityScore).ToList();

                stopwatch.Stop();
                _logger.LogInformation("Retrieved {count} task queue items from snapshots in {elapsed}ms for workgroup {workgroupId}, year {year}", 
                    result.Count, stopwatch.ElapsedMilliseconds, workgroupId, year);

                // Cache the result
                _cache.Set(cacheKey, result, _taskQueueCacheTime);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting prioritized tasks from snapshots");
                return new List<TaskQueueItem>();
            }
        }

        public async Task<TaskQueueItem?> GetNextTaskAsync(int? workgroupId = null, int? year = null)
        {
            var prioritizedTasks = await GetPrioritizedTasksAsync(workgroupId, year, take: 1);
            return prioritizedTasks.Count > 0 ? prioritizedTasks[0] : null;
        }

        public async Task<List<int>> GetAvailableYearsAsync()
        {
            try
            {
                // Get distinct years from PE numbers in the format PE2023xxx
                var years = await _context.PlannedEvents
                    .Where(p => p.PeNumber != null && 
                               p.PeNumber.StartsWith("PE") && 
                               p.PeNumber.Length >= 6 &&
                               EF.Functions.Like(p.PeNumber, "PE2[0-9][2-9][0-9]%"))
                    .Select(p => p.PeNumber!.Substring(2, 4))
                    .Distinct()
                    .ToListAsync();

                // Convert to integers and filter valid years, then sort descending
                var validYears = years
                    .Where(y => int.TryParse(y, out int year) && year >= 2020 && year <= DateTime.Now.Year + 1)
                    .Select(y => int.Parse(y))
                    .OrderByDescending(y => y)
                    .ToList();

                _logger.LogInformation("Found {count} available years: {years}", 
                    validYears.Count, string.Join(", ", validYears));

                return validYears;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available years from PE numbers");
                // Return default years if there's an error
                var currentYear = DateTime.Now.Year;
                return Enumerable.Range(currentYear - 4, 5).OrderByDescending(y => y).ToList();
            }
        }

        private async Task<WorkGroup?> GetWorkgroupAsync(int workgroupId)
        {
            string cacheKey = string.Format(WORKGROUP_CACHE_KEY, workgroupId);
            
            if (!_cache.TryGetValue(cacheKey, out WorkGroup? workgroup))
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
        /// Clear task queue cache for a specific workgroup
        /// </summary>
        public void ClearTaskQueueCache(int? workgroupId = null)
        {
            try
            {
                // Clear all cache entries for the specified workgroup
                var patterns = new List<string>();
                
                // Generate cache key patterns to clear
                for (int year = 2020; year <= DateTime.Now.Year + 1; year++)
                {
                    for (int take = 1; take <= 100; take += 19) // Common take values: 1, 20, 39, etc.
                    {
                        patterns.Add(string.Format(TASKQUEUE_CACHE_KEY, workgroupId ?? 0, year, take));
                        patterns.Add(string.Format(TASKQUEUE_CACHE_KEY, workgroupId ?? 0, 0, take)); // year = null
                    }
                }

                foreach (var pattern in patterns)
                {
                    _cache.Remove(pattern);
                }

                _logger.LogDebug("Cleared task queue cache for workgroup: {WorkgroupId}", workgroupId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing task queue cache for workgroup: {WorkgroupId}", workgroupId);
            }
        }

        /// <summary>
        /// Clear all task queue cache entries
        /// </summary>
        public void ClearAllTaskQueueCache()
        {
            try
            {
                // This is a simple implementation. In a production environment,
                // you might want to implement a more sophisticated cache key tracking mechanism
                _logger.LogDebug("Clearing all task queue cache entries");
                
                // Note: IMemoryCache doesn't provide a way to enumerate keys,
                // so we can't clear specific patterns. This method is here for future enhancement.
                // Consider using a different caching strategy if you need to clear all related cache entries.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing all task queue cache");
            }
        }

        /// <summary>
        /// Manually refresh task queue snapshots for specific workgroup and year
        /// </summary>
        public async Task RefreshTaskQueueSnapshotsAsync(int? workgroupId = null, int? year = null)
        {
            try
            {
                _logger.LogInformation("Manually refreshing task queue snapshots for workgroup {workgroupId}, year {year}", workgroupId, year);
                
                // If no specific workgroup provided, refresh all workgroups
                var workgroupsToRefresh = new List<int>();
                
                if (workgroupId.HasValue)
                {
                    workgroupsToRefresh.Add(workgroupId.Value);
                }
                else
                {
                    // Get all workgroup IDs
                    var allWorkgroups = await _context.WorkGroups.Select(w => w.Id).ToListAsync();
                    workgroupsToRefresh.AddRange(allWorkgroups);
                }

                // If no specific year provided, refresh all available years
                var yearsToRefresh = new List<int?>();
                
                if (year.HasValue)
                {
                    yearsToRefresh.Add(year.Value);
                }
                else
                {
                    var availableYears = await GetAvailableYearsAsync();
                    yearsToRefresh.AddRange(availableYears.Cast<int?>());
                    yearsToRefresh.Add(null); // Also include "all years"
                }

                // Refresh snapshots
                foreach (var wgId in workgroupsToRefresh)
                {
                    foreach (var yr in yearsToRefresh)
                    {
                        await RefreshTaskQueueForWorkgroupAndYearAsync(wgId, yr);
                    }
                }

                // Clear related cache entries
                foreach (var wgId in workgroupsToRefresh)
                {
                    ClearTaskQueueCache(wgId);
                }
                
                _logger.LogInformation("Completed manual refresh of task queue snapshots");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error manually refreshing task queue snapshots");
                throw;
            }
        }

        private async Task RefreshTaskQueueForWorkgroupAndYearAsync(int workgroupId, int? year)
        {
            try
            {
                _logger.LogDebug("Refreshing task queue snapshots for workgroup {workgroupId}, year {year}", workgroupId, year?.ToString() ?? "all");

                // Delete existing snapshots for this workgroup and year
                var existingSnapshots = _context.TaskQueueSnapshots
                    .Where(tqs => tqs.WorkGroupId == workgroupId && tqs.Year == year);
                _context.TaskQueueSnapshots.RemoveRange(existingSnapshots);

                // Note: Snapshot refresh is now handled by the background service
                // This method only clears existing snapshots to trigger fresh generation
                await _context.SaveChangesAsync();

                _logger.LogDebug("Cleared existing task queue snapshots for workgroup {workgroupId}, year {year}", 
                    workgroupId, year?.ToString() ?? "all");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing task queue snapshots for workgroup {workgroupId}, year {year}", workgroupId, year?.ToString() ?? "all");
                throw;
            }
        }
    }
}

