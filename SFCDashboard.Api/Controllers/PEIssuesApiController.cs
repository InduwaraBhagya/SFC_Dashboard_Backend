using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDashboard.Models;
using SFCDashboard.Services;

namespace SFCDashboard.Api.Controllers
{
    /// <summary>
    /// API Controller for PE Issues management
    /// </summary>
    [ApiController]
    [Route("api/peissues")]
    [Produces("application/json")]
    public class PEIssuesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PEIssuesApiController> _logger;
        private readonly IPEIssuesApiService _peIssuesService;

        public PEIssuesApiController(
            ApplicationDbContext context,
            ILogger<PEIssuesApiController> logger,
            IPEIssuesApiService peIssuesService)
        {
            _context = context;
            _logger = logger;
            _peIssuesService = peIssuesService;
        }

        /// <summary>
        /// Get all PE issues
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PEIssue>>> GetPEIssues()
        {
            try
            {
                var issues = await _context.PEIssues
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync();
                return Ok(issues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PE issues");
                return StatusCode(500, "An error occurred while retrieving PE issues");
            }
        }

        /// <summary>
        /// Get PE issue by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<PEIssue>> GetPEIssue(int id)
        {
            try
            {
                var issue = await _context.PEIssues.FindAsync(id);
                if (issue == null)
                {
                    return NotFound($"PE Issue with ID {id} not found");
                }
                return Ok(issue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PE issue {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the PE issue");
            }
        }

        /// <summary>
        /// Get inbox issues for a user
        /// </summary>
        [HttpGet("inbox/{userId}")]
        public async Task<ActionResult<IEnumerable<PEIssue>>> GetInboxIssues(int userId, [FromQuery] int limit = 10)
        {
            try
            {
                _logger.LogInformation("Getting inbox issues for user {userId} with limit: {limit}", userId, limit);
                var issues = await _context.PEIssues
                    .Where(i => i.ReceiverId == userId)
                    .OrderByDescending(i => i.CreatedAt)
                    .Take(limit)
                    .ToListAsync();

                return Ok(issues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inbox issues for user {userId}", userId);
                return StatusCode(500, "An error occurred while retrieving inbox issues");
            }
        }

        /// <summary>
        /// Get reminders for user
        /// </summary>
        [HttpGet("reminders/{userId}")]
        public async Task<ActionResult<IEnumerable<PEIssue>>> GetReminders(int userId, [FromQuery] bool showAll = true)
        {
            try
            {
                _logger.LogInformation("Getting reminders for user {userId}, showAll: {showAll}", userId, showAll);
                var query = _context.PEIssues
                    .Where(i => i.ReceiverId == userId);

                if (!showAll)
                {
                    // Only show unresolved reminders and actual reminders
                    query = query.Where(i => !i.IsResolved && i.IsReminder);
                }
                else
                {
                    // Show all reminders (including resolved ones)
                    query = query.Where(i => i.IsReminder);
                }

                var reminders = await query
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync();

                return Ok(reminders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reminders for user {userId}", userId);
                return StatusCode(500, "An error occurred while retrieving reminders");
            }
        }

        /// <summary>
        /// Get reminder count for user
        /// </summary>
        [HttpGet("reminders/{userId}/count")]
        public async Task<ActionResult<int>> GetReminderCount(int userId)
        {
            try
            {
                _logger.LogInformation("Getting reminder count for user {userId}", userId);
                var count = await _context.PEIssues
                    .Where(i => i.ReceiverId == userId && !i.IsResolved && i.IsReminder)
                    .CountAsync();
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reminder count for user {userId}", userId);
                return StatusCode(500, "An error occurred while retrieving reminder count");
            }
        }

        /// <summary>
        /// Get issues by multiple planned event IDs
        /// </summary>
        [HttpPost("by-plannedevent-ids")]
        public async Task<ActionResult<Dictionary<int, IEnumerable<PEIssue>>>> GetIssuesByPlannedEventIds([FromBody] List<int> peIds)
        {
            try
            {
                _logger.LogInformation("Getting issues for {Count} planned events", peIds.Count);
                
                if (peIds == null || !peIds.Any())
                {
                    return Ok(new Dictionary<int, IEnumerable<PEIssue>>());
                }

                var issues = await _context.PEIssues
                    .Where(i => peIds.Contains(i.PlannedEventId))
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync();

                // Group issues by PlannedEventId
                var groupedIssues = issues
                    .GroupBy(i => i.PlannedEventId)
                    .ToDictionary(g => g.Key, g => g.AsEnumerable());

                // Ensure all requested PE IDs are in the result, even if they have no issues
                foreach (var peId in peIds)
                {
                    if (!groupedIssues.ContainsKey(peId))
                    {
                        groupedIssues[peId] = Enumerable.Empty<PEIssue>();
                    }
                }

                return Ok(groupedIssues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting issues for planned events");
                return StatusCode(500, "An error occurred while retrieving issues for planned events");
            }
        }

        /// <summary>
        /// Get PE issues by planned event ID
        /// </summary>
        [HttpGet("plannedevent/{plannedEventId}")]
        public async Task<ActionResult<IEnumerable<PEIssue>>> GetPEIssuesByPlannedEvent(int plannedEventId)
        {
            try
            {
                _logger.LogInformation("Getting PE issues for planned event {plannedEventId}", plannedEventId);

                var issues = await _context.PEIssues
                    .Where(i => i.PlannedEventId == plannedEventId)
                    .OrderBy(i => i.CreatedAt)
                    .ToListAsync();

                _logger.LogInformation("Found {count} PE issues for planned event {plannedEventId}", issues.Count, plannedEventId);
                return Ok(issues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PE issues for planned event {plannedEventId}", plannedEventId);
                return StatusCode(500, "An error occurred while retrieving PE issues for the planned event");
            }
        }

        /// <summary>
        /// Get PE issue view models by planned event ID (includes sender/receiver names)
        /// </summary>
        [HttpGet("plannedevent/{plannedEventId}/viewmodels")]
        public async Task<ActionResult<IEnumerable<PEIssueViewModel>>> GetPEIssueViewModelsByPlannedEvent(int plannedEventId)
        {
            try
            {
                _logger.LogInformation("Getting PE issue view models for planned event {plannedEventId}", plannedEventId);

                var issueViewModels = await _peIssuesService.GetPEIssuesByPlannedEventAsync(plannedEventId);

                _logger.LogInformation("Found {count} PE issue view models for planned event {plannedEventId}", 
                    issueViewModels.Count(), plannedEventId);
                return Ok(issueViewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PE issue view models for planned event {plannedEventId}", plannedEventId);
                return StatusCode(500, "An error occurred while retrieving PE issue view models for the planned event");
            }
        }

        /// <summary>
        /// Create a new PE issue
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<PEIssue>> CreatePEIssue(PEIssue issue)
        {
            try
            {
                issue.CreatedAt = DateTime.Now;
                _context.PEIssues.Add(issue);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetPEIssue), new { id = issue.Id }, issue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PE issue");
                return StatusCode(500, "An error occurred while creating the PE issue");
            }
        }

        /// <summary>
        /// Update an existing PE issue
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePEIssue(int id, PEIssue issue)
        {
            if (id != issue.Id)
            {
                return BadRequest();
            }

            try
            {
                _context.Entry(issue).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PEIssueExists(id))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating PE issue {Id}", id);
                return StatusCode(500, "An error occurred while updating the PE issue");
            }
        }

        /// <summary>
        /// Delete a PE issue
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePEIssue(int id)
        {
            try
            {
                var issue = await _context.PEIssues.FindAsync(id);
                if (issue == null)
                {
                    return NotFound();
                }

                _context.PEIssues.Remove(issue);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting PE issue {Id}", id);
                return StatusCode(500, "An error occurred while deleting the PE issue");
            }
        }

        private bool PEIssueExists(int id)
        {
            return _context.PEIssues.Any(e => e.Id == id);
        }
    }
}
