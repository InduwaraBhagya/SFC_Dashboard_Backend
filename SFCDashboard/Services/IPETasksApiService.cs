using SFCDashboard.Models;

namespace SFCDashboard.Services
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
    }
}
