using SFCDashboard.Models;

namespace SFCDashboard.Services
{
    public interface IAreaNetworkEngineersApiService
    {
        Task<AreaNetworkEngineer?> GetAreaNetworkEngineerAsync(int id);
        Task<AreaNetworkEngineer?> GetAreaNetworkEngineerByAreaAsync(string area);
        Task<string?> GetEngineerNameByAreaAsync(string area);
        Task<IEnumerable<AreaNetworkEngineer>> GetAreaNetworkEngineersAsync();
        Task<AreaNetworkEngineer?> CreateAreaNetworkEngineerAsync(AreaNetworkEngineer engineer);
        Task<AreaNetworkEngineer?> UpdateAreaNetworkEngineerAsync(AreaNetworkEngineer engineer);
        Task<bool> DeleteAreaNetworkEngineerAsync(int id);
    }
}
