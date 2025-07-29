using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Api.Models;
using SFCDashboard.Api.Services;
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
        private readonly ILogger<PlannedEventsApiController> _logger;
        private readonly IPlannedEventsApiService _plannedEventsService;

        public PlannedEventsApiController(
            ILogger<PlannedEventsApiController> logger,
            IPlannedEventsApiService plannedEventsService)
        {
            _logger = logger;
            _plannedEventsService = plannedEventsService;
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
        /// Get OLA violating planned events for a specific user (filtered by backend)
        /// </summary>
        [HttpGet("ola-violating/user/{userId}")]
        public async Task<ActionResult<IEnumerable<PlannedEvent>>> GetOLAViolatingPlannedEventsByUserId(int userId)
        {
            try
            {
                var events = await _plannedEventsService.GetOLAViolatingPlannedEventsByUserIdAsync(userId);
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving OLA violating planned events for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving OLA violating planned events for the user");
            }
        }

        /// <summary>
        /// Get urgent planned events for a specific user (filtered by backend)
        /// </summary>
        [HttpGet("urgent/user/{userId}")]
        public async Task<ActionResult<IEnumerable<PlannedEvent>>> GetUrgentPlannedEventsByUserId(int userId)
        {
            try
            {
                var events = await _plannedEventsService.GetUrgentPlannedEventsByUserIdAsync(userId);
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving urgent planned events for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving urgent planned events for the user");
            }
        }

        /// <summary>
        /// Get hold planned events for a specific user (filtered by backend)
        /// </summary>
        [HttpGet("hold/user/{userId}")]
        public async Task<ActionResult<IEnumerable<PlannedEvent>>> GetHoldPlannedEventsByUserId(int userId)
        {
            try
            {
                var events = await _plannedEventsService.GetHoldPlannedEventsByUserIdAsync(userId);
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving hold planned events for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving hold planned events for the user");
            }
        }

        /// <summary>
        /// Get in-progress count for a specific user (filtered by backend)
        /// </summary>
        [HttpGet("inprogress/user/{userId}/count")]
        public async Task<ActionResult<int>> GetInProgressCountByUserId(int userId)
        {
            try
            {
                var count = await _plannedEventsService.GetInProgressCountByUserIdAsync(userId);
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving in-progress count for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving in-progress count for the user");
            }
        }

        /// <summary>
        /// Get OLA violating count for a specific user (filtered by backend)
        /// </summary>
        [HttpGet("ola-violating/user/{userId}/count")]
        public async Task<ActionResult<int>> GetOLAViolatingCountByUserId(int userId)
        {
            try
            {
                var count = await _plannedEventsService.GetOLAViolatingCountByUserIdAsync(userId);
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving OLA violating count for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving OLA violating count for the user");
            }
        }

        /// <summary>
        /// Get urgent count for a specific user (filtered by backend)
        /// </summary>
        [HttpGet("urgent/user/{userId}/count")]
        public async Task<ActionResult<int>> GetUrgentCountByUserId(int userId)
        {
            try
            {
                var count = await _plannedEventsService.GetUrgentCountByUserIdAsync(userId);
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving urgent count for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving urgent count for the user");
            }
        }

        /// <summary>
        /// Get hold count for a specific user (filtered by backend)
        /// </summary>
        [HttpGet("hold/user/{userId}/count")]
        public async Task<ActionResult<int>> GetHoldCountByUserId(int userId)
        {
            try
            {
                var count = await _plannedEventsService.GetHoldCountByUserIdAsync(userId);
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving hold count for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving hold count for the user");
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
                var events = await _plannedEventsService.GetPlannedEventsAsync();
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
                var plannedEvent = await _plannedEventsService.GetPlannedEventAsync(id);
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
                var urgentRequests = await _plannedEventsService.GetPendingUrgentRequestsAsync(limit);
                return Ok(urgentRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending urgent requests");
                return StatusCode(500, "An error occurred while retrieving pending urgent requests");
            }
        }
    }
}

