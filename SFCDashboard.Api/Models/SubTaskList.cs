using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SFCDashboard.Api.Models
{
    public class SubTaskList
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int PETaskListId { get; set; }
        
        [Required]
        [StringLength(500)]
        public string SubTaskName { get; set; } = string.Empty;
        
        [ForeignKey("PETaskListId")]
        public virtual PETaskList PETaskList { get; set; } = null!;
        
        public int Frequency { get; set; } = 1; // Track how often this subtask is reported
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? LastReported { get; set; }
    }
}

