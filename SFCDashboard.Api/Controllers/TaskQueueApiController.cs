using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SFCDashboard.Api.Models;
using SFCDashboard.Api.Services;

namespace SFCDashboard.Api.Controllers
{
    /// <summary>
    /// API Controller for Task Queue management
    /// </summary>
    [ApiController]
    [Route("api/taskqueue")]
    [Produces("application/json")]
    [Authorize]
    public class TaskQueueApiController : ControllerBase
    {
        private readonly ITaskQueueService _taskQueueService;
        private readonly ILogger<TaskQueueApiController> _logger;

        public TaskQueueApiController(
            ITaskQueueService taskQueueService,
            ILogger<TaskQueueApiController> logger)
        {
            _taskQueueService = taskQueueService;
            _logger = logger;
        }

        /// <summary>
        /// Get prioritized tasks from the task queue
        /// </summary>
        [HttpGet("prioritized")]
        public async Task<ActionResult<List<TaskQueueItem>>> GetPrioritizedTasks(
            [FromQuery] int? workgroupId = null, 
            [FromQuery] int? year = null, 
            [FromQuery] int take = 20)
        {
            try
            {
                var tasks = await _taskQueueService.GetPrioritizedTasksAsync(workgroupId, year, take);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving prioritized tasks with workgroupId: {workgroupId}, year: {year}, take: {take}",
                    workgroupId, year, take);
                return StatusCode(500, "An error occurred while retrieving prioritized tasks");
            }
        }

        /// <summary>
        /// Get the next task from the queue
        /// </summary>
        [HttpGet("next")]
        public async Task<ActionResult<TaskQueueItem?>> GetNextTask(
            [FromQuery] int? workgroupId = null, 
            [FromQuery] int? year = null)
        {
            try
            {
                var nextTask = await _taskQueueService.GetNextTaskAsync(workgroupId, year);
                if (nextTask == null)
                {
                    return NotFound("No tasks available in the queue");
                }
                return Ok(nextTask);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving next task with workgroupId: {workgroupId}, year: {year}",
                    workgroupId, year);
                return StatusCode(500, "An error occurred while retrieving the next task");
            }
        }

        /// <summary>
        /// Get available years for task queue filtering
        /// </summary>
        [HttpGet("years")]
        public async Task<ActionResult<List<int>>> GetAvailableYears()
        {
            try
            {
                var years = await _taskQueueService.GetAvailableYearsAsync();
                return Ok(years);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available years");
                return StatusCode(500, "An error occurred while retrieving available years");
            }
        }


    }
}
