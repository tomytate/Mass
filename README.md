<div align="center">

# 🚀 Mass

### Professional Deployment & Media Creation Suite

*The ultimate solution for bootable USB creation and network PXE booting*

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com)
[![Version](https://img.shields.io/badge/version-1.0.0-blue)](https://github.com)
[![.NET Version](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows-0078D4)](https://www.microsoft.com/windows)

---

</div>

## 📋 Table of Contents

- [Overview](#-overview)
- [Components](#-components)
- [Features](#-features)
- [Quick Start](#-quick-start)
- [Installation](#-installation)
- [Project Structure](#-project-structure)
- [Documentation](#-documentation)
- [Architecture](#-architecture)
- [Security](#-security)
- [License](#-license)

---

## 🎯 Overview

**Mass** is an enterprise-grade suite that combines powerful deployment tools into one unified solution:

1. **ProUSB** - Advanced bootable USB creation tool
2. **ProPXEServer** - Intelligent network PXE boot server with web management

Whether you're deploying operating systems across a datacenter or creating recovery media for field technicians, Mass provides professional-grade tools in a modern, cohesive platform.

---

## 🧩 Components

### ProUSB

<table>
<tr>
<td width="50%">

**Professional USB Media Creation**

Create bootable USB drives from ISO images with precision and reliability. Built with Avalonia for a modern cross-platform UI experience.

- ✅ Multi-format ISO support (Windows, Linux, macOS)
- ✅ Intelligent partitioning (GPT/MBR auto-detection)
- ✅ Filesystem flexibility (FAT32/NTFS)
- ✅ Large file handling (WIM splitting for FAT32)
- ✅ Post-burn verification
- ✅ Comprehensive disk sanitization
- ✅ Real-time progress tracking

</td>
<td width="50%">

**Supported Operating Systems**

- 🪟 Windows 11/10/Server
- 🐧 Ubuntu/Debian/RHEL/Fedora
- 📀 Custom boot media
- 🛠️ Rescue/Recovery ISOs
- 💿 Any bootable ISO image

**Key Technologies**
- Avalonia UI framework
- MVVM architecture
- Native disk management APIs
- ISO patching engine

</td>
</tr>
</table>

### ProPXEServer

<table>
<tr>
<td width="50%">

**Network PXE Boot Infrastructure**

Deploy a complete PXE boot environment with modern web-based management console.

- 🌐 Dual-mode DHCP (Standard + Proxy)
- 🎯 Automatic architecture detection (BIOS/UEFI/ARM64)
- 📡 Integrated TFTP server
- 🔐 Enterprise security (JWT, rate limiting)
- 📊 Real-time boot event logging
- 💳 Optional Stripe subscription integration
- 🖥️ Blazor WebAssembly admin interface

</td>
<td width="50%">

**Boot Support**

| Architecture | Boot File |
|-------------|-----------|
| BIOS (Legacy) | `netboot.xyz.kpxe` |
| UEFI x64 | `netboot.xyz.efi` |
| ARM64 | `netboot.xyz-arm64.efi` |

**Pre-configured with netboot.xyz** providing access to 100+ operating systems right out of the box.

</td>
</tr>
</table>

---

## ✨ Features

### ProUSB

| Feature | Description |
|---------|-------------|
| 🎨 **Modern UI** | Built with Avalonia for sleek, responsive cross-platform interface |
| ⚡ **High Performance** | Optimized burning engine with real-time progress tracking |
| 🔍 **Verification** | Comprehensive post-burn file integrity checking |
| 🗂️ **Smart Detection** | Automatic USB drive enumeration and metadata reading |
| 🛡️ **Safe Operations** | Confirmations, logging, and operational safeguards |
| 📦 **Plugin Architecture** | Integrates seamlessly with Mass.Launcher |

### ProPXEServer

| Feature | Description |
|---------|-------------|
| 🌐 **DHCP Server** | Standard DHCP on port 67 with PXE boot options |
| 🔀 **ProxyDHCP** | Coexists with existing DHCP servers on port 4011 |
| 📁 **TFTP Server** | High-performance boot file delivery over UDP port 69 |
| 🖥️ **Web Interface** | Modern Blazor WebAssembly admin panel |
| 🔐 **JWT Auth** | Secure API access with bearer token authentication |
| 🚦 **Rate Limiting** | Configurable request throttling and IP whitelisting |
| 📝 **Event Logging** | Comprehensive boot event tracking with EF Core |
| 💰 **Subscriptions** | Optional Stripe payment processing integration |
| 🌍 **netboot.xyz** | Pre-deployed with 100+ OS boot options |

---

## 🚀 Quick Start

### Prerequisites

- **.NET 10.0 SDK** (required)
- **Windows 10/11** or **Windows Server 2019+**
- **Administrator privileges** (for disk operations and network services)

### Build the Solution

```bash
# Clone the repository
git clone https://github.com/yourusername/mass.git
cd ProUSBMediaSuite

# Restore dependencies
dotnet restore Mass.sln

# Build all projects
dotnet build Mass.sln
```

### Run Mass.Launcher (Main Application)

```bash
cd src/Mass.Launcher
dotnet run
```

The launcher provides a unified interface to access both ProUSB and ProPXEServer.

### Run ProUSB Standalone

```bash
# ProUSB is a plugin, launched via Mass.Launcher
# Or build as standalone executable
```

### Run ProPXEServer

```bash
cd src/ProPXEServer/ProPXEServer.API
dotnet run
```

> **Note**: ProPXEServer requires administrator privileges for DHCP/TFTP server operations. Access the web interface at `https://localhost:5001`

---

## 📦 Installation

### Development Setup

1. **Clone and restore**
   ```bash
   git clone https://github.com/yourusername/mass.git
   cd ProUSBMediaSuite
   dotnet restore Mass.sln
   ```

2. **Build solution**
   ```bash
   dotnet build Mass.sln
   ```

3. **Initialize database** (ProPXEServer)
   ```bash
   cd src/ProPXEServer/ProPXEServer.API
   dotnet ef database update
   ```

4. **Configure settings**
   - Edit `src/ProPXEServer/ProPXEServer.API/appsettings.json`
   - Set advertised IP, JWT secret, and optional Stripe keys

5. **Run**
   ```bash
   cd src/Mass.Launcher
   dotnet run
   ```

### Production Deployment

```bash
# Publish optimized release build
dotnet publish Mass.sln -c Release -o ./publish

# ProPXEServer can be configured as Windows Service
sc create ProPXEServer binPath="C:\Path\To\publish\ProPXEServer.API.exe"
sc start ProPXEServer
```

---

## 🏗️ Project Structure

```
Mass Solution
├── Mass.Core                 → Shared core functionality
│   ├── Plugin system
│   ├── Shared services
│   └── Common models
│
├── Mass.Launcher             → Main application launcher
│   ├── Plugin discovery
│   ├── Navigation
│   └── Home interface
│
├── Mass.CLI                  → Command-line interface
│
├── Mass.PowerShell           → PowerShell module
│
├── ProUSB                    → USB media creation tool
│   ├── Services              → USB, ISO, Disk, Burn engines
│   ├── ViewModels            → MVVM pattern
│   ├── Views                 → Avalonia UI components
│   ├── Infrastructure        → Disk management, patching
│   └── Domain                → Core business logic
│
├── ProPXEServer              → PXE boot infrastructure
│   ├── ProPXEServer.API      → ASP.NET Core backend
│   ├── ProPXEServer.Client   → Blazor WebAssembly UI
│   └── ProPXEServer.Plugin   → Mass.Launcher integration
│
└── tests                     → Unit and Integration Tests
    ├── Mass.Core.Tests       → Core logic tests
    └── ProUSB.Tests          → USB engine tests
```

### Solution Projects

| Project | Type | Description |
|---------|------|-------------|
| **Mass.Core** | Library | Shared core functionality and plugin system |
| **Mass.Launcher** | Desktop App | Main launcher with plugin architecture |
| **Mass.CLI** | Console App | Command-line interface |
| **Mass.PowerShell** | PowerShell Module | PowerShell cmdlets |
| **ProUSB** | Library/Plugin | USB media creation engine |
| **ProPXEServer.API** | Web API | ASP.NET Core backend |
| **ProPXEServer.Client** | Blazor WASM | Admin web interface |
| **ProPXEServer.Plugin** | Library | Launcher integration |

---

## 📚 Documentation

**Complete documentation is available:**

- 📖 **[DOCUMENTATION.md](DOCUMENTATION.md)** - Architecture, deployment, troubleshooting
- 🔌 **[API.md](API.md)** - REST API reference with examples
- 🤝 **[CONTRIBUTING.md](CONTRIBUTING.md)** - Contribution guidelines

### Quick Links

- [Configuration Guide](DOCUMENTATION.md#configuration)
- [Deployment Guide](DOCUMENTATION.md#deployment)
- [Security Best Practices](DOCUMENTATION.md#security)
- [API Authentication](API.md#authentication-endpoints)
- [Troubleshooting](DOCUMENTATION.md#troubleshooting)

---

## 🏗️ Architecture

### Technology Stack

| Component | Technology |
|-----------|-----------|
| **Desktop UI** | Avalonia (cross-platform XAML) |
| **Web UI** | Blazor WebAssembly |
| **Backend** | ASP.NET Core 10.0 |
| **Database** | SQLite + Entity Framework Core |
| **Authentication** | JWT Bearer Tokens |
| **Payments** | Stripe (optional) |
| **Networking** | UDP Sockets (DHCP/TFTP) |
| **Language** | C# 14 (.NET 10.0) |

### Design Patterns

- **MVVM** - ProUSB UI architecture
- **Plugin System** - Modular component loading
- **Dependency Injection** - Service container throughout
- **Repository Pattern** - Data access abstraction
- **Strategy Pattern** - Multiple burn strategies (FileSystem, WinPE, etc.)

---

## 🔒 Security

Mass implements enterprise-grade security:

### Network Security
- ✅ **IP Whitelisting** - Configurable IP range restrictions
- ✅ **MAC Validation** - Hardware address verification
- ✅ **Rate Limiting** - Configurable request throttling (default: 100 req/min)

### API Security
- ✅ **JWT Authentication** - Secure token-based authentication
- ✅ **HTTPS** - TLS encryption for all API communication
- ✅ **CORS Control** - Restrict cross-origin requests

### Best Practices
1. Use strong JWT secret keys (256-bit minimum)
2. Enable HTTPS with valid certificates in production
3. Regularly rotate API keys and secrets
4. Monitor PXE event logs for suspicious activity
5. Keep boot files updated from trusted sources
6. Run services with minimum required privileges

---

## 🤝 Support

For support, documentation, or contributions:

- 📖 Check [DOCUMENTATION.md](DOCUMENTATION.md) for detailed guides
- 🐛 Report issues via GitHub Issues
- 💬 See [CONTRIBUTING.md](CONTRIBUTING.md) for contribution guidelines

---

## 📄 License

**Mass** is licensed under the MIT License.

Copyright © 2025 Tomy Tolledo

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

---

<div align="center">

### Built with ❤️ for Enterprise Deployment

**Mass** - *Deploy Anywhere, Boot Everywhere*

[Documentation](DOCUMENTATION.md) • [API Reference](API.md) • [Contributing](CONTRIBUTING.md)

</div>
