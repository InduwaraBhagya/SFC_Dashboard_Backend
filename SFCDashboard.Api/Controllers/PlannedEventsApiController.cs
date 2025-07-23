using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDashboard.Models;
using SFCDashboard.Services;
using Microsoft.AspNetCore.Authorization;

namespace SFCDashboard.Api.Controllers
{
    /// <summary>
    /// API Controller for Planned Events management
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class PlannedEventsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PlannedEventsApiController> _logger;
        private readonly IPlannedEventsApiService _plannedEventsService;

        public PlannedEventsApiController(
            ApplicationDbContext context,
            ILogger<PlannedEventsApiController> logger,
            IPlannedEventsApiService plannedEventsService)
        {
            _context = context;
            _logger = logger;
            _plannedEventsService = plannedEventsService;
        }

        /// <summary>
        /// Get all planned events
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PlannedEvent>>> GetPlannedEvents()
        {
            try
            {
                var events = await _context.PlannedEvents
                    .OrderByDescending(pe => pe.PECreatedDate)
                    .ToListAsync();
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving planned events");
                return StatusCode(500, "An error occurred while retrieving planned events");
            }
        }

        /// <summary>
        /// Get planned event by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<PlannedEvent>> GetPlannedEvent(int id)
        {
            try
            {
                var plannedEvent = await _context.PlannedEvents.FindAsync(id);
                if (plannedEvent == null)
                {
                    return NotFound();
                }
                return Ok(plannedEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving planned event {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the planned event");
            }
        }

        /// <summary>
        /// Create a new planned event
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<PlannedEvent>> CreatePlannedEvent(PlannedEvent plannedEvent)
        {
            try
            {
                _context.PlannedEvents.Add(plannedEvent);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetPlannedEvent), new { id = plannedEvent.Id }, plannedEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating planned event");
                return StatusCode(500, "An error occurred while creating the planned event");
            }
        }

        /// <summary>
        /// Update an existing planned event
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePlannedEvent(int id, PlannedEvent plannedEvent)
        {
            if (id != plannedEvent.Id)
            {
                return BadRequest();
            }

            try
            {
                _context.Entry(plannedEvent).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PlannedEventExists(id))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating planned event {Id}", id);
                return StatusCode(500, "An error occurred while updating the planned event");
            }
        }

        /// <summary>
        /// Delete a planned event
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePlannedEvent(int id)
        {
            try
            {
                var plannedEvent = await _context.PlannedEvents.FindAsync(id);
                if (plannedEvent == null)
                {
                    return NotFound();
                }

                _context.PlannedEvents.Remove(plannedEvent);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting planned event {Id}", id);
                return StatusCode(500, "An error occurred while deleting the planned event");
            }
        }

        private bool PlannedEventExists(int id)
        {
            return _context.PlannedEvents.Any(e => e.Id == id);
        }
    }
}
