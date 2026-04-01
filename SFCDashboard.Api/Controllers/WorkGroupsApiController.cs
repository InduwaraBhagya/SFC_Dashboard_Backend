using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Api.Models;
using SFCDashboard.Api.Services;
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

        /// <summary>
        /// Create a new workgroup
        /// </summary>
        /// <param name="workGroup">The workgroup to create</param>
        /// <returns>The created workgroup</returns>
        [HttpPost]
        public async Task<ActionResult<WorkGroup>> CreateWorkGroup([FromBody] WorkGroup workGroup)
        {
            try
            {
                _logger.LogInformation("Creating new workgroup: {name}", workGroup.Name);
                
                if (string.IsNullOrWhiteSpace(workGroup.Name))
                {
                    return BadRequest(new { error = "WorkGroup name is required" });
                }

                var createdWorkGroup = await _workGroupsService.CreateWorkGroupAsync(workGroup);
                
                if (createdWorkGroup == null)
                {
                    return StatusCode(500, new { error = "Failed to create workgroup" });
                }
                
                return CreatedAtAction(nameof(GetWorkGroup), new { id = createdWorkGroup.Id }, createdWorkGroup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating workgroup");
                return StatusCode(500, new { error = "An error occurred while creating the workgroup" });
            }
        }

        /// <summary>
        /// Update an existing workgroup
        /// </summary>
        /// <param name="id">The workgroup ID</param>
        /// <param name="workGroup">The updated workgroup data</param>
        /// <returns>The updated workgroup</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<WorkGroup>> UpdateWorkGroup(int id, [FromBody] WorkGroup workGroup)
        {
            try
            {
                _logger.LogInformation("Updating workgroup {id} with data: Name='{name}'", id, workGroup?.Name);
                
                if (id != workGroup.Id)
                {
                    _logger.LogWarning("ID mismatch: URL id={urlId}, Body id={bodyId}", id, workGroup.Id);
                    return BadRequest(new { error = "WorkGroup ID mismatch" });
                }

                if (string.IsNullOrWhiteSpace(workGroup.Name))
                {
                    return BadRequest(new { error = "WorkGroup name is required" });
                }

                var existingWorkGroup = await _workGroupsService.GetWorkGroupAsync(id);
                if (existingWorkGroup == null)
                {
                    return NotFound(new { error = "WorkGroup not found" });
                }

                var updatedWorkGroup = await _workGroupsService.UpdateWorkGroupAsync(workGroup);
                return Ok(updatedWorkGroup);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while updating workgroup {id}", id);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating workgroup {id}: {message}", id, ex.Message);
                return StatusCode(500, new { error = $"An error occurred while updating the workgroup: {ex.Message}" });
            }
        }

        /// <summary>
        /// Delete a workgroup
        /// </summary>
        /// <param name="id">The workgroup ID to delete</param>
        /// <returns>Success status</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWorkGroup(int id)
        {
            try
            {
                _logger.LogInformation("Deleting workgroup {id}", id);
                
                var existingWorkGroup = await _workGroupsService.GetWorkGroupAsync(id);
                if (existingWorkGroup == null)
                {
                    return NotFound(new { error = "WorkGroup not found" });
                }

                await _workGroupsService.DeleteWorkGroupAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting workgroup {id}", id);
                return StatusCode(500, new { error = "An error occurred while deleting the workgroup" });
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

