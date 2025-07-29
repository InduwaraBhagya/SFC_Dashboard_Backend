using SFCDashboard.Models;

namespace SFCDashboard.ApiClients
{
    public interface IUserRolesApiClient
    {
        Task<IEnumerable<UserRole>> GetAllAsync();
        Task<UserRole?> GetByIdAsync(int id);
        Task<UserRole> CreateAsync(UserRole userRole);
        Task<UserRole> UpdateAsync(UserRole userRole);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<UserRole?> GetWithPermissionsAsync(int id);
        Task<bool> IsUserAdminAsync(string serviceId);
    }
}
