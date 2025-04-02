using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SFCDashboard.Models
{
    public class TaskEscalation
    {
        [Key]
        public int Id { get; set; }

        public int PlannedEventId { get; set; }

        public DateTime EscalationTime { get; set; }

        [StringLength(500)]
        public string? EscalationReason { get; set; }

        public int EscalatedToUserId { get; set; }

        public bool IsResolved { get; set; }

        public DateTime? ResolvedTime { get; set; }

        [StringLength(500)]
        public string? ResolutionComments { get; set; }

        // Navigation Properties
        [ForeignKey("PlannedEventId")]
        public virtual required PlannedEvent PlannedEvent { get; set; }

        [ForeignKey("EscalatedToUserId")]
        public virtual required SystemUser EscalatedTo { get; set; }
    }
}