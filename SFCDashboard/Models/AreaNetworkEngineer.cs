using System.ComponentModel.DataAnnotations;

public class AreaNetworkEngineer
{
    [Key]
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Area is required")]
    [StringLength(50, ErrorMessage = "Area cannot exceed 50 characters")]
    [Display(Name = "Area Code/Name")]
    public string Area { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Engineer name is required")]
    [StringLength(100, ErrorMessage = "Engineer name cannot exceed 100 characters")]
    [Display(Name = "Engineer Name")]
    public string EngineerName { get; set; } = string.Empty;
}