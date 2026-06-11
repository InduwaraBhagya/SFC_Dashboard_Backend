using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SFCDashboard.Models
{
    public class TaskHistory
    {
        [Key]
        public int Id { get; set; }

        public int PlannedEventId { get; set; }

        public int UserId { get; set; }

        public DateTime ChangeTime { get; set; }

        [StringLength(50)]
        public string? PreviousStatus { get; set; }

        [StringLength(50)]
        public string? NewStatus { get; set; }

        [StringLength(500)]
        public string? Comments { get; set; }

        // Navigation Properties
        [ForeignKey("PlannedEventId")]
        public virtual required PlannedEvent PlannedEvent { get; set; }

        [ForeignKey("UserId")]
        public virtual SystemUser? ChangedBy { get; set; }
    }

    
}
