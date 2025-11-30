using System.Net;
using System.Net.Sockets;
using ProPXEServer.API.Data;
using ProPXEServer.API.Security;

namespace ProPXEServer.API.Services;

public class DhcpService(
    ILogger<DhcpService> logger,
    IConfiguration configuration,
    IServiceProvider serviceProvider) : BackgroundService {
    
    private readonly string _advertisedIp = configuration["ProPXEServer:AdvertisedIP"] ?? "192.168.1.100";
    private UdpClient? _dhcpSocket;
    private UdpClient? _proxyDhcpSocket;

    public override void Dispose() {
        _dhcpSocket?.Close();
        _proxyDhcpSocket?.Close();
        base.Dispose();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        logger.LogInformation("DHCP/ProxyDHCP service starting on ports 67 and 4011");
        
        try {
            _dhcpSocket = new UdpClient(new IPEndPoint(IPAddress.Any, 67)) {
                EnableBroadcast = true
            };
            
            _proxyDhcpSocket = new UdpClient(new IPEndPoint(IPAddress.Any, 4011));
            
            var dhcpTask = ListenDhcpAsync(stoppingToken);
            var proxyDhcpTask = ListenProxyDhcpAsync(stoppingToken);
            
            await Task.WhenAll(dhcpTask, proxyDhcpTask);
        }
        catch (Exception ex) {
            logger.LogError(ex, "DHCP service failed to start");
            throw;
        }
    }

    private async Task ListenDhcpAsync(CancellationToken ct) {
        logger.LogInformation("Listening for DHCP requests on port 67");
        
        while (!ct.IsCancellationRequested && _dhcpSocket != null) {
            try {
                var result = await _dhcpSocket.ReceiveAsync(ct);
                _ = Task.Run(() => HandleDhcpPacket(result.Buffer, result.RemoteEndPoint), ct);
            }
            catch (OperationCanceledException) {
                break;
            }
            catch (Exception ex) {
                logger.LogError(ex, "Error receiving DHCP packet");
            }
        }
    }

    private async Task ListenProxyDhcpAsync(CancellationToken ct) {
        logger.LogInformation("Listening for ProxyDHCP requests on port 4011");
        
        while (!ct.IsCancellationRequested && _proxyDhcpSocket != null) {
            try {
                var result = await _proxyDhcpSocket.ReceiveAsync(ct);
                _ = Task.Run(() => HandleProxyDhcpPacket(result.Buffer, result.RemoteEndPoint), ct);
            }
            catch (OperationCanceledException) {
                break;
            }
            catch (Exception ex) {
                logger.LogError(ex, "Error receiving ProxyDHCP packet");
            }
        }
    }

    private async Task HandleDhcpPacket(byte[] packet, IPEndPoint remoteEndPoint) {
        try {
            if (packet.Length < 236) return;
            
            var macAddress = BitConverter.ToString(packet[28..34]).Replace("-", ":");
            
            if (!SecurityConfiguration.IsValidMacAddress(macAddress)) {
                logger.LogWarning("Invalid MAC address received: {Mac}", macAddress);
                return;
            }

            if (!SecurityConfiguration.IsIpAllowed(remoteEndPoint.Address.ToString(), configuration)) {
                logger.LogWarning("Blocked request from unauthorized IP: {IP}", remoteEndPoint.Address);
                return;
            }

            var messageType = GetDhcpMessageType(packet);
            
            logger.LogInformation("DHCP {MessageType} from {Mac} at {IP}", 
                messageType, macAddress, remoteEndPoint.Address);
            
            await LogPxeEvent("DHCP_" + messageType, macAddress, remoteEndPoint.Address.ToString());
            
            if (messageType == "DISCOVER") {
                await SendDhcpOffer(packet, remoteEndPoint, macAddress);
            }
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error handling DHCP packet");
        }
    }

    private async Task HandleProxyDhcpPacket(byte[] packet, IPEndPoint remoteEndPoint) {
        try {
            if (packet.Length < 236) return;
            
            var macAddress = BitConverter.ToString(packet[28..34]).Replace("-", ":");

            if (!SecurityConfiguration.IsValidMacAddress(macAddress)) {
                logger.LogWarning("Invalid MAC address received: {Mac}", macAddress);
                return;
            }

            if (!SecurityConfiguration.IsIpAllowed(remoteEndPoint.Address.ToString(), configuration)) {
                logger.LogWarning("Blocked ProxyDHCP request from unauthorized IP: {IP}", remoteEndPoint.Address);
                return;
            }

            var arch = GetClientArchitecture(packet);
            
            logger.LogInformation("ProxyDHCP request from {Mac}, arch: {Arch}", macAddress, arch);
            
            await LogPxeEvent("PROXY_DHCP", macAddress, remoteEndPoint.Address.ToString(), arch.ToString("X4"));
            await SendProxyDhcpOffer(packet, remoteEndPoint, macAddress, arch);
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error handling ProxyDHCP packet");
        }
    }

    private async Task SendDhcpOffer(byte[] request, IPEndPoint remoteEndPoint, string macAddress) {
        var response = new byte[548];
        Array.Copy(request, 0, response, 0, 236);
        
        response[0] = 0x02;
        
        var serverIp = IPAddress.Parse(_advertisedIp).GetAddressBytes();
        Array.Copy(serverIp, 0, response, 20, 4);
        
        response[236] = 0x63;
        response[237] = 0x82;
        response[238] = 0x53;
        response[239] = 0x63;
        
        int offset = 240;
        offset = AddDhcpOption(response, offset, 53, [(byte)2]);
        offset = AddDhcpOption(response, offset, 54, serverIp);
        offset = AddDhcpOption(response, offset, 60, "PXEClient"u8.ToArray());
        response[offset++] = 0xFF;
        
        if (_dhcpSocket != null) {
            await _dhcpSocket.SendAsync(response.AsMemory(0, offset), 
                new IPEndPoint(IPAddress.Broadcast, 68));
        }
        
        logger.LogInformation("Sent DHCP OFFER to {Mac}", macAddress);
    }

    private async Task SendProxyDhcpOffer(byte[] request, IPEndPoint remoteEndPoint, string macAddress, int arch) {
        var response = new byte[548];
        Array.Copy(request, 0, response, 0, 236);
        
        response[0] = 0x02;
        
        var serverIp = IPAddress.Parse(_advertisedIp).GetAddressBytes();
        Array.Copy(serverIp, 0, response, 20, 4);
        
        response[236] = 0x63;
        response[237] = 0x82;
        response[238] = 0x53;
        response[239] = 0x63;
        
        var bootFile = GetBootFileForArchitecture(arch);
        Array.Copy(System.Text.Encoding.ASCII.GetBytes(bootFile), 0, response, 108, 
            Math.Min(bootFile.Length, 128));
        
        int offset = 240;
        offset = AddDhcpOption(response, offset, 53, [(byte)5]);
        offset = AddDhcpOption(response, offset, 54, serverIp);
        offset = AddDhcpOption(response, offset, 60, "PXEClient"u8.ToArray());
        
        byte[] pxeOptions = [0x08, 0x00, 0x00, 0x00];
        offset = AddDhcpOption(response, offset, 43, pxeOptions);
        response[offset++] = 0xFF;
        
        if (_proxyDhcpSocket != null) {
            await _proxyDhcpSocket.SendAsync(response.AsMemory(0, offset), remoteEndPoint);
        }
        
        logger.LogInformation("Sent ProxyDHCP offer to {Mac} with boot file: {BootFile}", 
            macAddress, bootFile);
    }

    private static int AddDhcpOption(byte[] buffer, int offset, byte option, byte[] data) {
        buffer[offset++] = option;
        buffer[offset++] = (byte)data.Length;
        Array.Copy(data, 0, buffer, offset, data.Length);
        return offset + data.Length;
    }

    private static string GetDhcpMessageType(byte[] packet) {
        for (int i = 240; i < packet.Length - 2; i++) {
            if (packet[i] == 53 && packet[i + 1] == 1) {
                return packet[i + 2] switch {
                    1 => "DISCOVER",
                    3 => "REQUEST",
                    _ => "UNKNOWN"
                };
            }
        }
        return "UNKNOWN";
    }

    private static int GetClientArchitecture(byte[] packet) {
        for (int i = 240; i < packet.Length - 4; i++) {
            if (packet[i] == 93 && packet[i + 1] == 2) {
                return (packet[i + 2] << 8) | packet[i + 3];
            }
        }
        return 0;
    }

    private static string GetBootFileForArchitecture(int arch) => arch switch {
        0x00 => "bios/mass.kpxe",       
        0x06 => "uefi/mass.efi",        
        0x07 => "uefi/mass.efi",        
        0x09 => "uefi/mass.efi",        
        0x0a => "arm64/mass-arm64.efi", 
        0x0b => "arm64/mass-arm64.efi", 
        _ => "bios/mass.kpxe"           
    };

    private async Task LogPxeEvent(string eventType, string macAddress, string ipAddress, string? arch = null) {
        try {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            dbContext.PxeEvents.Add(new PxeEvent {
                EventType = eventType,
                MacAddress = macAddress,
                IpAddress = ipAddress,
                Architecture = arch
            });
            
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to log PXE event");
        }
    }
}

