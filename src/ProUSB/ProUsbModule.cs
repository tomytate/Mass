using Mass.Core.Abstractions;
using Mass.Core.Plugins;
using Mass.Spec.Contracts.Plugins;
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
using System.Runtime.Versioning;

[assembly: SupportedOSPlatform("windows")]

namespace ProUSB;

public class ProUsbModule : IPlugin
{
    public PluginManifest Manifest => new()
    {
        Id = "prousb",
        Name = "ProUSB",
        Version = "1.0.0",
        Description = "USB bootable media creator",
        Author = "Tomy Tolledo",
        Icon = "💾",
        EntryAssembly = "ProUSB.dll",
        EntryType = "ProUSB.ProUsbModule",
        Enabled = true
    };

    public void Init(IServiceProvider services)
    {
    }

    public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
