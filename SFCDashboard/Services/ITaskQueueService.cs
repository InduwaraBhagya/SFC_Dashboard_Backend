using SFCDashboard.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SFCDashboard.Services
{
    public interface ITaskQueueService
    {
        Task<List<TaskQueueItem>> GetPrioritizedTasksAsync(int? workgroupId = null, int take = 20, int? year = null);
        Task<TaskQueueItem> GetNextTaskAsync(int? workgroupId = null, int? year = null);
        Task<List<int>> GetAvailableYearsAsync();
        Task<Dictionary<int, int>> GetTaskCountByYearAsync(int? workgroupId = null);
    }
}