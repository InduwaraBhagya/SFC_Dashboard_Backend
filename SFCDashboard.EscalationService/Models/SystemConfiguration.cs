using System.ComponentModel.DataAnnotations;

namespace SFCDashboard.EscalationService.Models
{
    public class SystemConfiguration
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string ConfigKey { get; set; } = string.Empty;
        
        public string? ConfigValue { get; set; }
        
        public string? Description { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
