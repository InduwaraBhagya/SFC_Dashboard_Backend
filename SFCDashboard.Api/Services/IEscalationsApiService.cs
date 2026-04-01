using SFCDashboard.Api.Models;

namespace SFCDashboard.Api.Services
{
    public interface IEscalationsApiService
    {
        Task<Escalation?> GetEscalationAsync(int id);
        Task<IEnumerable<Escalation>> GetEscalationsByTaskIdsAsync(List<int> taskIds);
        Task<IEnumerable<Escalation>> GetEscalationsAsync();
        Task<IEnumerable<Escalation>> GetEscalationsByUserRoleAsync(EscalationsByUserRoleRequest request);
        Task<bool> MarkAsReadAsync(int id);
        Task<Escalation?> CreateEscalationAsync(Escalation escalation);
        Task<Escalation?> UpdateEscalationAsync(Escalation escalation);
        Task<bool> DeleteEscalationAsync(int id);
    }
}


