using SFCDashboard.Models;

namespace SFCDashboard.ApiClients
{
    public interface IPEIssuesApiClient
    {
        Task<IEnumerable<PEIssue>> GetAllAsync();
        Task<PEIssue?> GetByIdAsync(int id);
        Task<PEIssue> CreateAsync(PEIssue peIssue);
        Task<PEIssue> UpdateAsync(PEIssue peIssue);
        Task DeleteAsync(int id);
        Task<IEnumerable<PEIssue>> GetInboxIssuesAsync(int userId, int limit = 10);
        Task<IEnumerable<PEIssue>> GetRemindersAsync(int userId, bool showAll = true);
        Task<int> GetReminderCountAsync(int userId);
        Task<bool> MarkAllRemindersAsReadAsync(int userId);
        Task<IEnumerable<PEIssue>> GetPEIssuesByPlannedEventAsync(int plannedEventId);
        Task<IEnumerable<PEIssueViewModel>> GetPEIssueViewModelsByPlannedEventAsync(int plannedEventId);
        Task<Dictionary<int, IEnumerable<PEIssueViewModel>>> GetPEIssueViewModelsByPlannedEventIdsAsync(List<int> peIds);
        Task<Dictionary<int, IEnumerable<PEIssue>>> GetIssuesByPlannedEventIdsAsync(List<int> peIds);
        Task<PEIssue?> GetPEIssueAsync(int id);
        Task<PEIssue> UpdatePEIssueAsync(PEIssue peIssue);

        // Additional methods for IssuesController
        Task<IEnumerable<PEIssue>> GetReceivedIssuesAsync(int userId);
        Task<IEnumerable<PEIssue>> GetSentIssuesAsync(int userId);
        Task<IEnumerable<PEIssue>> GetUnreadInboxIssuesAsync(int userId);
        Task<IEnumerable<PEIssueViewModel>> GetInboxViewModelsAsync(int userId);
        Task<IEnumerable<PEIssueViewModel>> GetInboxViewModelsAsync(int userId, int limit);
        Task<IEnumerable<PEIssueViewModel>> GetSentViewModelsAsync(int userId);

        Task<IEnumerable<PEIssue>> GetByTaskIdAsync(int taskId);
        Task<IEnumerable<PEIssue>> GetByPlannedEventIdAsync(int plannedEventId);
        Task<bool> MarkIssueAsReadAsync(int issueId);
    }
}
