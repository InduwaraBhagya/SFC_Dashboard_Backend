using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SFCDashboard.Models;

public class SystemUser
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public required string Username { get; set; }

    [Required]
    [StringLength(100)]
    public required string Email { get; set; }

    [Required]
    [StringLength(100)]
    public string? FullName { get; set; }

    [Required]
    [StringLength(255)]
    public required string PasswordHash { get; set; }

    public UserRole Role { get; set; }

    public int? WorkGroupId { get; set; }

    public bool IsActive { get; set; }

    public DateTime LastLoginDate { get; set; }

    public DateTime CreatedDate { get; set; }
    // Navigation properties
    [ForeignKey("WorkGroupId")]
    public virtual WorkGroup? WorkGroup { get; set; }

    public virtual ICollection<PlannedEvent>? AssignedEvents { get; set; }
    public virtual ICollection<TaskHistory>? TaskChanges { get; set; }
    public virtual ICollection<TaskExtensionRequest>? RequestedExtensions { get; set; }
    public virtual ICollection<TaskExtensionRequest>? ApprovedExtensions { get; set; }
}