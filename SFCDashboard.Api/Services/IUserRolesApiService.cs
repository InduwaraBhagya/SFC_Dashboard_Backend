using SFCDashboard.Api.Models;

namespace SFCDashboard.Api.Services
{
    public interface IUserRolesApiService
    {
        Task<IEnumerable<UserRole>> GetUserRolesAsync();
        Task<UserRole?> GetUserRoleAsync(int id);
        Task<UserRole?> GetUserRoleWithPermissionsAsync(int id);
        Task<UserRole?> CreateUserRoleAsync(UserRole userRole);
        Task<UserRole?> UpdateUserRoleAsync(UserRole userRole);
        Task<bool> DeleteUserRoleAsync(int id);
        Task<bool> UserRoleExistsAsync(int id);
        Task<bool> IsUserAdminAsync(string serviceId);
        Task<IEnumerable<int>> GetRolePermissionIdsAsync(int roleId);
    }
}
