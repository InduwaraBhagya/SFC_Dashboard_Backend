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
    }
}
