using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Api.Models;
using SFCDashboard.Api.Services;

namespace SFCDashboard.Api.Controllers
{
    /// <summary>
    /// API Controller for Escalations management
    /// </summary>
    [ApiController]
    [Route("api/escalations")]
    [Produces("application/json")]
    public class EscalationsApiController : ControllerBase
    {
        private readonly IEscalationsApiService _escalationsService;
        private readonly EscalationService _escalationService;
        private readonly ILogger<EscalationsApiController> _logger;
        private readonly IWebHostEnvironment _environment;

        public EscalationsApiController(
            IEscalationsApiService escalationsService,
            EscalationService escalationService,
            ILogger<EscalationsApiController> logger,
            IWebHostEnvironment environment)
        {
            _escalationsService = escalationsService;
            _escalationService = escalationService;
            _logger = logger;
            _environment = environment;
        }

        /// <summary>
        /// Get all escalations
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Escalation>>> GetEscalations()
        {
            // Check authorization in production only
            if (_environment.IsProduction() && !User.Identity?.IsAuthenticated == true)
            {
                return Unauthorized();
            }

            try
            {
                var escalations = await _escalationsService.GetEscalationsAsync();
                return Ok(escalations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting escalations");
                return StatusCode(500, "An error occurred while retrieving escalations");
            }
        }

        /// <summary>
        /// Get escalation by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Escalation>> GetEscalation(int id)
        {
            // Check authorization in production only
            if (_environment.IsProduction() && !User.Identity?.IsAuthenticated == true)
            {
                return Unauthorized();
            }

            try
            {
                var escalation = await _escalationsService.GetEscalationAsync(id);

                if (escalation == null)
                {
                    return NotFound();
                }

                return Ok(escalation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting escalation {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the escalation");
            }
        }

        /// <summary>
        /// Get escalations by task IDs
        /// </summary>
        [HttpPost("by-task-ids")]
        public async Task<ActionResult<IEnumerable<Escalation>>> GetEscalationsByTaskIds([FromBody] List<int> taskIds)
        {
            // Check authorization in production only
            if (_environment.IsProduction() && !User.Identity?.IsAuthenticated == true)
            {
                return Unauthorized();
            }

            try
            {
                _logger.LogInformation("Getting escalations for {Count} task IDs", taskIds.Count);
                var escalations = await _escalationsService.GetEscalationsByTaskIdsAsync(taskIds);
                return Ok(escalations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting escalations by task IDs");
                return StatusCode(500, "An error occurred while retrieving escalations");
            }
        }

        /// <summary>
        /// Get escalations by user role
        /// </summary>
        [HttpPost("by-user-role")]
        public async Task<ActionResult<List<Escalation>>> GetEscalationsByUserRole([FromBody] EscalationsByUserRoleRequest request)
        {
            // Check authorization in production only
            if (_environment.IsProduction() && !User.Identity?.IsAuthenticated == true)
            {
                return Unauthorized();
            }

            try
            {
                _logger.LogInformation("Getting escalations for user role level {Level}", request.UserRoleLevel);
                var escalations = await _escalationService.GetEscalationsByUserRoleAsync(request.UserRoleLevel, request.UserWorkgroupNames);
                return Ok(escalations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting escalations by user role");
                return StatusCode(500, "An error occurred while retrieving escalations");
            }
        }

        /// <summary>
        /// Mark escalation as read
        /// </summary>
        [HttpPost("{id}/mark-read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            // Check authorization in production only
            if (_environment.IsProduction() && !User.Identity?.IsAuthenticated == true)
            {
                return Unauthorized();
            }

            try
            {
                var success = await _escalationsService.MarkAsReadAsync(id);
                if (!success)
                {
                    return NotFound();
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking escalation {Id} as read", id);
                return StatusCode(500, "An error occurred while updating the escalation");
            }
        }

        /// <summary>
        /// Create a new escalation
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Escalation>> CreateEscalation([FromBody] Escalation escalation)
        {
            // Check authorization in production only
            if (_environment.IsProduction() && !User.Identity?.IsAuthenticated == true)
            {
                return Unauthorized();
            }

            try
            {
                var createdEscalation = await _escalationsService.CreateEscalationAsync(escalation);
                if (createdEscalation == null)
                {
                    return StatusCode(500, "An error occurred while creating the escalation");
                }

                return CreatedAtAction(nameof(GetEscalation), new { id = createdEscalation.Id }, createdEscalation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating escalation");
                return StatusCode(500, "An error occurred while creating the escalation");
            }
        }

        /// <summary>
        /// Get escalation service status
        /// </summary>
        [HttpGet("service-status")]
        public async Task<IActionResult> GetServiceStatus()
        {
            try
            {
                var isEnabled = await _escalationService.IsEscalationEnabledAsync();
                return Ok(new { enabled = isEnabled });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting escalation service status");
                return StatusCode(500, "An error occurred while retrieving service status");
            }
        }

        /// <summary>
        /// Toggle escalation service
        /// </summary>
        [HttpPost("toggle-service")]
        public async Task<IActionResult> ToggleService([FromBody] ToggleServiceRequest request)
        {
            // Check authorization in production only
            if (_environment.IsProduction() && !User.Identity?.IsAuthenticated == true)
            {
                return Unauthorized();
            }

            try
            {
                await _escalationService.SetEscalationEnabledAsync(request.Enabled);
                var statusMessage = request.Enabled ? "enabled" : "disabled";
                return Ok(new { 
                    success = true, 
                    enabled = request.Enabled, 
                    message = $"Escalation service has been {statusMessage}" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling escalation service");
                return StatusCode(500, "An error occurred while toggling service");
            }
        }

        /// <summary>
        /// Manual escalation check
        /// </summary>
        [HttpPost("manual-check")]
        public async Task<IActionResult> ManualCheck()
        {
            // Check authorization in production only
            if (_environment.IsProduction() && !User.Identity?.IsAuthenticated == true)
            {
                return Unauthorized();
            }

            try
            {
                var result = await _escalationService.ManualEscalationCheckAsync();
                return Ok(new { message = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running manual escalation check");
                return StatusCode(500, "An error occurred during manual escalation check");
            }
        }

        /// <summary>
        /// Get OLA violated tasks debug info
        /// </summary>
        [HttpGet("ola-violated-tasks-debug")]
        public async Task<IActionResult> GetOLAViolatedTasksDebugInfo()
        {
            // Check authorization in production only
            if (_environment.IsProduction() && !User.Identity?.IsAuthenticated == true)
            {
                return Unauthorized();
            }

            try
            {
                var debugInfo = await _escalationService.GetOLAViolatedTasksDebugInfoAsync();
                return Ok(debugInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OLA violated tasks debug info");
                return StatusCode(500, "An error occurred while retrieving debug info");
            }
        }
    }

    /// <summary>
    /// Request model for toggling service
    /// </summary>
    public class ToggleServiceRequest
    {
        public bool Enabled { get; set; }
    }
}

