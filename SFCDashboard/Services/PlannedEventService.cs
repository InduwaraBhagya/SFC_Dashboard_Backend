using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDashboard.Interfaces;
using SFCDashboard.Models;
using TaskStatus = SFCDashboard.Models.TaskStatus;

namespace SFCDashboard.Services
{
    public class PlannedEventService : IPlannedEventService
    {
        private readonly ApplicationDbContext _context;

        public PlannedEventService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PlannedEvent>> GetAllEventsAsync()
        {
            return await _context.PlannedEvents
                .Include(e => e.AssignedWorkGroup)
                .Include(e => e.AssignedEngineer)
                .OrderByDescending(e => e.CreatedDate)
                .ToListAsync();
        }

        public async Task<PlannedEvent?> GetEventByIdAsync(int id)
        {
            return await _context.PlannedEvents
                .Include(e => e.AssignedWorkGroup)
                .Include(e => e.AssignedEngineer)
                .Include(e => e.History)
                .Include(e => e.ExtensionRequests!)
                    .ThenInclude(er => er.RequestedBy)
                .Include(e => e.ExtensionRequests!)
                    .ThenInclude(er => er.ApprovedBy)
                .Include(e => e.Escalations!)
                    .ThenInclude(esc => esc.EscalatedTo)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<PlannedEvent> CreateEventAsync(PlannedEvent plannedEvent)
        {
            plannedEvent.CreatedDate = DateTime.UtcNow;
            plannedEvent.LastModifiedDate = DateTime.UtcNow;
            plannedEvent.CurrentStatus = TaskStatus.New;

            _context.PlannedEvents.Add(plannedEvent);
            await _context.SaveChangesAsync();

            // Create initial task history entry
            var historyEntry = new TaskHistory
            {
                PlannedEventId = plannedEvent.Id,
                UserId = plannedEvent.NetworkEngineerId ?? 0, // Assuming system entry if no engineer
                ChangeTime = DateTime.UtcNow,
                PreviousStatus = string.Empty,
                NewStatus = TaskStatus.New.ToString(),
                Comments = "Event created",
                PlannedEvent = plannedEvent // Set the required PlannedEvent property
            };

            _context.TaskHistories.Add(historyEntry);
            await _context.SaveChangesAsync();

            return plannedEvent;
        }

        public async Task UpdateEventAsync(PlannedEvent plannedEvent)
        {
            var existingEvent = await _context.PlannedEvents.FindAsync(plannedEvent.Id);

            if (existingEvent == null)
                throw new KeyNotFoundException($"PlannedEvent with ID {plannedEvent.Id} not found.");

            // Update the timestamp
            plannedEvent.LastModifiedDate = DateTime.UtcNow;

            _context.Entry(existingEvent).CurrentValues.SetValues(plannedEvent);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteEventAsync(int id)
        {
            var plannedEvent = await _context.PlannedEvents.FindAsync(id);

            if (plannedEvent == null)
                throw new KeyNotFoundException($"PlannedEvent with ID {id} not found.");

            _context.PlannedEvents.Remove(plannedEvent);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<PlannedEvent>> GetEventsByWorkGroupAsync(int workGroupId)
        {
            return await _context.PlannedEvents
                .Where(e => e.WorkGroupId == workGroupId)
                .Include(e => e.AssignedEngineer)
                .OrderByDescending(e => e.CreatedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<PlannedEvent>> GetEventsByEngineerAsync(int engineerId)
        {
            return await _context.PlannedEvents
                .Where(e => e.NetworkEngineerId == engineerId)
                .Include(e => e.AssignedWorkGroup)
                .OrderByDescending(e => e.CreatedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<PlannedEvent>> GetEventsByStatusAsync(TaskStatus status)
        {
            return await _context.PlannedEvents
                .Where(e => e.CurrentStatus == status)
                .Include(e => e.AssignedWorkGroup)
                .Include(e => e.AssignedEngineer)
                .OrderByDescending(e => e.CreatedDate)
                .ToListAsync();
        }

        public async Task<bool> UpdateEventStatusAsync(int id, TaskStatus newStatus, int userId, string comments)
        {
            var plannedEvent = await _context.PlannedEvents.FindAsync(id);

            if (plannedEvent == null)
                return false;

            var previousStatus = plannedEvent.CurrentStatus;
            plannedEvent.CurrentStatus = newStatus;
            plannedEvent.LastModifiedDate = DateTime.UtcNow;

            if (newStatus == TaskStatus.InProgress && !plannedEvent.ActualStartTime.HasValue)
            {
                plannedEvent.ActualStartTime = DateTime.UtcNow;
            }
            else if (newStatus == TaskStatus.Completed && !plannedEvent.ActualEndTime.HasValue)
            {
                plannedEvent.ActualEndTime = DateTime.UtcNow;
                if (plannedEvent.ActualStartTime.HasValue)
                {
                    plannedEvent.TotalDuration = plannedEvent.ActualEndTime.Value - plannedEvent.ActualStartTime.Value;
                }
            }

            // Create task history entry
            var historyEntry = new TaskHistory
            {
                PlannedEventId = plannedEvent.Id,
                UserId = userId,
                ChangeTime = DateTime.UtcNow,
                PreviousStatus = previousStatus.ToString(),
                NewStatus = newStatus.ToString(),
                Comments = comments,
                PlannedEvent = plannedEvent // Set the required PlannedEvent property
            };

            _context.TaskHistories.Add(historyEntry);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<PlannedEvent>> SearchEventsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllEventsAsync();

            return await _context.PlannedEvents
                .Where(e => (e.PENumber != null && e.PENumber.Contains(searchTerm)) ||
                            (e.JobReferenceNumber != null && e.JobReferenceNumber.Contains(searchTerm)) ||
                            (e.SONumber != null && e.SONumber.Contains(searchTerm)) ||
                            (e.Customer != null && e.Customer.Contains(searchTerm)) ||
                            (e.Task != null && e.Task.Contains(searchTerm)) ||
                            (e.Region != null && e.Region.Contains(searchTerm)) ||
                            (e.Area != null && e.Area.Contains(searchTerm)))
                .Include(e => e.AssignedWorkGroup)
                .Include(e => e.AssignedEngineer)
                .OrderByDescending(e => e.CreatedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<PlannedEvent>> GetUpcomingEventsAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.PlannedEvents
                .Where(e => e.ScheduledStartTime >= startDate && e.ScheduledStartTime <= endDate)
                .Include(e => e.AssignedWorkGroup)
                .Include(e => e.AssignedEngineer)
                .OrderBy(e => e.ScheduledStartTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskHistory>> GetEventHistoryAsync(int eventId)
        {
            return await _context.TaskHistories
                .Where(h => h.PlannedEventId == eventId)
                .Include(h => h.ChangedBy)
                .OrderByDescending(h => h.ChangeTime)
                .ToListAsync();
        }

        public List<PlannedEvent> GetAllPlannedEvents()
        {
            throw new NotImplementedException();
        }
    }
}
