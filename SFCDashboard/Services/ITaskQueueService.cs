using SFCDashboard.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SFCDashboard.Services
{
    public interface ITaskQueueService
    {
        Task<List<TaskQueueItem>> GetPrioritizedTasksAsync(int? workgroupId = null, int take = 20);
        // Add any other methods your TaskQueueingService has that should be exposed through the interface
    }
}