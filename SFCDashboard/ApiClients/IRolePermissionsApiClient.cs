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
        Task<UserPermissionResult> GetUserPermissionsAsync(int userId);
        Task<ApiResult> UpdateUserPermissionsAsync(int userId, bool manageProjects, bool canManageEstimatedTime);
    }
}
