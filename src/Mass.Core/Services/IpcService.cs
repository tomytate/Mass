using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Mass.Spec.Contracts.Ipc;

namespace Mass.Core.Services;

public class IpcService : IIpcService, IDisposable
{
    private const string PipeNamePrefix = "MassSuite_IPC_";
    private const int MaxMessageSize = 1024 * 1024; // 1MB
    private readonly string _pipeName;
    private readonly Dictionary<string, Func<IpcRequest, Task<IpcResponse>>> _handlers = new();
    private readonly JsonSerializerOptions _jsonOptions;
    private NamedPipeServerStream? _serverPipe;
    private CancellationTokenSource? _serverCts;
    private Task? _serverTask;
    private IServiceProvider? _serviceProvider;

    public event EventHandler<IpcMessage>? MessageReceived;
    public bool IsServerRunning => _serverPipe != null && _serverPipe.IsConnected;

    public IpcService()
    {
        _pipeName = $"{PipeNamePrefix}{Environment.UserName}";
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<bool> StartServerAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
    {
        try
        {
            if (_serverPipe != null)
                return true;

            _serviceProvider = serviceProvider;
            _serverCts = new CancellationTokenSource();
            _serverPipe = new NamedPipeServerStream(_pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

            _serverTask = Task.Run(async () => await RunServerAsync(_serverCts.Token), _serverCts.Token);
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> StopServerAsync(CancellationToken ct = default)
    {
        try
        {
            _serverCts?.Cancel();
            
            if (_serverTask != null)
            {
                await _serverTask;
            }

            _serverPipe?.Dispose();
            _serverPipe = null;
            _serverCts?.Dispose();
            _serverCts = null;
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IpcResponse> SendRequestAsync(IpcRequest request, CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var bytes = Encoding.UTF8.GetBytes(json);

            if (bytes.Length > MaxMessageSize)
            {
                return new IpcResponse
                {
                    Success = false,
                    ErrorMessage = $"Message exceeds maximum size of {MaxMessageSize} bytes. Use file paths for large data.",
                    CorrelationId = request.CorrelationId
                };
            }

            using var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await client.ConnectAsync(5000, ct);

            await client.WriteAsync(BitConverter.GetBytes(bytes.Length), ct);
            await client.WriteAsync(bytes, ct);

            var responseLengthBytes = new byte[4];
            await client.ReadAsync(responseLengthBytes, ct);
            var responseLength = BitConverter.ToInt32(responseLengthBytes);

            var responseBytes = new byte[responseLength];
            await client.ReadAsync(responseBytes, ct);

            var responseJson = Encoding.UTF8.GetString(responseBytes);
            return JsonSerializer.Deserialize<IpcResponse>(responseJson, _jsonOptions) ?? new IpcResponse
            {
                Success = false,
                ErrorMessage = "Failed to deserialize response"
            };
        }
        catch (Exception ex)
        {
            return new IpcResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                CorrelationId = request.CorrelationId
            };
        }
    }

    public void RegisterHandler(string messageType, Func<IpcRequest, Task<IpcResponse>> handler)
    {
        _handlers[messageType] = handler;
    }

    private async Task RunServerAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _serverPipe != null)
        {
            try
            {
                await _serverPipe.WaitForConnectionAsync(ct);

                var messageLengthBytes = new byte[4];
                await _serverPipe.ReadAsync(messageLengthBytes, ct);
                var messageLength = BitConverter.ToInt32(messageLengthBytes);

                if (messageLength > MaxMessageSize)
                {
                    var errorResponse = new IpcResponse
                    {
                        Success = false,
                        ErrorMessage = "Message too large"
                    };
                    await SendResponseAsync(errorResponse, ct);
                    _serverPipe.Disconnect();
                    continue;
                }

                var messageBytes = new byte[messageLength];
                await _serverPipe.ReadAsync(messageBytes, ct);

                var messageJson = Encoding.UTF8.GetString(messageBytes);
                var request = JsonSerializer.Deserialize<IpcRequest>(messageJson, _jsonOptions);

                if (request != null)
                {
                    MessageReceived?.Invoke(this, request);
                    var response = await HandleRequestAsync(request);
                    await SendResponseAsync(response, ct);
                }

                _serverPipe.Disconnect();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Log error but continue listening
                if (_serverPipe != null && _serverPipe.IsConnected)
                {
                    _serverPipe.Disconnect();
                }
            }
        }
    }

    private async Task<IpcResponse> HandleRequestAsync(IpcRequest request)
    {
        if (_handlers.TryGetValue(request.RequestType, out var handler))
        {
            return await handler(request);
        }

        return new IpcResponse
        {
            Success = false,
            ErrorMessage = $"No handler registered for message type: {request.RequestType}",
            CorrelationId = request.CorrelationId
        };
    }

    private async Task SendResponseAsync(IpcResponse response, CancellationToken ct)
    {
        if (_serverPipe == null) return;

        var json = JsonSerializer.Serialize(response, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        await _serverPipe.WriteAsync(BitConverter.GetBytes(bytes.Length), ct);
        await _serverPipe.WriteAsync(bytes, ct);
        await _serverPipe.FlushAsync(ct);
    }

    public void Dispose()
    {
        StopServerAsync().Wait();
    }
}
