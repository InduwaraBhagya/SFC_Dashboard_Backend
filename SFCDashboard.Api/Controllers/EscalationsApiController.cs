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
        private readonly ILogger<EscalationsApiController> _logger;
        private readonly IWebHostEnvironment _environment;

        public EscalationsApiController(
            IEscalationsApiService escalationsService,
            ILogger<EscalationsApiController> logger,
            IWebHostEnvironment environment)
        {
            _escalationsService = escalationsService;
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
                var escalations = await _escalationsService.GetEscalationsByUserRoleAsync(request);
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
    }
}

