using System.Collections.Generic;

namespace SFCDashboard.Models
{
    public class UDManagerViewModel
    {
        public List<UDCategory> Categories { get; set; } = new List<UDCategory>();
        public List<UDSubCategory> SubCategories { get; set; } = new List<UDSubCategory>();
        public List<UDName> UDNames { get; set; } = new List<UDName>();
        
        public UDCategory NewCategory { get; set; } = new UDCategory();
        public UDSubCategory NewSubCategory { get; set; } = new UDSubCategory();
        public UDName NewUDName { get; set; } = new UDName();
    }
}