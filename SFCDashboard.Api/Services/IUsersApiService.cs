using SFCDashboard.Api.Models;

namespace SFCDashboard.Api.Services
{
    public interface IUsersApiService
    {
        Task<SystemUser?> GetUserAsync(int id);
        Task<SystemUser?> GetUserWithRoleAndWorkGroupsAsync(int id);
        Task<SystemUser?> GetUserByServiceIdAsync(string serviceId);
        Task<SystemUser?> GetUserByEmailAsync(string email);
        Task<int> GetCurrentUserIdAsync(string userIdentity);
        Task<bool> HasMultipleWorkgroupsAsync(string userIdentity);
        Task<bool> IsUserInSalesWorkgroupAsync(string userIdentity);
        Task<bool> HasDrawFiberAccessAsync(int userId);
        Task<(int userWorkgroupId, string userWorkgroupName)> GetCurrentUserWorkGroupAsync(string userIdentity);
        Task<(List<int> userWorkgroupIds, List<string> userWorkgroupNames, bool canViewAll)> GetCurrentUserWorkGroupsAsync(string userIdentity);
        Task<(List<string> salesWorkgroups, bool canViewAll)> GetUserSalesWorkgroupsAsync(string userIdentity);
        Task<List<string>> GetUserAssignedCustomersAsync(int userId);
        Task<UserLayoutDataDto?> GetUserLayoutDataAsync(string serviceId);
        Task<ProjectUserPermissionsDto?> GetProjectUserPermissionsAsync(string serviceId);
        Task<SystemUser?> CreateUserAsync(SystemUser user);
        Task<SystemUser?> UpdateUserAsync(SystemUser user);
        Task<bool> DeleteUserAsync(int id);
    }
}


