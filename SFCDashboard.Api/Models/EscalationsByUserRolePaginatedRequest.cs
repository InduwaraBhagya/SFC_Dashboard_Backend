namespace SFCDashboard.Api.Models
{
    public class EscalationsByUserRolePaginatedRequest
    {
        public int UserRoleLevel { get; set; }
        public List<string>? UserWorkgroupNames { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}
