using SFCDashboard.Api.Models;

namespace SFCDashboard.Api.Services
{
    public interface IPlannedEventsApiService
    {
        Task<PlannedEvent?> GetPlannedEventAsync(int id);
        Task<PlannedEvent?> GetPlannedEventByIdAsync(int? id);
        Task<IEnumerable<PlannedEvent>> GetPlannedEventsAsync();
        Task<IEnumerable<PlannedEvent>> GetPlannedEventsByWorkgroupAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false, bool canViewAll = false);
        Task<IEnumerable<PlannedEvent>> GetInProgressPlannedEventsByUserIdAsync(int userId);
        Task<IEnumerable<PlannedEvent>> GetOLAViolatingPlannedEventsByUserIdAsync(int userId);
        Task<IEnumerable<PlannedEvent>> GetUrgentPlannedEventsByUserIdAsync(int userId);
        Task<IEnumerable<PlannedEvent>> GetHoldPlannedEventsByUserIdAsync(int userId);
        Task<IEnumerable<PlannedEvent>> GetInProgressPlannedEventsAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false, bool canViewAll = false);
        Task<IEnumerable<PlannedEvent>> GetUrgentPlannedEventsAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false, bool canViewAll = false);
        Task<IEnumerable<PlannedEvent>> GetHoldPlannedEventsAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false, bool canViewAll = false);
        Task<IEnumerable<PlannedEvent>> GetOLAViolatingPlannedEventsAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false, bool canViewAll = false);
        Task<IEnumerable<PlannedEvent>> SearchPlannedEventsAsync(string searchType, string searchValue, List<string> workgroupNames, bool hasDrawFiberAccess = false, bool canViewAll = false);
        Task<PlannedEvent?> CreatePlannedEventAsync(PlannedEvent plannedEvent);
        Task<PlannedEvent?> UpdatePlannedEventAsync(PlannedEvent plannedEvent);
        Task<bool> DeletePlannedEventAsync(int id);
        Task<bool> PlannedEventExistsAsync(int id);
        Task<int> GetUrgentCountAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false, bool canViewAll = false);
        Task<int> GetInProgressCountAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false, bool canViewAll = false);
        Task<int> GetOLAViolateCountAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false, bool canViewAll = false);
        Task<int> GetHoldCountAsync(List<string> workgroupNames, bool hasDrawFiberAccess = false, bool canViewAll = false);
        Task<int> GetUrgentCountForMultiWorkgroupAsync(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds, bool hasDrawFiberAccess = false);
        Task<int> GetInProgressCountForMultiWorkgroupAsync(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds, bool hasDrawFiberAccess = false);
        Task<int> GetOLAViolateCountForMultiWorkgroupAsync(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds, bool hasDrawFiberAccess = false);
        Task<int> GetHoldCountForMultiWorkgroupAsync(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds, bool hasDrawFiberAccess = false);
        Task<IEnumerable<PlannedEvent>> GetInProgressPlannedEventsForMultiWorkgroupAsync(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds, bool hasDrawFiberAccess = false);
        Task<IEnumerable<PlannedEvent>> GetUrgentPlannedEventsForMultiWorkgroupAsync(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds, bool hasDrawFiberAccess = false);
        Task<IEnumerable<PlannedEvent>> GetHoldPlannedEventsForMultiWorkgroupAsync(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds, bool hasDrawFiberAccess = false);
        Task<IEnumerable<PlannedEvent>> GetOLAViolatingPlannedEventsForMultiWorkgroupAsync(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds, bool hasDrawFiberAccess = false);
        // User-based count methods
        Task<int> GetInProgressCountByUserIdAsync(int userId);
        Task<int> GetOLAViolatingCountByUserIdAsync(int userId);
        Task<int> GetUrgentCountByUserIdAsync(int userId);
        Task<int> GetHoldCountByUserIdAsync(int userId);
        /// <summary>
        /// Gets pending urgent requests with optional limit parameter
        /// </summary>
        /// <param name="take">Maximum number of records to return (default: 10)</param>
        /// <returns>Collection of pending urgent planned events</returns>
        Task<IEnumerable<PlannedEvent>> GetPendingUrgentRequestsAsync(int take = 10);
        Task<PaginatedList<PlannedEvent>> SearchPlannedEventsAsync(string searchType, string searchValue, string? workgroupName, bool hasDrawFiberAccess, int pageIndex, int pageSize);
        Task<PaginatedList<PlannedEvent>> SearchPlannedEventsAsync(string searchType, string searchValue, List<string> salesWorkgroups, bool hasDrawFiberAccess, int pageIndex, int pageSize);

        // Sales-specific methods
        Task<IEnumerable<PlannedEvent>> GetSalesInProgressRecordsAsync(int userId);
        Task<IEnumerable<PlannedEvent>> GetSalesHoldRecordsAsync(int userId);
        Task<IEnumerable<PlannedEvent>> GetSalesUrgentRecordsAsync(int userId);
        Task<IEnumerable<PlannedEvent>> GetSalesOLAViolateRecordsAsync(int userId);
        
        // Sales count methods
        Task<int> GetSalesInProgressCountAsync(int userId);
        Task<int> GetSalesHoldCountAsync(int userId);
        Task<int> GetSalesUrgentCountAsync(int userId);
        Task<int> GetSalesOLAViolateCountAsync(int userId);

        // User-specific search method
        Task<PaginatedList<PlannedEvent>> SearchPlannedEventsForUserAsync(string searchType, string searchValue, int userId, int pageIndex, int pageSize);
    }
}


