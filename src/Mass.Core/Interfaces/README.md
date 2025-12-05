# Mass.Core Public Interfaces

This directory contains the **ONLY** public facades that Mass Suite consumers should depend on.

## Rules

1. **Depend ONLY on these interfaces** - Do not reference internal Mass.Core types directly
2. **Use Mass.Spec types** - All parameters and return types come from `Mass.Spec` contracts
3. **Implementations are internal** - Only interfaces are part of the public API surface

## Available Interfaces

### Core Operations

- **`IUsbBurner`** - USB device burning and verification operations
- **`IPxeManager`** - PXE boot file upload, listing, and deletion
- **`IWorkflowExecutor`** - Workflow execution and validation

### Infrastructure

- **`IConfigurationService`** - Application configuration management
- **`ILogService`** - Structured logging using Mass.Spec LogEntry

## Usage Example

```csharp
using Mass.Core.Interfaces;
using Mass.Spec.Contracts.Usb;

public class MyApp
{
    private readonly IUsbBurner _usbBurner;
    
    public MyApp(IUsbBurner usbBurner)
    {
        _usbBurner = usbBurner;
    }
    
    public async Task BurnImageAsync()
    {
        var job = new UsbJob
        {
            ImagePath = "ubuntu.iso",
            TargetDeviceId = "PhysicalDrive1"
        };
        
        var result = await _usbBurner.BurnAsync(job);
        Console.WriteLine(result.IsSuccess ? "Success!" : "Failed");
    }
}
```

## Dependency Injection

Register these interfaces in your DI container:

```csharp
services.AddSingleton<IUsbBurner, UsbBurnerImplementation>();
services.AddSingleton<IPxeManager, PxeManagerImplementation>();
services.AddSingleton<IWorkflowExecutor, WorkflowExecutorImplementation>();
services.AddSingleton<IConfigurationService, ConfigurationServiceImplementation>();
services.AddSingleton<ILogService, LogServiceImplementation>();
```

## Migration from Old Interfaces

If you were using old interfaces from:
- `Mass.Core.Devices.IUsbBurner` → Now `Mass.Core.Interfaces.IUsbBurner`
- `Mass.Core.Abstractions.IConfigurationService` → Now `Mass.Core.Interfaces.IConfigurationService`
- `Mass.Core.Logging.ILogService` → Now `Mass.Core.Interfaces.ILogService`

The old interfaces will be marked as `[Obsolete]` and should be replaced with the unified versions.
