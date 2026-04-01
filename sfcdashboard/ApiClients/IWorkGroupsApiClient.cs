using SFCDashboard.Models;

namespace SFCDashboard.ApiClients
{
    public interface IWorkGroupsApiClient
    {
        Task<IEnumerable<WorkGroup>> GetAllAsync();
        Task<WorkGroup?> GetByIdAsync(int id);
        Task<WorkGroup> CreateAsync(WorkGroup workGroup);
        Task<WorkGroup> UpdateAsync(WorkGroup workGroup);
        Task DeleteAsync(int id);
        Task<IEnumerable<WorkGroup>> GetWorkGroupsForUserAsync(List<int> userWorkgroupIds, bool canViewAll);
        Task<string?> GetWorkGroupNameAsync(int workgroupId);
        Task<IEnumerable<WorkGroup>> GetWorkGroupsByIdsAsync(List<int> workgroupIds);
        Task<WorkGroup?> GetWorkGroupAsync(int workgroupId);
        Task<IEnumerable<WorkGroup>> GetWorkGroupsAsync();
    }
}
