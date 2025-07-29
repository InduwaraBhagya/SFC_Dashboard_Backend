using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Api.Models;
using SFCDashboard.Api.Services;

namespace SFCDashboard.Api.Controllers
{
    [ApiController]
    [Route("api/customeruserassignments")]
    [Produces("application/json")]
    public class CustomerUserAssignmentsApiController : ControllerBase
    {
        private readonly ILogger<CustomerUserAssignmentsApiController> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly ICustomerUserAssignmentsApiService _customerUserAssignmentsService;

        public CustomerUserAssignmentsApiController(
            ILogger<CustomerUserAssignmentsApiController> logger,
            IWebHostEnvironment environment,
            ICustomerUserAssignmentsApiService customerUserAssignmentsService)
        {
            _logger = logger;
            _environment = environment;
            _customerUserAssignmentsService = customerUserAssignmentsService;
        }

        /// <summary>
        /// Get all customer user assignments
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomerUserAssignment>>> GetAllCustomerUserAssignments()
        {
            try
            {
                var assignments = await _customerUserAssignmentsService.GetAllCustomerUserAssignmentsAsync();
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
                var assignment = await _customerUserAssignmentsService.GetCustomerUserAssignmentAsync(id);
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
            if (_environment.IsProduction() && !User.Identity?.IsAuthenticated == true)
            {
                return Unauthorized();
            }

            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var createdAssignment = await _customerUserAssignmentsService.CreateCustomerUserAssignmentAsync(assignment);
                if (createdAssignment == null)
                    return StatusCode(500, "Failed to create customer user assignment");

                return CreatedAtAction(nameof(GetCustomerUserAssignment), new { id = createdAssignment.Id }, createdAssignment);
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
            if (_environment.IsProduction() && !User.Identity?.IsAuthenticated == true)
            {
                return Unauthorized();
            }

            if (id != assignment.Id)
            {
                return BadRequest("ID mismatch");
            }

            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var updatedAssignment = await _customerUserAssignmentsService.UpdateCustomerUserAssignmentAsync(assignment);
                if (updatedAssignment == null)
                    return NotFound();

                return NoContent();
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
            if (_environment.IsProduction() && !User.Identity?.IsAuthenticated == true)
            {
                return Unauthorized();
            }

            try
            {
                var deleted = await _customerUserAssignmentsService.DeleteCustomerUserAssignmentAsync(id);
                if (!deleted)
                    return NotFound();

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
                var assignments = await _customerUserAssignmentsService.SearchAssignmentsAsync(searchTerm);
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
                var availableCustomers = await _customerUserAssignmentsService.GetAvailableCustomersAsync(assignedCustomers);
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
                var assignment = await _customerUserAssignmentsService.GetExistingAssignmentAsync(customer, userId);
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
                var assignment = await _customerUserAssignmentsService.GetExistingAssignmentByCustomerAsync(customer);
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
                var assignments = await _customerUserAssignmentsService.GetAssignmentsWithUsersAsync();
                return Ok(assignments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assignments with users");
                return StatusCode(500, "An error occurred while getting assignments with users");
            }
        }

        private async Task<bool> CustomerUserAssignmentExists(int id)
        {
            return await _customerUserAssignmentsService.CustomerUserAssignmentExistsAsync(id);
        }
    }
}

