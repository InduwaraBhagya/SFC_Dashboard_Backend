using System.Collections.Generic;

namespace SFCDashboard.Models
{
    public class UDManagerViewModel
    {
        public UDManagerViewModel()
        {
            Categories = new List<UDCategory>();
            SubCategories = new List<UDSubCategory>();
            UDNames = new List<UDName>();
            NewCategory = new UDCategory();
            NewSubCategory = new UDSubCategory();
            NewUDName = new UDName();
        }

        public List<UDCategory> Categories { get; set; }
        public List<UDSubCategory> SubCategories { get; set; }
        public List<UDName> UDNames { get; set; }
        
        public UDCategory NewCategory { get; set; }
        public UDSubCategory NewSubCategory { get; set; }
        public UDName NewUDName { get; set; }
    }
}