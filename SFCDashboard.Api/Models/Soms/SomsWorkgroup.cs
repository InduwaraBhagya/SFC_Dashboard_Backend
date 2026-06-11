using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SFCDashboard.Api.Models.Soms
{
    [Table("Workgroups")]
    public class SomsWorkgroup
    {
        [Key]
        public int Id { get; set; }

        public string? WG_Name { get; set; }

        public int? Sections_id { get; set; }

        public int? Users_Id { get; set; }
    }
}
