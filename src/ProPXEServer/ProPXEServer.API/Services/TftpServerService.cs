using ProPXEServer.API.Security;
using System.Net;
using System.Net.Sockets;

namespace ProPXEServer.API.Services;

public class TftpServerService : BackgroundService {
    private readonly ILogger<TftpServerService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _tftpRoot;
    private UdpClient? _server;
    private const int TftpPort = 69;

    public TftpServerService(ILogger<TftpServerService> logger, IConfiguration configuration) {
        _logger = logger;
        _configuration = configuration;
        _tftpRoot = configuration["TftpSettings:RootPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "pxe");
        
        if (!Directory.Exists(_tftpRoot)) {
            Directory.CreateDirectory(_tftpRoot);
            _logger.LogInformation("Created TFTP root directory: {TftpRoot}", _tftpRoot);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        _logger.LogInformation("TFTP Server starting on port {Port} with root: {Root}", TftpPort, _tftpRoot);

        try {
            _server = new UdpClient(TftpPort);
            _logger.LogInformation("TFTP Server started successfully");

            while (!stoppingToken.IsCancellationRequested) {
                try {
                    var result = await _server.ReceiveAsync(stoppingToken);
                    _ = Task.Run(() => HandleClient(result.RemoteEndPoint, result.Buffer), stoppingToken);
                }
                catch (OperationCanceledException) {
                    break;
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "Error receiving TFTP request");
                }
            }
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to start TFTP server");
        }
        finally {
            _server?.Close();
            _logger.LogInformation("TFTP Server stopped");
        }
    }

    private async Task HandleClient(IPEndPoint clientEndpoint, byte[] requestData) {
        try {
            if (!SecurityConfiguration.IsIpAllowed(clientEndpoint.Address.ToString(), _configuration)) {
                _logger.LogWarning("Blocked TFTP request from unauthorized IP: {IP}", clientEndpoint.Address);
                await SendError(clientEndpoint, 2, "Access violation");
                return;
            }

            if (requestData.Length < 4) return;

            ushort opcode = (ushort)((requestData[0] << 8) | requestData[1]);

            if (opcode == 1) {
                await HandleReadRequest(clientEndpoint, requestData);
            }
            else if (opcode == 2) {
                _logger.LogWarning("Write request from {Client} - not supported", clientEndpoint);
            }
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error handling TFTP client {Client}", clientEndpoint);
        }
    }

    private async Task HandleReadRequest(IPEndPoint clientEndpoint, byte[] requestData) {
        string filename = "";
        try {
            int filenameStart = 2;
            int filenameEnd = Array.IndexOf(requestData, (byte)0, filenameStart);
            if (filenameEnd == -1) return;

            filename = System.Text.Encoding.ASCII.GetString(requestData, filenameStart, filenameEnd - filenameStart);
            filename = filename.Replace('/', Path.DirectorySeparatorChar);

            string fullPath = Path.Combine(_tftpRoot, filename);
            
            if (!File.Exists(fullPath)) {
                _logger.LogWarning("File not found: {Filename} (requested by {Client})", filename, clientEndpoint);
                await SendError(clientEndpoint, 1, "File not found");
                return;
            }

            _logger.LogInformation("Serving file: {Filename} to {Client}", filename, clientEndpoint);

            byte[] fileData = await File.ReadAllBytesAsync(fullPath);
            await SendFile(clientEndpoint, fileData);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error handling read request for {Filename} from {Client}", filename, clientEndpoint);
            await SendError(clientEndpoint, 0, "Internal server error");
        }
    }

    private async Task SendFile(IPEndPoint clientEndpoint, byte[] fileData) {
        using var client = new UdpClient();
        const int blockSize = 512;
        ushort blockNumber = 1;
        int offset = 0;

        while (offset < fileData.Length) {
            int dataLength = Math.Min(blockSize, fileData.Length - offset);
            byte[] packet = new byte[4 + dataLength];
            
            packet[0] = 0;
            packet[1] = 3;
            packet[2] = (byte)(blockNumber >> 8);
            packet[3] = (byte)(blockNumber & 0xFF);
            
            Array.Copy(fileData, offset, packet, 4, dataLength);

            int retries = 0;
            bool ackReceived = false;

            while (!ackReceived && retries < 5) {
                await client.SendAsync(packet, packet.Length, clientEndpoint);

                try {
                    client.Client.ReceiveTimeout = 1000;
                    var ackTask = client.ReceiveAsync();
                    var timeoutTask = Task.Delay(1000);
                    var completedTask = await Task.WhenAny(ackTask, timeoutTask);

                    if (completedTask == ackTask) {
                        var ack = await ackTask;
                        if (ack.Buffer.Length >= 4 && ack.Buffer[1] == 4) {
                            ushort ackBlock = (ushort)((ack.Buffer[2] << 8) | ack.Buffer[3]);
                            if (ackBlock == blockNumber) {
                                ackReceived = true;
                            }
                        }
                    }
                    else {
                        retries++;
                    }
                }
                catch {
                    retries++;
                }
            }

            if (!ackReceived) {
                _logger.LogWarning("Failed to receive ACK for block {Block} to {Client}", blockNumber, clientEndpoint);
                return;
            }

            offset += dataLength;
            blockNumber++;

            if (dataLength < blockSize) break;
        }
    }

    private async Task SendError(IPEndPoint clientEndpoint, ushort errorCode, string errorMessage) {
        using var client = new UdpClient();
        byte[] message = System.Text.Encoding.ASCII.GetBytes(errorMessage);
        byte[] packet = new byte[5 + message.Length];
        
        packet[0] = 0;
        packet[1] = 5;
        packet[2] = (byte)(errorCode >> 8);
        packet[3] = (byte)(errorCode & 0xFF);
        Array.Copy(message, 0, packet, 4, message.Length);
        packet[packet.Length - 1] = 0;

        await client.SendAsync(packet, packet.Length, clientEndpoint);
    }

    public override void Dispose() {
        _server?.Close();
        base.Dispose();
    }
}


