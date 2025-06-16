using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SFCDashboard.Models
{
    public class Escalation
    {
        public int Id { get; set; }
        
        public int TaskId { get; set; }
        [ForeignKey("TaskId")]
        public virtual PETask PETask { get; set; }  // Changed from Task to PETask
        
        public DateTime OLAViolationTime { get; set; }
        public DateTime CreatedAt { get; set; }
        
        public EscalationLevel Level { get; set; }
        
        public int? RecipientId { get; set; }
        [ForeignKey("RecipientId")]
        public virtual SystemUser Recipient { get; set; }
        
        public bool IsRead { get; set; }
        public bool IsIgnored { get; set; }
        public string? IgnoreReason { get; set; }
        public DateTime? IgnoredAt { get; set; }
        public int? IgnoredById { get; set; }
        [ForeignKey("IgnoredById")]
        public virtual SystemUser IgnoredBy { get; set; }
    }
    
    public enum EscalationLevel
    {
        Engineer = 1,
        DGM = 2,
        GM = 3
    }
}