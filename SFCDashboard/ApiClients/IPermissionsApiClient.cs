using SFCDashboard.Models;

namespace SFCDashboard.ApiClients
{
    public interface IPermissionsApiClient
    {
        Task<bool> IsUserAdminAsync(string serviceId);
        Task<bool> HasPermissionAsync(string serviceId, string permissionName);
        
        // CRUD operations for permissions
        Task<List<Permission>> GetAllPermissionsAsync();
        Task<Permission?> GetPermissionByIdAsync(int id);
        Task<Permission> CreatePermissionAsync(Permission permission);
        Task<Permission> UpdatePermissionAsync(Permission permission);
        Task<bool> DeletePermissionAsync(int id);
        Task<bool> PermissionExistsAsync(int id);
    }
}
