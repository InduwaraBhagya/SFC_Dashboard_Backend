namespace SFCDashboard.Models;
public class PEIssueViewModel
{
    public int SenderId { get; set; }
    public string SenderName { get; set; }
    public int ReceiverId { get; set; }
    public string ReceiverName { get; set; }
    public string IssueText { get; set; }
    public string AttachmentPath { get; set; }
    public DateTime CreatedAt { get; set; }
    public int PlannedEventId { get; set; }
    public bool IsRead { get; set; }
}