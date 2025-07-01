using SFCDashboard.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SFCDashboard.Services
{
    public interface ITaskQueueService
    {
        Task<List<TaskQueueItem>> GetPrioritizedTasksAsync(int? workgroupId = null, int? year = null, int take = 20);
        Task<TaskQueueItem> GetNextTaskAsync(int? workgroupId = null, int? year = null);
        Task<List<int>> GetAvailableYearsAsync();
        // Add any other methods your TaskQueueingService has that should be exposed through the interface
    }
}