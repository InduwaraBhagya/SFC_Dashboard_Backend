using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SFCDashboard.Models
{
    public class TaskExtensionRequest
    {
        [Key]
        public int Id { get; set; }

        public int PlannedEventId { get; set; }

        public int RequestedById { get; set; }

        public DateTime RequestTime { get; set; }

        [StringLength(500)]
        public string? Justification { get; set; }

        public bool IsApproved { get; set; }

        public DateTime? ApprovalTime { get; set; }

        public int? ApprovedById { get; set; }

        public TimeSpan RequestedExtension { get; set; }

        // Navigation Properties
        [ForeignKey("PlannedEventId")]
        public virtual required PlannedEvent PlannedEvent { get; set; }

        [ForeignKey("RequestedById")]
        public virtual required SystemUser RequestedBy { get; set; }

        [ForeignKey("ApprovedById")]
        public virtual required SystemUser ApprovedBy { get; set; }
    }
}