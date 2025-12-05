# Mass Suite

[![.NET 10](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)
[![C# 14](https://img.shields.io/badge/C%23-14-blue)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

**Mass Suite** is a unified deployment and automation platform for Windows system administrators and IT professionals. It provides USB bootable media creation, PXE network boot services, workflow automation, and remote agent management.

## Features

| Component | Description |
|-----------|-------------|
| **ProUSB** | Create bootable USB drives from ISO images with UEFI/BIOS support |
| **ProPXEServer** | Network boot server with DHCP, TFTP, and HTTP services |
| **Mass Agent** | Lightweight remote agent for distributed deployments |
| **Mass Dashboard** | Web-based administration portal (Blazor) |
| **Mass CLI** | Command-line interface for automation and scripting |
| **Mass Launcher** | Desktop application (Avalonia) |

## Tech Stack

- **.NET 10** with **C# 14** language features
- **Avalonia UI** for cross-platform desktop
- **Blazor Server** for web dashboard
- **SignalR** for real-time agent communication
- **Entity Framework Core** for data persistence

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows 10/11 or Windows Server 2019+ (for USB and PXE operations)
- Administrator privileges (for hardware operations)

### Build

```bash
git clone https://github.com/masssuite/mass.git
cd mass
dotnet build
```

### Run

```bash
# Desktop Launcher
dotnet run --project src/Mass.Launcher

# Web Dashboard
dotnet run --project src/Mass.Dashboard

# CLI
dotnet run --project src/Mass.CLI -- --help
```

### Test

```bash
dotnet test
```

## Project Structure

```
Mass/
├── src/
│   ├── Mass.Core/           # Core business logic and abstractions
│   ├── Mass.Spec/           # Shared contracts and DTOs
│   ├── Mass.CLI/            # Command-line interface
│   ├── Mass.Launcher/       # Desktop application (Avalonia)
│   ├── Mass.Dashboard/      # Web admin portal (Blazor)
│   ├── Mass.Agent/          # Remote deployment agent
│   ├── Mass.UI.Shared/      # Shared design system
│   ├── ProUSB/              # USB operations engine
│   └── ProPXEServer/        # PXE boot server
├── tests/                   # Unit and integration tests
├── docs/                    # Documentation
└── Directory.Build.props    # Centralized build configuration
```

## Configuration

Environment variables:

| Variable | Description | Default |
|----------|-------------|---------|
| `MASS_DASHBOARD_URL` | Dashboard server URL | `http://localhost:5000` |
| `MASS_AGENT_ID` | Unique agent identifier | Auto-generated |
| `MASS_LOG_LEVEL` | Logging verbosity | `Information` |

## Documentation

- [ARCHITECTURE.md](docs/ARCHITECTURE.md) - System design and patterns
- [API.md](docs/API.md) - HTTP API reference
- [SECURITY.md](docs/SECURITY.md) - Security considerations
- [OPERATIONS.md](docs/OPERATIONS.md) - Deployment and operations
- [CONTRIBUTING.md](CONTRIBUTING.md) - Development guidelines

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- [GitHub Issues](https://github.com/masssuite/mass/issues)
- [Discussions](https://github.com/masssuite/mass/discussions)
