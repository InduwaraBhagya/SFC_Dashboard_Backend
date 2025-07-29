using System.ComponentModel.DataAnnotations;

namespace SFCDashboard.Api.Models
{
    public class CustomerUserAssignmentViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Customer")]
        public string Customer { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Assigned User")]
        public int UserId { get; set; }

        [Display(Name = "User Name")]
        public string UserName { get; set; } = string.Empty;

        [Display(Name = "User Service ID")]
        public string UserServiceId { get; set; } = string.Empty;

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }
    }

    public class CustomerUserAssignmentCreateViewModel
    {
        [Required]
        [Display(Name = "Customer")]
        public string Customer { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Assigned User")]
        public int UserId { get; set; }
    }

    public class CustomerUserAssignmentManageViewModel
    {
        public List<CustomerUserAssignmentViewModel> Assignments { get; set; } = new();
        public List<string> AvailableCustomers { get; set; } = new();
        public List<SystemUser> SalesUsers { get; set; } = new();
        public string SearchTerm { get; set; } = string.Empty;
    }
}


