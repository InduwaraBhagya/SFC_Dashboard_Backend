using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private readonly ILogger<PEIssuesApiController> _logger;
        private readonly IPEIssuesApiService _peIssuesService;

        public PEIssuesApiController(
            ILogger<PEIssuesApiController> logger,
            IPEIssuesApiService peIssuesService)
        {
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
                var issues = await _peIssuesService.GetAllPEIssuesAsync();
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
                var issue = await _peIssuesService.GetPEIssueAsync(id);
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
                var issues = await _peIssuesService.GetInboxIssuesRawAsync(userId, limit);
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
                var reminders = await _peIssuesService.GetRemindersRawAsync(userId, showAll);
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
                var count = await _peIssuesService.GetReminderCountAsync(userId);
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
                
                var updatedCount = await _peIssuesService.MarkAllRemindersAsReadAsync(userId);
                
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

                var groupedIssues = await _peIssuesService.GetIssuesByPlannedEventIdsRawAsync(peIds);
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

                var issues = await _peIssuesService.GetPEIssuesByPlannedEventRawAsync(plannedEventId);

                _logger.LogInformation("Found {count} PE issues for planned event {plannedEventId}", issues.Count(), plannedEventId);
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
                var createdIssue = await _peIssuesService.CreatePEIssueAsync(issue);
                
                if (createdIssue == null)
                {
                    return StatusCode(500, "Failed to create PE issue");
                }

                return CreatedAtAction(nameof(GetPEIssue), new { id = createdIssue.Id }, createdIssue);
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

                var updatedIssue = await _peIssuesService.UpdatePEIssueAsync(id, issue);
                if (updatedIssue == null)
                {
                    _logger.LogWarning("PE issue {Id} not found for update", id);
                    return NotFound();
                }

                _logger.LogInformation("Update completed - Issue: Id={Id}, IsResolved={IsResolved}", 
                    updatedIssue.Id, updatedIssue.IsResolved);
                
                // Return the updated issue object (API client expects this)
                return Ok(updatedIssue);
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
                var deleted = await _peIssuesService.DeletePEIssueAsync(id);
                if (!deleted)
                {
                    return NotFound();
                }

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

                var updatedIssue = await _peIssuesService.MarkIssueAsResolvedAsync(id, isResolved);
                if (updatedIssue == null)
                {
                    _logger.LogWarning("PE issue {Id} not found", id);
                    return NotFound();
                }

                _logger.LogInformation("After update: Issue {Id} IsResolved={IsResolved}", 
                    updatedIssue.Id, updatedIssue.IsResolved);

                return Ok(new { 
                    id = updatedIssue.Id, 
                    isResolved = updatedIssue.IsResolved
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
                var status = await _peIssuesService.GetIssueStatusAsync(id);
                
                if (status is object statusObj && statusObj.GetType().GetProperty("error") != null)
                {
                    return NotFound($"Issue {id} not found");
                }
                
                return Ok(status);
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

                var success = await _peIssuesService.MarkIssueAsReadAsync(id);
                if (!success)
                {
                    _logger.LogWarning("Issue {issueId} not found", id);
                    return NotFound($"Issue with ID {id} not found");
                }

                _logger.LogInformation("Issue {issueId} marked as read", id);
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
            var issue = _peIssuesService.GetPEIssueAsync(id).Result;
            return issue != null;
        }
    }
}

