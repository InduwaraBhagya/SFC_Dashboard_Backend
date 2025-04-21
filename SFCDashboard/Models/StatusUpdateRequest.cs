namespace SFCDashboard.Models
{
    public class StatusUpdateRequest
    {
        public TaskStatus NewStatus { get; set; }
        public int UserId { get; set; }
        public string? Comments { get; set; }
    }
}
