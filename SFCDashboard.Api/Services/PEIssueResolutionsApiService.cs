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

        public async Task<IEnumerable<PEIssueResolution>> GetAllPEIssueResolutionsAsync()
        {
            try
            {
                _logger.LogInformation("Getting all PE issue resolutions");
                return await _context.PEIssueResolutions.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all PE issue resolutions");
                return new List<PEIssueResolution>();
            }
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
                _logger.LogInformation("Updating PE issue resolution: {id}, IsConfirmed: {isConfirmed}", 
                    resolution.Id, resolution.IsConfirmed);
                
                var existingResolution = await _context.PEIssueResolutions.FindAsync(resolution.Id);
                if (existingResolution == null)
                {
                    _logger.LogWarning("PE issue resolution {id} not found for update", resolution.Id);
                    return null;
                }

                _logger.LogInformation("Before update - Resolution {id}: IsConfirmed={isConfirmed}", 
                    existingResolution.Id, existingResolution.IsConfirmed);

                // Update properties explicitly
                existingResolution.ResolutionDetails = resolution.ResolutionDetails;
                existingResolution.ResolutionDate = resolution.ResolutionDate;
                existingResolution.IsConfirmed = resolution.IsConfirmed;
                existingResolution.ConfirmationRequestedDate = resolution.ConfirmationRequestedDate;
                existingResolution.ConfirmedDate = resolution.ConfirmedDate;
                existingResolution.PlannedEventId = resolution.PlannedEventId;
                // Note: Don't update IssueId as it should be immutable

                await _context.SaveChangesAsync();
                
                _logger.LogInformation("After update - Resolution {id}: IsConfirmed={isConfirmed}", 
                    existingResolution.Id, existingResolution.IsConfirmed);
                
                return existingResolution;
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

        public async Task<PEIssueResolution?> GetByIssueIdAsync(int issueId)
        {
            try
            {
                _logger.LogInformation("Getting resolution by issue ID: {issueId}", issueId);
                return await _context.PEIssueResolutions
                    .FirstOrDefaultAsync(r => r.IssueId == issueId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting resolution by issue ID: {issueId}", issueId);
                return null;
            }
        }

        public async Task<bool> ConfirmResolutionAsync(int resolutionId, bool isConfirmed)
        {
            try
            {
                _logger.LogInformation("Confirming resolution {resolutionId}: isConfirmed={isConfirmed}", resolutionId, isConfirmed);

                var resolution = await _context.PEIssueResolutions.FindAsync(resolutionId);
                if (resolution == null)
                {
                    _logger.LogWarning("Resolution {resolutionId} not found", resolutionId);
                    return false;
                }

                _logger.LogInformation("Found resolution {resolutionId} for issue {issueId}", resolutionId, resolution.IssueId);
                
                // Log resolution details for debugging
                _logger.LogInformation("Resolution details - ID: {resolutionId}, IssueId: {issueId}, PlannedEventId: {plannedEventId}, IsConfirmed: {isConfirmed}", 
                    resolution.Id, resolution.IssueId, resolution.PlannedEventId, resolution.IsConfirmed);

                // Update the resolution
                resolution.IsConfirmed = isConfirmed;
                if (isConfirmed)
                {
                    resolution.ConfirmedDate = DateTime.Now;
                }

                _logger.LogInformation("Updated resolution {resolutionId}: IsConfirmed={isConfirmed}, ConfirmedDate={confirmedDate}", 
                    resolutionId, resolution.IsConfirmed, resolution.ConfirmedDate);

                // Update the related issue's resolved status based on confirmation
                _logger.LogInformation("Looking for issue with ID: {issueId} (from resolution {resolutionId})", resolution.IssueId, resolutionId);
                
                // First check if the issue exists at all
                var issueExists = await _context.PEIssues.AnyAsync(i => i.Id == resolution.IssueId);
                _logger.LogInformation("Issue {issueId} exists in database: {exists}", resolution.IssueId, issueExists);
                
                var issue = await _context.PEIssues.FindAsync(resolution.IssueId);
                if (issue != null)
                {
                    _logger.LogInformation("Found issue {issueId}, current IsResolved status: {isResolved}", issue.Id, issue.IsResolved);
                    
                    // Set the issue resolved status to match confirmation status
                    issue.IsResolved = isConfirmed;
                    
                    // If confirmed as resolved, also hide from inbox to show "Fixed" status
                    if (isConfirmed)
                    {
                        issue.IsHiddenFromInbox = true;
                        _logger.LogInformation("Issue {issueId} hidden from inbox as it's confirmed resolved", issue.Id);
                    }
                    
                    // Ensure the change is tracked
                    _context.Entry(issue).State = EntityState.Modified;
                    
                    _logger.LogInformation("Updated issue {issueId} IsResolved to: {isResolved}, IsHiddenFromInbox: {isHidden}, Entity state: {entityState}", 
                        issue.Id, issue.IsResolved, issue.IsHiddenFromInbox, _context.Entry(issue).State);
                }
                else
                {
                    _logger.LogError("CRITICAL: Issue {issueId} not found for resolution {resolutionId}! This indicates a data integrity issue.", resolution.IssueId, resolutionId);
                    
                    // Let's also check what issues do exist in the system
                    var allIssueIds = await _context.PEIssues.Select(i => i.Id).Take(10).ToListAsync();
                    _logger.LogError("Sample of existing issue IDs: [{issueIds}]", string.Join(", ", allIssueIds));
                    
                    // Check if there are any resolutions with invalid issue IDs
                    var invalidResolutions = await _context.PEIssueResolutions
                        .Where(r => !_context.PEIssues.Any(i => i.Id == r.IssueId))
                        .Select(r => new { r.Id, r.IssueId })
                        .Take(5)
                        .ToListAsync();
                    _logger.LogError("Found {count} resolutions with invalid issue IDs: {invalidResolutions}", 
                        invalidResolutions.Count, string.Join(", ", invalidResolutions.Select(r => $"ResolutionId:{r.Id}->IssueId:{r.IssueId}")));
                }

                // Ensure the resolution change is tracked
                _context.Entry(resolution).State = EntityState.Modified;

                var changeCount = await _context.SaveChangesAsync();
                _logger.LogInformation("Resolution {resolutionId} confirmation completed successfully. {changeCount} entities changed.", resolutionId, changeCount);
                
                // Verify the changes were saved
                var verifyIssue = await _context.PEIssues.FindAsync(resolution.IssueId);
                _logger.LogInformation("Verification: Issue {issueId} IsResolved after save: {isResolved} (expected: {expected})", 
                    resolution.IssueId, verifyIssue?.IsResolved, isConfirmed);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming resolution {resolutionId}", resolutionId);
                return false;
            }
        }
    }
}


