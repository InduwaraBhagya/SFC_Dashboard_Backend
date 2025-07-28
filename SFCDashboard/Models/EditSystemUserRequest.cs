namespace SFCDashboard.Models
{
    /// <summary>
    /// Request model for editing system user
    /// </summary>
    public class EditSystemUserRequest
    {
        public string Name { get; set; } = string.Empty;
        public string ServiceId { get; set; } = string.Empty;
        public int? UserRoleId { get; set; }
        public List<int> WorkGroupIds { get; set; } = new List<int>();
    }
}
