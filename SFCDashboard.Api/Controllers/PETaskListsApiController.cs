using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Api.Models;
using SFCDashboard.Api.Services;
using Microsoft.AspNetCore.Authorization;

namespace SFCDashboard.Api.Controllers
{
    /// <summary>
    /// API Controller for PE Task Lists management
    /// </summary>
    [ApiController]
    [Route("api/petasklists")]
    [Produces("application/json")]
    public class PETaskListsApiController : ControllerBase
    {
        private readonly IPETaskListsApiService _taskListService;
        private readonly ILogger<PETaskListsApiController> _logger;

        public PETaskListsApiController(
            IPETaskListsApiService taskListService,
            ILogger<PETaskListsApiController> logger)
        {
            _taskListService = taskListService;
            _logger = logger;
        }

        /// <summary>
        /// Get all PE task lists
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PETaskList>>> GetPETaskLists()
        {
            try
            {
                var taskLists = await _taskListService.GetPETaskListsAsync();
                return Ok(taskLists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving PE task lists");
                return StatusCode(500, "An error occurred while retrieving PE task lists");
            }
        }

        /// <summary>
        /// Get PE task list by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<PETaskList>> GetPETaskList(int id)
        {
            try
            {
                var taskList = await _taskListService.GetPETaskListAsync(id);

                if (taskList == null)
                {
                    return NotFound();
                }
                return Ok(taskList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving PE task list {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the PE task list");
            }
        }

        /// <summary>
        /// Get PE task list by name
        /// </summary>
        [HttpGet("by-name/{taskName}")]
        public async Task<ActionResult<PETaskList>> GetPETaskListByName(string taskName)
        {
            try
            {
                var taskList = await _taskListService.GetPETaskListByNameAsync(taskName);

                if (taskList == null)
                {
                    return NotFound();
                }
                return Ok(taskList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving PE task list by name {TaskName}", taskName);
                return StatusCode(500, "An error occurred while retrieving the PE task list");
            }
        }

        /// <summary>
        /// Check if PE task list exists
        /// </summary>
        [HttpGet("{id}/exists")]
        public async Task<ActionResult<bool>> TaskListExists(int id)
        {
            try
            {
                var taskList = await _taskListService.GetPETaskListAsync(id);
                var exists = taskList != null;
                return Ok(exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if PE task list {Id} exists", id);
                return StatusCode(500, "An error occurred while checking task list existence");
            }
        }

        /// <summary>
        /// Create a new PE task list
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<PETaskList>> CreatePETaskList(PETaskList taskList)
        {
            try
            {
                var createdTaskList = await _taskListService.CreatePETaskListAsync(taskList);
                
                if (createdTaskList == null)
                {
                    return StatusCode(500, "An error occurred while creating the PE task list");
                }

                return CreatedAtAction(nameof(GetPETaskList), new { id = createdTaskList.Id }, createdTaskList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PE task list");
                return StatusCode(500, "An error occurred while creating the PE task list");
            }
        }

        /// <summary>
        /// Update an existing PE task list
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePETaskList(int id, PETaskList taskList)
        {
            if (id != taskList.Id)
            {
                return BadRequest();
            }

            try
            {
                var updatedTaskList = await _taskListService.UpdatePETaskListAsync(taskList);
                
                if (updatedTaskList == null)
                {
                    // Check if the task list exists
                    var existingTaskList = await _taskListService.GetPETaskListAsync(id);
                    if (existingTaskList == null)
                    {
                        return NotFound();
                    }
                    return StatusCode(500, "An error occurred while updating the PE task list");
                }
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating PE task list {Id}", id);
                return StatusCode(500, "An error occurred while updating the PE task list");
            }
        }

        /// <summary>
        /// Delete a PE task list
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePETaskList(int id)
        {
            try
            {
                var success = await _taskListService.DeletePETaskListAsync(id);
                
                if (!success)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting PE task list {Id}", id);
                return StatusCode(500, "An error occurred while deleting the PE task list");
            }
        }

        private async Task<bool> PETaskListExists(int id)
        {
            var taskList = await _taskListService.GetPETaskListAsync(id);
            return taskList != null;
        }
    }
}

