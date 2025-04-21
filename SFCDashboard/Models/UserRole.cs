using System.ComponentModel.DataAnnotations;

namespace SFCDashboard.Models
{
    public class UserRole
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public required string Name { get; set; }
    }
}
