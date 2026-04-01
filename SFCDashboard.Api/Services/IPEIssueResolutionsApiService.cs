using SFCDashboard.Api.Models;

namespace SFCDashboard.Api.Services
{
    public interface IPEIssueResolutionsApiService
    {
        Task<IEnumerable<PEIssueResolution>> GetAllPEIssueResolutionsAsync();
        Task<PEIssueResolution?> GetPEIssueResolutionAsync(int id);
        Task<IEnumerable<PEIssueResolution>> GetPEIssueResolutionsByIssueIdsAsync(List<int> issueIds);
        Task<Dictionary<int, PEIssueResolution>> GetResolutionsByIssueIdsAsync(List<int> issueIds);
        Task<PEIssueResolution?> CreatePEIssueResolutionAsync(PEIssueResolution resolution);
        Task<PEIssueResolution?> UpdatePEIssueResolutionAsync(PEIssueResolution resolution);
        Task<bool> DeletePEIssueResolutionAsync(int id);
        Task<PEIssueResolution?> GetPendingResolutionAsync(int issueId);
        Task<PEIssueResolution?> GetByIssueIdAsync(int issueId);
        Task<bool> ConfirmResolutionAsync(int resolutionId, bool isConfirmed);
    }
}


