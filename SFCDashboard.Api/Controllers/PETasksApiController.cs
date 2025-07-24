using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDashboard.Models;
using SFCDashboard.Services;
using Microsoft.AspNetCore.Authorization;

namespace SFCDashboard.Api.Controllers
{
    /// <summary>
    /// API Controller for PE Tasks management
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class PETasksApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PETasksApiController> _logger;
        private readonly IPETasksApiService _peTasksService;

        public PETasksApiController(
            ApplicationDbContext context,
            ILogger<PETasksApiController> logger,
            IPETasksApiService peTasksService)
        {
            _context = context;
            _logger = logger;
            _peTasksService = peTasksService;
        }

        /// <summary>
        /// Get all PE tasks
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PETask>>> GetPETasks()
        {
            try
            {
                var tasks = await _context.PETasks
                    .OrderByDescending(t => t.TaskCreatedDate)
                    .ToListAsync();
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving PE tasks");
                return StatusCode(500, "An error occurred while retrieving PE tasks");
            }
        }

        /// <summary>
        /// Get PE task by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<PETask>> GetPETask(int id)
        {
            try
            {
                var peTask = await _context.PETasks
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (peTask == null)
                {
                    return NotFound();
                }
                return Ok(peTask);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving PE task {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the PE task");
            }
        }

        /// <summary>
        /// Get pending task requests
        /// </summary>
        [HttpGet("pending-task-requests")]
        public async Task<ActionResult<IEnumerable<PETask>>> GetPendingTaskRequests([FromQuery] int limit = 10)
        {
            try
            {
                _logger.LogInformation("Getting pending task requests with limit: {limit}", limit);
                var pendingTasks = await _context.PETasks
                    .Where(t => t.TaskStatus == null || t.TaskStatus.ToLower() == "pending" || t.TaskStatus.ToLower() == "new")
                    .OrderByDescending(t => t.TaskCreatedDate)
                    .Take(limit)
                    .ToListAsync();

                return Ok(pendingTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending task requests");
                return StatusCode(500, "An error occurred while retrieving pending task requests");
            }
        }

        /// <summary>
        /// Create a new PE task
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<PETask>> CreatePETask(PETask peTask)
        {
            try
            {
                _context.PETasks.Add(peTask);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetPETask), new { id = peTask.Id }, peTask);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PE task");
                return StatusCode(500, "An error occurred while creating the PE task");
            }
        }

        /// <summary>
        /// Update an existing PE task
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePETask(int id, PETask peTask)
        {
            if (id != peTask.Id)
            {
                return BadRequest();
            }

            try
            {
                _context.Entry(peTask).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PETaskExists(id))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating PE task {Id}", id);
                return StatusCode(500, "An error occurred while updating the PE task");
            }
        }

        /// <summary>
        /// Delete a PE task
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePETask(int id)
        {
            try
            {
                var peTask = await _context.PETasks.FindAsync(id);
                if (peTask == null)
                {
                    return NotFound();
                }

                _context.PETasks.Remove(peTask);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting PE task {Id}", id);
                return StatusCode(500, "An error occurred while deleting the PE task");
            }
        }

        private bool PETaskExists(int id)
        {
            return _context.PETasks.Any(e => e.Id == id);
        }

        /// <summary>
        /// Get PE tasks by PE number
        /// </summary>
        [HttpGet("by-pe/{peNumber}")]
        public async Task<ActionResult<IEnumerable<PETask>>> GetPETasksByPENumber(string peNumber)
        {
            try
            {
                var tasks = await _context.PETasks
                    .Where(t => t.PENumber == peNumber)
                    .Include(t => t.PlannedEvent)
                    .OrderBy(t => t.TaskSeq)
                    .ToListAsync();
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving PE tasks for PE number {PENumber}", peNumber);
                return StatusCode(500, "An error occurred while retrieving PE tasks");
            }
        }

        /// <summary>
        /// Get PE tasks with urgent requests pending
        /// </summary>
        [HttpGet("urgent-requests")]
        public async Task<ActionResult<IEnumerable<PETask>>> GetUrgentRequests()
        {
            try
            {
                var pendingRequests = await _context.PETasks
                    .Where(t => t.UrgentRequested && !t.IsUrgent)
                    .Include(t => t.PlannedEvent)
                    .OrderBy(t => t.PENumber)
                    .ToListAsync();
                return Ok(pendingRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving urgent requests");
                return StatusCode(500, "An error occurred while retrieving urgent requests");
            }
        }

        /// <summary>
        /// Get PE tasks with OLA violations
        /// </summary>
        [HttpGet("ola-violations")]
        public async Task<ActionResult<IEnumerable<PETask>>> GetOLAViolations()
        {
            try
            {
                var currentDate = DateTime.Today;
                var violatingTasks = await _context.PETasks
                    .Include(t => t.PlannedEvent)
                    .Where(t => t.TaskStatus != "COMPLETED" &&
                               t.TaskCompleteDate.Date < currentDate)
                    .OrderBy(t => t.TaskCompleteDate)
                    .ToListAsync();

                // Update PE status for OLA violations
                foreach (var task in violatingTasks)
                {
                    if (task.PlannedEvent != null && task.PlannedEvent.PEStatus != "ola-violated")
                    {
                        task.PlannedEvent.PEStatus = "ola-violated";
                        _context.Update(task.PlannedEvent);
                    }
                }

                if (violatingTasks.Any(t => t.PlannedEvent != null))
                {
                    await _context.SaveChangesAsync();
                }

                return Ok(violatingTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving OLA violations");
                return StatusCode(500, "An error occurred while retrieving OLA violations");
            }
        }

        /// <summary>
        /// Get urgent tasks
        /// </summary>
        [HttpGet("urgent")]
        public async Task<ActionResult<IEnumerable<PETask>>> GetUrgentTasks()
        {
            try
            {
                var urgentTasks = await _context.PETasks
                    .Include(t => t.PlannedEvent)
                    .Where(t => t.IsUrgent == true && t.TaskStatus != "COMPLETED")
                    .OrderByDescending(t => t.UrgentMarkedDate)
                    .ThenBy(t => t.TaskCompleteDate)
                    .ToListAsync();
                return Ok(urgentTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving urgent tasks");
                return StatusCode(500, "An error occurred while retrieving urgent tasks");
            }
        }

        /// <summary>
        /// Mark task as urgent
        /// </summary>
        [HttpPost("{id}/mark-urgent")]
        public async Task<IActionResult> MarkAsUrgent(int id)
        {
            try
            {
                var task = await _context.PETasks
                    .Include(t => t.PlannedEvent)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (task == null || task.TaskStatus?.ToUpper() != "ONGOING")
                {
                    return NotFound();
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
                    task.PlannedEvent.PEStatus = "urgent";
                    _context.Update(task.PlannedEvent);
                }

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking task {Id} as urgent", id);
                return StatusCode(500, "An error occurred while marking task as urgent");
            }
        }

        /// <summary>
        /// Process urgent request
        /// </summary>
        [HttpPost("{id}/process-urgent-request")]
        public async Task<IActionResult> ProcessUrgentRequest(int id, [FromBody] ProcessUrgentRequestDto request)
        {
            try
            {
                var task = await _context.PETasks
                    .Include(t => t.PlannedEvent)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (task == null || task.TaskStatus == "COMPLETED")
                {
                    return NotFound();
                }

                bool markAsUrgent = false;
                string priorityMessage = "";
                int priorityLevel = 0;

                switch (request.UrgentReason)
                {
                    case "OpeningCeremony":
                        markAsUrgent = true;
                        task.IsUrgent = true;
                        task.UrgentMarkedDate = DateTime.Now;
                        priorityMessage = "[URGENT: Opening Ceremony - Priority 1]";
                        priorityLevel = 1;
                        task.Priority = priorityMessage;
                        break;

                    case "CriticalCustomer":
                        markAsUrgent = true;
                        task.IsUrgent = true;
                        task.UrgentMarkedDate = DateTime.Now;
                        priorityMessage = "[URGENT: Critical Customer - Priority 2]";
                        priorityLevel = 2;
                        task.Priority = priorityMessage;
                        break;

                    case "Reject":
                        task.UrgentRequested = false;
                        task.Priority = "Urgent Request Rejected";
                        break;

                    default:
                        return BadRequest("Invalid urgent reason");
                }

                task.UrgentRequested = false;
                _context.Update(task);

                if (markAsUrgent && task.PlannedEvent != null)
                {
                    task.PlannedEvent.PEStatus = "URGENT";
                    task.PlannedEvent.Priority = priorityMessage;
                    _context.Update(task.PlannedEvent);
                }

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing urgent request for task {Id}", id);
                return StatusCode(500, "An error occurred while processing urgent request");
            }
        }

        /// <summary>
        /// Complete violated task
        /// </summary>
        [HttpPost("{id}/complete-violated")]
        public async Task<IActionResult> CompleteViolatedTask(int id)
        {
            try
            {
                var task = await _context.PETasks
                    .Include(t => t.PlannedEvent)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (task == null)
                {
                    return NotFound();
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
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing violated task {Id}", id);
                return StatusCode(500, "An error occurred while completing violated task");
            }
        }

        /// <summary>
        /// Remove urgent status from task
        /// </summary>
        [HttpPost("{id}/remove-urgent")]
        public async Task<IActionResult> RemoveUrgentStatus(int id)
        {
            try
            {
                var task = await _context.PETasks
                    .Include(t => t.PlannedEvent)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (task == null)
                {
                    return NotFound();
                }

                task.IsUrgent = false;
                task.UrgentMarkedDate = null;
                task.Priority = task.Priority?.Replace("[URGENT: Opening Ceremony - Priority 1]", "")
                                             .Replace("[URGENT: Critical Customer - Priority 2]", "")
                                             .Trim();

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
                                                                          .Replace("[URGENT: Critical Customer - Priority 2]", "")
                                                                          .Trim();

                    if (string.IsNullOrWhiteSpace(task.PlannedEvent.Priority))
                    {
                        task.PlannedEvent.Priority = null;
                    }

                    _context.Update(task.PlannedEvent);
                }

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing urgent status from task {Id}", id);
                return StatusCode(500, "An error occurred while removing urgent status");
            }
        }

        /// <summary>
        /// Update estimated time for task
        /// </summary>
        [HttpPost("{id}/update-estimated-time")]
        public async Task<IActionResult> UpdateEstimatedTime(int id, [FromBody] UpdateEstimatedTimeDto request)
        {
            try
            {
                var task = await _context.PETasks
                    .Include(t => t.PlannedEvent)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (task == null)
                    return NotFound("Task not found");

                if (!string.Equals(task.Task?.Trim(), "Draw Fiber", StringComparison.OrdinalIgnoreCase))
                    return BadRequest("Estimated Time can only be set for 'Draw Fiber' tasks.");

                var status = task.TaskStatus?.ToUpper();
                if (status == "COMPLETED")
                    return BadRequest("Already completed task.");

                if (!task.IsOLAViolate && status != "ONGOING" && status != "WAITING")
                    return BadRequest("Estimated Time can only be set when TaskStatus is ONGOING or WAITING or task is OLA Violated.");

                if (request.EstimatedTime.Date < DateTime.Today)
                    return BadRequest("Estimated Time cannot be in the past.");

                var historyRecord = new TaskEstimationHistory
                {
                    TaskId = id,
                    EstimatedDate = request.EstimatedTime,
                    CreatedAt = DateTime.UtcNow
                };

                _context.TaskEstimationHistory.Add(historyRecord);

                task.EstimatedTime = request.EstimatedTime;
                task.TaskCompleteDate = request.EstimatedTime;

                if (task.IsOLAViolate && request.EstimatedTime.Date >= DateTime.Today)
                {
                    task.IsOLAViolate = false;
                }

                var allTasks = await _context.PETasks
                    .Where(t => t.PENumber == task.PENumber)
                    .OrderBy(t => t.TaskSeq)
                    .ToListAsync();

                int idx = allTasks.FindIndex(t => t.Id == id);
                DateTime prevCompleteDate = request.EstimatedTime;
                
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
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating estimated time for task {Id}", id);
                return StatusCode(500, "An error occurred while updating estimated time");
            }
        }
    }

    public class ProcessUrgentRequestDto
    {
        public string UrgentReason { get; set; } = string.Empty;
    }

    public class UpdateEstimatedTimeDto
    {
        public DateTime EstimatedTime { get; set; }
    }
}
