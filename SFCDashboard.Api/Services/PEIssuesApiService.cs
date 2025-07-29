using Microsoft.EntityFrameworkCore;
using SFCDashboard.Api.Data;
using SFCDashboard.Api.Models;

namespace SFCDashboard.Api.Services
{
    public class PEIssuesApiService : IPEIssuesApiService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PEIssuesApiService> _logger;

        public PEIssuesApiService(ApplicationDbContext context, ILogger<PEIssuesApiService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PEIssue?> GetPEIssueAsync(int id)
        {
            try
            {
                return await _context.PEIssues.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PE issue with id {Id}", id);
                return null;
            }
        }

        public async Task<IEnumerable<PEIssueViewModel>> GetPEIssuesByPlannedEventAsync(int plannedEventId)
        {
            try
            {
                return await _context.PEIssues
                    .Where(i => i.PlannedEventId == plannedEventId && i.IsReminder == false)
                    .OrderByDescending(i => i.CreatedAt)
                    .Select(i => new PEIssueViewModel
                    {
                        Id = i.Id,
                        SenderId = i.SenderId,
                        SenderName = _context.Users
                            .Where(u => u.Id == i.SenderId)
                            .Select(u => u.Name)
                            .FirstOrDefault() ?? "Unknown Sender",
                        ReceiverId = i.ReceiverId,
                        ReceiverName = _context.Users
                            .Where(u => u.Id == i.ReceiverId)
                            .Select(u => u.Name)
                            .FirstOrDefault() ?? "Unknown Receiver",
                        IssueText = i.IssueText,
                        AttachmentPath = i.AttachmentPath,
                        CreatedAt = i.CreatedAt,
                        PlannedEventId = i.PlannedEventId,
                        IsResolved = i.IsResolved,
                        IsHiddenFromInbox = i.IsHiddenFromInbox
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PE issues for planned event {PlannedEventId}", plannedEventId);
                return new List<PEIssueViewModel>();
            }
        }

        public async Task<IEnumerable<PEIssueViewModel>> GetRemindersAsync(int userId, bool showAll = true)
        {
            try
            {
                var query = _context.PEIssues
                    .Where(i => i.ReceiverId == userId && i.IsReminder == true);

                if (!showAll)
                {
                    query = query.Where(i => !i.IsRead);
                }

                return await query
                    .OrderByDescending(i => i.CreatedAt)
                    .Select(i => new PEIssueViewModel
                    {
                        Id = i.Id,
                        PlannedEventId = i.PlannedEventId,
                        IssueText = i.IssueText,
                        CreatedAt = i.CreatedAt,
                        IsRead = i.IsRead
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reminders for user {UserId}", userId);
                return new List<PEIssueViewModel>();
            }
        }

        public async Task<int> GetReminderCountAsync(int userId)
        {
            try
            {
                return await _context.PEIssues
                    .CountAsync(i => i.ReceiverId == userId && i.IsReminder == true && !i.IsRead);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reminder count for user {UserId}", userId);
                return 0;
            }
        }

        public async Task<Dictionary<int, List<PEIssueViewModel>>> GetIssuesByPlannedEventIdsAsync(List<int> plannedEventIds)
        {
            try
            {
                var issues = await _context.PEIssues
                    .Where(i => plannedEventIds.Contains(i.PlannedEventId) && i.IsReminder == false)
                    .OrderByDescending(i => i.CreatedAt)
                    .Select(i => new PEIssueViewModel
                    {
                        Id = i.Id,
                        SenderId = i.SenderId,
                        SenderName = _context.Users
                            .Where(u => u.Id == i.SenderId)
                            .Select(u => u.Name)
                            .FirstOrDefault() ?? "Unknown Sender",
                        ReceiverId = i.ReceiverId,
                        ReceiverName = _context.Users
                            .Where(u => u.Id == i.ReceiverId)
                            .Select(u => u.Name)
                            .FirstOrDefault() ?? "Unknown Receiver",
                        IssueText = i.IssueText,
                        AttachmentPath = i.AttachmentPath,
                        CreatedAt = i.CreatedAt,
                        PlannedEventId = i.PlannedEventId,
                        IsResolved = i.IsResolved,
                        IsHiddenFromInbox = i.IsHiddenFromInbox
                    })
                    .ToListAsync();

                return issues
                    .GroupBy(i => i.PlannedEventId)
                    .ToDictionary(g => g.Key, g => g.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting issues by planned event IDs");
                return new Dictionary<int, List<PEIssueViewModel>>();
            }
        }

        public async Task<PEIssue?> CreatePEIssueAsync(PEIssue peIssue)
        {
            try
            {
                _context.Add(peIssue);
                await _context.SaveChangesAsync();
                return peIssue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PE issue");
                return null;
            }
        }

        public async Task<PEIssue?> UpdatePEIssueAsync(PEIssue peIssue)
        {
            try
            {
                _context.Update(peIssue);
                await _context.SaveChangesAsync();
                return peIssue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating PE issue");
                return null;
            }
        }

        public async Task<bool> DeletePEIssueAsync(int id)
        {
            try
            {
                var peIssue = await _context.PEIssues.FindAsync(id);
                if (peIssue != null)
                {
                    _context.PEIssues.Remove(peIssue);
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting PE issue with id {Id}", id);
                return false;
            }
        }

        public async Task<bool> MarkAllRemindersAsReadAsync(int userId)
        {
            try
            {
                var unreadReminders = await _context.PEIssues
                    .Where(i => i.ReceiverId == userId && i.IsReminder == true && !i.IsRead)
                    .ToListAsync();

                foreach (var reminder in unreadReminders)
                {
                    reminder.IsRead = true;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all reminders as read for user {UserId}", userId);
                return false;
            }
        }

        public async Task<IEnumerable<PEIssueViewModel>> GetInboxIssuesAsync(int userId, int take = 10)
        {
            try
            {
                _logger.LogInformation("Getting inbox issues for user {UserId}, take: {take}", userId, take);

                return await _context.PEIssues
                    .Where(i => i.ReceiverId == userId && !i.IsReminder)
                    .OrderByDescending(i => i.CreatedAt)
                    .Take(take)
                    .Select(i => new PEIssueViewModel
                    {
                        Id = i.Id,
                        SenderId = i.SenderId,
                        SenderName = _context.Users
                            .Where(u => u.Id == i.SenderId)
                            .Select(u => u.Name)
                            .FirstOrDefault() ?? "Unknown Sender",
                        ReceiverId = i.ReceiverId,
                        ReceiverName = _context.Users
                            .Where(u => u.Id == i.ReceiverId)
                            .Select(u => u.Name)
                            .FirstOrDefault() ?? "Unknown Receiver",
                        IssueText = i.IssueText,
                        AttachmentPath = i.AttachmentPath,
                        CreatedAt = i.CreatedAt,
                        PlannedEventId = i.PlannedEventId,
                        IsRead = i.IsRead,
                        IsReply = i.IsReply,
                        OriginalIssueId = i.OriginalIssueId,
                        IsResolved = i.IsResolved,
                        IsResolutionRequest = i.IsResolutionRequest,
                        PETaskId = i.PETaskId
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inbox issues for user {UserId}", userId);
                return new List<PEIssueViewModel>();
            }
        }
    }
}


