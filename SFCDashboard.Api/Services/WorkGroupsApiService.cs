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
                _logger.LogInformation("Updating workgroup: {id} with name: '{name}'", workGroup.Id, workGroup.Name);
                
                // Validate input
                if (string.IsNullOrWhiteSpace(workGroup.Name))
                {
                    throw new ArgumentException("WorkGroup name cannot be empty");
                }
                
                // Check for duplicate names (excluding the current workgroup)
                var duplicateExists = await _context.WorkGroups
                    .AnyAsync(wg => wg.Name.ToLower() == workGroup.Name.ToLower() && wg.Id != workGroup.Id);
                
                if (duplicateExists)
                {
                    throw new InvalidOperationException($"A workgroup with the name '{workGroup.Name}' already exists");
                }
                
                // Find the existing workgroup in the database
                var existingWorkGroup = await _context.WorkGroups.FindAsync(workGroup.Id);
                if (existingWorkGroup == null)
                {
                    _logger.LogWarning("WorkGroup with id {id} not found for update", workGroup.Id);
                    throw new InvalidOperationException($"WorkGroup with id {workGroup.Id} not found");
                }

                // Update only the properties we want to change
                _logger.LogInformation("Changing workgroup {id} name from '{oldName}' to '{newName}'", 
                    workGroup.Id, existingWorkGroup.Name, workGroup.Name);
                existingWorkGroup.Name = workGroup.Name;
                
                // Save changes
                var saveResult = await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully updated workgroup {id}, {saveResult} rows affected", workGroup.Id, saveResult);
                
                return existingWorkGroup;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating workgroup: {id}. Exception: {message}", workGroup.Id, ex.Message);
                throw; // Re-throw the exception so the controller can handle it
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


