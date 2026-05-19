using System.ComponentModel.DataAnnotations;

public class AreaNetworkEngineer
{
    [Key]
    public int Id { get; set; }
    public string Area { get; set; } = string.Empty;
    public string EngineerName { get; set; } = string.Empty;
}

