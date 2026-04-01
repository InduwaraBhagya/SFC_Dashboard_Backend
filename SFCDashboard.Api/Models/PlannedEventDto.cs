namespace SFCDashboard.Api.Models
{
    public class PlannedEventDto
    {
        public int Id { get; set; }
        public string PeNumber { get; set; } = string.Empty;
        public string Customer { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string WorkOrder { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public string Priority { get; set; } = string.Empty;
    }
}


