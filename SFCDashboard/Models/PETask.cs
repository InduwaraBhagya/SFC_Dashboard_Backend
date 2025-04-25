using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SFCDashboard.Models
{
    public class PETask
    {
        [Key]
        public int Id { get; set; }
        
        [ForeignKey("PE")]
        public string? PENumber { get; set; }
        
        public int? TaskSeq { get; set; }
        
        public string? Task { get; set; }
        
        public string? TaskWorkGroup { get; set; } = "NULL";
        
        public string? OLA { get; set; } 
        
        public string? TaskStatus { get; set; } = "INPROGRESS";
        
        // Make all DateTime fields nullable or provide default values
        public DateTime TaskCreatedDate { get; set; } = DateTime.UtcNow;
        
        public DateTime TaskCompleteDate { get; set; } = DateTime.UtcNow.AddDays(1);
        
        public DateTime? ActualTaskCreatedDate { get; set; }
        
        public DateTime? ACtualTaskCompleteDate { get; set; }
        
        public string? TaskPhase { get; set; } = "ONGOING";
        
        public PlannedEvent? PlannedEvent { get; set; }
    }
}
