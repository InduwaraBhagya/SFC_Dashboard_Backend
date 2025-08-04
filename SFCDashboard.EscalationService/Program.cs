using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Serilog;
using SFCDashboard.EscalationService.Data;
using SFCDashboard.EscalationService.Services;

namespace SFCDashboard.EscalationService
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File("logs/escalation-service-.txt", 
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30)
                .CreateLogger();

            try
            {
                Log.Information("Starting SFC Dashboard Escalation Service");
                
                var builder = Host.CreateApplicationBuilder(args);

                // Add Serilog
                builder.Services.AddSerilog();

                // Add memory cache
                builder.Services.AddMemoryCache();

                // Add database context
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    Log.Fatal("Database connection string 'DefaultConnection' is not configured");
                    return;
                }

                builder.Services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(connectionString, sqlOptions => 
                    {
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                        sqlOptions.CommandTimeout(60); // 60 seconds timeout
                    }));

                // Add escalation service
                builder.Services.AddScoped<Services.EscalationService>();

                // Add the worker service
                builder.Services.AddHostedService<EscalationWorkerService>();

                // Configure as Windows Service if needed
                if (OperatingSystem.IsWindows())
                {
                    builder.Services.AddWindowsService(options =>
                    {
                        options.ServiceName = "SFC Dashboard Escalation Service";
                    });
                }

                var host = builder.Build();

                // Test database connection on startup
                using (var scope = host.Services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    try
                    {
                        await context.Database.CanConnectAsync();
                        Log.Information("Database connection test successful");
                    }
                    catch (Exception ex)
                    {
                        Log.Fatal(ex, "Failed to connect to database");
                        throw;
                    }
                }

                await host.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
