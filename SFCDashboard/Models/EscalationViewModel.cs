using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SFCDashboard.Models;
namespace SFCDashboard.Models
{
    public class EscalationViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public bool IsResolved { get; set; }

        // Task properties
        public int TaskId { get; set; }
        public string TaskName { get; set; }
        public string PENumber { get; set; }
        public string TaskStatus { get; set; }
        public DateTime? TaskDueDate { get; set; }
        public bool IsIgnored { get; set; }

        // Recipient properties
        public int? RecipientId { get; set; }
        public string RecipientName { get; set; }
        public string RecipientRole { get; set; }
        public string ResolvedByName { get; set; }

    }
}