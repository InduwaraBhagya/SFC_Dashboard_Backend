using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SFCDashboard.Models
{
    public class SubTaskList
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int PETaskListId { get; set; }
        
        [Required]
        [StringLength(500)]
        public string SubTaskName { get; set; }
        
        [ForeignKey("PETaskListId")]
        public virtual PETaskList PETaskList { get; set; }
        
        public int Frequency { get; set; } = 1; // Track how often this subtask is reported
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? LastReported { get; set; }
    }
}