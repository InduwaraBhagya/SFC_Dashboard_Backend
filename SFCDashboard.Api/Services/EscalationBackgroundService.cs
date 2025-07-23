using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SFCDashboard.Services
{
    public class EscalationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EscalationBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30); // Check every 30 minutes

        public EscalationBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<EscalationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Escalation Background Service is starting");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Running escalation check");
                      using (var scope = _serviceProvider.CreateScope())
                    {
                        var escalationService = scope.ServiceProvider.GetRequiredService<EscalationService>();
                        await escalationService.CheckAndCreateEscalationsAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing escalations");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }
    }
}