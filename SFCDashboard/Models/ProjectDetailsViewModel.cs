namespace SFCDashboard.Models
{
    public class ProjectDetailsViewModel
    {
        public int Id { get; set; }
        public string ProjectName { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<ProjectPEViewModel> ProjectPEs { get; set; } = new();
    }

    public class ProjectPEViewModel
    {
        public int Id { get; set; }
        public int PlannedEventId { get; set; }
        public PlannedEvent PlannedEvent { get; set; }
        public string? CurrentTask { get; set; }

        // Status property to determine PE status for visual indicators
        public string PEStatusType
        {
            get
            {
                // Check if it's urgent
                if (PlannedEvent?.Priority == "URGENT" || PlannedEvent?.PEStatus == "PENDING_URGENT_CONFIRMATION")
                    return "urgent";

                // Check if it's on hold
                if (PlannedEvent?.IsHold == true || PlannedEvent?.PEStatus?.Contains("HOLD") == true)
                    return "hold";

                // Check OLA violation through the current task
                // This is a simplified check - in real scenario, you'd need to check the actual OLA status
                if (PlannedEvent?.PEStatus?.Contains("OVERDUE") == true ||
                    PlannedEvent?.PEStatus?.Contains("VIOLATED") == true)
                    return "ola_violated";

                // Default to regular
                return "regular";
            }
        }

        public string StatusDisplayText
        {
            get
            {
                return PEStatusType switch
                {
                    "urgent" => "Urgent",
                    "hold" => "On Hold",
                    "ola_violated" => "OLA Violated",
                    "regular" => "Regular",
                    _ => "Regular"
                };
            }
        }

        public string StatusColor
        {
            get
            {
                return PEStatusType switch
                {
                    "urgent" => "#dc3545",      // Red
                    "hold" => "#ffc107",        // Yellow
                    "ola_violated" => "#0d6efd", // Blue
                    "regular" => "#198754",     // Green
                    _ => "#198754"              // Default Green
                };
            }
        }
    }
}
