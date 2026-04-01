using SFCDashboard.Api.Models;

namespace SFCDashboard.Api.Services
{
    public interface IIssueBusinessService
    {
        Task<IssueCreationResult> CreateIssueAsync(CreateIssueRequest request);
        Task<IssueReplyResult> CreateIssueReplyAsync(CreateIssueReplyRequest request);
        Task<IEnumerable<IssueInboxViewModel>> GetInboxIssuesAsync(int userId);
        Task<IEnumerable<IssueSentViewModel>> GetSentIssuesAsync(int userId);
        Task<bool> MarkIssueAsReadAsync(int issueId, int userId);
        Task<IssueDetailsViewModel?> GetIssueDetailsAsync(int issueId, int currentUserId);
        Task<int> GetUnreadCountAsync(int userId);
    }

    public class CreateIssueRequest
    {
        public int PlannedEventId { get; set; }
        public int? PETaskId { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string IssueText { get; set; } = string.Empty;
        public IFormFile? Attachment { get; set; }
        public bool IsReminder { get; set; } = false;
    }

    public class CreateIssueReplyRequest
    {
        public int OriginalIssueId { get; set; }
        public int SenderId { get; set; }
        public string ReplyText { get; set; } = string.Empty;
        public IFormFile? Attachment { get; set; }
    }

    public class IssueCreationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? IssueId { get; set; }
        public string? ErrorCode { get; set; }
    }

    public class IssueReplyResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? ReplyId { get; set; }
        public int? PlannedEventId { get; set; }
    }

    public class IssueInboxViewModel
    {
        public int Id { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string IssueText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public bool IsReminder { get; set; }
        public bool IsResolutionRequest { get; set; }
        public string? PeNumber { get; set; }
        public string? TaskName { get; set; }
        public string? AttachmentPath { get; set; }
    }

    public class IssueSentViewModel
    {
        public int Id { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
        public string IssueText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public string? PeNumber { get; set; }
        public string? TaskName { get; set; }
        public string? AttachmentPath { get; set; }
    }

    public class IssueDetailsViewModel
    {
        public int Id { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string ReceiverName { get; set; } = string.Empty;
        public string IssueText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public bool IsReminder { get; set; }
        public bool IsResolutionRequest { get; set; }
        public string? PeNumber { get; set; }
        public string? TaskName { get; set; }
        public string? AttachmentPath { get; set; }
        public int PlannedEventId { get; set; }
        public int? PETaskId { get; set; }
        public List<IssueReplyViewModel> Replies { get; set; } = new();
    }

    public class IssueReplyViewModel
    {
        public int Id { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string ReplyText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? AttachmentPath { get; set; }
    }
}
