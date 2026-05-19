public class SystemUserViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ServiceId { get; set; } = string.Empty;
    public int? UserRoleId { get; set; }
    public List<int> WorkGroupIds { get; set; } = new();
}

