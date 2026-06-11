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
        private readonly IUsersApiService _usersApiService;

        public PlannedEventsApiController(
            ILogger<PlannedEventsApiController> logger,
            IPlannedEventsApiService plannedEventsService,
            IUsersApiService usersApiService)
        {
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
        /// Get planned event by PE Number
        /// </summary>
        [HttpGet("penumber/{peNumber}")]
        public async Task<ActionResult<PlannedEvent>> GetPlannedEventByPENumber(string peNumber)
        {
            try
            {
                var plannedEvent = await _plannedEventsService.GetPlannedEventByPENumberAsync(peNumber);
                if (plannedEvent == null)
                {
                    return NotFound();
                }
                return Ok(plannedEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving planned event by PE Number {PENumber}", peNumber);
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

        /// <summary>
        /// Create a new planned event
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<PlannedEvent>> CreatePlannedEvent([FromBody] PlannedEvent plannedEvent)
        {
            try
            {
                if (plannedEvent == null)
                {
                    return BadRequest("Planned event data is required");
                }

                var createdEvent = await _plannedEventsService.CreatePlannedEventAsync(plannedEvent);
                if (createdEvent == null)
                {
                    return StatusCode(500, "Failed to create the planned event");
                }

                return CreatedAtAction(nameof(GetPlannedEvent), new { id = createdEvent.Id }, createdEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating planned event");
                return StatusCode(500, "An error occurred while creating the planned event");
            }
        }

        /// <summary>
        /// Update a planned event
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<PlannedEvent>> UpdatePlannedEvent(int id, [FromBody] PlannedEvent plannedEvent)
        {
            try
            {
                if (id != plannedEvent.Id)
                {
                    return BadRequest("ID in URL does not match ID in request body");
                }

                var updatedEvent = await _plannedEventsService.UpdatePlannedEventAsync(plannedEvent);
                if (updatedEvent == null)
                {
                    return NotFound($"Planned event with ID {id} not found");
                }

                return Ok(updatedEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating planned event {Id}", id);
                return StatusCode(500, "An error occurred while updating the planned event");
            }
        }

        /// <summary>
        /// Get urgent count for multi-workgroup
        /// </summary>
        [HttpPost("urgent-count-multi-workgroup")]
        public async Task<ActionResult<object>> GetUrgentCountForMultiWorkgroup([FromBody] MultiWorkgroupRequest request)
        {
            try
            {
                var count = await _plannedEventsService.GetUrgentCountForMultiWorkgroupAsync(
                    request.SelectedWorkgroupIds, 
                    request.UserWorkgroupIds, 
                    request.HasDrawFiberAccess);
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting urgent count for multi workgroup");
                return StatusCode(500, "An error occurred while retrieving urgent count for multi workgroup");
            }
        }

        /// <summary>
        /// Get in-progress count for multi-workgroup
        /// </summary>
        [HttpPost("inprogress-count-multi-workgroup")]
        public async Task<ActionResult<object>> GetInProgressCountForMultiWorkgroup([FromBody] MultiWorkgroupRequest request)
        {
            try
            {
                var count = await _plannedEventsService.GetInProgressCountForMultiWorkgroupAsync(
                    request.SelectedWorkgroupIds, 
                    request.UserWorkgroupIds, 
                    request.HasDrawFiberAccess);
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting in-progress count for multi workgroup");
                return StatusCode(500, "An error occurred while retrieving in-progress count for multi workgroup");
            }
        }

        /// <summary>
        /// Get OLA violate count for multi-workgroup
        /// </summary>
        [HttpPost("ola-violate-count-multi-workgroup")]
        public async Task<ActionResult<object>> GetOLAViolateCountForMultiWorkgroup([FromBody] MultiWorkgroupRequest request)
        {
            try
            {
                var count = await _plannedEventsService.GetOLAViolateCountForMultiWorkgroupAsync(
                    request.SelectedWorkgroupIds, 
                    request.UserWorkgroupIds, 
                    request.HasDrawFiberAccess);
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OLA violate count for multi workgroup");
                return StatusCode(500, "An error occurred while retrieving OLA violate count for multi workgroup");
            }
        }

        /// <summary>
        /// Get hold count for multi-workgroup
        /// </summary>
        [HttpPost("hold-count-multi-workgroup")]
        public async Task<ActionResult<object>> GetHoldCountForMultiWorkgroup([FromBody] MultiWorkgroupRequest request)
        {
            try
            {
                var count = await _plannedEventsService.GetHoldCountForMultiWorkgroupAsync(
                    request.SelectedWorkgroupIds, 
                    request.UserWorkgroupIds, 
                    request.HasDrawFiberAccess);
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hold count for multi workgroup");
                return StatusCode(500, "An error occurred while retrieving hold count for multi workgroup");
            }
        }

        /// <summary>
        /// Get in-progress records for multi-workgroup
        /// </summary>
        [HttpPost("inprogress-records-multi-workgroup")]
        public async Task<ActionResult<IEnumerable<PlannedEvent>>> GetInProgressRecordsForMultiWorkgroup([FromBody] MultiWorkgroupRequest request)
        {
            try
            {
                var records = await _plannedEventsService.GetInProgressPlannedEventsForMultiWorkgroupAsync(
                    request.SelectedWorkgroupIds, 
                    request.UserWorkgroupIds, 
                    request.HasDrawFiberAccess);
                return Ok(records);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting in-progress records for multi workgroup");
                return StatusCode(500, "An error occurred while retrieving in-progress records for multi workgroup");
            }
        }

        /// <summary>
        /// Get urgent records for multi-workgroup
        /// </summary>
        [HttpPost("urgent-records-multi-workgroup")]
        public async Task<ActionResult<IEnumerable<PlannedEvent>>> GetUrgentRecordsForMultiWorkgroup([FromBody] MultiWorkgroupRequest request)
        {
            try
            {
                var records = await _plannedEventsService.GetUrgentPlannedEventsForMultiWorkgroupAsync(
                    request.SelectedWorkgroupIds, 
                    request.UserWorkgroupIds, 
                    request.HasDrawFiberAccess);
                return Ok(records);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting urgent records for multi workgroup");
                return StatusCode(500, "An error occurred while retrieving urgent records for multi workgroup");
            }
        }

        /// <summary>
        /// Get hold records for multi-workgroup
        /// </summary>
        [HttpPost("hold-records-multi-workgroup")]
        public async Task<ActionResult<IEnumerable<PlannedEvent>>> GetHoldRecordsForMultiWorkgroup([FromBody] MultiWorkgroupRequest request)
        {
            try
            {
                var records = await _plannedEventsService.GetHoldPlannedEventsForMultiWorkgroupAsync(
                    request.SelectedWorkgroupIds, 
                    request.UserWorkgroupIds, 
                    request.HasDrawFiberAccess);
                return Ok(records);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hold records for multi workgroup");
                return StatusCode(500, "An error occurred while retrieving hold records for multi workgroup");
            }
        }

        /// <summary>
        /// Get OLA violating records for multi-workgroup
        /// </summary>
        [HttpPost("ola-violating-records-multi-workgroup")]
        public async Task<ActionResult<IEnumerable<PlannedEvent>>> GetOLAViolatingRecordsForMultiWorkgroup([FromBody] MultiWorkgroupRequest request)
        {
            try
            {
                var records = await _plannedEventsService.GetOLAViolatingPlannedEventsForMultiWorkgroupAsync(
                    request.SelectedWorkgroupIds, 
                    request.UserWorkgroupIds, 
                    request.HasDrawFiberAccess);
                return Ok(records);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OLA violating records for multi workgroup");
                return StatusCode(500, "An error occurred while retrieving OLA violating records for multi workgroup");
            }
        }

        /// <summary>
        /// Get sales in-progress records for the current user
        /// </summary>
        [HttpGet("sales-inprogress-records")]
        public async Task<ActionResult<IEnumerable<PlannedEvent>>> GetSalesInProgressRecords()
        {
            try
            {
                var userIdentity = User.Identity?.Name;
                if (string.IsNullOrEmpty(userIdentity))
                {
                    return Unauthorized("User identity not found");
                }

                var userId = await _usersApiService.GetCurrentUserIdAsync(userIdentity);
                var records = await _plannedEventsService.GetSalesInProgressRecordsAsync(userId);
                return Ok(records);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales in-progress records");
                return StatusCode(500, "An error occurred while retrieving sales in-progress records");
            }
        }

        /// <summary>
        /// Get sales hold records for the current user
        /// </summary>
        [HttpGet("sales-hold-records")]
        public async Task<ActionResult<IEnumerable<PlannedEvent>>> GetSalesHoldRecords()
        {
            try
            {
                var userIdentity = User.Identity?.Name;
                if (string.IsNullOrEmpty(userIdentity))
                {
                    return Unauthorized("User identity not found");
                }

                var userId = await _usersApiService.GetCurrentUserIdAsync(userIdentity);
                var records = await _plannedEventsService.GetSalesHoldRecordsAsync(userId);
                return Ok(records);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales hold records");
                return StatusCode(500, "An error occurred while retrieving sales hold records");
            }
        }

        /// <summary>
        /// Get sales urgent records for the current user
        /// </summary>
        [HttpGet("sales-urgent-records")]
        public async Task<ActionResult<IEnumerable<PlannedEvent>>> GetSalesUrgentRecords()
        {
            try
            {
                var userIdentity = User.Identity?.Name;
                if (string.IsNullOrEmpty(userIdentity))
                {
                    return Unauthorized("User identity not found");
                }

                var userId = await _usersApiService.GetCurrentUserIdAsync(userIdentity);
                var records = await _plannedEventsService.GetSalesUrgentRecordsAsync(userId);
                return Ok(records);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales urgent records");
                return StatusCode(500, "An error occurred while retrieving sales urgent records");
            }
        }

        /// <summary>
        /// Get sales OLA violate records for the current user
        /// </summary>
        [HttpGet("sales-ola-violate-records")]
        public async Task<ActionResult<IEnumerable<PlannedEvent>>> GetSalesOLAViolateRecords()
        {
            try
            {
                var userIdentity = User.Identity?.Name;
                if (string.IsNullOrEmpty(userIdentity))
                {
                    return Unauthorized("User identity not found");
                }

                var userId = await _usersApiService.GetCurrentUserIdAsync(userIdentity);
                var records = await _plannedEventsService.GetSalesOLAViolateRecordsAsync(userId);
                return Ok(records);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales OLA violate records");
                return StatusCode(500, "An error occurred while retrieving sales OLA violate records");
            }
        }

        /// <summary>
        /// Get sales in-progress count for the current user
        /// </summary>
        [HttpGet("sales-inprogress-count")]
        public async Task<ActionResult<int>> GetSalesInProgressCount()
        {
            try
            {
                var userIdentity = User.Identity?.Name;
                if (string.IsNullOrEmpty(userIdentity))
                {
                    return Unauthorized("User identity not found");
                }

                var userId = await _usersApiService.GetCurrentUserIdAsync(userIdentity);
                var count = await _plannedEventsService.GetSalesInProgressCountAsync(userId);
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales in-progress count");
                return StatusCode(500, "An error occurred while retrieving sales in-progress count");
            }
        }

        /// <summary>
        /// Get sales hold count for the current user
        /// </summary>
        [HttpGet("sales-hold-count")]
        public async Task<ActionResult<int>> GetSalesHoldCount()
        {
            try
            {
                var userIdentity = User.Identity?.Name;
                if (string.IsNullOrEmpty(userIdentity))
                {
                    return Unauthorized("User identity not found");
                }

                var userId = await _usersApiService.GetCurrentUserIdAsync(userIdentity);
                var count = await _plannedEventsService.GetSalesHoldCountAsync(userId);
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales hold count");
                return StatusCode(500, "An error occurred while retrieving sales hold count");
            }
        }

        /// <summary>
        /// Get sales urgent count for the current user
        /// </summary>
        [HttpGet("sales-urgent-count")]
        public async Task<ActionResult<int>> GetSalesUrgentCount()
        {
            try
            {
                var userIdentity = User.Identity?.Name;
                if (string.IsNullOrEmpty(userIdentity))
                {
                    return Unauthorized("User identity not found");
                }

                var userId = await _usersApiService.GetCurrentUserIdAsync(userIdentity);
                var count = await _plannedEventsService.GetSalesUrgentCountAsync(userId);
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales urgent count");
                return StatusCode(500, "An error occurred while retrieving sales urgent count");
            }
        }

        /// <summary>
        /// Get sales OLA violate count for the current user
        /// </summary>
        [HttpGet("sales-ola-violate-count")]
        public async Task<ActionResult<int>> GetSalesOLAViolateCount()
        {
            try
            {
                var userIdentity = User.Identity?.Name;
                if (string.IsNullOrEmpty(userIdentity))
                {
                    return Unauthorized("User identity not found");
                }

                var userId = await _usersApiService.GetCurrentUserIdAsync(userIdentity);
                var count = await _plannedEventsService.GetSalesOLAViolateCountAsync(userId);
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales OLA violate count");
                return StatusCode(500, "An error occurred while retrieving sales OLA violate count");
            }
        }

        /// <summary>
        /// Search planned events for a specific user with pagination
        /// </summary>
        [HttpPost("search-user-paginated")]
        public async Task<ActionResult<object>> SearchPlannedEventsForUser([FromBody] SearchPlannedEventsRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Search request is required");
                }

                // Validate pagination parameters
                if (request.PageIndex < 1)
                {
                    return BadRequest("Page index must be greater than 0");
                }

                if (request.PageSize < 1 || request.PageSize > 100)
                {
                    return BadRequest("Page size must be between 1 and 100");
                }

                // Use the service to search planned events for user
                var result = await _plannedEventsService.SearchPlannedEventsForUserAsync(
                    request.SearchType ?? string.Empty,
                    request.SearchString ?? string.Empty,
                    request.UserId,
                    request.PageIndex,
                    request.PageSize);

                // Return a simple object that can be easily serialized
                var response = new
                {
                    Items = result.ToList(),
                    PageIndex = result.PageIndex,
                    TotalPages = result.TotalPages,
                    HasPreviousPage = result.HasPreviousPage,
                    HasNextPage = result.HasNextPage,
                    TotalCount = result.Count
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching planned events for user {UserId}", request?.UserId);
                return StatusCode(500, "An error occurred while searching planned events");
            }
        }
    }

    /// <summary>
    /// Request model for searching planned events
    /// </summary>
    public class SearchPlannedEventsRequest
    {
        public int UserId { get; set; }
        public string? SearchType { get; set; }
        public string? SearchString { get; set; }
        public string? WorkgroupName { get; set; }
        public bool HasDrawFiberAccess { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    /// <summary>
    /// Request model for multi-workgroup operations
    /// </summary>
    public class MultiWorkgroupRequest
    {
        public List<int> SelectedWorkgroupIds { get; set; } = new();
        public List<int> UserWorkgroupIds { get; set; } = new();
        public bool HasDrawFiberAccess { get; set; }
    }
}

