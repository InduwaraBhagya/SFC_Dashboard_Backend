using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SFCDashboard.Background.Models
{
    public class SystemUser
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public required string Name { get; set; }

        [Required]
        [StringLength(100)]
        public required string ServiceId { get; set; }

        public int? UserRoleId { get; set; }

        // Navigation properties
        [ForeignKey("UserRoleId")]
        public virtual UserRole? UserRole { get; set; }

        // Navigation properties
        public virtual ICollection<UserWorkGroup> UserWorkGroups { get; set; }

        public SystemUser()
        {
            UserWorkGroups = new HashSet<UserWorkGroup>();
        }
    }
}
