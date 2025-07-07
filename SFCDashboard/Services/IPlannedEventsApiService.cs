using SFCDashboard.Models;

namespace SFCDashboard.Services
{
    public interface IPlannedEventsApiService
    {
        Task<PlannedEvent?> GetPlannedEventAsync(int id);
        Task<PlannedEvent?> GetPlannedEventByIdAsync(int? id);
        Task<IEnumerable<PlannedEvent>> GetPlannedEventsAsync();
        Task<IEnumerable<PlannedEvent>> GetPlannedEventsByWorkgroupAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false);
        Task<IEnumerable<PlannedEvent>> GetPlannedEventsByWorkgroupIdsAsync(List<int> workgroupIds, bool hasDrawFiberAccess = false);
        Task<IEnumerable<PlannedEvent>> GetInProgressPlannedEventsAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false);
        Task<IEnumerable<PlannedEvent>> GetUrgentPlannedEventsAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false);
        Task<IEnumerable<PlannedEvent>> GetHoldPlannedEventsAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false);
        Task<IEnumerable<PlannedEvent>> GetOLAViolatingPlannedEventsAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false);
        Task<IEnumerable<PlannedEvent>> SearchPlannedEventsAsync(string searchType, string searchValue, List<string> workgroupNames, bool hasDrawFiberAccess = false);
        Task<IEnumerable<PlannedEvent>> GetPlannedEventsBySalesWorkgroupAsync(List<string> salesWorkgroups, List<string> assignedCustomers, bool canViewAll);
        Task<PlannedEvent?> CreatePlannedEventAsync(PlannedEvent plannedEvent);
        Task<PlannedEvent?> UpdatePlannedEventAsync(PlannedEvent plannedEvent);
        Task<bool> DeletePlannedEventAsync(int id);
        Task<bool> PlannedEventExistsAsync(int id);
        Task<int> GetUrgentCountAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false);
        Task<int> GetInProgressCountAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false);
        Task<int> GetOLAViolateCountAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false);
        Task<int> GetHoldCountAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false);
        Task<int> GetUrgentCountForMultiWorkgroupAsync(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds);
        Task<int> GetInProgressCountForMultiWorkgroupAsync(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds);
        Task<int> GetOLAViolateCountForMultiWorkgroupAsync(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds);
        Task<int> GetHoldCountForMultiWorkgroupAsync(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds);
        /// <summary>
        /// Gets pending urgent requests with optional limit parameter
        /// </summary>
        /// <param name="take">Maximum number of records to return (default: 10)</param>
        /// <returns>Collection of pending urgent planned events</returns>
        Task<IEnumerable<PlannedEvent>> GetPendingUrgentRequestsAsync(int take = 10);
        Task<PaginatedList<PlannedEvent>> SearchPlannedEventsAsync(string searchType, string searchValue, string? workgroupName, bool hasDrawFiberAccess, int pageIndex, int pageSize);
    }
}
