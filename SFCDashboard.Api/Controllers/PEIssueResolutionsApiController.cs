using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Api.Models;
using SFCDashboard.Api.Services;

namespace SFCDashboard.Api.Controllers
{
    /// <summary>
    /// API Controller for PE Issue Resolutions management
    /// </summary>
    [ApiController]
    [Route("api/peissueresolutions")]
    [Produces("application/json")]
    public class PEIssueResolutionsApiController : ControllerBase
    {
        private readonly IPEIssueResolutionsApiService _peIssueResolutionsService;
        private readonly ILogger<PEIssueResolutionsApiController> _logger;

        public PEIssueResolutionsApiController(
            IPEIssueResolutionsApiService peIssueResolutionsService,
            ILogger<PEIssueResolutionsApiController> logger)
        {
            _peIssueResolutionsService = peIssueResolutionsService;
            _logger = logger;
        }

        /// <summary>
        /// Get all PE issue resolutions
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PEIssueResolution>>> GetPEIssueResolutions()
        {
            try
            {
                var resolutions = await _peIssueResolutionsService.GetAllPEIssueResolutionsAsync();
                return Ok(resolutions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PE issue resolutions");
                return StatusCode(500, "An error occurred while retrieving PE issue resolutions");
            }
        }

        /// <summary>
        /// Test endpoint to verify the controller is working
        /// </summary>
        [HttpGet("test")]
        public ActionResult Test()
        {
            _logger.LogInformation("Test endpoint called - PEIssueResolutionsApiController is working");
            return Ok(new { message = "PEIssueResolutionsApiController is working", timestamp = DateTime.Now });
        }

        /// <summary>
        /// Test endpoint to return a sample PE Issue Resolution
        /// </summary>
        [HttpGet("test-resolution")]
        public ActionResult<PEIssueResolution> TestResolution()
        {
            _logger.LogInformation("TestResolution endpoint called");
            
            var testResolution = new PEIssueResolution
            {
                Id = 999,
                IssueId = 1,
                ResolutionDetails = "Test resolution details",
                ResolutionDate = DateTime.Now,
                IsConfirmed = false,
                ConfirmationRequestedDate = DateTime.Now,
                ConfirmedDate = null,
                PlannedEventId = 1
            };
            
            return Ok(testResolution);
        }

        /// <summary>
        /// Get PE issue resolution by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<PEIssueResolution>> GetPEIssueResolution(int id)
        {
            try
            {
                _logger.LogInformation("GetPEIssueResolution called with ID: {Id}", id);
                
                var resolution = await _peIssueResolutionsService.GetPEIssueResolutionAsync(id);
                if (resolution == null)
                {
                    _logger.LogWarning("PE Issue Resolution with ID {Id} not found", id);
                    return NotFound($"PE Issue Resolution with ID {id} not found");
                }
                
                _logger.LogInformation("PE Issue Resolution found: ID={Id}, IssueId={IssueId}, Details={Details}", 
                    resolution.Id, resolution.IssueId, resolution.ResolutionDetails);
                
                return Ok(resolution);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PE issue resolution {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the PE issue resolution");
            }
        }

        /// <summary>
        /// Create a new PE issue resolution
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<PEIssueResolution>> CreatePEIssueResolution(PEIssueResolution resolution)
        {
            try
            {
                if (resolution == null)
                {
                    _logger.LogWarning("Attempted to create PE issue resolution with null resolution object");
                    return BadRequest("Resolution object cannot be null");
                }

                _logger.LogInformation("Creating PE issue resolution for issue {IssueId} with details: {Details}", 
                    resolution.IssueId, resolution.ResolutionDetails);

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ModelState is invalid. Errors: {Errors}", 
                        string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                    return BadRequest(ModelState);
                }

                var createdResolution = await _peIssueResolutionsService.CreatePEIssueResolutionAsync(resolution);
                if (createdResolution == null)
                {
                    _logger.LogError("Service returned null when creating PE issue resolution");
                    return StatusCode(500, "An error occurred while creating the PE issue resolution");
                }

                return CreatedAtAction(nameof(GetPEIssueResolution), new { id = createdResolution.Id }, createdResolution);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PE issue resolution");
                return StatusCode(500, "An error occurred while creating the PE issue resolution");
            }
        }

        /// <summary>
        /// Update an existing PE issue resolution
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<PEIssueResolution>> UpdatePEIssueResolution(int id, PEIssueResolution resolution)
        {
            try
            {
                if (id != resolution.Id)
                {
                    return BadRequest("ID mismatch");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var updatedResolution = await _peIssueResolutionsService.UpdatePEIssueResolutionAsync(resolution);
                if (updatedResolution == null)
                {
                    return NotFound($"PE Issue Resolution with ID {id} not found");
                }

                return Ok(updatedResolution);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating PE issue resolution {Id}", id);
                return StatusCode(500, "An error occurred while updating the PE issue resolution");
            }
        }

        /// <summary>
        /// Delete a PE issue resolution
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePEIssueResolution(int id)
        {
            try
            {
                var result = await _peIssueResolutionsService.DeletePEIssueResolutionAsync(id);
                if (!result)
                {
                    return NotFound($"PE Issue Resolution with ID {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting PE issue resolution {Id}", id);
                return StatusCode(500, "An error occurred while deleting the PE issue resolution");
            }
        }

        /// <summary>
        /// Get pending resolution for an issue
        /// </summary>
        [HttpGet("pending/{issueId}")]
        public async Task<ActionResult<PEIssueResolution>> GetPendingResolution(int issueId)
        {
            try
            {
                var resolution = await _peIssueResolutionsService.GetPendingResolutionAsync(issueId);
                if (resolution == null)
                {
                    return NotFound($"No pending resolution found for issue ID {issueId}");
                }
                return Ok(resolution);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending resolution for issue {IssueId}", issueId);
                return StatusCode(500, "An error occurred while retrieving the pending resolution");
            }
        }

        /// <summary>
        /// Get resolutions by issue IDs (bulk operation)
        /// </summary>
        [HttpPost("by-issue-ids")]
        public async Task<ActionResult<Dictionary<int, PEIssueResolution>>> GetResolutionsByIssueIds([FromBody] List<int> issueIds)
        {
            try
            {
                if (issueIds == null || !issueIds.Any())
                {
                    return BadRequest("Issue IDs list cannot be empty");
                }

                var resolutions = await _peIssueResolutionsService.GetResolutionsByIssueIdsAsync(issueIds);
                return Ok(resolutions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting resolutions by issue IDs");
                return StatusCode(500, "An error occurred while retrieving the resolutions");
            }
        }

        /// <summary>
        /// Get resolution by issue ID (single issue)
        /// </summary>
        [HttpGet("byissue/{issueId}")]
        public async Task<ActionResult<PEIssueResolution>> GetByIssueId(int issueId)
        {
            try
            {
                _logger.LogInformation("GetByIssueId called with Issue ID: {IssueId}", issueId);
                
                var resolution = await _peIssueResolutionsService.GetByIssueIdAsync(issueId);
                if (resolution == null)
                {
                    _logger.LogWarning("No resolution found for issue ID {IssueId}", issueId);
                    return NotFound($"No resolution found for issue ID {issueId}");
                }
                
                _logger.LogInformation("Resolution found for issue ID {IssueId}: ResolutionId={ResolutionId}, Details={Details}", 
                    issueId, resolution.Id, resolution.ResolutionDetails);
                
                return Ok(resolution);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting resolution by issue ID {IssueId}", issueId);
                return StatusCode(500, "An error occurred while retrieving the resolution");
            }
        }
    }
}
