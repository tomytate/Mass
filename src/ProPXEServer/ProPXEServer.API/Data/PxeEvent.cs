namespace ProPXEServer.API.Data;

public class PxeEvent {
    public int Id { get; set; }
    public required string EventType { get; set; }
    public required string MacAddress { get; set; }
    public required string IpAddress { get; set; }
    public string? Architecture { get; set; }
    public string? BootFileName { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}


