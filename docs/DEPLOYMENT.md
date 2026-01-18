# Mass Suite Deployment Guide

## Prerequisites

- **One of the following OSs**:
  - Windows Server 2022 / Windows 11
  - Ubuntu 22.04 LTS / Debian 12
  - macOS Sonoma (for development/testing)
- **.NET 10 Runtime** installed
- **Redis** (Optional, recommended for production caching)

## Infrastructure Requirements

- **CPU**: 2+ Cores
- **RAM**: 4GB Minimum
- **Storage**: 50GB+ SSD (for boot images)

## Deployment Steps

### 1. Build the Application

```bash
dotnet publish src/ProPXEServer/ProPXEServer.API/ProPXEServer.API.csproj -c Release -o ./publish/Server
dotnet publish src/Mass.Dashboard/Mass.Dashboard.csproj -c Release -o ./publish/Dashboard
```

### 2. Configure Environment Variables

Set the following environment variables (see `SECRETS.md` for details):

```bash
export MASS_JWTSETTINGS__SECRETKEY="<32-char-random-string>"
export MASS_CONNECTIONSTRINGS__DEFAULTCONNECTION="Data Source=/var/lib/mass/mass.db"
export ASPNETCORE_ENVIRONMENT="Production"
```

### 3. Service Setup (Systemd - Linux)

Create `/etc/systemd/system/mass-server.service`:

```ini
[Unit]
Description=Mass Suite PXE Server
After=network.target

[Service]
WorkingDirectory=/opt/mass/Server
ExecStart=/usr/bin/dotnet /opt/mass/Server/ProPXEServer.API.dll
Restart=always
# Restart service after 10 seconds if the dotnet service crashes:
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=mass-server
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=MASS_JWTSETTINGS__SECRETKEY=...

[Install]
WantedBy=multi-user.target
```

### 4. Reverse Proxy (Nginx)

```nginx
server {
    listen 80;
    server_name mass.yourdomain.com;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

## Update Procedure

1. `systemctl stop mass-server`
2. Backup `mass.db` and `credentials.dat`
3. Replace files in `/opt/mass/Server`
4. `systemctl start mass-server`
5. Check logs: `journalctl -u mass-server -f`
