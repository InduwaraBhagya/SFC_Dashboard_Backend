namespace SFCDashboard.Models
{
    public class UserLayoutDataDto
    {
        public string UserName { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public bool CanManageCustomerAssignments { get; set; }
        public bool CanManageDrawFiberPerms { get; set; }
        public bool CanManageEstimatedTime { get; set; }
        public bool CanSendPEUrgentRequests { get; set; }
        public bool CanAcceptUrgentRequests { get; set; }
        public bool CanMakeTasksUrgent { get; set; }
        public bool CanViewAll { get; set; }
        public bool CanReportIssues { get; set; }
        public bool ManageProjects { get; set; }
        public bool CanManageNotices { get; set; }
    }
}
