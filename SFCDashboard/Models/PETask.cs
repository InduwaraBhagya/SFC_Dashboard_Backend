using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SFCDashboard.Models
{
    public class PETask
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TaskSeq { get; set; }

        [Required]
        public string PlannedEvent { get; set; } = string.Empty;

        public string OLA_Parameters { get; set; } = string.Empty;

    }
}
