using System;
using System.Collections.Generic;

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
        public decimal ProgressPercent { get; set; }
        public string ProgressClass { get; set; }
    }
}
