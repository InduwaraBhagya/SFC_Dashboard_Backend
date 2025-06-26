using System;
using System.ComponentModel.DataAnnotations;

namespace SFCDashboard.Models
{
    public class Contractor
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string CompanyName { get; set; }
        
        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; }
        
        [Required]
        public string PasswordHash { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        public DateTime? LastLoginDate { get; set; }
    }
}