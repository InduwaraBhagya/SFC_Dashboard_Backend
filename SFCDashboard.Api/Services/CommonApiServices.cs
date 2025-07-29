using Microsoft.EntityFrameworkCore;
using SFCDashboard.Api.Data;
using SFCDashboard.Api.Models;

namespace SFCDashboard.Api.Services
{
    public class AreaNetworkEngineersApiService : IAreaNetworkEngineersApiService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AreaNetworkEngineersApiService> _logger;

        public AreaNetworkEngineersApiService(ApplicationDbContext context, ILogger<AreaNetworkEngineersApiService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AreaNetworkEngineer?> GetAreaNetworkEngineerAsync(int id)
        {
            try
            {
                return await _context.AreaNetworkEngineers.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting area network engineer with id {Id}", id);
                return null;
            }
        }

        public async Task<AreaNetworkEngineer?> GetAreaNetworkEngineerByAreaAsync(string area)
        {
            try
            {
                return await _context.AreaNetworkEngineers
                    .FirstOrDefaultAsync(e => e.Area == area);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting area network engineer for area {Area}", area);
                return null;
            }
        }

        public async Task<string?> GetEngineerNameByAreaAsync(string area)
        {
            try
            {
                if (string.IsNullOrEmpty(area))
                    return null;

                return await _context.AreaNetworkEngineers
                    .Where(e => e.Area == area)
                    .Select(e => e.EngineerName)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting engineer name for area {Area}", area);
                return null;
            }
        }

        public async Task<IEnumerable<AreaNetworkEngineer>> GetAreaNetworkEngineersAsync()
        {
            try
            {
                return await _context.AreaNetworkEngineers.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all area network engineers");
                return new List<AreaNetworkEngineer>();
            }
        }

        public async Task<AreaNetworkEngineer?> CreateAreaNetworkEngineerAsync(AreaNetworkEngineer engineer)
        {
            try
            {
                _context.Add(engineer);
                await _context.SaveChangesAsync();
                return engineer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating area network engineer");
                return null;
            }
        }

        public async Task<AreaNetworkEngineer?> UpdateAreaNetworkEngineerAsync(AreaNetworkEngineer engineer)
        {
            try
            {
                _context.Update(engineer);
                await _context.SaveChangesAsync();
                return engineer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating area network engineer");
                return null;
            }
        }

        public async Task<bool> DeleteAreaNetworkEngineerAsync(int id)
        {
            try
            {
                var engineer = await _context.AreaNetworkEngineers.FindAsync(id);
                if (engineer != null)
                {
                    _context.AreaNetworkEngineers.Remove(engineer);
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting area network engineer with id {Id}", id);
                return false;
            }
        }
    }
}


