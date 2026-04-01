namespace SFCDashboard.Api.Services
{
    /// <summary>
    /// Configuration options for the EscalationService to help prevent timeouts and optimize performance.
    /// </summary>
    public class EscalationServiceConfiguration
    {
        /// <summary>
        /// Database command timeout in seconds. Default is 30 seconds.
        /// Increase this value if you're experiencing timeout issues with large datasets.
        /// </summary>
        public int CommandTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Batch size for processing escalations. Default is 100.
        /// Smaller batches reduce memory usage and prevent long-running transactions.
        /// </summary>
        public int BatchSize { get; set; } = 100;

        /// <summary>
        /// Maximum number of escalations to return in a single query. Default is 1000.
        /// This prevents memory issues when loading large result sets.
        /// </summary>
        public int MaxEscalationsPerQuery { get; set; } = 1000;

        /// <summary>
        /// Default page size for paginated queries. Default is 50.
        /// </summary>
        public int DefaultPageSize { get; set; } = 50;

        /// <summary>
        /// Enable batch processing for large datasets. Default is true.
        /// When enabled, large escalation processing will be done in batches.
        /// </summary>
        public bool EnableBatchProcessing { get; set; } = true;

        /// <summary>
        /// Cache expiration time for escalation configuration. Default is 5 minutes.
        /// </summary>
        public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromMinutes(5);
    }
}


