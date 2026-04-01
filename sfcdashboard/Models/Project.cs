using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SFCDashboard.Models;

[Table("Projects")]
public class Project
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string ProjectName { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    // Navigation property
    public virtual ICollection<ProjectPEMapping> ProjectPEs { get; set; } = new List<ProjectPEMapping>();
}

[Table("ProjectPEMappings")]
public class ProjectPEMapping
{
    [Key]
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public int PlannedEventId { get; set; }

    [ForeignKey("ProjectId")]
    public virtual Project Project { get; set; }

    [ForeignKey("PlannedEventId")]
    public virtual PlannedEvent PlannedEvent { get; set; }
}