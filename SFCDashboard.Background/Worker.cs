using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SFCDashboard.Background.Data;
using SFCDashboard.Background.Services;

namespace SFCDashboard.Background
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _serviceProvider;

        public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SFC Dashboard Background Service Worker started at: {time}", DateTimeOffset.Now);

            // Create a list to track running services
            var serviceTasks = new List<Task>();

            try
            {
                // Start all background services
                using (var scope = _serviceProvider.CreateScope())
                {
                    var peRecordSyncService = scope.ServiceProvider.GetRequiredService<PERecordSyncService>();
                    var olaViolationService = scope.ServiceProvider.GetRequiredService<OLAViolationService>();
                    var holdTaskReminderService = scope.ServiceProvider.GetRequiredService<HoldTaskReminderService>();
                    var escalationWorkerService = scope.ServiceProvider.GetRequiredService<EscalationWorkerService>();

                    // Start all services concurrently
                    serviceTasks.Add(peRecordSyncService.StartAsync(stoppingToken));
                    serviceTasks.Add(olaViolationService.StartAsync(stoppingToken));
                    serviceTasks.Add(holdTaskReminderService.StartAsync(stoppingToken));
                    serviceTasks.Add(escalationWorkerService.StartAsync(stoppingToken));

                    _logger.LogInformation("All background services started successfully including escalation service");

                    // Wait for cancellation
                    await Task.Delay(Timeout.Infinite, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Background service worker is stopping due to cancellation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while running background services");
                throw;
            }
            finally
            {
                // Wait for all services to complete gracefully
                if (serviceTasks.Any())
                {
                    try
                    {
                        await Task.WhenAll(serviceTasks);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error while waiting for background services to complete");
                    }
                }

                _logger.LogInformation("SFC Dashboard Background Service Worker stopped at: {time}", DateTimeOffset.Now);
            }
        }
    }
}
