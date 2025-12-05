<div align="center">

# 🚀 Mass Suite

**Unified Deployment & Automation Platform for Windows**

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![C# 14](https://img.shields.io/badge/C%23-14-239120?style=for-the-badge&logo=csharp)](https://docs.microsoft.com/dotnet/csharp/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow?style=for-the-badge)](LICENSE)
[![Build](https://img.shields.io/github/actions/workflow/status/masssuite/mass/ci.yml?style=for-the-badge&logo=github)](https://github.com/masssuite/mass/actions)

[Features](#-features) •
[Quick Start](#-quick-start) •
[Documentation](#-documentation) •
[Contributing](#-contributing)

</div>

---

## ✨ Features

<table>
<tr>
<td width="50%">

### 💾 USB Burner
Create bootable USB drives from ISO images with full UEFI/BIOS support, Windows 11 bypass injection, and multi-drive parallel burning.

</td>
<td width="50%">

### 🌐 PXE Server
Network boot infrastructure with built-in DHCP, TFTP, and HTTP services. Boot Windows, Linux, or recovery tools over the network.

</td>
</tr>
<tr>
<td width="50%">

### ⚙️ Workflow Engine
YAML-based automation for deployment tasks. Chain operations, define dependencies, and execute complex workflows with a single command.

</td>
<td width="50%">

### 📡 Remote Agents
Deploy lightweight agents to remote machines. Execute workflows, collect telemetry, and manage devices from a central dashboard.

</td>
</tr>
<tr>
<td width="50%">

### 🔌 Plugin System
Extend functionality with a modular plugin architecture. Build custom integrations or install from the marketplace.

</td>
<td width="50%">

### 📊 Web Dashboard
Real-time monitoring and management through a modern Blazor-based admin portal. Track jobs, view telemetry, and manage your fleet.

</td>
</tr>
</table>

---

## 🚀 Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows 10/11 or Windows Server 2019+
- Administrator privileges (for hardware operations)

### Installation

```bash
# Clone the repository
git clone https://github.com/masssuite/mass.git
cd mass

# Build the solution
dotnet build

# Run tests
dotnet test
```

### Run

```bash
# Desktop Application
dotnet run --project src/Mass.Launcher

# Web Dashboard
dotnet run --project src/Mass.Dashboard

# Command Line
dotnet run --project src/Mass.CLI -- --help
```

---

## 📁 Project Structure

```
Mass/
├── 📂 src/
│   ├── Mass.Core/           # Core business logic
│   ├── Mass.Spec/           # Shared contracts & DTOs
│   ├── Mass.CLI/            # Command-line interface
│   ├── Mass.Launcher/       # Desktop app (Avalonia)
│   ├── Mass.Dashboard/      # Web portal (Blazor)
│   ├── Mass.Agent/          # Remote deployment agent
│   ├── ProUSB/              # USB operations engine
│   └── ProPXEServer/        # PXE boot server
├── 📂 tests/                # Unit & integration tests
├── 📂 docs/                 # Documentation
└── 📄 Mass.sln              # Solution file
```

---

## ⚙️ Configuration

| Environment Variable | Description | Default |
|---------------------|-------------|---------|
| `MASS_LOG_LEVEL` | Logging verbosity | `Information` |
| `MASS_DASHBOARD_URL` | Dashboard server URL | `http://localhost:5000` |
| `MASS_AGENT_ID` | Unique agent identifier | Auto-generated |

---

## 📚 Documentation

| Document | Description |
|----------|-------------|
| [Architecture](docs/ARCHITECTURE.md) | System design and patterns |
| [API Reference](docs/API.md) | REST API documentation |
| [Security](docs/SECURITY.md) | Security considerations |
| [Operations](docs/OPERATIONS.md) | Deployment and monitoring |

---

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

```bash
# Create a feature branch
git checkout -b feature/amazing-feature

# Make your changes and test
dotnet test

# Commit and push
git commit -m "Add amazing feature"
git push origin feature/amazing-feature
```

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

<div align="center">

Made with ❤️ by the Mass Suite Team

[Report Bug](https://github.com/masssuite/mass/issues) •
[Request Feature](https://github.com/masssuite/mass/issues)

</div>
