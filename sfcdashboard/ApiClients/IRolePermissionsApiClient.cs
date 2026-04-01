using SFCDashboard.Models;

namespace SFCDashboard.ApiClients
{
    public interface IRolePermissionsApiClient
    {
        Task<IEnumerable<RolePermission>> GetAllAsync();
        Task<RolePermission?> GetByIdAsync(int id);
        Task<RolePermission> CreateAsync(RolePermission rolePermission);
        Task<RolePermission> UpdateAsync(RolePermission rolePermission);
        Task DeleteAsync(int id);
        Task<IEnumerable<RolePermission>> GetByRoleIdAsync(int roleId);
        Task DeleteByRoleIdAsync(int roleId);
        Task CreateMultipleAsync(IEnumerable<RolePermission> rolePermissions);

        // Backend permission API integration
        Task<bool> HasPermissionAsync(string serviceId, string permissionName);
        Task<UserPermissionDetails> GetUserPermissionDetailsAsync(int userId);
        Task<ApiResult> UpdateUserPermissionsAsync(int userId, bool manageProjects, bool canManageEstimatedTime);
        Task<ApiResult> UpdateUserPermissionsAsync(UpdateUserPermissionsRequest request);
        Task<ApiResult> UpdateDrawFiberPermissionsAsync(int userId, bool manageProjects, bool canManageEstimatedTime);
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
