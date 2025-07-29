using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Api.Data;
using SFCDashboard.Api.Models;

namespace SFCDashboard.Api.Controllers
{
    [ApiController]
    [Route("api/customeruserassignments")]
    [Produces("application/json")]
    public class CustomerUserAssignmentsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CustomerUserAssignmentsApiController> _logger;
        private readonly IWebHostEnvironment _environment;

        public CustomerUserAssignmentsApiController(
            ApplicationDbContext context,
            ILogger<CustomerUserAssignmentsApiController> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
        }

        /// <summary>
        /// Get all customer user assignments
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomerUserAssignment>>> GetAllCustomerUserAssignments()
        {
            try
            {
                var assignments = await _context.CustomerUserAssignments
                    .Include(c => c.User)
                    .ToListAsync();
                return Ok(assignments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer user assignments");
                return StatusCode(500, "An error occurred while retrieving customer user assignments");
            }
        }

        /// <summary>
        /// Get customer user assignment by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<CustomerUserAssignment>> GetCustomerUserAssignment(int id)
        {
            try
            {
                var assignment = await _context.CustomerUserAssignments
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (assignment == null)
                {
                    return NotFound();
                }

                return Ok(assignment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer user assignment {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the customer user assignment");
            }
        }

        /// <summary>
        /// Create a new customer user assignment
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<CustomerUserAssignment>> CreateCustomerUserAssignment(CustomerUserAssignment assignment)
        {
            // Check authorization in production only
            if (_environment.IsProduction() && !User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            try
            {
                _context.CustomerUserAssignments.Add(assignment);
                await _context.SaveChangesAsync();

                // Load the user information
                var createdAssignment = await _context.CustomerUserAssignments
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.Id == assignment.Id);

                return CreatedAtAction(nameof(GetCustomerUserAssignment), new { id = assignment.Id }, createdAssignment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer user assignment");
                return StatusCode(500, "An error occurred while creating the customer user assignment");
            }
        }

        /// <summary>
        /// Update an existing customer user assignment
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomerUserAssignment(int id, CustomerUserAssignment assignment)
        {
            // Check authorization in production only
            if (_environment.IsProduction() && !User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            if (id != assignment.Id)
            {
                return BadRequest();
            }

            try
            {
                _context.Entry(assignment).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CustomerUserAssignmentExists(id))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer user assignment {Id}", id);
                return StatusCode(500, "An error occurred while updating the customer user assignment");
            }
        }

        /// <summary>
        /// Delete a customer user assignment
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomerUserAssignment(int id)
        {
            // Check authorization in production only
            if (_environment.IsProduction() && !User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            try
            {
                var assignment = await _context.CustomerUserAssignments.FindAsync(id);
                if (assignment == null)
                {
                    return NotFound();
                }

                _context.CustomerUserAssignments.Remove(assignment);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer user assignment {Id}", id);
                return StatusCode(500, "An error occurred while deleting the customer user assignment");
            }
        }

        /// <summary>
        /// Search customer user assignments
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<CustomerUserAssignment>>> SearchAssignments([FromQuery] string searchTerm)
        {
            try
            {
                if (string.IsNullOrEmpty(searchTerm))
                {
                    return await GetAllCustomerUserAssignments();
                }

                var assignments = await _context.CustomerUserAssignments
                    .Include(c => c.User)
                    .Where(c => c.Customer.Contains(searchTerm) ||
                               (c.User != null && (c.User.Name.Contains(searchTerm) || c.User.ServiceId.Contains(searchTerm))))
                    .ToListAsync();

                return Ok(assignments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching customer user assignments with term: {searchTerm}", searchTerm);
                return StatusCode(500, "An error occurred while searching customer user assignments");
            }
        }

        /// <summary>
        /// Get available customers (those not yet assigned)
        /// </summary>
        [HttpPost("available-customers")]
        public async Task<ActionResult<IEnumerable<string>>> GetAvailableCustomers([FromBody] List<string> assignedCustomers)
        {
            try
            {
                // Get all unique customers from PlannedEvents
                var allCustomers = await _context.PlannedEvents
                    .Where(pe => !string.IsNullOrEmpty(pe.Customer))
                    .Select(pe => pe.Customer)
                    .Distinct()
                    .ToListAsync();

                // Filter out customers that are already assigned
                var availableCustomers = allCustomers
                    .Where(customer => !assignedCustomers.Contains(customer))
                    .OrderBy(customer => customer)
                    .ToList();

                return Ok(availableCustomers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available customers");
                return StatusCode(500, "An error occurred while getting available customers");
            }
        }

        /// <summary>
        /// Get existing assignment by customer and user
        /// </summary>
        [HttpGet("existing")]
        public async Task<ActionResult<CustomerUserAssignment>> GetExistingAssignment([FromQuery] string customer, [FromQuery] int userId)
        {
            try
            {
                var assignment = await _context.CustomerUserAssignments
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.Customer == customer && c.UserId == userId);

                if (assignment == null)
                {
                    return NotFound();
                }

                return Ok(assignment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting existing assignment for customer: {customer}, userId: {userId}", customer, userId);
                return StatusCode(500, "An error occurred while getting the existing assignment");
            }
        }

        /// <summary>
        /// Get existing assignment by customer only
        /// </summary>
        [HttpGet("by-customer")]
        public async Task<ActionResult<CustomerUserAssignment>> GetExistingAssignmentByCustomer([FromQuery] string customer)
        {
            try
            {
                var assignment = await _context.CustomerUserAssignments
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.Customer == customer);

                if (assignment == null)
                {
                    return NotFound();
                }

                return Ok(assignment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting existing assignment for customer: {customer}", customer);
                return StatusCode(500, "An error occurred while getting the existing assignment");
            }
        }

        /// <summary>
        /// Get assignments with user details included
        /// </summary>
        [HttpGet("with-users")]
        public async Task<ActionResult<IEnumerable<CustomerUserAssignment>>> GetAssignmentsWithUsers()
        {
            try
            {
                var assignments = await _context.CustomerUserAssignments
                    .Include(c => c.User)
                    .OrderBy(c => c.Customer)
                    .ToListAsync();

                return Ok(assignments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assignments with users");
                return StatusCode(500, "An error occurred while getting assignments with users");
            }
        }

        private bool CustomerUserAssignmentExists(int id)
        {
            return _context.CustomerUserAssignments.Any(e => e.Id == id);
        }
    }
}

