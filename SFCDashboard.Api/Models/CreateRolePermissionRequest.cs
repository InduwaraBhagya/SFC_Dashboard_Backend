using System.ComponentModel.DataAnnotations;

namespace SFCDashboard.Api.Models
{
    public class CreateRolePermissionRequest
    {
        [Required]
        public int RoleId { get; set; }

        [Required]
        public int PermissionId { get; set; }
    }
}
