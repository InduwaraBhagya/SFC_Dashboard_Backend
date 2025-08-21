using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SFCDashboard.Models;
using System.Security.Claims;

namespace SFCDashboard.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public abstract class ApiBaseController : ControllerBase
    {
        protected string? GetCurrentServiceId()
        {
            var nameIdentifier = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(nameIdentifier))
            {
                var parts = nameIdentifier.Split('\\');
                return parts.Length > 1 ? parts[1] : parts[0];
            }
            return null;
        }

        protected IActionResult ApiResponse<T>(T data, string? message = null)
        {
            return Ok(new ApiResponse<T>
            {
                Success = true,
                Data = data,
                Message = message
            });
        }

        protected IActionResult ApiError(string message, int statusCode = 400)
        {
            return StatusCode(statusCode, new ApiResponse<object>
            {
                Success = false,
                Message = message,
                Data = null
            });
        }

        protected IActionResult ApiException(Exception ex)
        {
            // Log the exception here if needed
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An internal error occurred",
                Data = null
            });
        }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
    }
}
