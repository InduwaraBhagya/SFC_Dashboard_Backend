using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDashboard.Models;

namespace SFCDashboard.Services
{
    public class HoldTaskReminderService : BackgroundService
    {
        private readonly ILogger<HoldTaskReminderService> _logger;
        private readonly IServiceProvider _services;

        public HoldTaskReminderService(ILogger<HoldTaskReminderService> logger, IServiceProvider services)
        {
            _logger = logger;
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;

                // Check if current time is 9 AM or 2 PM
                if ((now.Hour == 9 || now.Hour == 14) && now.Minute == 0)
                {
                    await SendHoldTaskReminders();
                }

                // Wait for 1 minute before next check
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
                        IssueText = $"REMINDER: PE {task.PeNumber} has been on hold for more than 24 hours. Customer: {task.Customer}",
                        CreatedAt = DateTime.Now,
                        IsRead = false,
                        IsReply = false,
                        IsResolutionRequest = false,
                        SenderId = 1, // System user ID
                        ReceiverId = user.Id
                    };

                    context.PEIssues.Add(message);
                }
            }

            await context.SaveChangesAsync();
            _logger.LogInformation($"Sent hold task reminders at {DateTime.Now}");
        }
    }
}