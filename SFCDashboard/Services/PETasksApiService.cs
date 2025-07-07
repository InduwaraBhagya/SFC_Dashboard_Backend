using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDashboard.Models;

namespace SFCDashboard.Services
{
    public class PETasksApiService : IPETasksApiService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PETasksApiService> _logger;

        public PETasksApiService(ApplicationDbContext context, ILogger<PETasksApiService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PETask?> GetPETaskAsync(int id)
        {
            try
            {
                return await _context.PETasks.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PE task with id {Id}", id);
                return null;
            }
        }

        public async Task<IEnumerable<PETask>> GetPETasksByPENumberAsync(string peNumber)
        {
            try
            {
                return await _context.PETasks
                    .Where(t => t.PENumber == peNumber)
                    .OrderBy(t => t.TaskSeq)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PE tasks for PE number {PENumber}", peNumber);
                return new List<PETask>();
            }
        }

        public async Task<IEnumerable<PETask>> GetPETasksByPENumbersAsync(List<string> peNumbers)
        {
            try
            {
                return await _context.PETasks
                    .Where(t => peNumbers.Contains(t.PENumber))
                    .OrderBy(t => t.TaskSeq)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PE tasks for multiple PE numbers");
                return new List<PETask>();
            }
        }

        public async Task<IEnumerable<PETask>> GetPendingUrgentTaskRequestsAsync()
        {
            try
            {
                return await _context.PETasks
                    .Where(t => t.UrgentRequested && !t.IsUrgent)
                    .Include(t => t.PlannedEvent)
                    .OrderByDescending(t => t.TaskCreatedDate)
                    .Take(5)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending urgent task requests");
                return new List<PETask>();
            }
        }

        public async Task<IEnumerable<string>> GetOLAViolatingPENumbersAsync()
        {
            try
            {
                return await _context.PETasks
                    .Where(t => t.IsOLAViolate)
                    .Select(t => t.PENumber)
                    .Distinct()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OLA violating PE numbers");
                return new List<string>();
            }
        }

        public async Task<PETask?> CreatePETaskAsync(PETask peTask)
        {
            try
            {
                _context.Add(peTask);
                await _context.SaveChangesAsync();
                return peTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PE task");
                return null;
            }
        }

        public async Task<PETask?> UpdatePETaskAsync(PETask peTask)
        {
            try
            {
                _context.Update(peTask);
                await _context.SaveChangesAsync();
                return peTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating PE task");
                return null;
            }
        }

        public async Task<bool> DeletePETaskAsync(int id)
        {
            try
            {
                var peTask = await _context.PETasks.FindAsync(id);
                if (peTask != null)
                {
                    _context.PETasks.Remove(peTask);
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting PE task with id {Id}", id);
                return false;
            }
        }

        public async Task<IEnumerable<PETask>> GetPendingTaskRequestsAsync(int take = 5)
        {
            try
            {
                _logger.LogInformation("Getting pending task requests, take: {take}", take);
                
                return await _context.PETasks
                    .Where(t => t.UrgentRequested && !t.IsUrgent)
                    .Include(t => t.PlannedEvent)
                    .OrderByDescending(t => t.TaskCreatedDate)
                    .Take(take)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending task requests");
                return new List<PETask>();
            }
        }

        public async Task<Dictionary<string, IEnumerable<PETask>>> GetTasksByPeNumbersAsync(List<string?> peNumbers)
        {
            try
            {
                _logger.LogInformation("Getting tasks by PE numbers: {count} numbers", peNumbers.Count);
                
                var validPeNumbers = peNumbers.Where(pn => !string.IsNullOrEmpty(pn)).Cast<string>().ToList();
                if (!validPeNumbers.Any())
                {
                    return new Dictionary<string, IEnumerable<PETask>>();
                }

                var allTasks = await _context.PETasks
                    .Where(t => validPeNumbers.Contains(t.PENumber))
                    .OrderBy(t => t.TaskSeq)
                    .ToListAsync();

                return allTasks
                    .GroupBy(t => t.PENumber)
                    .ToDictionary(g => g.Key, g => (IEnumerable<PETask>)g.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks by PE numbers");
                return new Dictionary<string, IEnumerable<PETask>>();
            }
        }
    }
}
