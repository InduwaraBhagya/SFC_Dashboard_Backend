using Microsoft.EntityFrameworkCore;
using SFCDashboard.Api.Data;
using SFCDashboard.Api.Models;

namespace SFCDashboard.Api.Services
{
    public class PETaskListsApiService : IPETaskListsApiService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PETaskListsApiService> _logger;

        public PETaskListsApiService(ApplicationDbContext context, ILogger<PETaskListsApiService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PETaskList?> GetPETaskListAsync(int id)
        {
            try
            {
                return await _context.PETaskLists.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PE task list with id {Id}", id);
                return null;
            }
        }

        public async Task<PETaskList?> GetPETaskListByNameAsync(string name)
        {
            try
            {
                return await _context.PETaskLists
                    .FirstOrDefaultAsync(tl => tl.Name == name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PE task list with name {Name}", name);
                return null;
            }
        }

        public async Task<IEnumerable<PETaskList>> GetPETaskListsAsync()
        {
            try
            {
                return await _context.PETaskLists.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all PE task lists");
                return new List<PETaskList>();
            }
        }

        public async Task<PETaskList?> CreatePETaskListAsync(PETaskList taskList)
        {
            try
            {
                _context.Add(taskList);
                await _context.SaveChangesAsync();
                return taskList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PE task list");
                return null;
            }
        }

        public async Task<PETaskList?> UpdatePETaskListAsync(PETaskList taskList)
        {
            try
            {
                _context.Update(taskList);
                await _context.SaveChangesAsync();
                return taskList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating PE task list");
                return null;
            }
        }

        public async Task<bool> DeletePETaskListAsync(int id)
        {
            try
            {
                var taskList = await _context.PETaskLists.FindAsync(id);
                if (taskList != null)
                {
                    _context.PETaskLists.Remove(taskList);
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting PE task list with id {Id}", id);
                return false;
            }
        }
    }

    public class EscalationsApiService : IEscalationsApiService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EscalationsApiService> _logger;

        public EscalationsApiService(ApplicationDbContext context, ILogger<EscalationsApiService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Escalation?> GetEscalationAsync(int id)
        {
            try
            {
                return await _context.Escalations
                    .Include(e => e.PETask)
                    .FirstOrDefaultAsync(e => e.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting escalation with id {Id}", id);
                return null;
            }
        }

        public async Task<IEnumerable<Escalation>> GetEscalationsByTaskIdsAsync(List<int> taskIds)
        {
            try
            {
                return await _context.Escalations
                    .Include(e => e.PETask)
                    .Where(e => taskIds.Contains(e.TaskId))
                    .OrderByDescending(e => e.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting escalations by task IDs");
                return new List<Escalation>();
            }
        }

        public async Task<IEnumerable<Escalation>> GetEscalationsAsync()
        {
            try
            {
                return await _context.Escalations
                    .Include(e => e.PETask)
                    .OrderByDescending(e => e.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all escalations");
                return new List<Escalation>();
            }
        }

        public async Task<IEnumerable<Escalation>> GetEscalationsByUserRoleAsync(EscalationsByUserRoleRequest request)
        {
            try
            {
                var query = _context.Escalations
                    .Include(e => e.PETask)
                    .AsQueryable();

                // Filter by role level - implement specific business logic here
                if (request.UserRoleLevel > 0)
                {
                    query = query.Where(e => e.Level <= request.UserRoleLevel);
                }

                // Filter by workgroups if provided
                if (request.UserWorkgroupNames != null && request.UserWorkgroupNames.Any())
                {
                    // This would require additional navigation properties to filter by workgroups
                    // For now, we'll include all escalations that match the role level
                }

                var escalations = await query
                    .OrderByDescending(e => e.CreatedAt)
                    .ToListAsync();

                return escalations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting escalations by user role");
                return new List<Escalation>();
            }
        }

        public async Task<bool> MarkAsReadAsync(int id)
        {
            try
            {
                var escalation = await _context.Escalations.FindAsync(id);
                if (escalation == null)
                {
                    return false;
                }

                escalation.IsRead = true;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking escalation {Id} as read", id);
                return false;
            }
        }

        public async Task<Escalation?> CreateEscalationAsync(Escalation escalation)
        {
            try
            {
                escalation.CreatedAt = DateTime.UtcNow;
                _context.Escalations.Add(escalation);
                await _context.SaveChangesAsync();
                return escalation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating escalation");
                return null;
            }
        }

        public async Task<Escalation?> UpdateEscalationAsync(Escalation escalation)
        {
            try
            {
                _context.Update(escalation);
                await _context.SaveChangesAsync();
                return escalation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating escalation");
                return null;
            }
        }

        public async Task<bool> DeleteEscalationAsync(int id)
        {
            try
            {
                var escalation = await _context.Escalations.FindAsync(id);
                if (escalation != null)
                {
                    _context.Escalations.Remove(escalation);
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting escalation with id {Id}", id);
                return false;
            }
        }
    }
}


