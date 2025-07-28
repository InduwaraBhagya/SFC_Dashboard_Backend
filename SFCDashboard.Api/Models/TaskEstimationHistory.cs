using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SFCDashboard.Models
{
    public class TaskEstimationHistory
    {
        [Key]
        public int Id { get; set; }

        public int TaskId { get; set; }

        [Required]
        public DateTime EstimatedDate { get; set; }

        public DateTime CreatedAt { get; set; }

        [ForeignKey("TaskId")]
        public virtual PETask Task { get; set; }
    }
}