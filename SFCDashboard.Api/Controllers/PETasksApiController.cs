using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Api.Models;
using SFCDashboard.Api.Services;
using Microsoft.AspNetCore.Authorization;

namespace SFCDashboard.Api.Controllers
{
    /// <summary>
    /// API Controller for PE Tasks management
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    // [Authorize]
    public class PETasksApiController : ControllerBase
    {
        private readonly ILogger<PETasksApiController> _logger;
        private readonly IPETasksApiService _peTasksService;

        public PETasksApiController(
            ILogger<PETasksApiController> logger,
            IPETasksApiService peTasksService)
        {
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
                var tasks = await _peTasksService.GetAllPETasksAsync();
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
                var peTask = await _peTasksService.GetPETaskAsync(id);

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
                var pendingTasks = await _peTasksService.GetPendingTaskRequestsAsync(limit);
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
                var createdTask = await _peTasksService.CreatePETaskAsync(peTask);
                
                if (createdTask == null)
                {
                    return StatusCode(500, "An error occurred while creating the PE task");
                }

                return CreatedAtAction(nameof(GetPETask), new { id = createdTask.Id }, createdTask);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PE task");
                return StatusCode(500, "An error occurred while creating the PE task");
            }
        }

        /// <summary>
        /// Get PE tasks by multiple PE numbers
        /// </summary>
        [HttpPost("pe-tasks-by-pe-numbers")]
        public async Task<ActionResult<IEnumerable<PETask>>> GetPETasksByPENumbers([FromBody] PENumbersRequest request)
        {
            try
            {
                _logger.LogInformation("Getting PE tasks for {Count} PE numbers", request.PeNumbers?.Count ?? 0);

                if (request.PeNumbers == null || !request.PeNumbers.Any())
                {
                    return Ok(new List<PETask>());
                }

                var tasks = await _peTasksService.GetPETasksByPENumbersAsync(request.PeNumbers);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PE tasks by PE numbers");
                return StatusCode(500, "An error occurred while retrieving PE tasks by PE numbers");
            }
        }

        /// <summary>
        /// Get PE tasks by multiple PE numbers grouped by PE number
        /// </summary>
        [HttpPost("tasks-by-pe-numbers")]
        public async Task<ActionResult<Dictionary<string, IEnumerable<PETask>>>> GetTasksByPENumbers([FromBody] PENumbersRequest request)
        {
            try
            {
                _logger.LogInformation("Getting PE tasks grouped by PE numbers: {Count} numbers", request.PeNumbers?.Count ?? 0);

                if (request.PeNumbers == null || !request.PeNumbers.Any())
                {
                    return Ok(new Dictionary<string, IEnumerable<PETask>>());
                }

                var tasksDictionary = await _peTasksService.GetTasksByPeNumbersAsync(request.PeNumbers.Cast<string?>().ToList());
                return Ok(tasksDictionary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PE tasks grouped by PE numbers");
                return StatusCode(500, "An error occurred while retrieving PE tasks grouped by PE numbers");
            }
        }

        /// <summary>
        /// Request model for PE numbers
        /// </summary>
        public class PENumbersRequest
        {
            public List<string> PeNumbers { get; set; } = new List<string>();
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
                var updatedTask = await _peTasksService.UpdatePETaskAsync(peTask);
                
                if (updatedTask == null)
                {
                    // Check if the task exists
                    var existingTask = await _peTasksService.GetPETaskAsync(id);
                    if (existingTask == null)
                    {
                        return NotFound();
                    }
                    return StatusCode(500, "An error occurred while updating the PE task");
                }

                return Ok(updatedTask);
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
                var success = await _peTasksService.DeletePETaskAsync(id);
                
                if (!success)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting PE task {Id}", id);
                return StatusCode(500, "An error occurred while deleting the PE task");
            }
        }

        private async Task<bool> PETaskExists(int id)
        {
            var task = await _peTasksService.GetPETaskAsync(id);
            return task != null;
        }

        /// <summary>
        /// Get PE tasks by PE number
        /// </summary>
        [HttpGet("by-pe/{peNumber}")]
        public async Task<ActionResult<IEnumerable<PETask>>> GetPETasksByPENumber(string peNumber)
        {
            try
            {
                var tasks = await _peTasksService.GetPETasksByPENumberAsync(peNumber);
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
                var pendingRequests = await _peTasksService.GetPendingUrgentTaskRequestsAsync();
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
                var violatingTasks = await _peTasksService.GetOLAViolationsAsync();
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
                var urgentTasks = await _peTasksService.GetUrgentTasksAsync();
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
                var result = await _peTasksService.MarkAsUrgentAsync(id);

                if (!result)
                {
                    return NotFound("Task not found or not in ONGOING status");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking task {Id} as urgent", id);
                return StatusCode(500, "An error occurred while marking task as urgent");
            }
        }

        /// <summary>
        /// Process urgent request for a specific task only
        /// </summary>
        [HttpPost("{id}/process-urgent-request")]
        public async Task<IActionResult> ProcessUrgentRequest(int id, [FromBody] ProcessUrgentRequestDto request)
        {
            try
            {
                var result = await _peTasksService.ProcessTaskUrgentRequestAsync(id, request.UrgentReason);

                if (!result)
                {
                    return NotFound("Task not found or already completed");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing urgent request for task {Id}", id);
                return StatusCode(500, "An error occurred while processing urgent request");
            }
        }

        /// <summary>
        /// Process urgent request for entire PE (all tasks become urgent)
        /// </summary>
        [HttpPost("pe/{peNumber}/process-urgent-request")]
        public async Task<IActionResult> ProcessPEUrgentRequest(string peNumber, [FromBody] ProcessUrgentRequestDto request)
        {
            try
            {
                var result = await _peTasksService.ProcessPEUrgentRequestAsync(peNumber, request.UrgentReason);

                if (!result)
                {
                    return NotFound("No active tasks found for PE");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PE urgent request for PE {PENumber}", peNumber);
                return StatusCode(500, "An error occurred while processing PE urgent request");
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
                var result = await _peTasksService.CompleteViolatedTaskAsync(id);
                
                if (!result)
                {
                    return NotFound();
                }

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
                var result = await _peTasksService.RemoveUrgentStatusAsync(id);
                
                if (!result)
                {
                    return NotFound();
                }

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
        [HttpPut("{id}/estimated-time")]
        public async Task<IActionResult> UpdateEstimatedTime(int id, [FromBody] UpdateEstimatedTimeDto request)
        {
            try
            {
                var result = await _peTasksService.UpdateEstimatedTimeAsync(id, request.EstimatedTime);
                
                if (!result)
                {
                    return BadRequest("Unable to update estimated time. Please check the task status and requirements.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating estimated time for task {Id}", id);
                return StatusCode(500, "An error occurred while updating estimated time");
            }
        }

        /// <summary>
        /// Get estimation history for a task
        /// </summary>
        [HttpGet("{id}/estimation-history")]
        public async Task<IActionResult> GetEstimationHistory(int id)
        {
            try
            {
                var history = await _peTasksService.GetEstimationHistoryAsync(id);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving estimation history for task {Id}", id);
                return StatusCode(500, "An error occurred while retrieving estimation history");
            }
        }

        /// <summary>
        /// Get OLA violation details for specified PE numbers
        /// </summary>
        [HttpPost("violation-details")]
        public async Task<ActionResult<Dictionary<string, OLAViolationDetails>>> GetOLAViolationDetails([FromBody] List<string> peNumbers)
        {
            try
            {
                var violationDetails = await _peTasksService.GetOLAViolationDetailsAsync(peNumbers);
                return Ok(violationDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving OLA violation details for PE numbers: {PENumbers}", string.Join(", ", peNumbers ?? new List<string>()));
                return StatusCode(500, "An error occurred while retrieving OLA violation details");
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

