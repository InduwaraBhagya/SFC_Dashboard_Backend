using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SFCDashboard.Api.Models
{
    [Table("Notices")]
    public class Notice
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("ID")]
        public int ID { get; set; }

        [Column("Description")]
        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Column("CreatedDate")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Column("CreatedBy")]
        public int CreatedBy { get; set; }

        [Column("CreatedUserName")]
        [StringLength(255)]
        public string CreatedUserName { get; set; } = string.Empty;

        [Column("IsPinned")]
        public bool IsPinned { get; set; } = false;

        [Column("IsActive")]
        public bool IsActive { get; set; } = true;

        [Column("UpdatedDate")]
        public DateTime? UpdatedDate { get; set; }

        [Column("UpdatedBy")]
        public int? UpdatedBy { get; set; }

        [Column("UpdatedUserName")]
        [StringLength(255)]
        public string? UpdatedUserName { get; set; }
    }
}
