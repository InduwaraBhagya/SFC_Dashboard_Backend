using SFCDashboard.Api.Data;
using SFCDashboard.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace SFCDashboard.Api.Services
{
    public class PEIssueResolutionsApiService : IPEIssueResolutionsApiService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PEIssueResolutionsApiService> _logger;

        public PEIssueResolutionsApiService(ApplicationDbContext context, ILogger<PEIssueResolutionsApiService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PEIssueResolution?> GetPEIssueResolutionAsync(int id)
        {
            try
            {
                _logger.LogInformation("Getting PE issue resolution with id: {id}", id);
                return await _context.PEIssueResolutions.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PE issue resolution with id: {id}", id);
                return null;
            }
        }

        public async Task<IEnumerable<PEIssueResolution>> GetPEIssueResolutionsByIssueIdsAsync(List<int> issueIds)
        {
            try
            {
                _logger.LogInformation("Getting PE issue resolutions by issue ids: {ids}", string.Join(", ", issueIds));
                return await _context.PEIssueResolutions
                    .Where(r => issueIds.Contains(r.IssueId))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PE issue resolutions by issue ids");
                return new List<PEIssueResolution>();
            }
        }

        public async Task<Dictionary<int, PEIssueResolution>> GetResolutionsByIssueIdsAsync(List<int> issueIds)
        {
            try
            {
                _logger.LogInformation("Getting resolutions dictionary by issue ids: {ids}", string.Join(", ", issueIds));
                
                if (!issueIds.Any())
                {
                    return new Dictionary<int, PEIssueResolution>();
                }

                var resolutions = await _context.PEIssueResolutions
                    .Where(r => issueIds.Contains(r.IssueId) && !r.IsConfirmed)
                    .ToListAsync();

                return resolutions.ToDictionary(r => r.IssueId, r => r);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting resolutions dictionary by issue ids");
                return new Dictionary<int, PEIssueResolution>();
            }
        }

        public async Task<PEIssueResolution?> CreatePEIssueResolutionAsync(PEIssueResolution resolution)
        {
            try
            {
                _logger.LogInformation("Creating PE issue resolution for issue: {issueId}", resolution.IssueId);
                _context.PEIssueResolutions.Add(resolution);
                await _context.SaveChangesAsync();
                return resolution;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PE issue resolution");
                return null;
            }
        }

        public async Task<PEIssueResolution?> UpdatePEIssueResolutionAsync(PEIssueResolution resolution)
        {
            try
            {
                _logger.LogInformation("Updating PE issue resolution: {id}", resolution.Id);
                _context.PEIssueResolutions.Update(resolution);
                await _context.SaveChangesAsync();
                return resolution;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating PE issue resolution: {id}", resolution.Id);
                return null;
            }
        }

        public async Task<bool> DeletePEIssueResolutionAsync(int id)
        {
            try
            {
                _logger.LogInformation("Deleting PE issue resolution: {id}", id);
                var resolution = await _context.PEIssueResolutions.FindAsync(id);
                if (resolution == null) return false;

                _context.PEIssueResolutions.Remove(resolution);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting PE issue resolution: {id}", id);
                return false;
            }
        }

        public async Task<PEIssueResolution?> GetPendingResolutionAsync(int issueId)
        {
            try
            {
                _logger.LogInformation("Getting pending resolution for issue: {issueId}", issueId);
                return await _context.PEIssueResolutions
                    .FirstOrDefaultAsync(r => r.IssueId == issueId && !r.IsConfirmed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending resolution for issue: {issueId}", issueId);
                return null;
            }
        }
    }
}


