using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SFCDashboard.Models
{
    public class UDCategory
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Category { get; set; }
    }

    public class UDSubCategory
    {
        public int Id { get; set; }
        
        [Required]
        public int CategoryId { get; set; }
        
        [ForeignKey("CategoryId")]
        public UDCategory Category { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string SubCategory { get; set; }
    }

    public class UDName
    {
        public int Id { get; set; }
        
        [Required]
        public int CategoryId { get; set; }
        
        [ForeignKey("CategoryId")]
        public UDCategory Category { get; set; }
        
        [Required]
        public int SubCategoryId { get; set; }
        
        [ForeignKey("SubCategoryId")]
        public UDSubCategory SubCategory { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }
        
        [MaxLength(50)]
        public string Unit { get; set; }
        
        public decimal UnitPrice { get; set; }
        
        public bool IsActive { get; set; } = true; // Default to active
    }

    public class RTOMWeight
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string RTOM { get; set; }
        
        [Required]
        public decimal Weight { get; set; }
    }

    public class BOQ
    {
        public int Id { get; set; }
        
        [Required]
        public int TaskId { get; set; }
        
        [ForeignKey("TaskId")]
        public PETask Task { get; set; }
        
        [Required]
        public int CategoryId { get; set; }
        
        [ForeignKey("CategoryId")]
        public UDCategory Category { get; set; }
        
        [Required]
        public int SubCategoryId { get; set; }
        
        [ForeignKey("SubCategoryId")]
        public UDSubCategory SubCategory { get; set; }
        
        [Required]
        public int UDNameId { get; set; }
        
        [ForeignKey("UDNameId")]
        public UDName UDName { get; set; }
        
        [Required]
        public decimal Quantity { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Unit { get; set; }
        
        [Required]
        public decimal UnitPrice { get; set; }
        
        public decimal AdjustedUnitPrice { get; set; }
        
        public decimal Amount { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public int CreatedByUserId { get; set; }
        
        [ForeignKey("CreatedByUserId")]
        public SystemUser CreatedBy { get; set; }
    }

    public class BOQViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Task ID is required")]
        public int TaskId { get; set; }
        
        [Required(ErrorMessage = "Category is required")]
        public int CategoryId { get; set; }
        
        [Required(ErrorMessage = "Sub-Category is required")]
        public int SubCategoryId { get; set; }
        
        [Required(ErrorMessage = "UD Name is required")]
        public int UDNameId { get; set; }
        
        [Required(ErrorMessage = "Quantity is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public decimal Quantity { get; set; }
        
        [Required(ErrorMessage = "Unit is required")]
        public string Unit { get; set; }
        
        [Required(ErrorMessage = "Unit Price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Unit Price must be greater than 0")]
        public decimal UnitPrice { get; set; }
        
        public decimal AdjustedUnitPrice { get; set; }
        
        public decimal Amount { get; set; }
        
        // Additional properties for display
        public string CategoryName { get; set; }
        public string SubCategoryName { get; set; }
        public string UDNameValue { get; set; }
    }
}