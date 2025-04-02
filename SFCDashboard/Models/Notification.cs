using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SFCDashboard.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        public int? UserId { get; set; }

        public int? PlannedEventId { get; set; }

        [Required]
        [StringLength(200)]
        public required string Title { get; set; }

        [Required]
        [StringLength(1000)]
        public string? Message { get; set; }

        [StringLength(50)]
        public required string NotificationType { get; set; }

        public bool IsRead { get; set; }

        public DateTime CreatedDate { get; set; }

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual required SystemUser User { get; set; }

        [ForeignKey("PlannedEventId")]
        public virtual required PlannedEvent PlannedEvent { get; set; }
    }
}
