using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SFCDashboard.Models
{
    public class TaskQueueSnapshot
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int WorkGroupId { get; set; }

        [Required]
        public int TaskId { get; set; }

        [Required]
        public double PriorityScore { get; set; }

        [Required]
        public int DaysUntilDue { get; set; }

        public DateTime? EffectiveDeadline { get; set; }

        [Required]
        public int OLAInDays { get; set; }

        [Required]
        public double OLAPercentRemaining { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public int? Year { get; set; }

        // Navigation properties
        [ForeignKey("WorkGroupId")]
        public virtual WorkGroup WorkGroup { get; set; } = null!;

        [ForeignKey("TaskId")]
        public virtual PETask Task { get; set; } = null!;
    }
}
