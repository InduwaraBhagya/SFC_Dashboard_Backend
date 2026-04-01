using Microsoft.EntityFrameworkCore;
using SFCDashboard.Api.Data;
using SFCDashboard.Api.Models;

namespace SFCDashboard.Api.Services
{
    public class PETasksApiService : IPETasksApiService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PETasksApiService> _logger;

        public PETasksApiService(ApplicationDbContext context, ILogger<PETasksApiService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PETask?> GetPETaskAsync(int id)
        {
            try
            {
                return await _context.PETasks
                    .Include(t => t.PlannedEvent)
                    .FirstOrDefaultAsync(t => t.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PE task with id {Id}", id);
                return null;
            }
        }

        public async Task<IEnumerable<PETask>> GetPETasksByPENumberAsync(string peNumber)
        {
            try
            {
                return await _context.PETasks
                    .Where(t => t.PENumber == peNumber)
                    .OrderBy(t => t.TaskSeq)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PE tasks for PE number {PENumber}", peNumber);
                return new List<PETask>();
            }
        }

        public async Task<IEnumerable<PETask>> GetPETasksByPENumbersAsync(List<string> peNumbers)
        {
            try
            {
                return await _context.PETasks
                    .Where(t => peNumbers.Contains(t.PENumber))
                    .OrderBy(t => t.TaskSeq)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PE tasks for multiple PE numbers");
                return new List<PETask>();
            }
        }

        public async Task<IEnumerable<PETask>> GetPendingUrgentTaskRequestsAsync()
        {
            try
            {
                return await _context.PETasks
                    .Where(t => t.UrgentRequested && !t.IsUrgent)
                    .Include(t => t.PlannedEvent)
                    .OrderByDescending(t => t.TaskCreatedDate)
                    .Take(5)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending urgent task requests");
                return new List<PETask>();
            }
        }

        public async Task<IEnumerable<string>> GetOLAViolatingPENumbersAsync()
        {
            try
            {
                return await _context.PETasks
                    .Where(t => t.IsOLAViolate)
                    .Select(t => t.PENumber)
                    .Distinct()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OLA violating PE numbers");
                return new List<string>();
            }
        }

        public async Task<PETask?> CreatePETaskAsync(PETask peTask)
        {
            try
            {
                _context.Add(peTask);
                await _context.SaveChangesAsync();
                return peTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PE task");
                return null;
            }
        }

        public async Task<PETask?> UpdatePETaskAsync(PETask peTask)
        {
            try
            {
                _context.Update(peTask);
                await _context.SaveChangesAsync();
                return peTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating PE task");
                return null;
            }
        }

        public async Task<bool> DeletePETaskAsync(int id)
        {
            try
            {
                var peTask = await _context.PETasks.FindAsync(id);
                if (peTask != null)
                {
                    _context.PETasks.Remove(peTask);
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting PE task with id {Id}", id);
                return false;
            }
        }

        public async Task<IEnumerable<PETask>> GetPendingTaskRequestsAsync(int take = 5)
        {
            try
            {
                _logger.LogInformation("Getting pending task requests, take: {take}", take);
                
                return await _context.PETasks
                    .Where(t => t.UrgentRequested && !t.IsUrgent)
                    .Include(t => t.PlannedEvent)
                    .OrderByDescending(t => t.TaskCreatedDate)
                    .Take(take)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending task requests");
                return new List<PETask>();
            }
        }

        public async Task<Dictionary<string, IEnumerable<PETask>>> GetTasksByPeNumbersAsync(List<string?> peNumbers)
        {
            try
            {
                _logger.LogInformation("Getting tasks by PE numbers: {count} numbers", peNumbers.Count);
                
                var validPeNumbers = peNumbers.Where(pn => !string.IsNullOrEmpty(pn)).Cast<string>().ToList();
                if (!validPeNumbers.Any())
                {
                    return new Dictionary<string, IEnumerable<PETask>>();
                }

                var allTasks = await _context.PETasks
                    .Where(t => validPeNumbers.Contains(t.PENumber))
                    .OrderBy(t => t.TaskSeq)
                    .ToListAsync();

                return allTasks
                    .GroupBy(t => t.PENumber)
                    .ToDictionary(g => g.Key, g => (IEnumerable<PETask>)g.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks by PE numbers");
                return new Dictionary<string, IEnumerable<PETask>>();
            }
        }

        public async Task<IEnumerable<PETask>> GetUrgentTasksAsync()
        {
            try
            {
                return await _context.PETasks
                    .Include(t => t.PlannedEvent)
                    .Where(t => t.IsUrgent == true && t.TaskStatus != "COMPLETED")
                    .OrderByDescending(t => t.UrgentMarkedDate)
                    .ThenBy(t => t.TaskCompleteDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting urgent tasks");
                return new List<PETask>();
            }
        }

        public async Task<bool> ProcessTaskUrgentRequestAsync(int taskId, string urgentReason)
        {
            try
            {
                var task = await _context.PETasks
                    .Include(t => t.PlannedEvent)
                    .FirstOrDefaultAsync(t => t.Id == taskId);

                if (task == null || task.TaskStatus == "COMPLETED")
                {
                    return false;
                }

                bool markAsUrgent = false;
                string priorityMessage = "";

                switch (urgentReason)
                {
                    case "OpeningCeremony":
                        markAsUrgent = true;
                        task.IsUrgent = true;
                        task.UrgentMarkedDate = DateTime.Now;
                        priorityMessage = "[URGENT: Opening Ceremony - Priority 1]";
                        task.Priority = priorityMessage;
                        break;

                    case "CriticalCustomer":
                        markAsUrgent = true;
                        task.IsUrgent = true;
                        task.UrgentMarkedDate = DateTime.Now;
                        priorityMessage = "[URGENT: Critical Customer - Priority 2]";
                        task.Priority = priorityMessage;
                        break;

                    case "Reject":
                        task.UrgentRequested = false;
                        task.Priority = "Urgent Request Rejected";
                        break;

                    default:
                        return false;
                }

                task.UrgentRequested = false;
                _context.Update(task);

                // For task-level urgent, only update PE status if this is the first urgent task
                if (markAsUrgent && task.PlannedEvent != null)
                {
                    var existingUrgentTasks = await _context.PETasks
                        .Where(t => t.PENumber == task.PENumber && t.IsUrgent && t.Id != taskId)
                        .CountAsync();

                    if (existingUrgentTasks == 0)
                    {
                        task.PlannedEvent.PEStatus = "URGENT";
                        task.PlannedEvent.Priority = priorityMessage;
                        _context.Update(task.PlannedEvent);
                    }
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing task urgent request for task {TaskId}", taskId);
                return false;
            }
        }

        public async Task<bool> ProcessPEUrgentRequestAsync(string peNumber, string urgentReason)
        {
            try
            {
                var tasks = await _context.PETasks
                    .Include(t => t.PlannedEvent)
                    .Where(t => t.PENumber == peNumber && t.TaskStatus != "COMPLETED")
                    .ToListAsync();

                if (!tasks.Any())
                {
                    return false;
                }

                bool markAsUrgent = false;
                string priorityMessage = "";

                switch (urgentReason)
                {
                    case "OpeningCeremony":
                        markAsUrgent = true;
                        priorityMessage = "[URGENT: Opening Ceremony - Priority 1]";
                        break;

                    case "CriticalCustomer":
                        markAsUrgent = true;
                        priorityMessage = "[URGENT: Critical Customer - Priority 2]";
                        break;

                    case "Reject":
                        foreach (var task in tasks)
                        {
                            task.UrgentRequested = false;
                            task.Priority = "Urgent Request Rejected";
                            _context.Update(task);
                        }
                        break;

                    default:
                        return false;
                }

                if (markAsUrgent)
                {
                    // Mark ALL tasks in the PE as urgent
                    foreach (var task in tasks)
                    {
                        task.IsUrgent = true;
                        task.UrgentMarkedDate = DateTime.Now;
                        task.UrgentRequested = false;
                        task.Priority = priorityMessage;
                        _context.Update(task);
                    }

                    // Update PE status
                    var plannedEvent = tasks.FirstOrDefault()?.PlannedEvent;
                    if (plannedEvent != null)
                    {
                        plannedEvent.PEStatus = "URGENT";
                        plannedEvent.Priority = priorityMessage;
                        _context.Update(plannedEvent);
                    }
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PE urgent request for PE {PENumber}", peNumber);
                return false;
            }
        }
        
        public async Task<bool> MarkAsUrgentAsync(int taskId)
        {
            try
            {
                var task = await _context.PETasks
                    .Include(t => t.PlannedEvent)
                    .FirstOrDefaultAsync(t => t.Id == taskId);

                if (task == null || task.TaskStatus?.ToUpper() != "ONGOING")
                {
                    return false;
                }

                task.IsUrgent = true;
                task.UrgentMarkedDate = DateTime.Now;
                task.UrgentRequested = false;

                if (task.PlannedEvent != null && !string.IsNullOrWhiteSpace(task.PlannedEvent.Priority))
                {
                    task.Priority = task.PlannedEvent.Priority;
                }
                else
                {
                    task.Priority = (task.Priority ?? "") + " [URGENT]";
                }

                _context.Update(task);

                if (task.PlannedEvent != null)
                {
                    task.PlannedEvent.PEStatus = "URGENT";
                    _context.Update(task.PlannedEvent);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking task {TaskId} as urgent", taskId);
                return false;
            }
        }

        public async Task<IEnumerable<PETask>> GetAllPETasksAsync()
        {
            try
            {
                return await _context.PETasks
                    .OrderByDescending(t => t.TaskCreatedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all PE tasks");
                return new List<PETask>();
            }
        }

        public async Task<IEnumerable<PETask>> GetOLAViolationsAsync()
        {
            try
            {
                var currentDate = DateTime.Today;
                var violatingTasks = await _context.PETasks
                    .Include(t => t.PlannedEvent)
                    .Where(t => t.TaskStatus != "COMPLETED" &&
                               t.TaskCompleteDate < currentDate)
                    .OrderBy(t => t.TaskCompleteDate)
                    .ToListAsync();

                // Update PE status for OLA violations
                var hasUpdates = false;
                foreach (var task in violatingTasks)
                {
                    if (task.PlannedEvent != null && task.PlannedEvent.PEStatus != "ola-violated")
                    {
                        task.PlannedEvent.PEStatus = "ola-violated";
                        _context.Update(task.PlannedEvent);
                        hasUpdates = true;
                    }
                }

                if (hasUpdates)
                {
                    await _context.SaveChangesAsync();
                }

                // Return tasks without the PlannedEvent navigation property to avoid serialization issues
                var result = violatingTasks.Select(t => new PETask
                {
                    Id = t.Id,
                    PENumber = t.PENumber,
                    TaskSeq = t.TaskSeq,
                    Task = t.Task,
                    TaskWorkGroup = t.TaskWorkGroup,
                    OLA = t.OLA,
                    TaskStatus = t.TaskStatus,
                    TaskCreatedDate = t.TaskCreatedDate,
                    TaskCompleteDate = t.TaskCompleteDate,
                    ActualTaskCreatedDate = t.ActualTaskCreatedDate,
                    ACtualTaskCompleteDate = t.ACtualTaskCompleteDate,
                    IsUrgent = t.IsUrgent,
                    UrgentMarkedDate = t.UrgentMarkedDate,
                    UrgentRequested = t.UrgentRequested,
                    Priority = t.Priority,
                    EstimatedTime = t.EstimatedTime,
                    IsOLAViolate = t.IsOLAViolate,
                    OLADateTime = t.OLADateTime,
                    ViolationStartTime = t.ViolationStartTime,
                    EscalationsDisabled = t.EscalationsDisabled
                    // Intentionally excluding PlannedEvent to avoid circular references
                }).ToList();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OLA violations");
                return new List<PETask>();
            }
        }

        public async Task<bool> CompleteViolatedTaskAsync(int id)
        {
            try
            {
                var task = await _context.PETasks
                    .Include(t => t.PlannedEvent)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (task == null)
                {
                    return false;
                }

                task.TaskStatus = "COMPLETED";
                task.ACtualTaskCompleteDate = DateTime.Now;

                var otherViolationsExist = await _context.PETasks
                    .AnyAsync(t => t.PENumber == task.PENumber &&
                                  t.Id != task.Id &&
                                  t.TaskStatus != "COMPLETED" &&
                                  t.TaskCompleteDate.Date < DateTime.Today);

                if (!otherViolationsExist && task.PlannedEvent != null && task.PlannedEvent.PEStatus == "ola-violated")
                {
                    task.PlannedEvent.PEStatus = "ongoing";
                    _context.Update(task.PlannedEvent);
                }

                _context.Update(task);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing violated task {Id}", id);
                return false;
            }
        }

        public async Task<bool> RemoveUrgentStatusAsync(int id)
        {
            try
            {
                var task = await _context.PETasks
                    .Include(t => t.PlannedEvent)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (task == null)
                {
                    return false;
                }

                task.IsUrgent = false;
                task.UrgentMarkedDate = null;
                task.Priority = task.Priority?.Replace("[URGENT: Opening Ceremony - Priority 1]", "")
                                             ?.Replace("[URGENT: Critical Customer - Priority 2]", "")
                                             ?.Trim();

                if (string.IsNullOrWhiteSpace(task.Priority))
                {
                    task.Priority = null;
                }

                _context.Update(task);

                var remainingUrgentTasks = await _context.PETasks
                    .Where(t => t.PENumber == task.PENumber && t.IsUrgent == true && t.Id != id)
                    .CountAsync();

                if (remainingUrgentTasks == 0 && task.PlannedEvent != null)
                {
                    task.PlannedEvent.PEStatus = "ongoing";
                    task.PlannedEvent.Priority = task.PlannedEvent.Priority?.Replace("[URGENT: Opening Ceremony - Priority 1]", "")
                                                                          ?.Replace("[URGENT: Critical Customer - Priority 2]", "")
                                                                          ?.Trim();

                    if (string.IsNullOrWhiteSpace(task.PlannedEvent.Priority))
                    {
                        task.PlannedEvent.Priority = null;
                    }

                    _context.Update(task.PlannedEvent);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing urgent status from task {Id}", id);
                return false;
            }
        }

        public async Task<bool> UpdateEstimatedTimeAsync(int id, DateTime estimatedTime)
        {
            try
            {
                var task = await _context.PETasks
                    .Include(t => t.PlannedEvent)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (task == null)
                    return false;

                if (!string.Equals(task.Task?.Trim(), "Draw Fiber", StringComparison.OrdinalIgnoreCase))
                    return false; // Only allowed for Draw Fiber tasks

                var status = task.TaskStatus?.ToUpper();
                if (status == "COMPLETED")
                    return false; // Already completed

                if (!task.IsOLAViolate && status != "ONGOING" && status != "WAITING")
                    return false; // Only allowed for specific statuses

                if (estimatedTime.Date < DateTime.Today)
                    return false; // Cannot be in the past

                var historyRecord = new TaskEstimationHistory
                {
                    TaskId = id,
                    EstimatedDate = estimatedTime,
                    CreatedAt = DateTime.UtcNow
                };

                _context.TaskEstimationHistory.Add(historyRecord);

                task.EstimatedTime = estimatedTime;
                task.TaskCompleteDate = estimatedTime;

                if (task.IsOLAViolate && estimatedTime.Date >= DateTime.Today)
                {
                    task.IsOLAViolate = false;
                }

                var allTasks = await _context.PETasks
                    .Where(t => t.PENumber == task.PENumber)
                    .OrderBy(t => t.TaskSeq)
                    .ToListAsync();

                int idx = allTasks.FindIndex(t => t.Id == id);
                DateTime prevCompleteDate = estimatedTime;

                for (int i = idx + 1; i < allTasks.Count; i++)
                {
                    var currentTask = allTasks[i];
                    currentTask.TaskCreatedDate = prevCompleteDate;
                    int olaDays = 0;
                    int.TryParse(currentTask.OLA, out olaDays);
                    currentTask.TaskCompleteDate = currentTask.TaskCreatedDate.AddDays(olaDays);
                    prevCompleteDate = currentTask.TaskCompleteDate;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating estimated time for task {Id}", id);
                return false;
            }
        }

        public async Task<IEnumerable<object>> GetEstimationHistoryAsync(int id)
        {
            try
            {
                var history = await _context.TaskEstimationHistory
                    .Where(h => h.TaskId == id)
                    .OrderByDescending(h => h.CreatedAt)
                    .Select(h => new
                    {
                        h.Id,
                        h.TaskId,
                        h.EstimatedDate,
                        h.CreatedAt
                    })
                    .ToListAsync();

                return history;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting estimation history for task {Id}", id);
                return new List<object>();
            }
        }

        public async Task<Dictionary<string, OLAViolationDetails>> GetOLAViolationDetailsAsync(List<string> peNumbers)
        {
            try
            {
                if (peNumbers == null || !peNumbers.Any())
                {
                    return new Dictionary<string, OLAViolationDetails>();
                }

                var violatingTasks = await _context.PETasks
                    .Where(t => peNumbers.Contains(t.PENumber) && t.IsOLAViolate)
                    .ToListAsync();

                var currentDate = DateTime.Today;
                var violationDetails = violatingTasks
                    .GroupBy(t => t.PENumber)
                    .ToDictionary(
                        g => g.Key,
                        g => new OLAViolationDetails
                        {
                            PENumber = g.Key,
                            TasksCount = g.Count(),
                            MaxDaysOverdue = g.Max(t =>
                                t.EstimatedTime.HasValue
                                    ? (currentDate - t.EstimatedTime.Value).Days
                                    : (t.ActualTaskCreatedDate.HasValue && t.OLA != null && int.TryParse(t.OLA, out var olaDays))
                                        ? (currentDate - t.ActualTaskCreatedDate.Value.AddDays(olaDays)).Days
                                        : 0
                            ),
                            OldestViolation = g.Min(t =>
                                t.EstimatedTime ?? (t.ActualTaskCreatedDate.HasValue && t.OLA != null && int.TryParse(t.OLA, out var olaDays)
                                    ? t.ActualTaskCreatedDate.Value.AddDays(olaDays)
                                    : (DateTime?)null))
                        }
                    );

                return violationDetails;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OLA violation details for PE numbers: {PENumbers}", string.Join(", ", peNumbers ?? new List<string>()));
                return new Dictionary<string, OLAViolationDetails>();
            }
        }
    }
}


