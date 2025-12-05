namespace Mass.Spec.Contracts.Ipc;

public class IpcRequest : IpcMessage
{
    public string RequestType { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
}
