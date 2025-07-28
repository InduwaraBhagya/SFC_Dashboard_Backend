using SFCDashboard.Models;

namespace SFCDashboard.ApiClients
{
    public interface ICustomerUserAssignmentsApiClient
    {
        Task<IEnumerable<CustomerUserAssignment>> GetAllAsync();
        Task<CustomerUserAssignment?> GetByIdAsync(int id);
        Task<CustomerUserAssignment> CreateAsync(CustomerUserAssignment customerUserAssignment);
        Task<CustomerUserAssignment> UpdateAsync(CustomerUserAssignment customerUserAssignment);
        Task DeleteAsync(int id);
        
        // Additional methods for CustomerUserAssignmentController
        Task<IEnumerable<CustomerUserAssignment>> SearchAssignmentsAsync(string searchTerm);
        Task<IEnumerable<string>> GetAvailableCustomersAsync(List<string> assignedCustomers);
        Task<CustomerUserAssignment?> GetExistingAssignmentAsync(string customer, int userId);
        Task<CustomerUserAssignment?> GetExistingAssignmentByCustomerAsync(string customer);
        Task<IEnumerable<CustomerUserAssignment>> GetAssignmentsWithUsersAsync();
    }
}
