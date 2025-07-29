using SFCDashboard.Api.Data;
using SFCDashboard.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace SFCDashboard.Api.Services
{
    public class WorkGroupsApiService : IWorkGroupsApiService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WorkGroupsApiService> _logger;

        public WorkGroupsApiService(ApplicationDbContext context, ILogger<WorkGroupsApiService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<WorkGroup?> GetWorkGroupAsync(int id)
        {
            try
            {
                _logger.LogInformation("Getting workgroup with id: {id}", id);
                return await _context.WorkGroups.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workgroup with id: {id}", id);
                return null;
            }
        }

        public async Task<IEnumerable<WorkGroup>> GetWorkGroupsAsync()
        {
            try
            {
                _logger.LogInformation("Getting all workgroups");
                return await _context.WorkGroups.OrderBy(w => w.Name).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all workgroups");
                return new List<WorkGroup>();
            }
        }

        public async Task<IEnumerable<WorkGroup>> GetWorkGroupsByIdsAsync(List<int> workgroupIds)
        {
            try
            {
                _logger.LogInformation("Getting workgroups by ids: {ids}", string.Join(", ", workgroupIds));
                return await _context.WorkGroups
                    .Where(w => workgroupIds.Contains(w.Id))
                    .OrderBy(w => w.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workgroups by ids");
                return new List<WorkGroup>();
            }
        }

        public async Task<IEnumerable<WorkGroup>> GetUserWorkGroupsAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Getting workgroups for user: {userId}", userId);
                return await _context.WorkGroups
                    .Where(w => w.UserWorkGroups != null && w.UserWorkGroups.Any(uwg => uwg.SystemUserId == userId))
                    .OrderBy(w => w.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workgroups for user: {userId}", userId);
                return new List<WorkGroup>();
            }
        }

        public async Task<WorkGroup?> CreateWorkGroupAsync(WorkGroup workGroup)
        {
            try
            {
                _logger.LogInformation("Creating workgroup: {name}", workGroup.Name);
                _context.WorkGroups.Add(workGroup);
                await _context.SaveChangesAsync();
                return workGroup;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating workgroup");
                return null;
            }
        }

        public async Task<WorkGroup?> UpdateWorkGroupAsync(WorkGroup workGroup)
        {
            try
            {
                _logger.LogInformation("Updating workgroup: {id}", workGroup.Id);
                _context.WorkGroups.Update(workGroup);
                await _context.SaveChangesAsync();
                return workGroup;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating workgroup: {id}", workGroup.Id);
                return null;
            }
        }

        public async Task<bool> DeleteWorkGroupAsync(int id)
        {
            try
            {
                _logger.LogInformation("Deleting workgroup: {id}", id);
                var workGroup = await _context.WorkGroups.FindAsync(id);
                if (workGroup == null) return false;

                _context.WorkGroups.Remove(workGroup);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting workgroup: {id}", id);
                return false;
            }
        }

        public async Task<IEnumerable<WorkGroup>> GetWorkGroupsForUserAsync(List<int> userWorkgroupIds, bool canViewAll)
        {
            try
            {
                _logger.LogInformation("Getting workgroups for user display, canViewAll: {canViewAll}", canViewAll);
                
                if (canViewAll)
                {
                    return await _context.WorkGroups.OrderBy(w => w.Name).ToListAsync();
                }
                else
                {
                    return await _context.WorkGroups
                        .Where(w => userWorkgroupIds.Contains(w.Id))
                        .OrderBy(w => w.Name)
                        .ToListAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workgroups for user display");
                return new List<WorkGroup>();
            }
        }

        public async Task<string?> GetWorkGroupNameAsync(int workgroupId)
        {
            try
            {
                _logger.LogInformation("Getting workgroup name for id: {id}", workgroupId);
                return await _context.WorkGroups
                    .Where(w => w.Id == workgroupId)
                    .Select(w => w.Name)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workgroup name for id: {id}", workgroupId);
                return null;
            }
        }
    }
}


