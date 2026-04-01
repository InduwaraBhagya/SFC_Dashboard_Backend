namespace SFCDashboard.Api.Models
{
    public class ProjectDetailsDto
    {
        public int Id { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public List<ProjectPEViewModelDto> ProjectPEs { get; set; } = new List<ProjectPEViewModelDto>();
    }

    public class ProjectPEViewModelDto
    {
        public int Id { get; set; }
        public int PlannedEventId { get; set; }
        public PlannedEvent PlannedEvent { get; set; } = null!;
        public string? CurrentTask { get; set; }
    }
}


