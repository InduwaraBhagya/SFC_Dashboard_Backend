using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SFCDashboard.Models
{
    public class ContractorNotification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string ContractorName { get; set; }

        [Required]
        [MaxLength(50)]
        public string PENumber { get; set; }

        [Required]
        [MaxLength(500)]
        public string Message { get; set; }

        [Required]
        [MaxLength(50)]
        public string NotificationType { get; set; } // "BOQ_ACTIVITY", "BOQ_RETURN", etc.

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int? RelatedTaskId { get; set; }

        public int? RelatedActivityId { get; set; }

        [ForeignKey("RelatedTaskId")]
        public virtual PETask? RelatedTask { get; set; }

        [ForeignKey("RelatedActivityId")]
        public virtual SurveyTaskActivity? RelatedActivity { get; set; }
    }
}