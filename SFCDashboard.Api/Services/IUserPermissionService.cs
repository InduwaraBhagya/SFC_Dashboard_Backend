using SFCDashboard.Api.Models;

namespace SFCDashboard.Api.Services
{
    public interface IUserPermissionService
    {
        Task<UserPermissionInfo> GetUserPermissionInfoAsync(string serviceId);
        Task<UserWorkgroupInfo> GetUserWorkgroupInfoAsync(string serviceId);
        Task<bool> HasDrawFiberAccessAsync(string serviceId);
        Task<bool> IsUserInSalesWorkgroupAsync(string serviceId);
        Task<List<string>> GetUserAssignedCustomersAsync(string serviceId);
        Task<UserRedirectInfo> DetermineUserRedirectAsync(string serviceId, SearchCriteria searchCriteria);
    }
}
