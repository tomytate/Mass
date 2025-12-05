# Developer Setup Guide

Welcome to the Mass Suite development team! This guide will help you set up your environment and get started.

## Prerequisites

- **.NET 10.0 SDK**: Required to build and run the project.
- **Git**: For version control.
- **VS Code** (Recommended): With C# Dev Kit extension.

## Quick Start (Bootstrap)

We provide a bootstrap script to automate the setup process. This script will:
1. Restore NuGet packages.
2. Create a local NuGet feed for `Mass.Spec`.
3. Build the solution.
4. Create sample configuration files.

### Windows (PowerShell)
```powershell
./scripts/dev/bootstrap.ps1
```

### Linux / macOS
```bash
chmod +x scripts/dev/bootstrap.sh
./scripts/dev/bootstrap.sh
```

## Running the Application

### VS Code
1. Open the Debug view (`Ctrl+Shift+D`).
2. Select **Launch Mass.Launcher**.
3. Press `F5`.

### Command Line
```bash
dotnet run --project src/Mass.Launcher
```

## Running Tests

### VS Code
Use the Test Explorer to run unit and integration tests.

### Command Line
```bash
dotnet test Mass.sln
```

## Project Structure

- `src/Mass.Spec`: Core contracts and interfaces (Shared).
- `src/Mass.Core`: Core logic and services.
- `src/Mass.Launcher`: GUI application (Avalonia).
- `src/Mass.CLI`: Command-line interface.
- `src/ProUSB`: USB burning module.
- `src/ProPXEServer`: PXE server subsystem.
- `tests/`: Unit and integration tests.

## Contributing

Please read [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines on code style, versioning, and pull requests.
