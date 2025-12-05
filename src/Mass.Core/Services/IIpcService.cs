using Mass.Spec.Contracts.Ipc;

namespace Mass.Core.Services;

public interface IIpcService
{
    Task<bool> StartServerAsync(IServiceProvider serviceProvider, CancellationToken ct = default);
    Task<bool> StopServerAsync(CancellationToken ct = default);
    Task<IpcResponse> SendRequestAsync(IpcRequest request, CancellationToken ct = default);
    void RegisterHandler(string messageType, Func<IpcRequest, Task<IpcResponse>> handler);
    event EventHandler<IpcMessage>? MessageReceived;
    bool IsServerRunning { get; }
}
