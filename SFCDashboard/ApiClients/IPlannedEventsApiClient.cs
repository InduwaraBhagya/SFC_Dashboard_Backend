using SFCDashboard.Models;

namespace SFCDashboard.ApiClients
{
    public interface IPlannedEventsApiClient
    {
        Task<IEnumerable<PlannedEvent>> GetAllAsync();
        Task<PlannedEvent?> GetByIdAsync(int id);
        Task<PlannedEvent> CreateAsync(PlannedEvent plannedEvent);
        Task<PlannedEvent> UpdateAsync(PlannedEvent plannedEvent);
        Task DeleteAsync(int id);
        Task<IEnumerable<PlannedEvent>> SearchAsync(string searchTerm);
    }
}
