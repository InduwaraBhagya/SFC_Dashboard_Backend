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
    }
}
