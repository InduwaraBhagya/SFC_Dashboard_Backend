using System;

namespace SFCDashboard.Models
{
    public class PEIssueViewModel
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; }
        public int ReceiverId { get; set; }
        public string ReceiverName { get; set; }
        public string IssueText { get; set; }
        public string? AttachmentPath { get; set; }
        public DateTime CreatedAt { get; set; }
        public int PlannedEventId { get; set; }
        public bool IsRead { get; set; }
        public bool IsReply { get; set; }
        public int? OriginalIssueId { get; set; }
        public bool IsResolved { get; set; }
        public bool IsResolutionRequest { get; set; }
        public int PETaskId { get; set; }

        // New properties for resolution information
        public string ResolutionDetails { get; set; }
        public int? ResolutionId { get; set; }

        // Property to indicate if the issue is hidden from the inbox
        public bool IsHiddenFromInbox { get; set; }
    }
}