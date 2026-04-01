using SFCDashboard.Api.Models;

namespace SFCDashboard.Api.Services
{
    public interface ISubTaskListsApiService
    {
        Task<IEnumerable<SubTaskList>> GetAllSubTaskListsAsync();
        Task<SubTaskList?> GetSubTaskListByIdAsync(int id);
        Task<IEnumerable<SubTaskList>> GetSubTaskListsByTaskListIdAsync(int taskListId);
        Task<SubTaskList?> GetSubTaskListByTaskListIdAndNameAsync(int taskListId, string name);
        Task<SubTaskList?> CreateSubTaskListAsync(SubTaskList subTaskList);
        Task<SubTaskList?> UpdateSubTaskListAsync(SubTaskList subTaskList);
        Task<bool> DeleteSubTaskListAsync(int id);
        Task<bool> SubTaskListExistsAsync(int id);
    }
}
