using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace SFCDashboard.Models
{
    public class UserRole
    {
        public UserRole()
        {
            RolePermissions = new List<RolePermission>();
        }

        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Role name is required")]
        [StringLength(100)]
        public string Name { get; set; }
        
        [Required(ErrorMessage = "Role level is required")]
        [Range(0, 3, ErrorMessage = "Role level must be between 0 and 3")]
        public int Level { get; set; }
        // Navigation property without Required attribute
        public virtual ICollection<RolePermission> RolePermissions { get; set; }
    }
}
