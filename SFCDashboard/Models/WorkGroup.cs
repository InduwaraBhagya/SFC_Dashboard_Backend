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
        public virtual ICollection<PlannedEvent>? AssignedEvents { get; set; }
        public virtual ICollection<UserWorkGroup>? UserWorkGroups { get; set; }
    }
}
