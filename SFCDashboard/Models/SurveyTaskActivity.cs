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
        public PETask PETask { get; set; }

        [Required]
        public string Description { get; set; }

        public string FilePath { get; set; }

        public string FileName { get; set; }

        [Required]
        public int SystemUserId { get; set; }

        [ForeignKey("SystemUserId")]
        public SystemUser User { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class SurveyTaskActivityViewModel
    {
        public int TaskId { get; set; }
        
        [Required(ErrorMessage = "Please enter a description")]
        public string Description { get; set; }
        
        public IFormFile File { get; set; }
    }

    public class SurveyTaskViewModel
    {
        public PETask Task { get; set; }
        
        public List<SurveyTaskActivity> Activities { get; set; }
        
        public SurveyTaskActivityViewModel NewActivity { get; set; }
    }
}