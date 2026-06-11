using SFCDashboard.Models;

namespace SFCDashboard.Services
{
    public interface ITaskQueueService
    {
        Task<List<TaskQueueItem>> GetPrioritizedTasksAsync(int? workgroupId = null, int? year = null, int take = 20);
        Task<TaskQueueItem?> GetNextTaskAsync(int? workgroupId = null, int? year = null);
        Task<List<int>> GetAvailableYearsAsync();
    }
}
