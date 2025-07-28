using SFCDashboard.Models;

namespace SFCDashboard.Services
{
    public interface IEscalationsApiService
    {
        Task<Escalation?> GetEscalationAsync(int id);
        Task<IEnumerable<Escalation>> GetEscalationsByTaskIdsAsync(List<int> taskIds);
        Task<IEnumerable<Escalation>> GetEscalationsAsync();
        Task<Escalation?> CreateEscalationAsync(Escalation escalation);
        Task<Escalation?> UpdateEscalationAsync(Escalation escalation);
        Task<bool> DeleteEscalationAsync(int id);
    }
}
