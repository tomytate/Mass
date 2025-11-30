using Mass.Core.Abstractions;
using Mass.Core.Plugins;
using Microsoft.Extensions.DependencyInjection;
using ProUSB.Domain.Drivers;
using ProUSB.Domain.Services;
using ProUSB.Infrastructure;
using ProUSB.Infrastructure.Drivers.Windows;
using ProUSB.Infrastructure.DiskManagement;
using ProUSB.Services.Burn;
using ProUSB.Services.Burn.Strategies;
using ProUSB.Services.Security;
using ProUSB.Services.Iso;
using ProUSB.Services.Logging;
using ProUSB.Services.Verification;
using ProUSB.Services.Diagnostics;
using ProUSB.Services.IsoCreation;
using ProUSB.Services.Profiles;
using ProUSB.Services.PxeBoot;
using ProUSB.Services.IsoDownload;
using ProUSB.UI.ViewModels;

namespace ProUSB;

public class ProUsbModule : IModule
{
    public PluginManifest Manifest => new()
    {
        Id = "prousb",
        Name = "ProUSB",
        Version = "1.0.0",
        Description = "USB bootable media creator",
        Author = "Mass Suite Team",
        Icon = "💾",
        EntryAssembly = "ProUSB.dll",
        EntryType = "ProUSB.ProUsbModule",
        Enabled = true
    };

    public void RegisterServices(IServiceCollection s)
    {
        s.AddSingleton<PortablePathManager>();
        s.AddSingleton<FileLogger>();
        s.AddSingleton<IDriverFactory, WindowsDriverFactory>();
        s.AddSingleton<NativeDiskFormatter>();
        s.AddSingleton<ISafetyGuard, StandardSafetyGuard>();
        s.AddSingleton<IsoIntegrityVerifier>();
        s.AddSingleton<IBurnStrategy, RawPipelinedWriteStrategy>();
        s.AddSingleton<IBurnStrategy, FileSystemDeploymentStrategy>();
        s.AddSingleton<ParallelBurnService>();
        s.AddSingleton<MultiDeviceBurnOrchestrator>();
        s.AddSingleton<BootVerificationService>();
        s.AddSingleton<SmartHealthChecker>();
        s.AddSingleton<IsoCreationService>();
        s.AddSingleton<ProfileManager>();
        s.AddSingleton<PxeBootImageService>();
        s.AddSingleton<OsCatalogService>();
        s.AddSingleton<IsoDownloadService>();
        s.AddSingleton<Services.IUsbBurnerService, Services.UsbBurnerService>();
        s.AddTransient<MainViewModel>();
    }

    public Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task ActivateAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task DeactivateAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task UnloadAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

