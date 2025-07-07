using SFCDashboard.Models;

namespace SFCDashboard.Services
{
    public interface ICustomerUserAssignmentsApiService
    {
        Task<CustomerUserAssignment?> GetCustomerUserAssignmentAsync(int id);
        Task<IEnumerable<CustomerUserAssignment>> GetCustomerUserAssignmentsByUserIdAsync(int userId);
        Task<List<string>> GetAssignedCustomersAsync(int userId);
        Task<CustomerUserAssignment?> CreateCustomerUserAssignmentAsync(CustomerUserAssignment assignment);
        Task<CustomerUserAssignment?> UpdateCustomerUserAssignmentAsync(CustomerUserAssignment assignment);
        Task<bool> DeleteCustomerUserAssignmentAsync(int id);
    }
}
