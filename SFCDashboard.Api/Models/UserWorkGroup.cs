using System.ComponentModel.DataAnnotations.Schema;

namespace SFCDashboard.Models
{
    public class UserWorkGroup
    {
        public int Id { get; set; }
        public int SystemUserId { get; set; }
        public int WorkGroupId { get; set; }

        [ForeignKey(nameof(SystemUserId))]
        public virtual SystemUser SystemUser { get; set; } = null!;

        [ForeignKey(nameof(WorkGroupId))]
        public virtual WorkGroup WorkGroup { get; set; } = null!;
    }
}