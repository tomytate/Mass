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

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProUsb(this IServiceCollection s)
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
        s.AddTransient<MainViewModel>();
        
        // Register the plugin itself so it can be resolved if needed, 
        // though PluginLifecycleManager usually loads it. 
        // If it's a "System Plugin", maybe we register it here?
        // For now, let's just register the services.
        
        return s;
    }
}
