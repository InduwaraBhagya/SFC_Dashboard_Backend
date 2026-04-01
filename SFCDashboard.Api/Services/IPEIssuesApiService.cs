using SFCDashboard.Api.Models;

namespace SFCDashboard.Api.Services
{
    public interface IPEIssuesApiService
    {
        Task<IEnumerable<PEIssue>> GetAllPEIssuesAsync();
        Task<PEIssue?> GetPEIssueAsync(int id);
        Task<IEnumerable<PEIssue>> GetInboxIssuesRawAsync(int userId, int limit = 10);
        Task<IEnumerable<PEIssueViewModel>> GetInboxIssuesAsync(int userId, int take = 10);
        Task<IEnumerable<PEIssue>> GetRemindersRawAsync(int userId, bool showAll = true);
        Task<IEnumerable<PEIssueViewModel>> GetRemindersAsync(int userId, bool showAll = true);
        Task<int> GetReminderCountAsync(int userId);
        Task<int> MarkAllRemindersAsReadAsync(int userId);
        Task<Dictionary<int, IEnumerable<PEIssue>>> GetIssuesByPlannedEventIdsRawAsync(List<int> peIds);
        Task<Dictionary<int, List<PEIssueViewModel>>> GetIssuesByPlannedEventIdsAsync(List<int> plannedEventIds);
        Task<IEnumerable<PEIssue>> GetPEIssuesByPlannedEventRawAsync(int plannedEventId);
        Task<IEnumerable<PEIssueViewModel>> GetPEIssuesByPlannedEventAsync(int plannedEventId);
        Task<PEIssue?> CreatePEIssueAsync(PEIssue peIssue);
        Task<PEIssue?> UpdatePEIssueAsync(int id, PEIssue peIssue);
        Task<bool> DeletePEIssueAsync(int id);
        Task<PEIssue?> MarkIssueAsResolvedAsync(int id, bool isResolved = true);
        Task<object> GetIssueStatusAsync(int id);
        Task<bool> MarkIssueAsReadAsync(int id);
    }
}


