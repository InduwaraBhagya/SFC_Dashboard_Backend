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

        public async Task<IEnumerable<PEIssue>> GetAllPEIssuesAsync()
        {
            try
            {
                return await _context.PEIssues
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all PE issues");
                return new List<PEIssue>();
            }
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
                // Handle hierarchical reply system for OriginalIssueId
                if (peIssue.OriginalIssueId.HasValue)
                {
                    var parentIssue = await _context.PEIssues.FindAsync(peIssue.OriginalIssueId.Value);
                    if (parentIssue != null)
                    {
                        // If the parent issue has an OriginalIssueId, use that as the root
                        // Otherwise, use the parent issue's ID as the root
                        peIssue.OriginalIssueId = parentIssue.OriginalIssueId ?? parentIssue.Id;
                        
                        _logger.LogInformation("Setting OriginalIssueId to {OriginalIssueId} for new issue replying to issue {ParentIssueId}", 
                            peIssue.OriginalIssueId, parentIssue.Id);
                    }
                }

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

        public async Task<PEIssue?> UpdatePEIssueAsync(int id, PEIssue peIssue)
        {
            try
            {
                var existingIssue = await _context.PEIssues.FindAsync(id);
                if (existingIssue == null)
                {
                    return null;
                }

                var wasResolved = existingIssue.IsResolved;

                // Update the properties explicitly
                existingIssue.IssueText = peIssue.IssueText;
                existingIssue.IsRead = peIssue.IsRead;
                existingIssue.IsResolved = peIssue.IsResolved;
                existingIssue.IsReply = peIssue.IsReply;
                existingIssue.IsReminder = peIssue.IsReminder;
                existingIssue.IsResolutionRequest = peIssue.IsResolutionRequest;
                existingIssue.IsHiddenFromInbox = peIssue.IsHiddenFromInbox;
                existingIssue.OriginalIssueId = peIssue.OriginalIssueId;
                existingIssue.AttachmentPath = peIssue.AttachmentPath;

                // Handle cascading resolution logic for hierarchical issues
                if (peIssue.IsResolved && !wasResolved)
                {
                    _logger.LogInformation("Issue {Id} is being marked as resolved. Checking for original issue to resolve.", existingIssue.Id);
                    
                    // If this is a reply being resolved, also resolve the original issue
                    if (existingIssue.OriginalIssueId.HasValue)
                    {
                        var originalIssue = await _context.PEIssues.FindAsync(existingIssue.OriginalIssueId.Value);
                        if (originalIssue != null && !originalIssue.IsResolved)
                        {
                            originalIssue.IsResolved = true;
                            _context.Entry(originalIssue).State = EntityState.Modified;
                            _logger.LogInformation("Original issue {OriginalIssueId} also marked as resolved due to reply {ReplyId} being resolved.", 
                                originalIssue.Id, existingIssue.Id);
                        }
                    }
                    // If this is an original issue being resolved, also resolve all its replies
                    else
                    {
                        var relatedReplies = await _context.PEIssues
                            .Where(i => i.OriginalIssueId == existingIssue.Id && !i.IsResolved)
                            .ToListAsync();

                        foreach (var reply in relatedReplies)
                        {
                            reply.IsResolved = true;
                            _context.Entry(reply).State = EntityState.Modified;
                            _logger.LogInformation("Reply {ReplyId} also marked as resolved due to original issue {OriginalId} being resolved.", 
                                reply.Id, existingIssue.Id);
                        }
                    }
                }

                _context.Entry(existingIssue).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                
                return existingIssue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating PE issue {Id}", id);
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

        public async Task<int> MarkAllRemindersAsReadAsync(int userId)
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

                var updatedCount = await _context.SaveChangesAsync();
                return updatedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all reminders as read for user {UserId}", userId);
                return 0;
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

        public async Task<IEnumerable<PEIssue>> GetInboxIssuesRawAsync(int userId, int limit = 10)
        {
            try
            {
                return await _context.PEIssues
                    .Where(i => i.ReceiverId == userId)
                    .OrderByDescending(i => i.CreatedAt)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inbox issues for user {UserId}", userId);
                return new List<PEIssue>();
            }
        }

        public async Task<IEnumerable<PEIssue>> GetRemindersRawAsync(int userId, bool showAll = true)
        {
            try
            {
                var query = _context.PEIssues
                    .Where(i => i.ReceiverId == userId);

                if (!showAll)
                {
                    // Only show unresolved reminders and actual reminders
                    query = query.Where(i => !i.IsResolved && i.IsReminder);
                }
                else
                {
                    // Show all reminders (including resolved ones)
                    query = query.Where(i => i.IsReminder);
                }

                return await query
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reminders for user {UserId}", userId);
                return new List<PEIssue>();
            }
        }

        public async Task<Dictionary<int, IEnumerable<PEIssue>>> GetIssuesByPlannedEventIdsRawAsync(List<int> peIds)
        {
            try
            {
                if (peIds == null || !peIds.Any())
                {
                    return new Dictionary<int, IEnumerable<PEIssue>>();
                }

                var issues = await _context.PEIssues
                    .Where(i => peIds.Contains(i.PlannedEventId))
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync();

                // Group issues by PlannedEventId
                var groupedIssues = issues
                    .GroupBy(i => i.PlannedEventId)
                    .ToDictionary(g => g.Key, g => g.AsEnumerable());

                // Ensure all requested PE IDs are in the result, even if they have no issues
                foreach (var peId in peIds)
                {
                    if (!groupedIssues.ContainsKey(peId))
                    {
                        groupedIssues[peId] = Enumerable.Empty<PEIssue>();
                    }
                }

                return groupedIssues;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting issues for planned events");
                return new Dictionary<int, IEnumerable<PEIssue>>();
            }
        }

        public async Task<IEnumerable<PEIssue>> GetPEIssuesByPlannedEventRawAsync(int plannedEventId)
        {
            try
            {
                return await _context.PEIssues
                    .Where(i => i.PlannedEventId == plannedEventId)
                    .OrderBy(i => i.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PE issues for planned event {PlannedEventId}", plannedEventId);
                return new List<PEIssue>();
            }
        }

        public async Task<PEIssue?> MarkIssueAsResolvedAsync(int id, bool isResolved = true)
        {
            try
            {
                var existingIssue = await _context.PEIssues.FindAsync(id);
                if (existingIssue == null)
                {
                    return null;
                }

                var wasResolved = existingIssue.IsResolved;
                existingIssue.IsResolved = isResolved;

                // Handle cascading resolution logic for hierarchical issues
                if (isResolved && !wasResolved)
                {
                    _logger.LogInformation("Issue {Id} is being marked as resolved. Checking for original issue to resolve.", existingIssue.Id);
                    
                    // If this is a reply being resolved, also resolve the original issue
                    if (existingIssue.OriginalIssueId.HasValue)
                    {
                        var originalIssue = await _context.PEIssues.FindAsync(existingIssue.OriginalIssueId.Value);
                        if (originalIssue != null && !originalIssue.IsResolved)
                        {
                            originalIssue.IsResolved = true;
                            _context.Entry(originalIssue).State = EntityState.Modified;
                            _logger.LogInformation("Original issue {OriginalIssueId} also marked as resolved due to reply {ReplyId} being resolved.", 
                                originalIssue.Id, existingIssue.Id);
                        }
                    }
                    // If this is an original issue being resolved, also resolve all its replies
                    else
                    {
                        var relatedReplies = await _context.PEIssues
                            .Where(i => i.OriginalIssueId == existingIssue.Id && !i.IsResolved)
                            .ToListAsync();

                        foreach (var reply in relatedReplies)
                        {
                            reply.IsResolved = true;
                            _context.Entry(reply).State = EntityState.Modified;
                            _logger.LogInformation("Reply {ReplyId} also marked as resolved due to original issue {OriginalId} being resolved.", 
                                reply.Id, existingIssue.Id);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                
                return existingIssue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking issue {Id} as resolved", id);
                return null;
            }
        }

        public async Task<object> GetIssueStatusAsync(int id)
        {
            try
            {
                var issue = await _context.PEIssues.FindAsync(id);
                if (issue == null)
                {
                    return new { error = "Issue not found" };
                }

                var resolution = await _context.PEIssueResolutions.FirstOrDefaultAsync(r => r.IssueId == id);

                return new { 
                    IssueId = issue.Id,
                    IssueIsResolved = issue.IsResolved,
                    IssueText = issue.IssueText,
                    HasResolution = resolution != null,
                    ResolutionId = resolution?.Id,
                    ResolutionIsConfirmed = resolution?.IsConfirmed,
                    ResolutionDetails = resolution?.ResolutionDetails,
                    ResolutionConfirmedDate = resolution?.ConfirmedDate
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting issue status {Id}", id);
                return new { error = ex.Message };
            }
        }

        public async Task<bool> MarkIssueAsReadAsync(int id)
        {
            try
            {
                var issue = await _context.PEIssues.FindAsync(id);
                if (issue == null)
                {
                    return false;
                }

                if (!issue.IsRead)
                {
                    issue.IsRead = true;
                    _context.Entry(issue).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking issue {Id} as read", id);
                return false;
            }
        }
    }
}


