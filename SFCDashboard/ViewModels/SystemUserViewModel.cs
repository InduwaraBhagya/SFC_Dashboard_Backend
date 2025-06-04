public class SystemUserViewModel
{
    public string Name { get; set; }
    public string ServiceId { get; set; }
    public List<int> WorkGroupIds { get; set; } = new();
}