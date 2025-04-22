using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SFCDB.Models
{
    [Table("UrgentReasons")]
    public class UrgentReason
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        public int PERecordID { get; set; }

        [ForeignKey("PERecordID")]
        public virtual PERecord PERecord { get; set; }

        public string Reason { get; set; }

        public int Priority { get; set; }

        public string RequestedBy { get; set; }

        public DateTime RequestedDate { get; set; }
    }
}