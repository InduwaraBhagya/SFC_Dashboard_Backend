using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Api.Models;
using SFCDashboard.Api.Services;
using System.ComponentModel.DataAnnotations;

namespace SFCDashboard.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class NoticesApiController : ControllerBase
    {
        private readonly INoticesService _noticesService;
        private readonly ILogger<NoticesApiController> _logger;

        public NoticesApiController(INoticesService noticesService, ILogger<NoticesApiController> logger)
        {
            _noticesService = noticesService;
            _logger = logger;
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
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid data provided.", errors = ModelState });
                }

                var notice = new Notice
                {
                    Description = request.Description,
                    CreatedBy = request.CreatedBy,
                    CreatedUserName = request.CreatedUserName,
                    CreatedDate = DateTime.Now,
                    IsPinned = request.IsPinned,
                    ExpireDate = request.ExpireDate,
                    IsActive = true
                };

                var createdNotice = await _noticesService.CreateNoticeAsync(notice);

                return Ok(new { success = true, data = createdNotice, message = "Notice created successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notice");
                return StatusCode(500, new { success = false, message = "An error occurred while creating the notice." });
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
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid data provided.", errors = ModelState });
                }

                var notice = await _noticesService.UpdateNoticeAsync(id, request.Description, request.UpdatedBy, request.UpdatedUserName);

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
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public int CreatedBy { get; set; }

        [Required]
        [StringLength(255)]
        public string CreatedUserName { get; set; } = string.Empty;

        public bool IsPinned { get; set; } = false;

        public DateTime? ExpireDate { get; set; }
    }

    public class UpdateNoticeRequest
    {
        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public int UpdatedBy { get; set; }

        [Required]
        [StringLength(255)]
        public string UpdatedUserName { get; set; } = string.Empty;
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
