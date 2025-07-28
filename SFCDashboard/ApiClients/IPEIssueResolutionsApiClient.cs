using SFCDashboard.Models;

namespace SFCDashboard.ApiClients
{
    public interface IPEIssueResolutionsApiClient
    {
        Task<IEnumerable<PEIssueResolution>> GetAllAsync();
        Task<PEIssueResolution?> GetByIdAsync(int id);
        Task<PEIssueResolution> CreateAsync(PEIssueResolution peIssueResolution);
        Task<PEIssueResolution> UpdateAsync(PEIssueResolution peIssueResolution);
        Task DeleteAsync(int id);
        Task<PEIssueResolution?> GetPendingResolutionAsync(int issueId);
        Task<Dictionary<int, PEIssueResolution>> GetResolutionsByIssueIdsAsync(List<int> issueIds);
        Task RemoveAsync(int id);
        Task<PEIssueResolution> GetByIssueIdAsync(int issueId);
    }
}
