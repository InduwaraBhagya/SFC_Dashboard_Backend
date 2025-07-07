using SFCDashboard.Models;

namespace SFCDashboard.Services
{
    public interface IPEIssueResolutionsApiService
    {
        Task<PEIssueResolution?> GetPEIssueResolutionAsync(int id);
        Task<IEnumerable<PEIssueResolution>> GetPEIssueResolutionsByIssueIdsAsync(List<int> issueIds);
        Task<Dictionary<int, PEIssueResolution>> GetResolutionsByIssueIdsAsync(List<int> issueIds);
        Task<PEIssueResolution?> CreatePEIssueResolutionAsync(PEIssueResolution resolution);
        Task<PEIssueResolution?> UpdatePEIssueResolutionAsync(PEIssueResolution resolution);
        Task<bool> DeletePEIssueResolutionAsync(int id);
        Task<PEIssueResolution?> GetPendingResolutionAsync(int issueId);
    }
}
