using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Api.Models;
using SFCDashboard.Api.Services;

namespace SFCDashboard.Api.Controllers
{
    /// <summary>
    /// API Controller for SubTask Lists management
    /// </summary>
    [ApiController]
    [Route("api/subtasklists")]
    [Produces("application/json")]
    public class SubTaskListsApiController : ControllerBase
    {
        private readonly ISubTaskListsApiService _subTaskListsService;
        private readonly ILogger<SubTaskListsApiController> _logger;

        public SubTaskListsApiController(
            ISubTaskListsApiService subTaskListsService,
            ILogger<SubTaskListsApiController> logger)
        {
            _subTaskListsService = subTaskListsService;
            _logger = logger;
        }

        /// <summary>
        /// Get all subtask lists
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SubTaskList>>> GetSubTaskLists()
        {
            try
            {
                var subTaskLists = await _subTaskListsService.GetAllSubTaskListsAsync();
                return Ok(subTaskLists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subtask lists");
                return StatusCode(500, "An error occurred while retrieving subtask lists");
            }
        }

        /// <summary>
        /// Get subtask list by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<SubTaskList>> GetSubTaskList(int id)
        {
            try
            {
                var subTaskList = await _subTaskListsService.GetSubTaskListByIdAsync(id);
                if (subTaskList == null)
                {
                    return NotFound($"SubTask list with ID {id} not found");
                }
                return Ok(subTaskList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subtask list {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the subtask list");
            }
        }

        /// <summary>
        /// Get subtask lists by task list ID
        /// </summary>
        [HttpGet("bytasklist/{taskListId}")]
        public async Task<ActionResult<IEnumerable<SubTaskList>>> GetSubTaskListsByTaskListId(int taskListId)
        {
            try
            {
                var subTaskLists = await _subTaskListsService.GetSubTaskListsByTaskListIdAsync(taskListId);
                return Ok(subTaskLists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subtask lists for task list {TaskListId}", taskListId);
                return StatusCode(500, "An error occurred while retrieving subtask lists for the task list");
            }
        }

        /// <summary>
        /// Get subtask list by task list ID and name
        /// </summary>
        [HttpGet("bytasklistandname/{taskListId}")]
        public async Task<ActionResult<SubTaskList>> GetSubTaskListByTaskListIdAndName(int taskListId, [FromQuery] string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return BadRequest("Name parameter is required");
                }

                var subTaskList = await _subTaskListsService.GetSubTaskListByTaskListIdAndNameAsync(taskListId, name);
                if (subTaskList == null)
                {
                    return StatusCode(500, "An error occurred while retrieving/creating the subtask list");
                }

                return Ok(subTaskList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving/creating subtask list for task list {TaskListId} and name {Name}", taskListId, name);
                return StatusCode(500, "An error occurred while retrieving/creating the subtask list");
            }
        }

        /// <summary>
        /// Create a new subtask list
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<SubTaskList>> CreateSubTaskList(SubTaskList subTaskList)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var createdSubTaskList = await _subTaskListsService.CreateSubTaskListAsync(subTaskList);
                if (createdSubTaskList == null)
                {
                    return StatusCode(500, "An error occurred while creating the subtask list");
                }

                return CreatedAtAction(nameof(GetSubTaskList), new { id = createdSubTaskList.Id }, createdSubTaskList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subtask list");
                return StatusCode(500, "An error occurred while creating the subtask list");
            }
        }

        /// <summary>
        /// Update an existing subtask list
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSubTaskList(int id, SubTaskList subTaskList)
        {
            if (id != subTaskList.Id)
            {
                return BadRequest("ID mismatch");
            }

            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var updatedSubTaskList = await _subTaskListsService.UpdateSubTaskListAsync(subTaskList);
                if (updatedSubTaskList == null)
                {
                    var exists = await _subTaskListsService.SubTaskListExistsAsync(id);
                    if (!exists)
                    {
                        return NotFound();
                    }
                    return StatusCode(500, "An error occurred while updating the subtask list");
                }
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subtask list {Id}", id);
                return StatusCode(500, "An error occurred while updating the subtask list");
            }
        }

        /// <summary>
        /// Delete a subtask list
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSubTaskList(int id)
        {
            try
            {
                var success = await _subTaskListsService.DeleteSubTaskListAsync(id);
                if (!success)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting subtask list {Id}", id);
                return StatusCode(500, "An error occurred while deleting the subtask list");
            }
        }

        private async Task<bool> SubTaskListExists(int id)
        {
            return await _subTaskListsService.SubTaskListExistsAsync(id);
        }
    }
}
