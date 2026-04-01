namespace SFCDashboard.Api.Services.Configuration
{
    /// <summary>
    /// Configuration options for the EscalationService
    /// </summary>
    public class EscalationServiceOptions
    {
        public const string SectionName = "EscalationService";

        /// <summary>
        /// Cache expiration time for escalation configuration (default: 5 minutes)
        /// </summary>
        public TimeSpan ConfigCacheExpiration { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Cache expiration time for escalation statistics (default: 1 minute)
        /// </summary>
        public TimeSpan StatsCacheExpiration { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Hours threshold for Level 1 escalations (default: 24 hours)
        /// </summary>
        public int Level1HoursThreshold { get; set; } = 24;

        /// <summary>
        /// Days threshold for Level 2 escalations (default: 1 day)
        /// </summary>
        public int Level2DaysThreshold { get; set; } = 1;

        /// <summary>
        /// Days threshold for Level 3 escalations (default: 3 days)
        /// </summary>
        public int Level3DaysThreshold { get; set; } = 3;

        /// <summary>
        /// Default page size for escalation queries (default: 50)
        /// </summary>
        public int DefaultPageSize { get; set; } = 50;

        /// <summary>
        /// Maximum page size allowed for escalation queries (default: 200)
        /// </summary>
        public int MaxPageSize { get; set; } = 200;

        /// <summary>
        /// Interval for automatic escalation checks in background service (default: 30 minutes)
        /// </summary>
        public TimeSpan AutoCheckInterval { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Whether to enable automatic escalation creation (default: true)
        /// </summary>
        public bool EnableAutoCreation { get; set; } = true;

        /// <summary>
        /// Whether to enable performance logging (default: false)
        /// </summary>
        public bool EnablePerformanceLogging { get; set; } = false;
    }
}


