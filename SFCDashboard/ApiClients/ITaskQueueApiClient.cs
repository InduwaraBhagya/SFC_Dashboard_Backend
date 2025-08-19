using SFCDashboard.Models;

namespace SFCDashboard.ApiClients
{
    public interface ITaskQueueApiClient
    {
        Task<List<TaskQueueItem>> GetPrioritizedTasksAsync(int? workgroupId = null, int? year = null, int take = 20);
        Task<TaskQueueItem?> GetNextTaskAsync(int? workgroupId = null, int? year = null);
        Task<List<int>> GetAvailableYearsAsync();
    }
}
