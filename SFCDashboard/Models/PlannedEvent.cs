using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SFCDashboard.Models
{
    public class PlannedEvent
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public required string PENumber { get; set; }

        [StringLength(50)]
        public string? JobReferenceNumber { get; set; }


        [StringLength(50)]
        public required string SONumber { get; set; }

        [StringLength(100)]
        public string? Region { get; set; }

        [StringLength(100)]
        public string? Area { get; set; }

        [StringLength(200)]
        public required string Task { get; set; }

        public int? WorkGroupId { get; set; }

        public int? NetworkEngineerId { get; set; }

        [StringLength(200)]
        public required string Customer { get; set; }

        public TaskStatus CurrentStatus { get; set; }

        public bool IsUrgent { get; set; }

        public DateTime? ScheduledStartTime { get; set; }

        public DateTime? ActualStartTime { get; set; }

        public DateTime? ScheduledEndTime { get; set; }

        public DateTime? ActualEndTime { get; set; }

        public TimeSpan? TotalDuration { get; set; }

        public TimeSpan? SideProcessDuration { get; set; }

        [StringLength(500)]
        public string? Comments { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime LastModifiedDate { get; set; }

        // Navigation Properties
        [ForeignKey("WorkGroupId")]
        public virtual required WorkGroup AssignedWorkGroup { get; set; }

        [ForeignKey("NetworkEngineerId")]
        public virtual SystemUser? AssignedEngineer { get; set; }

        public virtual ICollection<TaskEscalation>? Escalations { get; set; }

        public virtual ICollection<TaskExtensionRequest>? ExtensionRequests { get; set; }

        public virtual ICollection<TaskHistory>? History { get; set; }

        public virtual ICollection<Notification>? Notifications { get; set; }
    }
}
