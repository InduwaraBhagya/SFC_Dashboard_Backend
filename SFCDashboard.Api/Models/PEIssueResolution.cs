using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SFCDashboard.Api.Models
{
    public class PEIssueResolution
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int IssueId { get; set; }
        
        [Required]
        public string ResolutionDetails { get; set; }
        
        [Required]
        public DateTime ResolutionDate { get; set; }
        
        public bool IsConfirmed { get; set; }
        
        [Required]
        public DateTime ConfirmationRequestedDate { get; set; }
        
        public DateTime? ConfirmedDate { get; set; }
        
        [Required]
        public int PlannedEventId { get; set; }
        
        [ForeignKey("IssueId")]
        public virtual PEIssue Issue { get; set; }
        
        [ForeignKey("PlannedEventId")]
        public virtual PlannedEvent PlannedEvent { get; set; }
    }
}

