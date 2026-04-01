namespace SFCDashboard.Api.Services
{
    public class UserPermissionInfo
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string ServiceId { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public bool CanViewAll { get; set; }
        public bool HasDrawFiberAccess { get; set; }
        public bool CanAcceptUrgentRequests { get; set; }
        public bool CanManageCustomerAssignments { get; set; }
        public bool CanManageDrawFiberPerms { get; set; }
        public List<string> WorkgroupNames { get; set; } = new List<string>();
        public List<int> WorkgroupIds { get; set; } = new List<int>();
    }

    public class UserWorkgroupInfo
    {
        public List<int> WorkgroupIds { get; set; } = new List<int>();
        public List<string> WorkgroupNames { get; set; } = new List<string>();
        public bool CanViewAll { get; set; }
        public bool IsInSalesWorkgroup { get; set; }
    }

    public class UserRedirectInfo
    {
        public bool ShouldRedirect { get; set; }
        public string? ActionName { get; set; }
        public object? RouteValues { get; set; }
    }

    public class SearchCriteria
    {
        public string? SearchType { get; set; }
        public string? PeNumber { get; set; }
        public string? Customer { get; set; }
        public string? JobReference { get; set; }
        public string? SoNumber { get; set; }
        public List<int> WorkgroupIds { get; set; } = new List<int>();
        public int PageIndex { get; set; } = 1;
    }
}
