using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SFCDashboard.Api.Models
{
    public class Escalation
    {
        public int Id { get; set; }

        public int TaskId { get; set; }
        [ForeignKey("TaskId")]
        public virtual PETask PETask { get; set; }

        public int? Level { get; set; } // 1, 2, or 3
        public string Title { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }

        public bool IsRead { get; set; }

        public bool IsIgnored { get; set; }

        [MaxLength(500)]
        public string? IgnoreReason { get; set; }

        public DateTime? IgnoredAt { get; set; }

        [ForeignKey("IgnoredBy")]
        public int? IgnoredById { get; set; }

        public virtual SystemUser IgnoredBy { get; set; }

    }
    
    public enum EscalationLevel
    {
        Engineer = 1,
        DGM = 2,
        GM = 3
    }
}

