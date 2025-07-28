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
        private readonly IUsersApiService _usersApiService;

        public PlannedEventsApiController(
            ApplicationDbContext context,
            ILogger<PlannedEventsApiController> logger,
            IPlannedEventsApiService plannedEventsService,
            IUsersApiService usersApiService)
        {
            _context = context;
            _logger = logger;
            _plannedEventsService = plannedEventsService;
            _usersApiService = usersApiService;
        }
        /// <summary>
        /// Get in-progress planned events for a specific user (filtered by backend)
        /// </summary>
        [HttpGet("inprogress/user/{userId}")]
        public async Task<ActionResult<IEnumerable<PlannedEvent>>> GetInProgressPlannedEventsByUserId(int userId)
        {
            try
            {
                var events = await _plannedEventsService.GetInProgressPlannedEventsByUserIdAsync(userId);
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving in-progress planned events for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving in-progress planned events for the user");
            }
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
        /// Get pending urgent requests
        /// </summary>
        [HttpGet("pending-urgent-requests")]
        public async Task<ActionResult<IEnumerable<PlannedEvent>>> GetPendingUrgentRequests([FromQuery] int limit = 10)
        {
            try
            {
                _logger.LogInformation("Getting pending urgent requests with limit: {limit}", limit);
                var urgentRequests = await _context.PlannedEvents
                    .Where(pe => pe.UrgentRequestedById != null && pe.UrgentRequestedById > 0)
                    .OrderByDescending(pe => pe.PECreatedDate)
                    .Take(limit)
                    .ToListAsync();

                return Ok(urgentRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending urgent requests");
                return StatusCode(500, "An error occurred while retrieving pending urgent requests");
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
