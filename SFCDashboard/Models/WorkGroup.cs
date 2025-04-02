using System.ComponentModel.DataAnnotations;

namespace SFCDashboard.Models
{
    public class WorkGroup
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public required string Name { get; set; }

        // Navigation properties
        public virtual ICollection<SystemUser>? Users { get; set; }
        public virtual ICollection<PlannedEvent>? AssignedEvents { get; set; }
    }
}
