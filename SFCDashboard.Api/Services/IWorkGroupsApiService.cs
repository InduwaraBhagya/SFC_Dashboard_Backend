using SFCDashboard.Api.Models;

namespace SFCDashboard.Api.Services
{
    public interface IWorkGroupsApiService
    {
        Task<WorkGroup?> GetWorkGroupAsync(int id);
        Task<IEnumerable<WorkGroup>> GetWorkGroupsAsync();
        Task<IEnumerable<WorkGroup>> GetWorkGroupsByIdsAsync(List<int> workgroupIds);
        Task<IEnumerable<WorkGroup>> GetUserWorkGroupsAsync(int userId);
        Task<WorkGroup?> CreateWorkGroupAsync(WorkGroup workGroup);
        Task<WorkGroup?> UpdateWorkGroupAsync(WorkGroup workGroup);
        Task<bool> DeleteWorkGroupAsync(int id);
        Task<IEnumerable<WorkGroup>> GetWorkGroupsForUserAsync(List<int> userWorkgroupIds, bool canViewAll);
        Task<string?> GetWorkGroupNameAsync(int workgroupId);
    }
}


