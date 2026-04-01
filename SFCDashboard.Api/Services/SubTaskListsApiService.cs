using Microsoft.EntityFrameworkCore;
using SFCDashboard.Api.Data;
using SFCDashboard.Api.Models;

namespace SFCDashboard.Api.Services
{
    public class SubTaskListsApiService : ISubTaskListsApiService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SubTaskListsApiService> _logger;

        public SubTaskListsApiService(ApplicationDbContext context, ILogger<SubTaskListsApiService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<SubTaskList>> GetAllSubTaskListsAsync()
        {
            try
            {
                return await _context.SubTaskLists
                    .Include(st => st.PETaskList)
                    .OrderBy(st => st.SubTaskName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all subtask lists");
                return new List<SubTaskList>();
            }
        }

        public async Task<SubTaskList?> GetSubTaskListByIdAsync(int id)
        {
            try
            {
                return await _context.SubTaskLists
                    .Include(st => st.PETaskList)
                    .FirstOrDefaultAsync(st => st.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subtask list with id {Id}", id);
                return null;
            }
        }

        public async Task<IEnumerable<SubTaskList>> GetSubTaskListsByTaskListIdAsync(int taskListId)
        {
            try
            {
                return await _context.SubTaskLists
                    .Where(st => st.PETaskListId == taskListId)
                    .Include(st => st.PETaskList)
                    .OrderBy(st => st.SubTaskName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subtask lists for task list {TaskListId}", taskListId);
                return new List<SubTaskList>();
            }
        }

        public async Task<SubTaskList?> GetSubTaskListByTaskListIdAndNameAsync(int taskListId, string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    _logger.LogWarning("Name parameter is null or empty");
                    return null;
                }

                var subTaskList = await _context.SubTaskLists
                    .Where(st => st.PETaskListId == taskListId && st.SubTaskName == name)
                    .Include(st => st.PETaskList)
                    .FirstOrDefaultAsync();

                if (subTaskList == null)
                {
                    // Create a new subtask if it doesn't exist
                    var newSubTask = new SubTaskList
                    {
                        PETaskListId = taskListId,
                        SubTaskName = name,
                        Frequency = 1,
                        CreatedAt = DateTime.Now,
                        LastReported = DateTime.Now
                    };

                    _context.SubTaskLists.Add(newSubTask);
                    await _context.SaveChangesAsync();

                    // Reload with the PETaskList included
                    var createdSubTask = await _context.SubTaskLists
                        .Include(st => st.PETaskList)
                        .FirstOrDefaultAsync(st => st.Id == newSubTask.Id);

                    _logger.LogInformation("Created new subtask: {SubTaskName} for task list {TaskListId}", name, taskListId);
                    return createdSubTask;
                }
                else
                {
                    // Update frequency and last reported
                    subTaskList.Frequency++;
                    subTaskList.LastReported = DateTime.Now;
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Updated existing subtask frequency: {SubTaskName}, new count: {Frequency}", 
                        subTaskList.SubTaskName, subTaskList.Frequency);
                    return subTaskList;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving/creating subtask list for task list {TaskListId} and name {Name}", taskListId, name);
                return null;
            }
        }

        public async Task<SubTaskList?> CreateSubTaskListAsync(SubTaskList subTaskList)
        {
            try
            {
                subTaskList.CreatedAt = DateTime.Now;
                _context.SubTaskLists.Add(subTaskList);
                await _context.SaveChangesAsync();

                // Reload with the PETaskList included
                var createdSubTaskList = await _context.SubTaskLists
                    .Include(st => st.PETaskList)
                    .FirstOrDefaultAsync(st => st.Id == subTaskList.Id);

                _logger.LogInformation("Created subtask list: {SubTaskName}", subTaskList.SubTaskName);
                return createdSubTaskList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subtask list");
                return null;
            }
        }

        public async Task<SubTaskList?> UpdateSubTaskListAsync(SubTaskList subTaskList)
        {
            try
            {
                _context.Entry(subTaskList).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                // Reload with the PETaskList included
                var updatedSubTaskList = await _context.SubTaskLists
                    .Include(st => st.PETaskList)
                    .FirstOrDefaultAsync(st => st.Id == subTaskList.Id);

                _logger.LogInformation("Updated subtask list: {SubTaskName}", subTaskList.SubTaskName);
                return updatedSubTaskList;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error updating subtask list {Id}", subTaskList.Id);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subtask list {Id}", subTaskList.Id);
                return null;
            }
        }

        public async Task<bool> DeleteSubTaskListAsync(int id)
        {
            try
            {
                var subTaskList = await _context.SubTaskLists.FindAsync(id);
                if (subTaskList == null)
                {
                    _logger.LogWarning("SubTask list with id {Id} not found for deletion", id);
                    return false;
                }

                _context.SubTaskLists.Remove(subTaskList);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted subtask list: {SubTaskName}", subTaskList.SubTaskName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting subtask list {Id}", id);
                return false;
            }
        }

        public async Task<bool> SubTaskListExistsAsync(int id)
        {
            try
            {
                return await _context.SubTaskLists.AnyAsync(e => e.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if subtask list exists {Id}", id);
                return false;
            }
        }
    }
}
