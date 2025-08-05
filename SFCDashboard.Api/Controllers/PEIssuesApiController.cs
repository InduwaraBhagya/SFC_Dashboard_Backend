using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Api.Data;
using SFCDashboard.Api.Models;
using SFCDashboard.Api.Services;

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
        /// Get inbox issue view models for a user (includes sender/receiver names)
        /// </summary>
        [HttpGet("inbox-viewmodels/{userId}")]
        public async Task<ActionResult<IEnumerable<PEIssueViewModel>>> GetInboxIssueViewModels(int userId, [FromQuery] int limit = 10)
        {
            try
            {
                _logger.LogInformation("Getting inbox issue view models for user {userId} with limit: {limit}", userId, limit);
                var issueViewModels = await _peIssuesService.GetInboxIssuesAsync(userId, limit);
                return Ok(issueViewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inbox issue view models for user {userId}", userId);
                return StatusCode(500, "An error occurred while retrieving inbox issue view models");
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
        /// Mark all reminders as read for a user
        /// </summary>
        [HttpPost("reminders/{userId}/markallread")]
        public async Task<ActionResult<object>> MarkAllRemindersAsRead(int userId)
        {
            try
            {
                _logger.LogInformation("Marking all reminders as read for user {userId}", userId);
                
                var reminders = await _context.PEIssues
                    .Where(i => i.ReceiverId == userId && !i.IsResolved && i.IsReminder && !i.IsRead)
                    .ToListAsync();

                foreach (var reminder in reminders)
                {
                    reminder.IsRead = true;
                }

                var updatedCount = await _context.SaveChangesAsync();
                
                _logger.LogInformation("Marked {count} reminders as read for user {userId}", updatedCount, userId);
                
                return Ok(new { success = true, count = updatedCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all reminders as read for user {userId}", userId);
                return StatusCode(500, "An error occurred while marking reminders as read");
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
        /// Get issue view models by multiple planned event IDs (includes sender/receiver names)
        /// </summary>
        [HttpPost("viewmodels-by-plannedevent-ids")]
        public async Task<ActionResult<Dictionary<int, IEnumerable<PEIssueViewModel>>>> GetPEIssueViewModelsByPlannedEventIds([FromBody] List<int> peIds)
        {
            try
            {
                _logger.LogInformation("Getting issue view models for {Count} planned events", peIds.Count);

                if (peIds == null || !peIds.Any())
                {
                    return Ok(new Dictionary<int, IEnumerable<PEIssueViewModel>>());
                }

                var result = new Dictionary<int, IEnumerable<PEIssueViewModel>>();

                foreach (var peId in peIds)
                {
                    var issueViewModels = await _peIssuesService.GetPEIssuesByPlannedEventAsync(peId);
                    result[peId] = issueViewModels;
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting issue view models for planned events");
                return StatusCode(500, "An error occurred while retrieving issue view models for planned events");
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
                _logger.LogWarning("ID mismatch in UpdatePEIssue: URL id={UrlId}, Issue id={IssueId}", id, issue.Id);
                return BadRequest("ID mismatch");
            }

            try
            {
                _logger.LogInformation("UpdatePEIssue called - Received issue data: Id={Id}, IsResolved={IsResolved}, IsRead={IsRead}, IssueText='{IssueText}'", 
                    issue.Id, issue.IsResolved, issue.IsRead, issue.IssueText);

                var existingIssue = await _context.PEIssues.FindAsync(id);
                if (existingIssue == null)
                {
                    _logger.LogWarning("PE issue {Id} not found for update", id);
                    return NotFound();
                }

                _logger.LogInformation("Before update - Existing issue: Id={Id}, IsResolved={IsResolved}, IsRead={IsRead}", 
                    existingIssue.Id, existingIssue.IsResolved, existingIssue.IsRead);

                // Update the properties explicitly
                existingIssue.IssueText = issue.IssueText;
                existingIssue.IsRead = issue.IsRead;
                existingIssue.IsResolved = issue.IsResolved;
                existingIssue.IsReply = issue.IsReply;
                existingIssue.IsReminder = issue.IsReminder;
                existingIssue.IsResolutionRequest = issue.IsResolutionRequest;
                existingIssue.IsHiddenFromInbox = issue.IsHiddenFromInbox;
                existingIssue.OriginalIssueId = issue.OriginalIssueId;
                existingIssue.AttachmentPath = issue.AttachmentPath;
                // Note: Don't update CreatedAt, SenderId, ReceiverId, PlannedEventId, PETaskId as these should be immutable

                _logger.LogInformation("After property assignment - Issue: Id={Id}, IsResolved={IsResolved}, IsRead={IsRead}", 
                    existingIssue.Id, existingIssue.IsResolved, existingIssue.IsRead);

                // Mark the entity as modified to ensure EF tracks the changes
                _context.Entry(existingIssue).State = EntityState.Modified;
                
                var changesSaved = await _context.SaveChangesAsync();
                
                _logger.LogInformation("SaveChanges completed - Changes saved: {ChangesSaved}, Final issue state: Id={Id}, IsResolved={IsResolved}", 
                    changesSaved, existingIssue.Id, existingIssue.IsResolved);
                
                // Return the updated issue object (API client expects this)
                return Ok(existingIssue);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error updating PE issue {Id}", id);
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

        /// <summary>
        /// Test endpoint to directly update IsResolved status
        /// </summary>
        [HttpPost("{id}/mark-resolved")]
        public async Task<IActionResult> MarkIssueAsResolved(int id, [FromBody] bool isResolved = true)
        {
            try
            {
                _logger.LogInformation("MarkIssueAsResolved called for issue {Id} with isResolved={IsResolved}", id, isResolved);

                var existingIssue = await _context.PEIssues.FindAsync(id);
                if (existingIssue == null)
                {
                    _logger.LogWarning("PE issue {Id} not found", id);
                    return NotFound();
                }

                _logger.LogInformation("Before update: Issue {Id} IsResolved={IsResolved}", existingIssue.Id, existingIssue.IsResolved);

                existingIssue.IsResolved = isResolved;
                
                var changesSaved = await _context.SaveChangesAsync();
                
                _logger.LogInformation("After update: Issue {Id} IsResolved={IsResolved}, Changes saved: {ChangesSaved}", 
                    existingIssue.Id, existingIssue.IsResolved, changesSaved);

                return Ok(new { 
                    id = existingIssue.Id, 
                    isResolved = existingIssue.IsResolved, 
                    changesSaved = changesSaved 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking issue {Id} as resolved", id);
                return StatusCode(500, "An error occurred while updating the issue");
            }
        }

        /// <summary>
        /// Test endpoint to check issue resolution status
        /// </summary>
        [HttpGet("test-status/{id}")]
        public async Task<ActionResult> TestIssueStatus(int id)
        {
            try
            {
                var issue = await _context.PEIssues.FindAsync(id);
                if (issue == null)
                {
                    return NotFound($"Issue {id} not found");
                }

                var resolution = await _context.PEIssueResolutions.FirstOrDefaultAsync(r => r.IssueId == id);

                return Ok(new { 
                    IssueId = issue.Id,
                    IssueIsResolved = issue.IsResolved,
                    IssueText = issue.IssueText,
                    HasResolution = resolution != null,
                    ResolutionId = resolution?.Id,
                    ResolutionIsConfirmed = resolution?.IsConfirmed,
                    ResolutionDetails = resolution?.ResolutionDetails,
                    ResolutionConfirmedDate = resolution?.ConfirmedDate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing issue status {Id}", id);
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Mark a specific issue as read
        /// </summary>
        [HttpPost("{id}/markread")]
        public async Task<IActionResult> MarkIssueAsRead(int id)
        {
            try
            {
                _logger.LogInformation("MarkIssueAsRead called for issue {issueId}", id);

                var issue = await _context.PEIssues.FindAsync(id);
                if (issue == null)
                {
                    _logger.LogWarning("Issue {issueId} not found", id);
                    return NotFound($"Issue with ID {id} not found");
                }

                if (!issue.IsRead)
                {
                    issue.IsRead = true;
                    _context.Entry(issue).State = EntityState.Modified;
                    
                    var changesSaved = await _context.SaveChangesAsync();
                    _logger.LogInformation("Issue {issueId} marked as read, changes saved: {changesSaved}", id, changesSaved);
                }
                else
                {
                    _logger.LogInformation("Issue {issueId} was already marked as read", id);
                }

                return Ok(new { success = true, message = "Issue marked as read" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking issue {issueId} as read", id);
                return StatusCode(500, "An error occurred while marking the issue as read");
            }
        }

        private bool PEIssueExists(int id)
        {
            return _context.PEIssues.Any(e => e.Id == id);
        }
    }
}

