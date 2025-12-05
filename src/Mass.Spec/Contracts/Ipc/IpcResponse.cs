namespace Mass.Spec.Contracts.Ipc;

public class IpcResponse : IpcMessage
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CorrelationId { get; set; }
}
