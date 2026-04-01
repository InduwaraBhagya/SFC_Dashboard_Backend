using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace SFCDashboard.Background.Services
{
    public class EscalationWorkerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EscalationWorkerService> _logger;
        private readonly List<TimeSpan> _scheduledTimes;

        public EscalationWorkerService(
            IServiceProvider serviceProvider,
            ILogger<EscalationWorkerService> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            
            // Get scheduled times from configuration, default to 9 AM
            var scheduledTimesConfig = configuration.GetSection("BackgroundServices:EscalationService:ScheduledTimes").Get<string[]>();
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
                        _logger.LogWarning("Invalid time format in configuration: {Time}", timeStr);
                    }
                }
            }
            
            // Default to 9 AM if no valid times configured
            if (_scheduledTimes.Count == 0)
            {
                _scheduledTimes.Add(new TimeSpan(9, 0, 0));  // 9 AM
                _logger.LogInformation("Using default scheduled time: 9:00 AM");
            }
            
            _scheduledTimes.Sort(); // Sort times in ascending order
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Escalation Worker Service is starting with scheduled times: {Times}", 
                string.Join(", ", _scheduledTimes.Select(t => t.ToString(@"hh\:mm"))));

            // Wait a bit on startup to allow the application to fully initialize
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            var lastRunDate = DateTime.Today.AddDays(-1); // Initialize to yesterday to ensure first run
            var completedRunsToday = new HashSet<TimeSpan>(); // Track which scheduled times have already run today
            
            while (!stoppingToken.IsCancellationRequested)
            {
                TimeSpan sleepDuration = TimeSpan.FromMinutes(1); // Default sleep duration
                
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
                            _logger.LogInformation("Starting escalation check at scheduled time {Time}", scheduledDateTime);
                            
                            using (var scope = _serviceProvider.CreateScope())
                            {
                                var escalationService = scope.ServiceProvider.GetRequiredService<EscalationService>();
                                await escalationService.CheckAndCreateEscalationsAsync();
                            }
                            
                            _logger.LogInformation("Escalation check completed successfully at {Time}", DateTime.Now);
                            completedRunsToday.Add(scheduledTime); // Mark this scheduled time as completed
                        }
                    }
                    
                    // Calculate next scheduled time to optimize sleep duration
                    var nextScheduledTime = GetNextScheduledTime(now);
                    var delayUntilNext = nextScheduledTime - now;
                    
                    // If next scheduled time is more than 1 hour away, sleep for 1 hour
                    // Otherwise, sleep until 1 minute before the scheduled time
                    sleepDuration = delayUntilNext > TimeSpan.FromHours(1) 
                        ? TimeSpan.FromHours(1)
                        : delayUntilNext > TimeSpan.FromMinutes(1) 
                            ? delayUntilNext.Subtract(TimeSpan.FromMinutes(1))
                            : TimeSpan.FromMinutes(1);
                    
                    _logger.LogDebug("Next escalation scheduled run: {NextRun}, sleeping for: {SleepDuration}", 
                        nextScheduledTime, sleepDuration);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing escalations at {Time}", DateTime.Now);
                    // sleepDuration keeps its default value of 1 minute on error
                }

                try
                {
                    // Wait calculated duration before checking again
                    await Task.Delay(sleepDuration, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // This is expected when cancellation is requested
                    break;
                }
            }

            _logger.LogInformation("Escalation Worker Service is stopping");
        }

        private DateTime GetNextScheduledTime(DateTime now)
        {
            var today = now.Date;
            
            // Check if there's a scheduled time today that hasn't passed yet
            foreach (var scheduledTime in _scheduledTimes)
            {
                var todayScheduled = today.Add(scheduledTime);
                if (todayScheduled > now)
                {
                    return todayScheduled;
                }
            }
            
            // All scheduled times for today have passed, return first scheduled time for tomorrow
            var tomorrow = today.AddDays(1);
            return tomorrow.Add(_scheduledTimes[0]);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Escalation Worker Service stop requested");
            await base.StopAsync(cancellationToken);
        }
    }
}
