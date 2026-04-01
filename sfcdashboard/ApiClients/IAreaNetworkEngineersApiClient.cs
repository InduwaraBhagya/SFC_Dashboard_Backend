namespace SFCDashboard.ApiClients
{
    public interface IAreaNetworkEngineersApiClient
    {
        Task<IEnumerable<AreaNetworkEngineer>> GetAllAsync();
        Task<AreaNetworkEngineer?> GetByIdAsync(int id);
        Task<AreaNetworkEngineer> CreateAsync(AreaNetworkEngineer areaNetworkEngineer);
        Task<AreaNetworkEngineer> UpdateAsync(AreaNetworkEngineer areaNetworkEngineer);
        Task DeleteAsync(int id);
        Task<string?> GetEngineerNameByAreaAsync(string area);
    Task<string> ImportExcelAsync(Stream excelStream, string fileName, CancellationToken cancellationToken = default);
    }
}
