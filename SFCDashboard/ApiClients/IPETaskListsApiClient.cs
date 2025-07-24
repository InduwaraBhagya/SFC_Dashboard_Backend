using SFCDashboard.Models;

namespace SFCDashboard.ApiClients
{
    public interface IPETaskListsApiClient
    {
        Task<IEnumerable<PETaskList>> GetAllAsync();
        Task<PETaskList?> GetByIdAsync(int id);
        Task<PETaskList> CreateAsync(PETaskList peTaskList);
        Task<PETaskList> UpdateAsync(PETaskList peTaskList);
        Task DeleteAsync(int id);
        Task<PETaskList?> GetPETaskListByNameAsync(string taskName);
        Task<IEnumerable<PETaskList>> GetPETaskListsAsync();
    }
}
