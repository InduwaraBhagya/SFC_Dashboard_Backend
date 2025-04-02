using SFCDashboard.Models;
using TaskStatus = SFCDashboard.Models.TaskStatus;

namespace SFCDashboard.Interfaces
{
    public interface IPlannedEventService
    {
        Task<IEnumerable<PlannedEvent>> GetAllEventsAsync();
        Task<PlannedEvent?> GetEventByIdAsync(int id); // Updated to match the implementation
        Task<PlannedEvent> CreateEventAsync(PlannedEvent plannedEvent);
        Task UpdateEventAsync(PlannedEvent plannedEvent);
        Task DeleteEventAsync(int id);
        Task<IEnumerable<PlannedEvent>> GetEventsByWorkGroupAsync(int workGroupId);
        Task<IEnumerable<PlannedEvent>> GetEventsByEngineerAsync(int engineerId);
        Task<IEnumerable<PlannedEvent>> GetEventsByStatusAsync(TaskStatus status);
        Task<bool> UpdateEventStatusAsync(int id, TaskStatus newStatus, int userId, string comments);
        Task<IEnumerable<PlannedEvent>> SearchEventsAsync(string searchTerm);
        Task<IEnumerable<PlannedEvent>> GetUpcomingEventsAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<TaskHistory>> GetEventHistoryAsync(int eventId);
        List<PlannedEvent> GetAllPlannedEvents();
    }
}
