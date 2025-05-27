using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SFCDashboard.Models
{
    public class CreatePEIssueViewModel
    {
        [Required]
        public int PlannedEventId { get; set; }

        [Required]
        public int ReceiverId { get; set; }

        [Required]
        public string IssueText { get; set; }

        // No [Required] attribute here
        public IFormFile Attachment { get; set; }

        // Other properties...
    }
}