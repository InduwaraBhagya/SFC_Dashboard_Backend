using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SFCDashboard.Data;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SFCDashboard.Services
{
    public class OLAViolationService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<OLAViolationService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(6);

        public OLAViolationService(
            IServiceProvider services,
            ILogger<OLAViolationService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OLA Violation Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Checking for OLA violations");

                try
                {
                    using (var scope = _services.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        await CheckAndUpdateOLAViolations(dbContext);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while checking OLA violations");
                }

                // Wait for next check interval
                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task CheckAndUpdateOLAViolations(ApplicationDbContext dbContext)
        {
            var currentDate = DateTime.Today;

            // Load tasks with their related PlannedEvent (for IsHold)
            var candidateTasks = await dbContext.PETasks
                .Include(t => t.PlannedEvent)
                .Where(t => t.TaskStatus != "COMPLETED" &&
                    (t.EstimatedTime.HasValue || t.ActualTaskCreatedDate.HasValue))
                .ToListAsync();

            foreach (var task in candidateTasks)
            {
                bool isHold = task.PlannedEvent != null && task.PlannedEvent.IsHold;
                bool isOlaViolate =
                    !isHold && (
                        (task.EstimatedTime.HasValue && task.EstimatedTime.Value < currentDate) ||
                        (!task.EstimatedTime.HasValue && task.ActualTaskCreatedDate.HasValue &&
                            task.OLA != null &&
                            int.TryParse(task.OLA, out var olaDays) &&
                            task.ActualTaskCreatedDate.Value.AddDays(olaDays) < currentDate)
                    );

                if (task.IsOLAViolate != isOlaViolate)
                {
                    task.IsOLAViolate = isOlaViolate;
                    dbContext.Update(task);
                }
            }

            await dbContext.SaveChangesAsync();
        }
    }
}