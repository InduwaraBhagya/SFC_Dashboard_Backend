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
        Task<int> GetCurrentUserIdAsync(string serviceId);
        Task<SystemUser?> GetUserWithRoleAndWorkGroupsAsync(int userId);
        Task<SystemUser?> GetUserByServiceIdAsync(string serviceId);
        
        // Additional methods for user workgroup operations
        Task<(List<int> userWorkgroupIds, List<string> userWorkgroupNames, bool canViewAll)> GetCurrentUserWorkGroupsAsync(string serviceId);
        Task<(int userWorkgroupId, string userWorkgroupName)> GetCurrentUserWorkGroupAsync(string serviceId);
        Task<bool> HasMultipleWorkgroupsAsync(string serviceId);
        Task<bool> IsUserInSalesWorkgroupAsync(string serviceId);
        Task<bool> HasDrawFiberAccessAsync(int userId);
        Task<List<string>> GetUserAssignedCustomersAsync(int userId);
        Task<(List<string> salesWorkgroups, bool canViewAll)> GetUserSalesWorkgroupsAsync(string serviceId);
        Task<SystemUser?> GetUserAsync(int userId);
    }
}
