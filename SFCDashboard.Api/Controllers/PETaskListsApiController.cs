using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Api.Data;
using SFCDashboard.Api.Models;
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
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PETaskListsApiController> _logger;

        public PETaskListsApiController(
            ApplicationDbContext context,
            ILogger<PETaskListsApiController> logger)
        {
            _context = context;
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
                var taskLists = await _context.PETaskLists
                    .OrderBy(tl => tl.TaskSeq)
                    .ToListAsync();
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
                var taskList = await _context.PETaskLists
                    .FirstOrDefaultAsync(tl => tl.Id == id);

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
                var taskList = await _context.PETaskLists
                    .FirstOrDefaultAsync(tl => tl.Name == taskName);

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
                var exists = await _context.PETaskLists.AnyAsync(tl => tl.Id == id);
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
                _context.PETaskLists.Add(taskList);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetPETaskList), new { id = taskList.Id }, taskList);
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
                _context.Entry(taskList).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await PETaskListExists(id))
                {
                    return NotFound();
                }
                throw;
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
                var taskList = await _context.PETaskLists.FindAsync(id);
                if (taskList == null)
                {
                    return NotFound();
                }

                _context.PETaskLists.Remove(taskList);
                await _context.SaveChangesAsync();

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
            return await _context.PETaskLists.AnyAsync(e => e.Id == id);
        }
    }
}

