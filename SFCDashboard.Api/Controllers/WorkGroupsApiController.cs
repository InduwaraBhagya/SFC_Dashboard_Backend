using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Models;
using SFCDashboard.Services;
using System.Text.Json;

namespace SFCDashboard.Api.Controllers
{
    /// <summary>
    /// API Controller for WorkGroups management
    /// </summary>
    [ApiController]
    [Route("api/workgroups")]
    [Produces("application/json")]
    public class WorkGroupsApiController : ControllerBase

    {
        private readonly IWorkGroupsApiService _workGroupsService;
        private readonly ILogger<WorkGroupsApiController> _logger;

        public WorkGroupsApiController(
            IWorkGroupsApiService workGroupsService,
            ILogger<WorkGroupsApiController> logger)
        {
            _workGroupsService = workGroupsService;
            _logger = logger;
        }

        /// <summary>
        /// Get all workgroups
        /// </summary>
        /// <returns>List of workgroups</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<WorkGroup>>> GetWorkGroups()
        {
            try
            {
                _logger.LogInformation("Getting all workgroups");
                var workGroups = await _workGroupsService.GetWorkGroupsAsync();
                return Ok(workGroups);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all workgroups");
                return StatusCode(500, "An error occurred while retrieving workgroups");
            }
        }

        /// <summary>
        /// Get workgroup by ID
        /// </summary>
        /// <param name="id">Workgroup ID</param>
        /// <returns>Workgroup</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<WorkGroup>> GetWorkGroup(int id)
        {
            try
            {
                _logger.LogInformation("Getting workgroup with id: {id}", id);
                var workGroup = await _workGroupsService.GetWorkGroupAsync(id);

                if (workGroup == null)
                {
                    return NotFound($"WorkGroup with ID {id} not found");
                }

                return Ok(workGroup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workgroup with id: {id}", id);
                return StatusCode(500, "An error occurred while retrieving the workgroup");
            }
        }

        /// <summary>
        /// Get workgroups for user based on their workgroup IDs and permissions
        /// </summary>
        /// <param name="request">Request containing user workgroup IDs and canViewAll flag</param>
        /// <returns>List of workgroups the user can access</returns>
        [HttpPost("for-user")]
        public async Task<ActionResult<IEnumerable<WorkGroup>>> GetWorkGroupsForUser([FromBody] GetWorkGroupsForUserRequest request)
        {
            try
            {
                _logger.LogInformation("Getting workgroups for user with IDs: {ids}, canViewAll: {canViewAll}",
                    string.Join(", ", request.UserWorkgroupIds), request.CanViewAll);

                var workGroups = await _workGroupsService.GetWorkGroupsForUserAsync(request.UserWorkgroupIds, request.CanViewAll);
                return Ok(workGroups);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workgroups for user");
                return StatusCode(500, "An error occurred while retrieving workgroups for user");
            }
        }

        /// <summary>
        /// Get workgroups by specific IDs
        /// </summary>
        /// <param name="request">Request containing workgroup IDs</param>
        /// <returns>List of workgroups</returns>
        [HttpPost("by-ids")]
        public async Task<ActionResult<IEnumerable<WorkGroup>>> GetWorkGroupsByIds([FromBody] GetWorkGroupsByIdsRequest request)
        {
            try
            {
                _logger.LogInformation("Getting workgroups by ids: {ids}", string.Join(", ", request.WorkgroupIds));
                var workGroups = await _workGroupsService.GetWorkGroupsByIdsAsync(request.WorkgroupIds);
                return Ok(workGroups);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workgroups by ids");
                return StatusCode(500, "An error occurred while retrieving workgroups");
            }
        }
        
        /// <summary>
        /// Get the name of a workgroup by ID
        /// </summary>
        [HttpGet("{id}/name")]
        public async Task<IActionResult> GetWorkGroupName(int id)
        {
            try
            {
                var workGroup = await _workGroupsService.GetWorkGroupAsync(id);
                if (workGroup == null)
                {
                    return NotFound();
                }
                return Ok(new { name = workGroup.Name });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workgroup name for id: {id}", id);
                return StatusCode(500, "An error occurred while retrieving the workgroup name");
            }
        }
    }

    /// <summary>
    /// Request model for getting workgroups for a user
    /// </summary>
    public class GetWorkGroupsForUserRequest
    {
        public List<int> UserWorkgroupIds { get; set; } = new();
        public bool CanViewAll { get; set; }
    }

    /// <summary>
    /// Request model for getting workgroups by IDs
    /// </summary>
    public class GetWorkGroupsByIdsRequest
    {
        public List<int> WorkgroupIds { get; set; } = new();
    }
}
