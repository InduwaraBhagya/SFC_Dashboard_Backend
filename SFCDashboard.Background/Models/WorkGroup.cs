using System.ComponentModel.DataAnnotations;

namespace SFCDashboard.Background.Models
{
    public class WorkGroup
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public required string Name { get; set; }

        // Navigation properties
        public virtual ICollection<UserWorkGroup>? UserWorkGroups { get; set; }
    }
}
