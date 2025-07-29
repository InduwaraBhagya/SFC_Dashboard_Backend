using SFCDashboard.Api.Models;

namespace SFCDashboard.Api.Services
{
    public interface IPEIssuesApiService
    {
        Task<PEIssue?> GetPEIssueAsync(int id);
        Task<IEnumerable<PEIssueViewModel>> GetPEIssuesByPlannedEventAsync(int plannedEventId);
        Task<IEnumerable<PEIssueViewModel>> GetInboxIssuesAsync(int userId, int take = 10);
        Task<IEnumerable<PEIssueViewModel>> GetRemindersAsync(int userId, bool showAll = true);
        Task<int> GetReminderCountAsync(int userId);
        Task<PEIssue?> CreatePEIssueAsync(PEIssue peIssue);
        Task<PEIssue?> UpdatePEIssueAsync(PEIssue peIssue);
        Task<bool> DeletePEIssueAsync(int id);
        Task<bool> MarkAllRemindersAsReadAsync(int userId);
        Task<Dictionary<int, List<PEIssueViewModel>>> GetIssuesByPlannedEventIdsAsync(List<int> plannedEventIds);
    }
}


