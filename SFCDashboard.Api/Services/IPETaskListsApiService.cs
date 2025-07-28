using SFCDashboard.Models;

namespace SFCDashboard.Services
{
    public interface IPETaskListsApiService
    {
        Task<PETaskList?> GetPETaskListAsync(int id);
        Task<PETaskList?> GetPETaskListByNameAsync(string name);
        Task<IEnumerable<PETaskList>> GetPETaskListsAsync();
        Task<PETaskList?> CreatePETaskListAsync(PETaskList taskList);
        Task<PETaskList?> UpdatePETaskListAsync(PETaskList taskList);
        Task<bool> DeletePETaskListAsync(int id);
    }
}
