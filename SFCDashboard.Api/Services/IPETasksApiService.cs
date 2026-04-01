using SFCDashboard.Api.Models;

namespace SFCDashboard.Api.Services
{
    public interface IPETasksApiService
    {
        Task<PETask?> GetPETaskAsync(int id);
        Task<IEnumerable<PETask>> GetPETasksByPENumberAsync(string peNumber);
        Task<IEnumerable<PETask>> GetPETasksByPENumbersAsync(List<string> peNumbers);
        Task<IEnumerable<PETask>> GetPendingUrgentTaskRequestsAsync();
        Task<IEnumerable<string>> GetOLAViolatingPENumbersAsync();
        Task<PETask?> CreatePETaskAsync(PETask peTask);
        Task<PETask?> UpdatePETaskAsync(PETask peTask);
        Task<bool> DeletePETaskAsync(int id);
        Task<IEnumerable<PETask>> GetPendingTaskRequestsAsync(int take = 5);
        Task<Dictionary<string, IEnumerable<PETask>>> GetTasksByPeNumbersAsync(List<string?> peNumbers);
        Task<IEnumerable<PETask>> GetUrgentTasksAsync();
        Task<bool> ProcessTaskUrgentRequestAsync(int taskId, string urgentReason);
        Task<bool> ProcessPEUrgentRequestAsync(string peNumber, string urgentReason);
        Task<bool> MarkAsUrgentAsync(int taskId);
        
        // Additional methods needed for controller
        Task<IEnumerable<PETask>> GetAllPETasksAsync();
        Task<IEnumerable<PETask>> GetOLAViolationsAsync();
        Task<bool> CompleteViolatedTaskAsync(int id);
        Task<bool> RemoveUrgentStatusAsync(int id);
        Task<bool> UpdateEstimatedTimeAsync(int id, DateTime estimatedTime);
        Task<IEnumerable<object>> GetEstimationHistoryAsync(int id);
        Task<Dictionary<string, OLAViolationDetails>> GetOLAViolationDetailsAsync(List<string> peNumbers);
    }
}


