using SFCDashboard.Models;

namespace SFCDashboard.ApiClients
{
    public interface ISubTaskListsApiClient
    {
        Task<SubTaskList> GetByIdAsync(int id);
        Task<IEnumerable<SubTaskList>> GetByTaskListIdAsync(int taskListId);
        Task<SubTaskList> GetByTaskListIdAndNameAsync(int taskListId, string subTaskName);
        Task AddAsync(SubTaskList subTaskList);
        Task UpdateAsync(SubTaskList subTaskList);
    }
}
