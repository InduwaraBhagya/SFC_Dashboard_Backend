using SFCDashboard.Models;

namespace SFCDashboard.ApiClients
{
    public interface IUsersApiClient
    {
        Task<IEnumerable<SystemUser>> GetAllAsync();
        Task<SystemUser?> GetByIdAsync(int id);
        Task<SystemUser?> GetByServiceIdAsync(string serviceId);
        Task<SystemUser> CreateAsync(SystemUser user);
        Task<SystemUser> UpdateAsync(SystemUser user);
        Task DeleteAsync(int id);
    }
}
