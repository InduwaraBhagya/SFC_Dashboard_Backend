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
        public virtual PETask PETask { get; set; }
        
        public int? RecipientId { get; set; }
        public int? Level { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
        
        public bool IsRead { get; set; }
        public bool IsResolved { get; set; }
        
        public int? IgnoredById { get; set; }
    }
    
    public enum EscalationLevel
    {
        Engineer = 1,
        DGM = 2,
        GM = 3
    }
}