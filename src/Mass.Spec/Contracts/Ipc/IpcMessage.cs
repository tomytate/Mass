namespace Mass.Spec.Contracts.Ipc;

public class IpcMessage
{
    public string MessageId { get; set; } = Guid.NewGuid().ToString();
    public string MessageType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object>? Data { get; set; }
}
