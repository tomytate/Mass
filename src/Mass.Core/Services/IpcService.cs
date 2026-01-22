using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Mass.Spec.Contracts.Ipc;
using Mass.Core.Interfaces;

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

    private readonly ILogService _logger;

    public IpcService(ILogService logger)
    {
        _logger = logger;
        _pipeName = $"{PipeNamePrefix}{Environment.UserName}";
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNameCaseInsensitive = true
        };
    }

    private Process? _serverProcess;

    public async Task<bool> StartServerAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
    {
        try
        {
            if (_serverPipe != null)
                return true;

            // Start the server process
            var serverPath = FindServerExecutable();
            if (string.IsNullOrEmpty(serverPath))
            {
                _logger.LogError("Could not find ProPXEServer.API.exe");
                return false;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = serverPath,
                UseShellExecute = false,
                CreateNoWindow = true, // Hide console window
                WorkingDirectory = Path.GetDirectoryName(serverPath)
            };

            _serverProcess = Process.Start(startInfo);

            // Give it a moment to start
            await Task.Delay(2000, ct);

            if (_serverProcess == null || _serverProcess.HasExited)
            {
                _logger.LogError("Server process failed to start or exited immediately.");
                return false;
            }

            _serviceProvider = serviceProvider;
            _serverCts = new CancellationTokenSource();
            _serverPipe = new NamedPipeServerStream(_pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

            _serverTask = Task.Run(async () => await RunServerAsync(_serverCts.Token), _serverCts.Token);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error starting server: {ex.Message}");
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

            if (_serverProcess != null && !_serverProcess.HasExited)
            {
                _serverProcess.Kill();
                _serverProcess.WaitForExit();
                _serverProcess.Dispose();
                _serverProcess = null;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private string? FindServerExecutable()
    {
        var currentDir = AppContext.BaseDirectory;

        // 1. Production/Same Directory
        var localPath = Path.Combine(currentDir, "ProPXEServer.API.exe");
        if (File.Exists(localPath)) return localPath;

        // 2. Development (Relative path from Mass.Launcher bin)
        var devPath = Path.GetFullPath(Path.Combine(currentDir, "../../../../ProPXEServer/ProPXEServer.API/bin/Debug/net10.0/ProPXEServer.API.exe"));
        if (File.Exists(devPath)) return devPath;

        // 3. Deployment Structure (e.g. plugins folder)
        var pluginPath = Path.Combine(currentDir, "ProPXEServer", "ProPXEServer.API.exe");
        if (File.Exists(pluginPath)) return pluginPath;

        // 4. Detailed recursive search for dev environments
        var searchPaths = new List<string>
        {
            // Direct Dev Path found in diagnostics
            Path.GetFullPath(Path.Combine(currentDir, "../../../../ProPXEServer/ProPXEServer.API/bin/Debug/net10.0/ProPXEServer.API.exe")),
            // Standard relative path
            Path.GetFullPath(Path.Combine(currentDir, "../ProPXEServer/ProPXEServer.API/bin/Debug/net10.0/ProPXEServer.API.exe")),
             // Flat deployment
            Path.GetFullPath(Path.Combine(currentDir, "ProPXEServer.API.exe")),
            // Subfolder deployment
             Path.GetFullPath(Path.Combine(currentDir, "ProPXEServer", "ProPXEServer.API.exe"))
        };

        foreach (var path in searchPaths)
        {
            // _logger.LogInformation($"Searching for server at: {path}"); // Optional verbose logging
            if (File.Exists(path)) return path;
        }

        return null;
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
            await ReadExactAsync(client, responseLengthBytes, ct);
            var responseLength = BitConverter.ToInt32(responseLengthBytes);

            var responseBytes = new byte[responseLength];
            await ReadExactAsync(client, responseBytes, ct);

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
                await ReadExactAsync(_serverPipe, messageLengthBytes, ct);
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
                await ReadExactAsync(_serverPipe, messageBytes, ct);

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

    private async Task ReadExactAsync(Stream stream, byte[] buffer, CancellationToken ct)
    {
        int totalRead = 0;
        while (totalRead < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(totalRead, buffer.Length - totalRead), ct);
            if (read == 0) throw new System.IO.EndOfStreamException("Stream closed before all bytes were read.");
            totalRead += read;
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
        // Synchronous cleanup to prevent deadlocks from .Wait()
        _serverCts?.Cancel();
        _serverPipe?.Dispose();
        _serverCts?.Dispose();
        
        if (_serverProcess != null && !_serverProcess.HasExited)
        {
            try
            {
                _serverProcess.Kill();
                _serverProcess.WaitForExit(5000);
            }
            catch { }
            _serverProcess.Dispose();
        }
        
        GC.SuppressFinalize(this);
    }
}
