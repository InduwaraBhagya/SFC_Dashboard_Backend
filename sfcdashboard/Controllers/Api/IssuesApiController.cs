using Microsoft.AspNetCore.Mvc;
using SFCDashboard.ApiClients;
using SFCDashboard.Models;

namespace SFCDashboard.Controllers.Api
{
    [Route("api/issues")]
    public class IssuesApiController : ApiBaseController
    {
        private readonly IPEIssuesApiClient _peIssuesApi;
        private readonly IPEIssueResolutionsApiClient _peIssueResolutionsApi;
        private readonly IUsersApiClient _usersApi;
        private readonly ILogger<IssuesApiController> _logger;

        public IssuesApiController(
            IPEIssuesApiClient peIssuesApi,
            IPEIssueResolutionsApiClient peIssueResolutionsApi,
            IUsersApiClient usersApi,
            ILogger<IssuesApiController> logger)
        {
            _peIssuesApi = peIssuesApi;
            _peIssueResolutionsApi = peIssueResolutionsApi;
            _usersApi = usersApi;
            _logger = logger;
        }

        [HttpPost("{issueId}/mark-read")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int issueId)
        {
            try
            {
                var serviceId = GetCurrentServiceId();
                if (string.IsNullOrEmpty(serviceId))
                {
                    return ApiError("User not authenticated", 401);
                }

                var result = await _peIssuesApi.MarkIssueAsReadAsync(issueId);
                if (result)
                {
                    return ApiResponse(new { }, "Issue marked as read");
                }
                else
                {
                    return ApiError("Failed to mark issue as read", 500);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking issue {IssueId} as read", issueId);
                return ApiException(ex);
            }
        }

        [HttpGet("{issueId}/resolution")]
        public async Task<IActionResult> GetResolution(int issueId)
        {
            try
            {
                var resolution = await _peIssueResolutionsApi.GetByIssueIdAsync(issueId);
                if (resolution != null)
                {
                    return ApiResponse(new { id = resolution.Id, details = resolution.ResolutionDetails });
                }
                else
                {
                    return ApiError("Resolution not found", 404);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting resolution for issue {IssueId}", issueId);
                return ApiException(ex);
            }
        }

        [HttpGet("resolution/{resolutionId}")]
        public async Task<IActionResult> GetResolutionById(int resolutionId)
        {
            try
            {
                var resolution = await _peIssueResolutionsApi.GetByIdAsync(resolutionId);
                if (resolution != null)
                {
                    return ApiResponse(new { id = resolution.Id, details = resolution.ResolutionDetails });
                }
                else
                {
                    return ApiError("Resolution not found", 404);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting resolution {ResolutionId}", resolutionId);
                return ApiException(ex);
            }
        }

        [HttpPost("resolution/{resolutionId}/confirm")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmResolution(int resolutionId, [FromBody] ConfirmResolutionModel request)
        {
            try
            {
                var serviceId = GetCurrentServiceId();
                if (string.IsNullOrEmpty(serviceId))
                {
                    return ApiError("User not authenticated", 401);
                }

                var result = await _peIssueResolutionsApi.ConfirmResolutionAsync(resolutionId, request.IsConfirmed);
                if (result)
                {
                    return ApiResponse(new { }, 
                        request.IsConfirmed ? "Resolution confirmed successfully" : "Resolution rejected successfully");
                }
                else
                {
                    return ApiError("Failed to process resolution confirmation", 500);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming resolution {ResolutionId}", resolutionId);
                return ApiException(ex);
            }
        }

        [HttpGet("task-list-id")]
        public IActionResult GetTaskListIdByName([FromQuery] string taskName)
        {
            try
            {
                if (string.IsNullOrEmpty(taskName))
                {
                    return ApiError("Task name is required", 400);
                }

                // This would need to be implemented in the API client
                // For now, return a placeholder
                return ApiResponse(0, "Task list lookup not implemented");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task list ID for task name {TaskName}", taskName);
                return ApiException(ex);
            }
        }

        [HttpGet("suggestions")]
        public IActionResult GetIssueSuggestions([FromQuery] int? peId, [FromQuery] int? taskListId, [FromQuery] int? taskId)
        {
            try
            {
                // This would need to be implemented to get issue suggestions
                // For now, return empty suggestions
                var suggestions = new
                {
                    commonIssues = new List<object>(),
                    recentIssues = new List<object>()
                };

                return ApiResponse(suggestions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting issue suggestions");
                return ApiException(ex);
            }
        }
    }

    public class ConfirmResolutionModel
    {
        public bool IsConfirmed { get; set; }
    }
}
