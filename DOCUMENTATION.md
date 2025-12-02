# Mass - Complete Documentation

**Mass** is a proprietary deployment and media creation suite combining professional-grade tools for USB media creation and network booting.

## Table of Contents
1. [Getting Started](#getting-started)
2. [Architecture](#architecture)
3. [Components](#components)
4. [Configuration](#configuration)
5. [Deployment](#deployment)
6. [API Reference](#api-reference)
7. [Security](#security)
8. [Troubleshooting](#troubleshooting)

---

## Getting Started

### Prerequisites
- .NET 10.0 SDK or later
- Windows 10/11 or Windows Server 2019+
- Administrator privileges (for USB operations and network ports)

### Quick Start

**Building the Solution:**
```bash
cd C:\Path\To\ProUSBMediaSuite
dotnet build Mass.sln
```

**Running ProUSBMediaSuite (UI):**
```bash
cd src\UI
dotnet run
```

**Running ProPXEServer (Backend):**
```bash
cd ProPXEServer\ProPXEServer.API
dotnet run
```

---

## Architecture

### Solution Structure
```
Mass/
├── Mass.sln                    # Main solution file
├── Mass.Core/                  # Shared core functionality
├── Mass.CLI/                   # Command-line interface
├── Mass.Launcher/              # Main application launcher
├── ProUSB/                     # USB media creation tool
├── ProPXEServer/               # Backend directory
│   ├── ProPXEServer.API/
│   └── ProPXEServer.Client/
├── tests/                      # Unit and Integration Tests
└── README.md
```

### Technology Stack
- **Frontend**: Avalonia (ProUSBMediaSuite), Blazor WebAssembly (ProPXEServer Admin)
- **Backend**: ASP.NET Core 10.0, Entity Framework Core
- **Database**: SQLite
- **Authentication**: JWT Bearer tokens
- **Payment**: Stripe integration
- **Network**: UDP sockets (DHCP, TFTP)

---

## Components

### 1. ProUSBMediaSuite
Professional USB bootable media creation tool.

**Features:**
- Multi-format ISO support (Windows, Linux, macOS)
- Advanced partitioning (GPT/MBR, FAT32/NTFS)
- Large file handling (WIM splitting for FAT32)
- Post-burn verification
- Disk formatting and sanitization

**Key Classes:**
- `MainViewModel` - Main UI logic and orchestration
- `NativeDiskFormatter` - Low-level disk operations
- `IsoMediaWriter` - ISO to USB burning engine

### 2. ProPXEServer
Network PXE boot server with web management interface.

**Features:**
- **Dual Mode DHCP**: Standard DHCP (port 67) and ProxyDHCP (port 4011)
- **Architecture Detection**: Automatically serves correct boot files for BIOS/UEFI/ARM64
- **TFTP Server**: Serves boot files over TFTP (port 69)
- **Web API**: Manage boot files, view logs, configure settings
- **Security**: IP whitelisting, MAC validation, rate limiting
- **Subscription**: Stripe-powered monthly billing

**Key Services:**
- `DhcpService` - DHCP and ProxyDHCP server
- `TftpServerService` - TFTP file server
- `HttpBootService` - HTTP boot file serving
- `AuthController` - User authentication
- `BootFilesController` - Boot file management

---

## Configuration

### ProPXEServer Configuration
**Location**: `ProPXEServer/ProPXEServer.API/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=ProPXEServer.db"
  },
  "ProPXEServer": {
    "AdvertisedIP": "192.168.1.100",
    "BootFilesDirectory": "BootFiles",
    "DhcpListenPort": 67,
    "ProxyDhcpListenPort": 4011,
    "TftpListenPort": 69
  },
  "TftpSettings": {
    "RootPath": "wwwroot/pxe",
    "Port": 69,
    "BlockSize": 512,
    "MaxRetries": 5
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key-here",
    "Issuer": "ProPXEServer",
    "Audience": "MassBootClient",
    "ExpiryMinutes": 60
  },
  "Security": {
    "IpWhitelist": [],
    "IpBlacklist": [],
    "EnableRateLimiting": true,
    "MaxRequestsPerMinute": 100
  },
  "Stripe": {
    "SecretKey": "sk_live_YOUR_KEY",
    "WebhookSecret": "whsec_YOUR_SECRET",
    "MonthlyPriceId": "price_YOUR_PRICE_ID",
    "MonthlyAmount": 2000
  }
}
```

### Boot Files Configuration
Boot files must be organized in architecture-specific directories:
```
wwwroot/pxe/
├── bios/
│   └── mass.kpxe
├── uefi/
│   └── mass.efi
└── arm64/
    └── mass-arm64.efi
```

---

## Deployment

### Development Deployment

1. **Set up database:**
```bash
cd ProPXEServer/ProPXEServer.API
dotnet ef database update
```

2. **Configure settings:**
   - Update `appsettings.json` with your IP, JWT secret, and Stripe keys

3. **Run services:**
```bash
# Terminal 1 - API
dotnet run --project ProPXEServer/ProPXEServer.API

# Terminal 2 - Client (optional)
dotnet run --project ProPXEServer/ProPXEServer.Client
```

### Production Deployment

1. **Publish the application:**
```bash
dotnet publish Mass.sln -c Release -o ./publish
```

2. **Configure firewall:**
   - Allow UDP ports: 67, 69, 4011
   - Allow TCP ports: 80, 443 (for API)

3. **Run as Windows Service:**
```bash
sc create ProPXEServer binPath="C:\Path\To\publish\ProPXEServer.API.exe"
sc start ProPXEServer
```

4. **Configure reverse proxy (IIS/Nginx):**
   - Proxy API requests to Kestrel
   - Serve static files from wwwroot

---

## API Reference

### Authentication

**POST /api/auth/register**
```json
{
  "email": "user@example.com",
  "password": "SecurePass123!"
}
```

**POST /api/auth/login**
```json
{
  "email": "user@example.com",
  "password": "SecurePass123!"
}
```
Returns: JWT token

### Boot Files Management

**GET /api/bootfiles**
- Returns list of all boot files

**POST /api/bootfiles/upload**
- Uploads a boot file (multipart/form-data)
- Automatically categorizes by architecture

**GET /api/bootfiles/download/{filename}**
- Downloads a boot file

**DELETE /api/bootfiles/{filename}**
- Deletes a boot file

### Subscription

**POST /api/subscription/create-checkout**
- Creates Stripe checkout session

**POST /api/subscription/webhook**
- Stripe webhook endpoint for payment events

---

## Security

### Network Security
- **IP Whitelisting**: Configure allowed IPs in `Security.IpWhitelist`
- **MAC Validation**: Validates MAC addresses for DHCP/PXE requests
- **Rate Limiting**: 100 requests/minute per IP (configurable)

### API Security
- **JWT Authentication**: All protected endpoints require bearer token
- **HTTPS**: Use reverse proxy (IIS/Nginx) for SSL termination
- **CORS**: Configure allowed origins in `Program.cs`

### Best Practices
1. Change default JWT secret key
2. Use strong passwords for admin accounts
3. Keep Stripe keys secure (use environment variables)
4. Regularly update boot files
5. Monitor PXE event logs for suspicious activity

---

## Troubleshooting

### Common Issues

**1. Build Errors**
```bash
# Clean and rebuild
dotnet clean Mass.sln
dotnet build Mass.sln
```

**2. DHCP Server Not Starting**
- Ensure administrator privileges
- Check port 67 is not in use: `netstat -an | findstr :67`
- Verify firewall allows UDP 67

**3. TFTP Files Not Serving**
- Verify files exist in `wwwroot/pxe/{bios,uefi,arm64}/`
- Check TFTP port 69 is open
- Review logs in `ProPXEServer.db` PxeEvents table

**4. Client Not Booting**
- Verify network boot is enabled in BIOS
- Check client received IP from DHCP
- Ensure correct boot file for architecture
- Review DHCP logs for client MAC address

**5. Authentication Fails**
- Verify JWT secret matches across all configs
- Check token expiry (default 60 minutes)
- Ensure database is initialized

### Logging
Logs are stored in:
- **Database**: `ProPXEServer.db` → `PxeEvents` table
- **Console**: Real-time output (when running with `dotnet run`)
- **Application Insights**: Configure in `appsettings.json` for production

---

## Support
This is proprietary software. For support, contact the system administrator.

## License
**Mass** is licensed under the MIT License. See [LICENSE](LICENSE) file for details.
