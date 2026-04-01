using SFCDashboard.Api.Models;

namespace SFCDashboard.Api.Services
{
    public interface IRolePermissionsApiService
    {
        Task<bool> HasPermissionAsync(string serviceId, string permissionName);
        Task<object> GetUserPermissionsAsync(int userId);
        Task<bool> UpdateUserPermissionsAsync(UpdateUserPermissionsRequest request);
        Task<bool> UpdateDrawFiberPermissionsAsync(int userId, bool manageProjects, bool canManageEstimatedTime);
        Task<bool> CanUserManagePermissions(string serviceId);
        Task<IEnumerable<RolePermission>> GetByRoleIdAsync(int roleId);
        Task<bool> DeleteByRoleIdAsync(int roleId);
        Task<bool> CreateMultipleAsync(IEnumerable<RolePermission> rolePermissions);
    }

    public class UpdateUserPermissionsRequest
    {
        public int UserId { get; set; }
        public bool ManageProjects { get; set; }
        public bool CanManageEstimatedTime { get; set; }
        public bool ManageCustomerAssignments { get; set; }
        public bool CanReportIssues { get; set; }
        public bool ManageDrawFiberPerms { get; set; }
        public bool CanSendPEUrgentRequests { get; set; }
        public bool CanAcceptUrgentRequests { get; set; }
        public bool CanMakeTasksUrgent { get; set; }
        public bool Admin { get; set; }
        public bool ViewAll { get; set; }
    }
}
