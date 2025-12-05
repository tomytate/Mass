# Mass Suite Architecture

## Overview

Mass Suite follows a modular, layered architecture designed for extensibility, testability, and separation of concerns.

```
┌─────────────────────────────────────────────────────────────────┐
│                      Presentation Layer                          │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │ Mass.CLI    │  │ Mass.       │  │ Mass.Dashboard (Blazor) │  │
│  │ (Console)   │  │ Launcher    │  │ Mass.Agent (Worker)     │  │
│  │             │  │ (Avalonia)  │  │                         │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘  │
├─────────────────────────────────────────────────────────────────┤
│                      Application Layer                           │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │                     Mass.Core                                ││
│  │  • Workflows (Parser, Validator, Executor)                   ││
│  │  • Plugins (Loader, Lifecycle Manager)                       ││
│  │  • SaaS (Tenancy, Subscriptions, Usage)                     ││
│  │  • Marketplace (Plugin Registry)                             ││
│  └─────────────────────────────────────────────────────────────┘│
├─────────────────────────────────────────────────────────────────┤
│                      Domain Layer                                │
│  ┌─────────────────┐  ┌─────────────────────────────────────┐   │
│  │   Mass.Spec     │  │           Plugin Modules            │   │
│  │   (Contracts)   │  │  ┌─────────┐  ┌─────────────────┐   │   │
│  │                 │  │  │ ProUSB  │  │  ProPXEServer   │   │   │
│  │                 │  │  └─────────┘  └─────────────────┘   │   │
│  └─────────────────┘  └─────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────────────┤
│                    Infrastructure Layer                          │
│  • File System, Disk Management, Native Interop                 │
│  • HTTP Clients, SignalR Hubs, Entity Framework Core            │
│  • Logging (Serilog-style structured logging)                   │
└─────────────────────────────────────────────────────────────────┘
```

## Key Patterns

### Plugin Architecture

Plugins implement `IPlugin` and provide a `PluginManifest`:

```csharp
public interface IPlugin
{
    PluginManifest Manifest { get; }
    void Init(IServiceProvider services);
    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
}
```

### Workflow Engine

YAML-based workflow definitions executed by `WorkflowExecutor`:

```yaml
id: deploy-image
name: Deploy Windows Image
version: 1.0.0
steps:
  - id: format
    name: Format Drive
    type: FormatDrive
    parameters:
      fileSystem: NTFS
  - id: copy
    name: Copy Files
    type: CopyFiles
    dependsOn: [format]
```

### Multi-Tenancy (SaaS)

Tenant-aware data access via `ITenantProvider`:

```csharp
public interface ITenantProvider
{
    string? CurrentTenantId { get; }
    Tenant? CurrentTenant { get; }
    void SetTenant(string tenantId);
}
```

## Data Flow

### USB Burn Operation

```
User → Launcher/CLI → IUsbBurner → BurnEngine → NativeDiskFormatter → Win32 API
                                 ↓
                     Progress → IProgress<BurnProgress> → UI Update
```

### Remote Agent Communication

```
Dashboard ←→ SignalR Hub ←→ Agent Workers
    ↓                           ↓
 Workflow DB              Local Execution
```

## Technology Decisions

| Decision | Rationale |
|----------|-----------|
| .NET 10 / C# 14 | Latest LTS with modern language features |
| Blazor Server | Real-time UI, server-side rendering, SignalR integration |
| Avalonia | True cross-platform desktop (Windows, Linux, macOS) |
| YAML workflows | Human-readable, version-controllable automation |
| Plugin architecture | Extensibility without core modifications |

## Security Boundaries

- **Elevation**: Hardware write operations require admin privileges
- **Tenant Isolation**: SaaS data partitioned by tenant ID
- **Agent Authentication**: SignalR connections authenticated via token
- **API Versioning**: Breaking changes isolated to major versions

## Author

Developed by [Tomy Tolledo](https://github.com/tomytate)
