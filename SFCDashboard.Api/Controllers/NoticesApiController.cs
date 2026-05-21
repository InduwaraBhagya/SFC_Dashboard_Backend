using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Api.Models;
using SFCDashboard.Api.Services;
using System.ComponentModel.DataAnnotations;

namespace SFCDashboard.Api.Controllers
{
    [ApiController]
    [Route("api/notices")]
    [Produces("application/json")]
    public class NoticesApiController : ControllerBase
    {
        private readonly INoticesService _noticesService;
        private readonly IPermissionsApiService _permissionsService;
        private readonly ILogger<NoticesApiController> _logger;

        public NoticesApiController(
            INoticesService noticesService, 
            IPermissionsApiService permissionsService,
            ILogger<NoticesApiController> logger)
        {
            _noticesService = noticesService;
            _permissionsService = permissionsService;
            _logger = logger;
        }

        /// <summary>
        /// Helper method to get current user's service ID from the identity
        /// </summary>
        private string GetCurrentUserServiceId()
        {
            var serviceId = User?.Identity?.Name ?? string.Empty;
            return serviceId.Length > 6 ? serviceId.Substring(0, 6) : serviceId;
        }

        /// <summary>
        /// Get all active notices ordered by pinned first, then by created date
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetNotices()
        {
            try
            {
                var notices = await _noticesService.GetActiveNoticesAsync();
                return Ok(new { success = true, data = notices });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notices");
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving notices." });
            }
        }

        /// <summary>
        /// Get a specific notice by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetNotice(int id)
        {
            try
            {
                var notice = await _noticesService.GetNoticeByIdAsync(id);

                if (notice == null)
                {
                    return NotFound(new { success = false, message = "Notice not found." });
                }

                return Ok(new { success = true, data = notice });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notice with ID {NoticeId}", id);
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving the notice." });
            }
        }

        /// <summary>
        /// Create a new notice
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateNotice([FromBody] CreateNoticeRequest request)
        {
            try
            {
                // Check if user has permission to create notices
                var currentUserServiceId = GetCurrentUserServiceId();
                if (string.IsNullOrEmpty(currentUserServiceId))
                {
                    return Unauthorized(new { success = false, message = "Unable to identify current user." });
                }

                // TEMPORARY BYPASS FOR TESTING: Allow any authenticated user to create a notice
                // var hasPermission = await _permissionsService.HasPermissionAsync(currentUserServiceId, "ManageNotices") ||
                //                    await _permissionsService.HasPermissionAsync(currentUserServiceId, "Admin");
                // if (!hasPermission)
                // {
                //     return StatusCode(403, new { success = false, message = "You do not have permission to create notices." });
                // }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid data provided.", errors = ModelState });
                }

                var notice = new Notice
                {
                    Title = request.Title,
                    Description = request.Description,
                    CreatedBy = request.CreatedBy,
                    CreatedUserName = request.CreatedUserName,
                    StartDate = request.StartDate ?? DateTime.Now,
                    CreatedDate = DateTime.Now,
                    IsPinned = request.IsPinned,
                    ExpireDate = request.ExpireDate,
                    IsActive = request.IsActive
                };

                var createdNotice = await _noticesService.CreateNoticeAsync(notice);

                return Ok(new { success = true, data = createdNotice, message = "Notice created successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notice.");
                return StatusCode(500, new { success = false, message = "Error creating notice in database.", detail = ex.Message, innerDetail = ex.InnerException?.Message });
            }
        }

        /// <summary>
        /// Update an existing notice
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNotice(int id, [FromBody] UpdateNoticeRequest request)
        {
            try
            {
                // Check if user has permission to update notices
                var currentUserServiceId = GetCurrentUserServiceId();
                if (string.IsNullOrEmpty(currentUserServiceId))
                {
                    return Unauthorized(new { success = false, message = "Unable to identify current user." });
                }

                // TEMPORARY BYPASS FOR TESTING
                // var hasPermission = await _permissionsService.HasPermissionAsync(currentUserServiceId, "ManageNotices") ||
                //                    await _permissionsService.HasPermissionAsync(currentUserServiceId, "Admin");
                // if (!hasPermission)
                // {
                //     return StatusCode(403, new { success = false, message = "You do not have permission to update notices." });
                // }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid data provided.", errors = ModelState });
                }

                var notice = await _noticesService.UpdateNoticeAsync(id, request.Title, request.Description, request.UpdatedBy, request.UpdatedUserName, request.StartDate, request.ExpireDate, request.IsActive);

                if (notice == null)
                {
                    return NotFound(new { success = false, message = "Notice not found." });
                }

                return Ok(new { success = true, data = notice, message = "Notice updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notice with ID {NoticeId}", id);
                return StatusCode(500, new { success = false, message = "An error occurred while updating the notice." });
            }
        }

        /// <summary>
        /// Pin or unpin a notice
        /// </summary>
        [HttpPatch("{id}/pin")]
        public async Task<IActionResult> TogglePinNotice(int id, [FromBody] TogglePinRequest request)
        {
            try
            {
                // Check if user has permission to pin/unpin notices
                var currentUserServiceId = GetCurrentUserServiceId();
                if (string.IsNullOrEmpty(currentUserServiceId))
                {
                    return Unauthorized(new { success = false, message = "Unable to identify current user." });
                }

                // TEMPORARY BYPASS FOR TESTING
                // var hasPermission = await _permissionsService.HasPermissionAsync(currentUserServiceId, "ManageNotices") ||
                //                    await _permissionsService.HasPermissionAsync(currentUserServiceId, "Admin");
                // if (!hasPermission)
                // {
                //     return StatusCode(403, new { success = false, message = "You do not have permission to pin or unpin notices." });
                // }

                var notice = await _noticesService.TogglePinNoticeAsync(id, request.IsPinned, request.UpdatedBy, request.UpdatedUserName);

                if (notice == null)
                {
                    return NotFound(new { success = false, message = "Notice not found." });
                }

                return Ok(new
                {
                    success = true,
                    data = notice,
                    message = request.IsPinned ? "Notice pinned successfully." : "Notice unpinned successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling pin status for notice {NoticeId}", id);
                return StatusCode(500, new { success = false, message = "An error occurred while updating the notice pin status." });
            }
        }

        /// <summary>
        /// Delete a notice (soft delete by setting IsActive to false)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotice(int id, [FromBody] DeleteNoticeRequest request)
        {
            try
            {
                // Check if user has permission to delete notices
                var currentUserServiceId = GetCurrentUserServiceId();
                if (string.IsNullOrEmpty(currentUserServiceId))
                {
                    return Unauthorized(new { success = false, message = "Unable to identify current user." });
                }

                // TEMPORARY BYPASS FOR TESTING
                // var hasPermission = await _permissionsService.HasPermissionAsync(currentUserServiceId, "ManageNotices") ||
                //                    await _permissionsService.HasPermissionAsync(currentUserServiceId, "Admin");
                // if (!hasPermission)
                // {
                //     return StatusCode(403, new { success = false, message = "You do not have permission to delete notices." });
                // }

                var result = await _noticesService.DeleteNoticeAsync(id, request.UpdatedBy, request.UpdatedUserName);

                if (!result)
                {
                    return NotFound(new { success = false, message = "Notice not found." });
                }

                return Ok(new { success = true, message = "Notice deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notice with ID {NoticeId}", id);
                return StatusCode(500, new { success = false, message = "An error occurred while deleting the notice." });
            }
        }
    }

    public class CreateNoticeRequest
    {
        [Required]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public int CreatedBy { get; set; }

        [Required]
        [StringLength(255)]
        public string CreatedUserName { get; set; } = string.Empty;

        public bool IsPinned { get; set; } = false;

        public DateTime? StartDate { get; set; }

        public DateTime? ExpireDate { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class UpdateNoticeRequest
    {
        [Required]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public int UpdatedBy { get; set; }

        [Required]
        [StringLength(255)]
        public string UpdatedUserName { get; set; } = string.Empty;

        public DateTime? StartDate { get; set; }

        public DateTime? ExpireDate { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class TogglePinRequest
    {
        public bool IsPinned { get; set; }

        [Required]
        public int UpdatedBy { get; set; }

        [Required]
        [StringLength(255)]
        public string UpdatedUserName { get; set; } = string.Empty;
    }

    public class DeleteNoticeRequest
    {
        [Required]
        public int UpdatedBy { get; set; }

        [Required]
        [StringLength(255)]
        public string UpdatedUserName { get; set; } = string.Empty;
    }
}
