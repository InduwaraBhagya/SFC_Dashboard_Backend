namespace SFCDashboard.Models;
public class PEIssueViewModel
{
    public int Id { get; set; }  // Add this missing property
    public int SenderId { get; set; }
    public string SenderName { get; set; }
    public int ReceiverId { get; set; }
    public string ReceiverName { get; set; }
    public string IssueText { get; set; }
    public string AttachmentPath { get; set; }
    public DateTime CreatedAt { get; set; }
    public int PlannedEventId { get; set; }
    public bool IsRead { get; set; }
    
    // Add these properties to match with PEIssue
    public bool IsReply { get; set; }
    public int? OriginalIssueId { get; set; }
    public bool IsResolved { get; set; }
    public bool IsResolutionRequest { get; set; }
    public int PETaskId { get; set; }
}