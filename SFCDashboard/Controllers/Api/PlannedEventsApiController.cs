using Microsoft.AspNetCore.Mvc;
using SFCDashboard.ApiClients;
using SFCDashboard.Models;
using SFCDB.Models;

namespace SFCDashboard.Controllers.Api
{
    [Route("api/planned-events")]
    public class PlannedEventsApiController : ApiBaseController
    {
        private readonly IPlannedEventsApiClient _plannedEventsApi;
        private readonly INoticesApiClient _noticesApi;
        private readonly IUsersApiClient _usersApi;
        private readonly ILogger<PlannedEventsApiController> _logger;

        public PlannedEventsApiController(
            IPlannedEventsApiClient plannedEventsApi,
            INoticesApiClient noticesApi,
            IUsersApiClient usersApi,
            ILogger<PlannedEventsApiController> logger)
        {
            _plannedEventsApi = plannedEventsApi;
            _noticesApi = noticesApi;
            _usersApi = usersApi;
            _logger = logger;
        }

        [HttpGet("{peId}/basic-details")]
        public async Task<IActionResult> GetBasicDetails(int peId)
        {
            try
            {
                var serviceId = GetCurrentServiceId();
                if (string.IsNullOrEmpty(serviceId))
                {
                    return ApiError("User not authenticated", 401);
                }

                var plannedEvent = await _plannedEventsApi.GetByIdAsync(peId);
                if (plannedEvent == null)
                {
                    return ApiError("Planned event not found", 404);
                }

                var basicDetails = new
                {
                    plannedEvent.PeNumber,
                    plannedEvent.Customer,
                    PeStatus = plannedEvent.PEStatus,
                    plannedEvent.ServiceType,
                    plannedEvent.TaskName,
                    TaskWg = plannedEvent.TaskWg
                };

                return ApiResponse(basicDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting basic details for PE {PeId}", peId);
                return ApiException(ex);
            }
        }

        [HttpGet("notices")]
        public async Task<IActionResult> GetNotices()
        {
            try
            {
                var response = await _noticesApi.GetNoticesAsync();
                if (response.Success)
                {
                    return ApiResponse(response.Data);
                }
                else
                {
                    return ApiError(response.Message ?? "Failed to get notices");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notices");
                return ApiException(ex);
            }
        }

        [HttpPost("notices")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateNotice([FromBody] CreateNoticeModel request)
        {
            try
            {
                var serviceId = GetCurrentServiceId();
                if (string.IsNullOrEmpty(serviceId))
                {
                    return ApiError("User not authenticated", 401);
                }

                if (!ModelState.IsValid)
                {
                    return ApiError("Invalid notice data", 400);
                }

                // Get current user info
                var currentUser = await _usersApi.GetUserByServiceIdAsync(serviceId);
                if (currentUser == null)
                {
                    return ApiError("User not found", 404);
                }

                var response = await _noticesApi.CreateNoticeAsync(
                    request.Description,
                    currentUser.Id,
                    currentUser.Name,
                    request.IsPinned,
                    request.ExpireDate);

                if (response.Success)
                {
                    return ApiResponse(response.Data, "Notice created successfully");
                }
                else
                {
                    return ApiError(response.Message ?? "Failed to create notice");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notice");
                return ApiException(ex);
            }
        }

        [HttpPost("notices/{noticeId}/toggle-pin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePinNotice(int noticeId, [FromBody] TogglePinModel request)
        {
            try
            {
                var serviceId = GetCurrentServiceId();
                if (string.IsNullOrEmpty(serviceId))
                {
                    return ApiError("User not authenticated", 401);
                }

                // Get current user info
                var currentUser = await _usersApi.GetUserByServiceIdAsync(serviceId);
                if (currentUser == null)
                {
                    return ApiError("User not found", 404);
                }

                var response = await _noticesApi.TogglePinNoticeAsync(
                    noticeId, 
                    request.IsPinned, 
                    currentUser.Id, 
                    currentUser.Name);

                if (response.Success)
                {
                    return ApiResponse(response.Data, $"Notice {(request.IsPinned ? "pinned" : "unpinned")} successfully");
                }
                else
                {
                    return ApiError(response.Message ?? "Failed to toggle pin status");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling pin for notice {NoticeId}", noticeId);
                return ApiException(ex);
            }
        }

        [HttpDelete("notices/{noticeId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteNotice(int noticeId)
        {
            try
            {
                var serviceId = GetCurrentServiceId();
                if (string.IsNullOrEmpty(serviceId))
                {
                    return ApiError("User not authenticated", 401);
                }

                // Get current user info
                var currentUser = await _usersApi.GetUserByServiceIdAsync(serviceId);
                if (currentUser == null)
                {
                    return ApiError("User not found", 404);
                }

                var response = await _noticesApi.DeleteNoticeAsync(
                    noticeId, 
                    currentUser.Id, 
                    currentUser.Name);

                if (response.Success)
                {
                    return ApiResponse(new { }, "Notice deleted successfully");
                }
                else
                {
                    return ApiError(response.Message ?? "Failed to delete notice");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notice {NoticeId}", noticeId);
                return ApiException(ex);
            }
        }
    }

    public class CreateNoticeModel
    {
        public string Description { get; set; } = string.Empty;
        public bool IsPinned { get; set; }
        public DateTime? ExpireDate { get; set; }
    }

    public class TogglePinModel
    {
        public bool IsPinned { get; set; }
    }
}
