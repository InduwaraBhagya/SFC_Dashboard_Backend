using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SFCDashboard.Background.Data;
using SFCDashboard.Background.Models;
using SFCDashboard.Background.Enums;

namespace SFCDashboard.Background.Services
{
    public class TaskQueueService : BackgroundService
    {
        private readonly ILogger<TaskQueueService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly Timer _timer;

        // Run every 10 minutes
        private readonly TimeSpan _period = TimeSpan.FromMinutes(10);

        public TaskQueueService(ILogger<TaskQueueService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _timer = new Timer(ExecuteTaskQueueRefresh, null, TimeSpan.Zero, _period);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 🚀 TaskQueue Background Service started");
            _logger.LogInformation("TaskQueue Background Service started at: {time}", DateTimeOffset.Now);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RefreshTaskQueueSnapshotsAsync();
                    
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ⏰ Waiting {_period.TotalMinutes} minutes until next refresh...");
                    await Task.Delay(_period, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 🛑 TaskQueue service is being stopped");
                    _logger.LogInformation("TaskQueue service is being stopped");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ❌ ERROR in TaskQueue background service: {ex.Message}");
                    _logger.LogError(ex, "Error occurred in TaskQueue background service");
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ⏸️  Waiting 5 minutes before retry...");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Wait 5 minutes before retry
                }
            }
        }

        private void ExecuteTaskQueueRefresh(object? state)
        {
            // This is used by the timer for additional refreshes
            _ = Task.Run(async () =>
            {
                try
                {
                    await RefreshTaskQueueSnapshotsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in timer-triggered task queue refresh");
                }
            });
        }

        private async Task RefreshTaskQueueSnapshotsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 🔄 Starting task queue snapshots refresh...");
                _logger.LogInformation("Starting task queue snapshots refresh at {time}", DateTime.Now);

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // Get all workgroups
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 📋 Loading workgroups...");
                var workgroups = await context.WorkGroups.ToListAsync();
                
                // Get available years from PlannedEvents
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 📅 Loading available years...");
                var availableYears = await context.PlannedEvents
                    .Where(pe => pe.ServiceRequiredDate.HasValue)
                    .Select(pe => pe.ServiceRequiredDate!.Value.Year)
                    .Distinct()
                    .ToListAsync();

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ✅ Found {workgroups.Count} workgroups and {availableYears.Count} years to process");

                int totalOperations = workgroups.Count * availableYears.Count; // Only specific years, no null year
                int currentOperation = 0;

                foreach (var workgroup in workgroups)
                {
                    
                    foreach (var year in availableYears)
                    {
                        currentOperation++;
                        var progress = (double)currentOperation / totalOperations * 100;
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ⚙️  Processing Task Queue : ({progress:F1}% complete)");
                        
                        await RefreshTaskQueueForWorkgroupAndYearAsync(context, workgroup.Id, year);
                    }
                }

                stopwatch.Stop();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ✅ Completed task queue snapshots refresh in {stopwatch.ElapsedMilliseconds}ms");
                _logger.LogInformation("Completed task queue snapshots refresh in {elapsed}ms. Processed {workgroupCount} workgroups and {yearCount} years", 
                    stopwatch.ElapsedMilliseconds, workgroups.Count, availableYears.Count);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ❌ ERROR refreshing task queue snapshots: {ex.Message}");
                _logger.LogError(ex, "Error refreshing task queue snapshots");
            }
        }

        private async Task RefreshTaskQueueForWorkgroupAndYearAsync(ApplicationDbContext context, int workgroupId, int? year)
        {
            try
            {
                var yearDisplay = year?.ToString() ?? "all years";
                _logger.LogDebug("Refreshing task queue for workgroup {workgroupId}, year {year}", workgroupId, year?.ToString() ?? "all");

                // Delete existing snapshots for this workgroup and year using raw SQL to avoid concurrency issues
                if (year.HasValue)
                {
                    await context.Database.ExecuteSqlRawAsync(
                        "DELETE FROM TaskQueueSnapshots WHERE WorkGroupId = {0} AND Year = {1}", 
                        workgroupId, year.Value);
                }
                else
                {
                    await context.Database.ExecuteSqlRawAsync(
                        "DELETE FROM TaskQueueSnapshots WHERE WorkGroupId = {0} AND Year IS NULL", 
                        workgroupId);
                }

                // Generate new task queue data
                var taskQueueItems = await GenerateTaskQueueAsync(context, workgroupId, year);

                // Create new snapshots
                var snapshots = taskQueueItems.Select(item => new TaskQueueSnapshot
                {
                    WorkGroupId = workgroupId,
                    TaskId = item.Task.Id,
                    PriorityScore = item.PriorityScore,
                    DaysUntilDue = item.DaysUntilDue,
                    EffectiveDeadline = item.EffectiveDeadline,
                    OLAInDays = item.OLAInDays,
                    OLAPercentRemaining = item.OLAPercentRemaining,
                    CreatedAt = DateTime.Now,
                    Year = year
                }).ToList();

                context.TaskQueueSnapshots.AddRange(snapshots);
                await context.SaveChangesAsync();

                _logger.LogDebug("Created {count} task queue snapshots for workgroup {workgroupId}, year {year}", 
                    snapshots.Count, workgroupId, year?.ToString() ?? "all");
            }
            catch (Exception ex)
            {
                var yearDisplay = year?.ToString() ?? "all years";
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}]   ❌ ERROR refreshing workgroup {workgroupId}, {yearDisplay}: {ex.Message}");
                _logger.LogError(ex, "Error refreshing task queue for workgroup {workgroupId}, year {year}", workgroupId, year?.ToString() ?? "all");
            }
        }

        private async Task<List<TaskQueueItem>> GenerateTaskQueueAsync(ApplicationDbContext context, int workgroupId, int? year)
        {
            var today = DateTime.Today;
            var workgroup = await context.WorkGroups.FindAsync(workgroupId);
            
            if (workgroup == null)
            {
                _logger.LogWarning("Workgroup {workgroupId} not found", workgroupId);
                return new List<TaskQueueItem>();
            }

            // Get all active tasks (not completed, not on hold)
            var query = context.PETasks
                .Include(t => t.PlannedEvent)
                .AsNoTracking()
                .Where(t => t.TaskStatus != "COMPLETED" && 
                           (t.PlannedEvent == null || t.PlannedEvent.IsHold == false));

            // Filter by workgroup
            query = query.Where(t => t.PlannedEvent == null || 
                                t.PlannedEvent.TaskWg == workgroup.Name);

            // Filter by year if specified
            if (year.HasValue)
            {
                query = query.Where(t => t.PlannedEvent == null || 
                                    (t.PlannedEvent.ServiceRequiredDate.HasValue && 
                                     t.PlannedEvent.ServiceRequiredDate.Value.Year == year.Value));
            }

            var tasks = await query.ToListAsync();
            var taskQueueItems = new List<TaskQueueItem>();

            foreach (var task in tasks)
            {
                try
                {
                    var queueItem = await CreateTaskQueueItemAsync(context, task, today, workgroup);
                    if (queueItem != null)
                    {
                        taskQueueItems.Add(queueItem);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating task queue item for task {taskId}", task.Id);
                }
            }

            // Group by PlannedEvent and return only the current task (highest priority) for each PE
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

            // Sort by priority score (descending)
            return currentTasksOnly.OrderByDescending(tqi => tqi.PriorityScore).ToList();
        }

        private async Task<TaskQueueItem?> CreateTaskQueueItemAsync(ApplicationDbContext context, PETask task, DateTime today, WorkGroup workgroup)
        {
            try
            {
                var taskList = await context.PETaskLists
                    .FirstOrDefaultAsync(tl => tl.TaskSeq == task.TaskSeq);
                if (taskList == null)
                {
                    _logger.LogWarning("TaskList not found for task {taskId} with TaskSeq {taskSeq}", task.Id, task.TaskSeq);
                    return null;
                }

                // Parse OLA from task list
                var olaInDays = ParseOLAFromParameters(taskList.OLA_Parameters);
                
                // Calculate effective deadline
                var effectiveDeadline = task.PlannedEvent?.ServiceRequiredDate?.AddDays(-olaInDays);
                var daysUntilDue = effectiveDeadline.HasValue ? 
                    (int)(effectiveDeadline.Value.Date - today).TotalDays : int.MaxValue;

                // Calculate OLA percentage remaining
                var olaPercentRemaining = CalculateOLAPercentRemaining(task, today, olaInDays);

                // Calculate priority score
                var priorityScore = CalculateTaskPriority(task, today, daysUntilDue, olaInDays, olaPercentRemaining);

                return new TaskQueueItem
                {
                    Task = task,
                    PriorityScore = priorityScore,
                    DaysUntilDue = daysUntilDue,
                    EffectiveDeadline = effectiveDeadline,
                    OLAInDays = olaInDays,
                    OLAPercentRemaining = olaPercentRemaining
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task queue item for task {taskId}", task.Id);
                return null;
            }
        }

        private int ParseOLAFromParameters(string olaParameters)
        {
            if (string.IsNullOrEmpty(olaParameters))
                return 0;

            try
            {
                // Try to extract OLA days from parameters string
                // This might need adjustment based on your actual OLA parameter format
                var parts = olaParameters.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    if (part.Trim().Contains("OLA", StringComparison.OrdinalIgnoreCase))
                    {
                        var numbers = System.Text.RegularExpressions.Regex.Matches(part, @"\d+");
                        if (numbers.Count > 0 && int.TryParse(numbers[0].Value, out int ola))
                        {
                            return ola;
                        }
                    }
                }
                
                // If no OLA found, try to parse the first number
                if (int.TryParse(olaParameters.Trim().Split(' ')[0], out int defaultOla))
                {
                    return defaultOla;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing OLA from parameters: {parameters}", olaParameters);
            }

            return 0; // Default OLA if parsing fails
        }

        private double CalculateOLAPercentRemaining(PETask task, DateTime today, int olaInDays)
        {
            if (task.PlannedEvent?.ServiceRequiredDate == null || olaInDays <= 0)
                return 100.0;

            var scheduledDate = task.PlannedEvent.ServiceRequiredDate.Value;
            var olaStartDate = scheduledDate.AddDays(-olaInDays);
            var totalOlaDays = (scheduledDate - olaStartDate).TotalDays;
            var daysElapsed = (today - olaStartDate).TotalDays;

            if (totalOlaDays <= 0)
                return 100.0;

            var percentElapsed = (daysElapsed / totalOlaDays) * 100;
            return Math.Max(0, 100 - percentElapsed);
        }

        private double CalculateTaskPriority(PETask task, DateTime today, int daysUntilDue, int olaInDays, double olaPercentRemaining)
        {
            // Initialize with base score for regular tasks (using same logic as API service)
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

        public override void Dispose()
        {
            _timer?.Dispose();
            base.Dispose();
        }
    }

    // Simple TaskQueueItem for background service use
    public class TaskQueueItem
    {
        public required PETask Task { get; set; }
        public double PriorityScore { get; set; }
        public int DaysUntilDue { get; set; }
        public DateTime? EffectiveDeadline { get; set; }
        public int OLAInDays { get; set; }
        public double OLAPercentRemaining { get; set; }
    }
}
