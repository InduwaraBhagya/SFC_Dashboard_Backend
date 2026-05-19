using Microsoft.EntityFrameworkCore;
using SFCDashboard.Api.Data;
using SFCDashboard.Api.Models;

namespace SFCDashboard.Api.Services
{
    public class NoticesService : INoticesService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NoticesService> _logger;

        public NoticesService(ApplicationDbContext context, ILogger<NoticesService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Notice>> GetActiveNoticesAsync()
        {
            try
            {
                var currentDate = DateTime.Now.Date;
                return await _context.Notices
                    .Where(n => n.IsActive && (n.ExpireDate == null || n.ExpireDate >= currentDate))
                    .OrderByDescending(n => n.IsPinned)
                    .ThenByDescending(n => n.CreatedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active notices from database");
                throw;
            }
        }

        public async Task<Notice?> GetNoticeByIdAsync(int id)
        {
            try
            {
                return await _context.Notices
                    .FirstOrDefaultAsync(n => n.ID == id && n.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notice with ID {NoticeId} from database", id);
                throw;
            }
        }

        public async Task<Notice> CreateNoticeAsync(Notice notice)
        {
            try
            {
                _context.Notices.Add(notice);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Notice created with ID {NoticeId} by user {UserId}", notice.ID, notice.CreatedBy);
                return notice;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notice in database");
                throw;
            }
        }

        public async Task<Notice?> UpdateNoticeAsync(int id, string title, string description, int updatedBy, string updatedUserName, DateTime? startDate, DateTime? expireDate, bool isActive)
        {
            try
            {
                var notice = await _context.Notices
                    .FirstOrDefaultAsync(n => n.ID == id && n.IsActive);

                if (notice == null)
                {
                    return null;
                }

                notice.Title = title;
                notice.Description = description;
                notice.UpdatedBy = updatedBy;
                notice.UpdatedUserName = updatedUserName;
                notice.UpdatedDate = DateTime.Now;
                if (startDate.HasValue) notice.StartDate = startDate.Value;
                notice.ExpireDate = expireDate;
                notice.IsActive = isActive;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Notice {NoticeId} updated by user {UserId}", id, updatedBy);
                return notice;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notice with ID {NoticeId} in database", id);
                throw;
            }
        }

        public async Task<Notice?> TogglePinNoticeAsync(int id, bool isPinned, int updatedBy, string updatedUserName)
        {
            try
            {
                var notice = await _context.Notices
                    .FirstOrDefaultAsync(n => n.ID == id && n.IsActive);

                if (notice == null)
                {
                    return null;
                }

                notice.IsPinned = isPinned;
                notice.UpdatedBy = updatedBy;
                notice.UpdatedUserName = updatedUserName;
                notice.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Notice {NoticeId} pin status changed to {IsPinned} by user {UserId}",
                    id, isPinned, updatedBy);

                return notice;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling pin status for notice {NoticeId} in database", id);
                throw;
            }
        }

        public async Task<bool> DeleteNoticeAsync(int id, int updatedBy, string updatedUserName)
        {
            try
            {
                var notice = await _context.Notices
                    .FirstOrDefaultAsync(n => n.ID == id && n.IsActive);

                if (notice == null)
                {
                    return false;
                }

                notice.IsActive = false;
                notice.UpdatedBy = updatedBy;
                notice.UpdatedUserName = updatedUserName;
                notice.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Notice {NoticeId} deleted by user {UserId}", id, updatedBy);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notice with ID {NoticeId} from database", id);
                throw;
            }
        }
    }
}
