using System.ComponentModel.DataAnnotations;

namespace SFCDashboard.Models
{
    public class PEIssue
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PlannedEventId { get; set; }

        [Required]
        public int PETaskId { get; set; } // Link to the task list/subtask

        [Required]
        public int ReceiverId { get; set; } // SystemUser Id

        [Required]
        public int SenderId { get; set; } // SystemUser Id

        [Required]
        public string IssueText { get; set; }

        public string? AttachmentPath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false;

        // Add these properties to your PEIssue model
        public bool IsReply { get; set; }
        public int? OriginalIssueId { get; set; }
        public bool IsResolved { get; set; }
        public bool IsResolutionRequest { get; set; }
        public bool IsHiddenFromInbox { get; set; } = false;
        public bool IsReminder { get; set; }

    }
}