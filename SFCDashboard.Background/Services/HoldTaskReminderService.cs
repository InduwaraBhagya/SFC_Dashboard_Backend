using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Background.Data;
using SFCDashboard.Background.Models;

namespace SFCDashboard.Background.Services
{
    public class HoldTaskReminderService : BackgroundService
    {
        private readonly ILogger<HoldTaskReminderService> _logger;
        private readonly IServiceProvider _services;
        private readonly List<TimeSpan> _scheduledTimes;

        public HoldTaskReminderService(ILogger<HoldTaskReminderService> logger, IServiceProvider services, IConfiguration configuration)
        {
            _logger = logger;
            _services = services;
            
            // Get scheduled times from configuration
            var scheduledTimesConfig = configuration.GetSection("BackgroundServices:HoldTaskReminderService:ScheduledTimes").Get<string[]>();
            _scheduledTimes = new List<TimeSpan>();
            
            if (scheduledTimesConfig != null && scheduledTimesConfig.Length > 0)
            {
                foreach (var timeStr in scheduledTimesConfig)
                {
                    if (TimeSpan.TryParse(timeStr, out var time))
                    {
                        _scheduledTimes.Add(time);
                    }
                    else
                    {
                        _logger.LogWarning("Invalid time format in HoldTaskReminderService configuration: {Time}", timeStr);
                    }
                }
            }
            
            // Default to 9 AM and 2 PM if no valid times configured
            if (_scheduledTimes.Count == 0)
            {
                _scheduledTimes.Add(new TimeSpan(9, 0, 0));   // 9:00 AM
                _scheduledTimes.Add(new TimeSpan(14, 0, 0));  // 2:00 PM
                _logger.LogInformation("Using default scheduled times for Hold Task Reminders: 9:00 AM and 2:00 PM");
            }
            
            _scheduledTimes.Sort(); // Sort times in ascending order
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Hold Task Reminder Service is starting with scheduled times: {Times}", 
                string.Join(", ", _scheduledTimes.Select(t => t.ToString(@"hh\:mm"))));

            var lastRunDate = DateTime.Today.AddDays(-1); // Initialize to yesterday to ensure first run
            var completedRunsToday = new HashSet<TimeSpan>(); // Track which scheduled times have already run today
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.Now;
                    var today = now.Date;
                    
                    // Reset completed runs if it's a new day
                    if (today > lastRunDate)
                    {
                        completedRunsToday.Clear();
                        lastRunDate = today;
                    }
                    
                    // Check if we need to run at any of the scheduled times today
                    foreach (var scheduledTime in _scheduledTimes)
                    {
                        var scheduledDateTime = today.Add(scheduledTime);
                        
                        // Run if we've passed the scheduled time and haven't run for this time today yet
                        if (now >= scheduledDateTime && !completedRunsToday.Contains(scheduledTime))
                        {
                            _logger.LogInformation("Starting hold task reminder check at scheduled time {Time}", scheduledDateTime);
                            
                            await SendHoldTaskReminders();
                            
                            _logger.LogInformation("Hold task reminder check completed at {Time}", DateTime.Now);
                            completedRunsToday.Add(scheduledTime); // Mark this scheduled time as completed
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in hold task reminder service");
                }

                // Wait 1 minute before checking again
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task SendHoldTaskReminders()
        {
            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Get tasks that have been on hold for more than 24 hours
            var holdTasks = await context.PlannedEvents
                .Where(p => p.IsHold && p.PECreatedDate < DateTime.Now.AddDays(-1))
                .ToListAsync();

            foreach (var task in holdTasks)
            {
                // Get users in the workgroup
                var workgroupUsers = await context.Users
                    .Where(u => u.UserWorkGroups.Any(uwg => 
                        context.WorkGroups.Any(wg => 
                            wg.Id == uwg.WorkGroupId && wg.Name == task.TaskWg)))
                    .ToListAsync();

                foreach (var user in workgroupUsers)
                {
                    // Create system generated inbox message for each user in the workgroup
                    var message = new PEIssue
                    {
                        PlannedEventId = task.Id,
                        PETaskId = 0, // Set default value as required field
                        IssueText = $"REMINDER: PE {task.PeNumber} has been on hold for more than 24 hours. Customer: {task.Customer}",
                        CreatedAt = DateTime.Now,
                        IsRead = false,
                        IsReply = false,
                        IsResolutionRequest = false,
                        SenderId = 1, // System user ID
                        ReceiverId = user.Id,
                        IsReminder = true
                    };

                    context.PEIssues.Add(message);
                }
            }

            await context.SaveChangesAsync();
            _logger.LogInformation($"Sent hold task reminders at {DateTime.Now}");
        }
    }
}
