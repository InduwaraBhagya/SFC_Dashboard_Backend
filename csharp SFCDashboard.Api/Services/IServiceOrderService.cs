using SFCDashboard.Api.Models;

namespace SFCDashboard.Api.Services
{
    public interface IServiceOrderService
    {
        Task<ServiceOrder?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<ServiceOrder?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
        Task<List<ServiceOrder>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<ServiceOrder> CreateAsync(ServiceOrder order, CancellationToken cancellationToken = default);
        Task<ServiceOrder?> UpdateAsync(ServiceOrder order, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}