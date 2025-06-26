using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace SFCDashboard.Models
{
    public class SurveyTaskActivity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PETaskId { get; set; }

        [ForeignKey("PETaskId")]
        public PETask? PETask { get; set; }

        [Required]
        public required string Description { get; set; }

        public required string FilePath { get; set; }

        public required string FileName { get; set; }

        [Required]
        public int SystemUserId { get; set; }

        [ForeignKey("SystemUserId")]
        public SystemUser? User { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class SurveyTaskActivityViewModel
    {
        public int TaskId { get; set; }
        
        public string PENumber { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Please enter a description")]
        public string Description { get; set; } = string.Empty;
        
        public IFormFile? File { get; set; }
    }

    public class SurveyTaskViewModel
    {
        public required PETask Task { get; set; }
        
        public required List<SurveyTaskActivity> Activities { get; set; }
        
        public required SurveyTaskActivityViewModel NewActivity { get; set; }
        
        public List<BOQ> BOQItems { get; set; } = new List<BOQ>();
    }
}