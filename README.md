# 🚀 Mass Suite

<div align="center">

![Mass Suite Banner](https://capsule-render.vercel.app/api?type=waving&color=0:512BD4,100:239120&height=300&section=header&text=Mass%20Suite&fontSize=90&animation=fadeIn&fontAlignY=38&desc=The%20Gold%20Standard%20for%20IT%20Deployment%20%26%20Automation&descAlignY=55&descAlign=50)

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![C# 14](https://img.shields.io/badge/C%23-14-239120?style=for-the-badge&logo=csharp)](https://docs.microsoft.com/dotnet/csharp/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow?style=for-the-badge)](LICENSE)
[![Build Status](https://img.shields.io/badge/Build-Passing-success?style=for-the-badge)](https://github.com/tomytate/Mass/actions)
[![Tests](https://img.shields.io/badge/Tests-80%2F83%20Passing-success?style=for-the-badge)](tests/)

**[Features](#-features) • [Quick Start](#-quick-start) • [Architecture](#-architecture) • [Contributing](#-contributing)**

</div>

---

## 💎 The Gold Standard in IT Management

**Mass Suite** is a unified, premium ecosystem designed to replace the fragmented "tool belt" of IT professionals. It consolidates USB burning, Network Boot (PXE), and Remote Monitoring into a single, cohesive platform suitable for enterprise environments.

No more switching between Rufus, Tftpd32, and random scripts. Mass Suite does it all, with style.

### 🌟 Why Mass Suite?
- **Unified**: One dashboard for all your deployment needs.
- **Modern**: Built on the bleeding edge of **.NET 10** and **Avalonia UI**.
- **Extensible**: A robust Plugin and Scripting (Lua) engine.
- **Beautiful**: A "Gold Standard" UI that looks as good as it performs.

---

## ✨ Features

<table>
<tr>
<td width="50%">

### 💾 ProUSB
**The Ultimate Bootable Media Tool**
- **ISO to USB**: Burn Windows/Linux ISOs with zero friction.
- **Parallel Burning**: Write to multiple drives simultaneously.
- **Smart Formatting**: Auto-handles UEFI/BIOS and large files (Split WIM).
- **Validation**: Verifies integrity after every write.

</td>
<td width="50%">

### 🌐 ProPXEServer
**Network Boot Reimagined**
- **Zero-Touch Deployment**: Boot machines over LAN seamlessly.
- **Integrated Stack**: Built-in DHCP, TFTP, and HTTP servers. No external dependencies.
- **Secure**: Authentication and Policy enforcement for network boots.
- **Fast**: Optimized file transfer protocols (HTTP Boot support).

</td>
</tr>
<tr>
<td width="50%">

### 🤖 Mass.Agent
**Intelligent Endpoint Monitoring**
- **Real-Time Telemetry**: Monitor CPU, RAM, and Uptime instantly.
- **Command & Control**: Execute remote commands (PowerShell/Bash) via SignalR.
- **Workflow Engine**: Run complex automation sequences (e.g., "Install Office -> Join Domain -> Reboot").

</td>
<td width="50%">

### 🧩 Extensibility
**Built for Developers**
- **Plugin System**: Add new features without recompiling the core.
- **Lua Scripting**: Customize logic with lightweight scripts.
- **OpenTelemetry**: Enterprise-grade observability and tracing built-in.

</td>
</tr>
</table>

---

## 🏗 Architecture

Mass Suite employs a modular **Client-Server-Agent** topology:

```mermaid
graph TD
    User[Admin User] -->|Manages| Launcher["Mass.Launcher (Desktop)"]
    User -->|Views| Dashboard["Mass.Dashboard (Blazor Server)"]
    
    Launcher -->|Controls| ProUSB["ProUSB Engine"]
    Launcher -->|Configures| PXE[ProPXEServer]
    
    PXE -->|Boots| ClientPC["Client Machine (Bare Metal)"]
    
    subgraph "Managed Network"
        ClientPC -->|Installs| Agent[Mass.Agent]
        Agent -->|Reports Telemetry| Dashboard
    end
```

---

## 🚀 Quick Start

### Prerequisites
- **OS**: Windows 10/11 or Windows Server.
- **Runtime**: [.NET 10 Runtime](https://dotnet.microsoft.com/download) (or SDK to build).
- **Privileges**: Administrator rights are required for USB formatting and Port binding.

### 📦 Installation

#### Option A: Build from Source
```bash
git clone https://github.com/tomytate/Mass.git
cd Mass
dotnet build
```

#### Option B: Run the Launcher
```bash
cd src/Mass.Launcher
dotnet run
```

### ⚡ Common Commands

| Component | Command | Description |
|-----------|---------|-------------|
| **Launcher** | `dotnet run --project src/Mass.Launcher` | Starts the main Desktop UI. |
| **CLI** | `dotnet run --project src/Mass.CLI` | Runs the command-line tool. |
| **Agent** | `dotnet run --project src/Mass.Agent` | Starts the background agent service. |
| **Dashboard** | `dotnet run --project src/Mass.Dashboard` | Starts the Blazor Server web dashboard. |

---

## 🤝 Contributing

We welcome contributions! Please read our [Contributing Guide](CONTRIBUTING.md) to get started.

### Project Structure
```
src/
├── Mass.Core/          # Shared business logic and abstractions
├── Mass.Spec/          # Shared contracts, DTOs and interfaces
├── Mass.Launcher/      # Main Avalonia UI desktop entry point
├── Mass.Dashboard/     # Blazor Server web admin portal
├── Mass.Agent/         # Remote deployment agent (SignalR)
├── Mass.CLI/           # Command-line interface
├── Mass.UI.Shared/     # Unified design system components
├── ProUSB/             # USB burning engine
└── ProPXEServer/       # Network boot server API and logic
```

---

## 📄 License
Released under the [MIT License](LICENSE).

<div align="center">
    <b>Built with ❤️ by Tomy Tate</b>
</div>
