using System.Net;
using System.Net.Sockets;
using ProPXEServer.API.Data;

namespace ProPXEServer.API.Services;

public class TftpService(
    ILogger<TftpService> logger,
    IConfiguration configuration,
    IServiceProvider serviceProvider) : BackgroundService {
    
    private readonly string _bootFilesDirectory = configuration["ProPXEServer:BootFilesDirectory"] ?? "BootFiles";
    private UdpClient? _tftpSocket;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        logger.LogInformation("TFTP service starting on port 69");
        
        Directory.CreateDirectory(_bootFilesDirectory);
        
        try {
            _tftpSocket = new UdpClient(new IPEndPoint(IPAddress.Any, 69));
            
            while (!stoppingToken.IsCancellationRequested) {
                var result = await _tftpSocket.ReceiveAsync(stoppingToken);
                _ = Task.Run(() => HandleTftpRequest(result.Buffer, result.RemoteEndPoint), stoppingToken);
            }
        }
        catch (OperationCanceledException) {
            logger.LogInformation("TFTP service stopping");
        }
        catch (Exception ex) {
            logger.LogError(ex, "TFTP service failed");
            throw;
        }
    }

    private async Task HandleTftpRequest(byte[] packet, IPEndPoint remoteEndPoint) {
        try {
            if (packet.Length < 4) return;
            
            ushort opcode = (ushort)((packet[0] << 8) | packet[1]);
            
            if (opcode == 1) {
                await HandleReadRequest(packet, remoteEndPoint);
            }
            else if (opcode == 2) {
                logger.LogWarning("TFTP write requests are not supported");
                await SendError(remoteEndPoint, 2, "Write requests not allowed");
            }
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error handling TFTP request from {EndPoint}", remoteEndPoint);
            await SendError(remoteEndPoint, 0, "Server error");
        }
    }

    private async Task HandleReadRequest(byte[] packet, IPEndPoint remoteEndPoint) {
        var fileName = ExtractString(packet, 2);
        var mode = ExtractString(packet, 2 + fileName.Length + 1);
        
        logger.LogInformation("TFTP RRQ: {FileName} from {EndPoint}", fileName, remoteEndPoint);
        
        var filePath = Path.Combine(_bootFilesDirectory, fileName.TrimStart('/'));
        
        if (!File.Exists(filePath)) {
            logger.LogWarning("File not found: {FilePath}", filePath);
            await SendError(remoteEndPoint, 1, "File not found");
            return;
        }
        
        await LogPxeEvent("TFTP_READ", remoteEndPoint.Address.ToString(), fileName);
        
        await SendFile(filePath, remoteEndPoint);
    }

    private async Task SendFile(string filePath, IPEndPoint remoteEndPoint) {
        const int blockSize = 512;
        ushort blockNumber = 1;
        
        using var fileStream = File.OpenRead(filePath);
        using var transferSocket = new UdpClient();
        transferSocket.Client.ReceiveTimeout = 5000;
        
        var buffer = new byte[blockSize];
        int bytesRead;
        
        while ((bytesRead = await fileStream.ReadAsync(buffer)) > 0) {
            var dataPacket = new byte[4 + bytesRead];
            dataPacket[0] = 0;
            dataPacket[1] = 3;
            dataPacket[2] = (byte)(blockNumber >> 8);
            dataPacket[3] = (byte)(blockNumber & 0xFF);
            Array.Copy(buffer, 0, dataPacket, 4, bytesRead);
            
            bool ackReceived = false;
            int retries = 5;
            
            while (!ackReceived && retries > 0) {
                await transferSocket.SendAsync(dataPacket, remoteEndPoint);
                
                try {
                    var ackResult = await transferSocket.ReceiveAsync();
                    
                    if (ackResult.Buffer.Length >= 4 &&
                        ackResult.Buffer[0] == 0 && ackResult.Buffer[1] == 4) {
                        ushort ackBlock = (ushort)((ackResult.Buffer[2] << 8) | ackResult.Buffer[3]);
                        if (ackBlock == blockNumber) {
                            ackReceived = true;
                        }
                    }
                }
                catch (SocketException) {
                    retries--;
                    logger.LogWarning("TFTP timeout for block {Block}, retries left: {Retries}", 
                        blockNumber, retries);
                }
            }
            
            if (!ackReceived) {
                logger.LogError("TFTP transfer failed after retries for {File}", filePath);
                return;
            }
            
            blockNumber++;
            
            if (bytesRead < blockSize) {
                break;
            }
        }
        
        logger.LogInformation("TFTP transfer completed: {File}, {Blocks} blocks", 
            Path.GetFileName(filePath), blockNumber - 1);
    }

    private async Task SendError(IPEndPoint remoteEndPoint, ushort errorCode, string message) {
        var errorPacket = new byte[4 + message.Length + 1];
        errorPacket[0] = 0;
        errorPacket[1] = 5;
        errorPacket[2] = (byte)(errorCode >> 8);
        errorPacket[3] = (byte)(errorCode & 0xFF);
        System.Text.Encoding.ASCII.GetBytes(message, 0, message.Length, errorPacket, 4);
        errorPacket[^1] = 0;
        
        if (_tftpSocket != null) {
            await _tftpSocket.SendAsync(errorPacket, remoteEndPoint);
        }
    }

    private static string ExtractString(byte[] buffer, int startIndex) {
        int endIndex = Array.IndexOf<byte>(buffer, 0, startIndex);
        if (endIndex == -1) endIndex = buffer.Length;
        return System.Text.Encoding.ASCII.GetString(buffer, startIndex, endIndex - startIndex);
    }

    private async Task LogPxeEvent(string eventType, string ipAddress, string fileName) {
        try {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            dbContext.PxeEvents.Add(new PxeEvent {
                EventType = eventType,
                MacAddress = "unknown",
                IpAddress = ipAddress,
                BootFileName = fileName
            });
            
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to log PXE event");
        }
    }

    public override void Dispose() {
        _tftpSocket?.Close();
        base.Dispose();
    }
}


