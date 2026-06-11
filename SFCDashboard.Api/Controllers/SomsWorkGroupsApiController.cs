using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Api.Data;
using SFCDashboard.Api.Models.Soms;

namespace SFCDashboard.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/somsworkgroups")]
    [Produces("application/json")]
    public class SomsWorkGroupsApiController : ControllerBase
    {
        private readonly SomsDbContext _context;
        private readonly ILogger<SomsWorkGroupsApiController> _logger;

        public SomsWorkGroupsApiController(SomsDbContext context, ILogger<SomsWorkGroupsApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SomsWorkgroup>>> GetWorkGroups()
        {
            try
            {
                _logger.LogInformation("Getting all SOMS workgroups");
                var workGroups = await _context.Workgroups.ToListAsync();
                return Ok(workGroups);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all SOMS workgroups");
                return StatusCode(500, "An error occurred while retrieving SOMS workgroups");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SomsWorkgroup>> GetWorkGroup(int id)
        {
            try
            {
                var workGroup = await _context.Workgroups.FindAsync(id);
                if (workGroup == null) return NotFound($"WorkGroup with ID {id} not found");
                return Ok(workGroup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting SOMS workgroup {id}");
                return StatusCode(500, "An error occurred while retrieving the workgroup");
            }
        }

        [HttpPost]
        public async Task<ActionResult<SomsWorkgroup>> CreateWorkGroup([FromBody] SomsWorkgroup workGroup)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(workGroup.WG_Name))
                    return BadRequest(new { error = "WorkGroup name is required" });

                _context.Workgroups.Add(workGroup);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetWorkGroup), new { id = workGroup.Id }, workGroup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating SOMS workgroup");
                return StatusCode(500, new { error = "An error occurred while creating the workgroup" });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<SomsWorkgroup>> UpdateWorkGroup(int id, [FromBody] SomsWorkgroup workGroup)
        {
            try
            {
                if (id != workGroup.Id)
                    return BadRequest(new { error = "WorkGroup ID mismatch" });

                if (string.IsNullOrWhiteSpace(workGroup.WG_Name))
                    return BadRequest(new { error = "WorkGroup name is required" });

                var existingWorkGroup = await _context.Workgroups.FindAsync(id);
                if (existingWorkGroup == null)
                    return NotFound(new { error = "WorkGroup not found" });

                existingWorkGroup.WG_Name = workGroup.WG_Name;
                // Update other fields if necessary
                existingWorkGroup.Sections_id = workGroup.Sections_id;
                existingWorkGroup.Users_Id = workGroup.Users_Id;

                await _context.SaveChangesAsync();
                return Ok(existingWorkGroup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating SOMS workgroup {id}");
                return StatusCode(500, new { error = "An error occurred while updating the workgroup" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWorkGroup(int id)
        {
            try
            {
                var workGroup = await _context.Workgroups.FindAsync(id);
                if (workGroup == null)
                    return NotFound(new { error = "WorkGroup not found" });

                _context.Workgroups.Remove(workGroup);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting SOMS workgroup {id}");
                return StatusCode(500, new { error = "An error occurred while deleting the workgroup" });
            }
        }
    }
}
