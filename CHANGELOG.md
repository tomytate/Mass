# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-12-05

### Added
- **Mass.Core** - Core business logic with workflow engine, plugin system, and SaaS infrastructure
- **Mass.Spec** - Shared contracts and DTOs for cross-project communication
- **Mass.CLI** - Command-line interface for automation and scripting
- **Mass.Launcher** - Desktop application built with Avalonia UI
- **Mass.Dashboard** - Web admin portal built with Blazor Server
- **Mass.Agent** - Remote deployment agent with SignalR connectivity
- **ProUSB** - USB bootable media creation engine with UEFI/BIOS support
- **ProPXEServer** - Network boot server with DHCP, TFTP, and HTTP services

### Technical
- Built with .NET 10 and C# 14
- Implemented unified design system (`Mass.UI.Shared`)
- Added multi-tenancy support for SaaS deployment
- Created plugin marketplace infrastructure
- Comprehensive test coverage (80+ tests)

### Documentation
- Architecture documentation
- API reference
- Security guidelines
- Operations manual
- Contributing guide

[1.0.0]: https://github.com/tomytate/Mass/releases/tag/v1.0.0
