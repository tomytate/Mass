using System.ComponentModel.DataAnnotations;

namespace ProPXEServer.API.Data;

public class Agent
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(17)]
    public string MacAddress { get; set; } = string.Empty;

    [MaxLength(255)]
    public string Hostname { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Version { get; set; } = string.Empty;

    public string Capabilities { get; set; } = "[]";

    [MaxLength(50)]
    public string Status { get; set; } = "Offline";

    public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;
}
