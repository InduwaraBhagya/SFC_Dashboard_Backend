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
        Task<IEnumerable<PlannedEvent>> GetPendingUrgentRequestsAsync(int limit = 10);
        Task<PaginatedList<PlannedEvent>> SearchPlannedEventsAsync(string searchType, string searchString, string? workgroupName, bool hasDrawFiberAccess, int pageIndex, int pageSize);
        Task<IEnumerable<PlannedEvent>> GetPlannedEventsAsync();
        Task<PlannedEvent?> GetPlannedEventByIdAsync(int? id);
        // Add this method to support EscalationController
        Task<PlannedEvent> GetPlannedEventByPENumberAsync(string peNumber);
        Task<PlannedEvent> CreatePlannedEventAsync(PlannedEvent plannedEvent);
        Task<PlannedEvent> UpdatePlannedEventAsync(PlannedEvent plannedEvent);
        Task<bool> DeletePlannedEventAsync(int id);
        Task<bool> PlannedEventExistsAsync(int id);
        
        // Additional methods for filtered planned events
        Task<IEnumerable<PlannedEvent>> GetInProgressPlannedEventsAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false, bool canViewAll = false);
        /// <summary>
        /// Gets in-progress planned events for a specific user by userId (filtered in backend)
        /// </summary>
        Task<IEnumerable<PlannedEvent>> GetInProgressPlannedEventsByUserIdAsync(int userId);
        Task<IEnumerable<PlannedEvent>> GetUrgentPlannedEventsAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false, bool canViewAll = false);
        Task<IEnumerable<PlannedEvent>> GetOLAViolatingPlannedEventsAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false, bool canViewAll = false);
        Task<IEnumerable<PlannedEvent>> GetHoldPlannedEventsAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false, bool canViewAll = false);
        Task<int> GetUrgentCountAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false, bool canViewAll = false);
        Task<int> GetInProgressCountAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false, bool canViewAll = false);
        Task<int> GetOLAViolateCountAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false, bool canViewAll = false);
        Task<int> GetHoldCountAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false, bool canViewAll = false);
        Task<PlannedEvent?> GetPlannedEventAsync(int id);
        
        // Missing methods for workgroup filtering
        Task<IEnumerable<PlannedEvent>> GetPlannedEventsByWorkgroupAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false);
        
        // Missing methods for multi-workgroup count operations
        Task<int> GetUrgentCountForMultiWorkgroupAsync(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds, bool hasDrawFiberAccess);
        Task<int> GetInProgressCountForMultiWorkgroupAsync(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds, bool hasDrawFiberAccess);
        Task<int> GetOLAViolateCountForMultiWorkgroupAsync(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds, bool hasDrawFiberAccess);
        Task<int> GetHoldCountForMultiWorkgroupAsync(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds, bool hasDrawFiberAccess);
        
        // Method for getting distinct customers
        Task<List<string>> GetDistinctCustomersAsync();
    }
}
