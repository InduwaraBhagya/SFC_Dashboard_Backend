using SFCDashboard.Models;

namespace SFCDashboard.ApiClients
{
    public interface IPETasksApiClient
    {
        Task<IEnumerable<PETask>> GetAllAsync();
        Task<PETask?> GetByIdAsync(int id);
        Task<PETask> CreateAsync(PETask peTask);
        Task<PETask> UpdateAsync(PETask peTask);
        Task DeleteAsync(int id);
        Task<IEnumerable<PETask>> GetByPENumberAsync(string peNumber);
        Task<IEnumerable<PETask>> GetUrgentRequestsAsync();
        Task<IEnumerable<PETask>> GetOLAViolationsAsync();
        Task<IEnumerable<PETask>> GetUrgentTasksAsync();
        Task MarkAsUrgentAsync(int id);
        Task ProcessUrgentRequestAsync(int id, string urgentReason);
        Task ProcessPEUrgentRequestAsync(string peNumber, string urgentReason);
        Task CompleteViolatedTaskAsync(int id);
        Task RemoveUrgentStatusAsync(int id);
        Task UpdateEstimatedTimeAsync(int id, DateTime estimatedTime);
        Task<object> GetEstimationHistoryAsync(int id);
        Task<IEnumerable<PETask>> GetPendingTaskRequestsAsync(int limit = 5);
        Task<Dictionary<string, IEnumerable<PETask>>> GetTasksByPeNumbersAsync(List<string> peNumbers);
        Task<List<string>> GetOLAViolatingPENumbersAsync();
        Task<IEnumerable<PETask>> GetPETasksByPENumberAsync(string peNumber);
        Task<IEnumerable<PETask>> GetPETasksByPENumbersAsync(List<string> peNumbers);
        Task<PETask> UpdatePETaskAsync(PETask peTask);
        Task<IEnumerable<PETask>> GetPendingUrgentTaskRequestsAsync(int limit = 5);
    }
}
