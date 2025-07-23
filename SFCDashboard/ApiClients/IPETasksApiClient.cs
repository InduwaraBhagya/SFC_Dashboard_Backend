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
    }
}
