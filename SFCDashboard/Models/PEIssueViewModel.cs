namespace SFCDashboard.Models
{
    public class PEIssueViewModel
    {
        public string SenderName { get; set; }
        public string ReceiverName { get; set; }
        public string IssueText { get; set; }
        public string AttachmentPath { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}