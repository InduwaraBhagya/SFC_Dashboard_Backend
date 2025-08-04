using System.ComponentModel.DataAnnotations;

namespace SFCDashboard.EscalationService.Models
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
    }
}
