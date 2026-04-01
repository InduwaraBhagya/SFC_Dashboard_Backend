using SFCDashboard.Api.Models;

namespace SFCDashboard.Api.Services
{
    public interface ICustomerUserAssignmentsApiService
    {
        Task<IEnumerable<CustomerUserAssignment>> GetAllCustomerUserAssignmentsAsync();
        Task<CustomerUserAssignment?> GetCustomerUserAssignmentAsync(int id);
        Task<IEnumerable<CustomerUserAssignment>> GetCustomerUserAssignmentsByUserIdAsync(int userId);
        Task<List<string>> GetAssignedCustomersAsync(int userId);
        Task<CustomerUserAssignment?> CreateCustomerUserAssignmentAsync(CustomerUserAssignment assignment);
        Task<CustomerUserAssignment?> UpdateCustomerUserAssignmentAsync(CustomerUserAssignment assignment);
        Task<bool> DeleteCustomerUserAssignmentAsync(int id);
        Task<IEnumerable<CustomerUserAssignment>> SearchAssignmentsAsync(string searchTerm);
        Task<IEnumerable<string>> GetAvailableCustomersAsync(List<string> assignedCustomers);
        Task<CustomerUserAssignment?> GetExistingAssignmentAsync(string customer, int userId);
        Task<CustomerUserAssignment?> GetExistingAssignmentByCustomerAsync(string customer);
        Task<IEnumerable<CustomerUserAssignment>> GetAssignmentsWithUsersAsync();
        Task<bool> CustomerUserAssignmentExistsAsync(int id);
    }
}


