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
            
            // Find tasks with OLA violations
            var violatingTasks = await dbContext.PETasks
                .Include(t => t.PlannedEvent)
                .Where(t => t.TaskStatus != "COMPLETED" && 
                           t.TaskCompleteDate.Date < currentDate)
                .ToListAsync();
            
            int updatedCount = 0;
            
            // Update PE status for each violating task
            foreach (var task in violatingTasks)
            {
                if (task.PlannedEvent != null && task.PlannedEvent.PEStatus != "ola-violated")
                {
                    task.PlannedEvent.PEStatus = "ola-violated";
                    dbContext.Update(task.PlannedEvent);
                    updatedCount++;
                }
            }
            
            // Save changes if any PEs were updated
            if (updatedCount > 0)
            {
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("Updated {count} PE records to OLA-violated status", updatedCount);
            }
            
            _logger.LogInformation("Found {count} tasks with OLA violations", violatingTasks.Count);
        }
    }
}