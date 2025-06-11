using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SFCDashboard.Services
{
    public class EscalationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;
        
        public EscalationBackgroundService(IServiceProvider services)
        {
            _services = services;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Run every 30 minutes
                using (var scope = _services.CreateScope())
                {
                    var escalationService = scope.ServiceProvider.GetRequiredService<EscalationService>();
                    await escalationService.CheckAndCreateEscalationsAsync();
                }
                
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }
    }
}