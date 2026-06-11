using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SFCDashboard.Api.Models;
namespace SFCDashboard.Api.Models
{
    public class EscalationViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public int Level { get; set; }

        // Task properties
        public int TaskId { get; set; }
        public string TaskName { get; set; } = string.Empty;
        public string PENumber { get; set; } = string.Empty;
        public string TaskStatus { get; set; } = string.Empty;
        public DateTime? TaskDueDate { get; set; }
        public bool IsIgnored { get; set; }

        // Recipient properties
        public int? RecipientId { get; set; }
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientRole { get; set; } = string.Empty;

    }
}
