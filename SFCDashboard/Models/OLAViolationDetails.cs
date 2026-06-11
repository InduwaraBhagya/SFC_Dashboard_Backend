namespace SFCDashboard.Models
{
    public class OLAViolationDetails
    {
        public string PENumber { get; set; } = string.Empty;
        public int TasksCount { get; set; }
        public int MaxDaysOverdue { get; set; }
        public DateTime? OldestViolation { get; set; }
    }
}
