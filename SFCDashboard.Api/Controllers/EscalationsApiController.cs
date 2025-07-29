using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Api.Data;
using SFCDashboard.Api.Models;

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
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EscalationsApiController> _logger;
        private readonly IWebHostEnvironment _environment;

        public EscalationsApiController(
            ApplicationDbContext context,
            ILogger<EscalationsApiController> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
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
            if (_environment.IsProduction() && !User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            try
            {
                var escalations = await _context.Escalations
                    .Include(e => e.PETask)
                    .OrderByDescending(e => e.CreatedAt)
                    .ToListAsync();
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
            if (_environment.IsProduction() && !User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            try
            {
                var escalation = await _context.Escalations
                    .Include(e => e.PETask)
                    .FirstOrDefaultAsync(e => e.Id == id);

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
            if (_environment.IsProduction() && !User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            try
            {
                _logger.LogInformation("Getting escalations for {Count} task IDs", taskIds.Count);
                var escalations = await _context.Escalations
                    .Include(e => e.PETask)
                    .Where(e => taskIds.Contains(e.TaskId))
                    .OrderByDescending(e => e.CreatedAt)
                    .ToListAsync();

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
            if (_environment.IsProduction() && !User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            try
            {
                _logger.LogInformation("Getting escalations for user role level {Level}", request.UserRoleLevel);
                
                var query = _context.Escalations
                    .Include(e => e.PETask)
                    .AsQueryable();

                // Filter by role level - could implement specific business logic here
                if (request.UserRoleLevel > 0)
                {
                    query = query.Where(e => e.Level <= request.UserRoleLevel);
                }

                // Filter by workgroups if provided
                if (request.UserWorkgroupNames != null && request.UserWorkgroupNames.Any())
                {
                    // This would require additional navigation properties to filter by workgroups
                    // For now, we'll include all escalations
                }

                var escalations = await query
                    .OrderByDescending(e => e.CreatedAt)
                    .ToListAsync();

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
            if (_environment.IsProduction() && !User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            try
            {
                var escalation = await _context.Escalations.FindAsync(id);
                if (escalation == null)
                {
                    return NotFound();
                }

                escalation.IsRead = true;
                await _context.SaveChangesAsync();

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
            if (_environment.IsProduction() && !User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            try
            {
                escalation.CreatedAt = DateTime.UtcNow;
                _context.Escalations.Add(escalation);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetEscalation), new { id = escalation.Id }, escalation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating escalation");
                return StatusCode(500, "An error occurred while creating the escalation");
            }
        }
    }

    /// <summary>
    /// Request model for getting escalations by user role
    /// </summary>
    public class EscalationsByUserRoleRequest
    {
        public int UserRoleLevel { get; set; }
        public List<string>? UserWorkgroupNames { get; set; }
    }
}

