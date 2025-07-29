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
                return await _context.Escalations.FindAsync(id);
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
                    .Include(e => e.IgnoredBy)
                    .Where(e => taskIds.Contains(e.TaskId))
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
                    .Include(e => e.IgnoredBy)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all escalations");
                return new List<Escalation>();
            }
        }

        public async Task<Escalation?> CreateEscalationAsync(Escalation escalation)
        {
            try
            {
                _context.Add(escalation);
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

    public class CustomerUserAssignmentsApiService : ICustomerUserAssignmentsApiService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CustomerUserAssignmentsApiService> _logger;

        public CustomerUserAssignmentsApiService(ApplicationDbContext context, ILogger<CustomerUserAssignmentsApiService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<CustomerUserAssignment?> GetCustomerUserAssignmentAsync(int id)
        {
            try
            {
                return await _context.CustomerUserAssignments.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer user assignment with id {Id}", id);
                return null;
            }
        }

        public async Task<IEnumerable<CustomerUserAssignment>> GetCustomerUserAssignmentsByUserIdAsync(int userId)
        {
            try
            {
                return await _context.CustomerUserAssignments
                    .Where(c => c.UserId == userId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer user assignments for user {UserId}", userId);
                return new List<CustomerUserAssignment>();
            }
        }

        public async Task<List<string>> GetAssignedCustomersAsync(int userId)
        {
            try
            {
                return await _context.CustomerUserAssignments
                    .Where(c => c.UserId == userId)
                    .Select(c => c.Customer)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assigned customers for user {UserId}", userId);
                return new List<string>();
            }
        }

        public async Task<CustomerUserAssignment?> CreateCustomerUserAssignmentAsync(CustomerUserAssignment assignment)
        {
            try
            {
                _context.Add(assignment);
                await _context.SaveChangesAsync();
                return assignment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer user assignment");
                return null;
            }
        }

        public async Task<CustomerUserAssignment?> UpdateCustomerUserAssignmentAsync(CustomerUserAssignment assignment)
        {
            try
            {
                _context.Update(assignment);
                await _context.SaveChangesAsync();
                return assignment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer user assignment");
                return null;
            }
        }

        public async Task<bool> DeleteCustomerUserAssignmentAsync(int id)
        {
            try
            {
                var assignment = await _context.CustomerUserAssignments.FindAsync(id);
                if (assignment != null)
                {
                    _context.CustomerUserAssignments.Remove(assignment);
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer user assignment with id {Id}", id);
                return false;
            }
        }
    }
}


