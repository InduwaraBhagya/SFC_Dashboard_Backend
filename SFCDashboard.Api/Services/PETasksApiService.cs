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
    }
}


