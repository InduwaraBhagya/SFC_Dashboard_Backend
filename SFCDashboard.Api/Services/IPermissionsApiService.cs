using SFCDashboard.Api.Models;

namespace SFCDashboard.Api.Services
{
    public interface IPermissionsApiService
    {
        Task<bool> HasPermissionAsync(string serviceId, string permissionName);
        Task<IEnumerable<Permission>> GetAllPermissionsAsync();
        Task<Permission?> GetPermissionByIdAsync(int id);
        Task<Permission?> CreatePermissionAsync(Permission permission);
        Task<Permission?> UpdatePermissionAsync(Permission permission);
        Task<bool> DeletePermissionAsync(int id);
        Task<bool> PermissionExistsAsync(int id);
    }
}
