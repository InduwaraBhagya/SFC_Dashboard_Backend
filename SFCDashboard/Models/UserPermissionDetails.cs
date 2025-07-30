namespace SFCDashboard.Models
{
    public class UserPermissionDetails
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public UserPermissions Permissions { get; set; } = new UserPermissions();
        public List<PermissionInfo> AllPermissions { get; set; } = new List<PermissionInfo>();
    }

    public class UserPermissions
    {
        public bool ManageProjects { get; set; }
        public bool CanManageEstimatedTime { get; set; }
        public bool ManageCustomerAssignments { get; set; }
        public bool CanReportIssues { get; set; }
        public bool ManageDrawFiberPerms { get; set; }
        public bool Admin { get; set; }
        public bool ViewAll { get; set; }
    }

    public class PermissionInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
