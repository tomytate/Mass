using System.ComponentModel.DataAnnotations;

namespace ProPXEServer.API.Data;

public class Agent
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(17)] // MAC address format XX:XX:XX:XX:XX:XX
    public string MacAddress { get; set; } = string.Empty;

    [Required]
    public string Hostname { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string Capabilities { get; set; } = "[]";

    public string Status { get; set; } = "Offline";

    public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;
}
