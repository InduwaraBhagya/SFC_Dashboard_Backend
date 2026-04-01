using SFCDashboard.Models;

namespace SFCDashboard.ApiClients
{
    public interface IEscalationsApiClient
    {
        Task<IEnumerable<Escalation>> GetAllAsync();
        Task<Escalation?> GetByIdAsync(int id);
        Task<Escalation> CreateAsync(Escalation escalation);
        Task<Escalation> UpdateAsync(Escalation escalation);
        Task DeleteAsync(int id);
        Task<IEnumerable<Escalation>> GetEscalationsByTaskIdsAsync(List<int> peTaskIds);
        Task<List<Escalation>> GetEscalationsByUserRoleAsync(int userRoleLevel, List<string>? userWorkgroupNames = null);
        Task MarkAsReadAsync(int escalationId);
        Task<string> ManualEscalationCheckAsync();
        Task<object> GetOLAViolatedTasksDebugInfoAsync();
        Task<bool> IsEscalationEnabledAsync();
        Task SetEscalationEnabledAsync(bool enabled);
    }
}
