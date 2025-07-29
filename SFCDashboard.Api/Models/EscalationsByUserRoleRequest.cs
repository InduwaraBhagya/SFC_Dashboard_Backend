namespace SFCDashboard.Api.Models
{
    /// <summary>
    /// Request model for getting escalations by user role
    /// </summary>
    public class EscalationsByUserRoleRequest
    {
        public int UserRoleLevel { get; set; }
        public List<string>? UserWorkgroupNames { get; set; }
    }
}
