public class SystemUserViewModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string ServiceId { get; set; }
    public int? UserRoleId { get; set; }
    public List<int> WorkGroupIds { get; set; } = new();
}

