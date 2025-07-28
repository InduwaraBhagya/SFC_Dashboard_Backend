namespace SFCDashboard.Models
{
    public class UserLayoutDataDto
    {
        public string UserName { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public bool CanManageCustomerAssignments { get; set; }
        public bool CanManageDrawFiberPerms { get; set; }
    }
}
