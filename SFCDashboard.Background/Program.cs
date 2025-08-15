using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SFCDashboard.Background.Data;
using SFCDashboard.Background.Services;

namespace SFCDashboard.Background
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            try
            {
                // Test database connection on startup
                using (var scope = host.Services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                    
                    try
                    {
                        await context.Database.CanConnectAsync();
                        logger.LogInformation("Database connection successful");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to connect to database");
                        throw;
                    }
                }

                await host.RunAsync();
            }
            catch (Exception ex)
            {
                // Log critical startup errors
                var logger = host.Services.GetService<ILogger<Program>>();
                logger?.LogCritical(ex, "Application terminated unexpectedly");
                throw;
            }
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService(options =>
                {
                    options.ServiceName = "SFC Dashboard Background Service";
                })
                .ConfigureServices((hostContext, services) =>
                {
                    // Add Entity Framework
                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseSqlServer(hostContext.Configuration.GetConnectionString("DefaultConnection"),
                            sqlOptions => 
                            {
                                sqlOptions.CommandTimeout(300); // 5 minute timeout
                                sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                            }));

                    // Add memory cache for escalation service
                    services.AddMemoryCache();

                    // Register background services
                    services.AddSingleton<PERecordSyncService>();
                    services.AddSingleton<OLAViolationService>();
                    services.AddSingleton<HoldTaskReminderService>();
                    services.AddSingleton<EscalationWorkerService>();
                    services.AddSingleton<TaskQueueService>();
                    
                    // Register escalation service as scoped (for database operations)
                    services.AddScoped<EscalationService>();

                    // Register the main worker
                    services.AddHostedService<Worker>();
                });
    }
}
