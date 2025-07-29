using Microsoft.EntityFrameworkCore;
using SFCDashboard.Api.Data;
using SFCDashboard.Api.Models;

namespace SFCDashboard.Api.Services
{
    public class CustomerUserAssignmentsApiService : ICustomerUserAssignmentsApiService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CustomerUserAssignmentsApiService> _logger;

        public CustomerUserAssignmentsApiService(ApplicationDbContext context, ILogger<CustomerUserAssignmentsApiService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<CustomerUserAssignment>> GetAllCustomerUserAssignmentsAsync()
        {
            try
            {
                return await _context.CustomerUserAssignments
                    .Include(c => c.User)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all customer user assignments");
                throw;
            }
        }

        public async Task<CustomerUserAssignment?> GetCustomerUserAssignmentAsync(int id)
        {
            try
            {
                return await _context.CustomerUserAssignments
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer user assignment {Id}", id);
                return null;
            }
        }

        public async Task<IEnumerable<CustomerUserAssignment>> GetCustomerUserAssignmentsByUserIdAsync(int userId)
        {
            try
            {
                return await _context.CustomerUserAssignments
                    .Include(c => c.User)
                    .Where(c => c.UserId == userId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer user assignments for user {UserId}", userId);
                throw;
            }
        }

        public async Task<List<string>> GetAssignedCustomersAsync(int userId)
        {
            try
            {
                return await _context.CustomerUserAssignments
                    .Where(c => c.UserId == userId)
                    .Select(c => c.Customer)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving assigned customers for user {UserId}", userId);
                throw;
            }
        }

        public async Task<CustomerUserAssignment?> CreateCustomerUserAssignmentAsync(CustomerUserAssignment assignment)
        {
            try
            {
                _context.CustomerUserAssignments.Add(assignment);
                await _context.SaveChangesAsync();
                return assignment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer user assignment");
                return null;
            }
        }

        public async Task<CustomerUserAssignment?> UpdateCustomerUserAssignmentAsync(CustomerUserAssignment assignment)
        {
            try
            {
                _context.Entry(assignment).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return assignment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer user assignment {Id}", assignment.Id);
                return null;
            }
        }

        public async Task<bool> DeleteCustomerUserAssignmentAsync(int id)
        {
            try
            {
                var assignment = await _context.CustomerUserAssignments.FindAsync(id);
                if (assignment == null)
                {
                    return false;
                }

                _context.CustomerUserAssignments.Remove(assignment);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer user assignment {Id}", id);
                return false;
            }
        }

        public async Task<IEnumerable<CustomerUserAssignment>> SearchAssignmentsAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrEmpty(searchTerm))
                {
                    return await GetAllCustomerUserAssignmentsAsync();
                }

                return await _context.CustomerUserAssignments
                    .Include(c => c.User)
                    .Where(c => c.Customer.Contains(searchTerm) ||
                               (c.User != null && (c.User.Name.Contains(searchTerm) || c.User.ServiceId.Contains(searchTerm))))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching customer user assignments with term: {searchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<IEnumerable<string>> GetAvailableCustomersAsync(List<string> assignedCustomers)
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

                return availableCustomers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available customers");
                throw;
            }
        }

        public async Task<CustomerUserAssignment?> GetExistingAssignmentAsync(string customer, int userId)
        {
            try
            {
                return await _context.CustomerUserAssignments
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.Customer == customer && c.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting existing assignment for customer: {customer}, userId: {userId}", customer, userId);
                return null;
            }
        }

        public async Task<CustomerUserAssignment?> GetExistingAssignmentByCustomerAsync(string customer)
        {
            try
            {
                return await _context.CustomerUserAssignments
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.Customer == customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting existing assignment for customer: {customer}", customer);
                return null;
            }
        }

        public async Task<IEnumerable<CustomerUserAssignment>> GetAssignmentsWithUsersAsync()
        {
            try
            {
                return await _context.CustomerUserAssignments
                    .Include(c => c.User)
                    .OrderBy(c => c.Customer)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assignments with users");
                throw;
            }
        }

        public async Task<bool> CustomerUserAssignmentExistsAsync(int id)
        {
            try
            {
                return await _context.CustomerUserAssignments.AnyAsync(e => e.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if customer user assignment exists {Id}", id);
                return false;
            }
        }
    }
}
