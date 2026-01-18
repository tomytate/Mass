using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Platform.Storage;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProUSB.Domain;
using ProUSB.Domain.Drivers;
using ProUSB.Services.Burn;
using ProUSB.Services.Logging;
using ProUSB.Services.Verification;
using ProUSB.Services.IsoCreation;
using ProUSB.Services.Profiles;
using ProUSB.Services.Diagnostics;
using ProUSB.UI.Views;
using ProUSB.UI;

using Mass.Core.UI;

namespace ProUSB.UI.ViewModels;

public partial class MainViewModel : ViewModelBase {
    private readonly ParallelBurnService _burn;
    private readonly MultiDeviceBurnOrchestrator _multiDeviceBurner;
    private readonly BootVerificationService _verifier;
    private readonly IsoCreationService _isoCreator;
    private readonly ProfileManager _profileManager;
    private readonly SmartHealthChecker _healthChecker;
    private readonly IDriverFactory _fac;
    private readonly FileLogger _logger;
    private CancellationTokenSource? _cts;

    [ObservableProperty] string status = "Ready.";
    [ObservableProperty] double prog;
    [ObservableProperty] UsbDeviceInfo? selDev;
    [ObservableProperty] ObservableCollection<UsbDeviceInfo> selectedDevices = new();
    [ObservableProperty] ObservableCollection<DeviceBurnProgress> deviceProgress = new();
    [ObservableProperty] string selectionSummary = "No devices selected";
    [ObservableProperty] int persistenceSize = 0;
    [ObservableProperty] string iso = "";
    [ObservableProperty] bool isBusy;
    [ObservableProperty] string logText = "";
    [ObservableProperty] bool canCancel;
    [ObservableProperty] bool isRefreshing;
    [ObservableProperty] bool logScrollTrigger;
    
    
    [ObservableProperty] ObservableCollection<LogEntry> filteredLogs = new();
    [ObservableProperty] string searchText = "";
    [ObservableProperty] LogLevel selectedLogLevel = LogLevel.Info;
    [ObservableProperty] bool showInfoLogs = true;
    [ObservableProperty] bool showWarnLogs = true;
    [ObservableProperty] bool showErrorLogs = true;
    [ObservableProperty] bool showDebugLogs = false;

    
    [ObservableProperty] ObservableCollection<BurnProfile> profiles = new();
    [ObservableProperty] BurnProfile? selectedProfile;
    [ObservableProperty] string newProfileName = "";

    
    [ObservableProperty] bool isVerifying;
    [ObservableProperty] string verificationResult = "";

    
    [ObservableProperty] SmartHealthReport? healthReport;
    [ObservableProperty] bool isCheckingHealth;

    
    [ObservableProperty] bool isCreatingIso;

    public ObservableCollection<string> FileSystems { get; } = new() {
        "FAT32", "NTFS", "exFAT", "UDF", "ReFS"
    };

    public ObservableCollection<string> ClusterSizes { get; } = new() {
        "Default", "512 bytes", "1024 bytes (1K)", "2048 bytes (2K)", "4096 bytes (4K)",
        "8192 bytes (8K)", "16384 bytes (16K)", "32768 bytes (32K)", "65536 bytes (64K)"
    };

    [ObservableProperty] int partitionSchemeIndex = 0;
    [ObservableProperty] int fileSystemIndex = 0;
    [ObservableProperty] int clusterSizeIndex = 0;
    [ObservableProperty] bool quickFormat = true;
    [ObservableProperty] bool bypassWin11;
    [ObservableProperty] bool isRaw;

    public ObservableCollection<UsbDeviceInfo> Devs { get; } = new();

    public MainViewModel(
        ParallelBurnService b,
        MultiDeviceBurnOrchestrator multiDeviceBurner,
        BootVerificationService verifier,
        IsoCreationService isoCreator,
        ProfileManager profileManager,
        SmartHealthChecker healthChecker,
        IDriverFactory f,
        FileLogger l
    ) {
        _burn = b;
        _multiDeviceBurner = multiDeviceBurner;
        _verifier = verifier;
        _isoCreator = isoCreator;
        _profileManager = profileManager;
        _healthChecker = healthChecker;
        _fac = f;
        _logger = l;

        RefreshProfiles();
        FilterLogs(); 
        // Fire and forget Refresh on the current synchronization context (UI thread)
        _ = Refresh();
    }

    private void Log(string m) {
        string msg = $"{DateTime.Now:HH:mm:ss}: {m}";
        LogText += msg + Environment.NewLine;
        _logger.Info(m);
        LogScrollTrigger = !LogScrollTrigger;
        FilterLogs();
    }

    private void RefreshProfiles() {
        Profiles.Clear();
        foreach(var profile in _profileManager.GetAllProfiles()) {
            Profiles.Add(profile);
        }
    }

    private Window? GetMainWindow() {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            return desktop.MainWindow;
        }
        return null;
    }

    private void UpdateSelectionSummary() {
        if(SelectedDevices.Count == 0) {
            SelectionSummary = "No devices selected";
        } else if(SelectedDevices.Count == 1) {
            SelectionSummary = $"1 device selected: {SelectedDevices[0].FriendlyName}";
        } else {
            var totalSize = SelectedDevices.Sum(d => d.TotalSize);
            var sizeMB = totalSize / (1024.0 * 1024.0 * 1024.0);
            SelectionSummary = $"{SelectedDevices.Count} devices selected ({sizeMB:F1} GB total)";
        }
    }

    private void UpdateDeviceProgress(string deviceId, double progress, string status) {
        var item = DeviceProgress.FirstOrDefault(d => d.DeviceId == deviceId);
        if(item != null) {
            item.Progress = progress;
            item.Status = status;
            item.IsComplete = status.Contains("Complete");
            item.HasFailed = status.Contains("Failed");
        }

        Prog = DeviceProgress.Count > 0 ? DeviceProgress.Average(d => d.Progress) : 0;

        var active = DeviceProgress.Count(d => !d.IsComplete && !d.HasFailed);
        if(active > 0) {
            Status = $"Burning {active} device(s)...";
        }
    }

    private string FormatFileSize(long bytes) {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while(len >= 1024 && order < sizes.Length - 1) {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    [RelayCommand]
    public void FilterLogs() {
        FilteredLogs.Clear();
        var logs = _logger.FilterAndSearch(ShowInfoLogs, ShowWarnLogs, ShowErrorLogs, ShowDebugLogs, SearchText);
        foreach(var log in logs) {
            FilteredLogs.Add(log);
        }
    }

    [RelayCommand]
    public async Task ExportLogs() {
        var w = GetMainWindow();
        if(w == null) return;

        var saveOptions = new FilePickerSaveOptions {
            Title = "Export Logs",
            SuggestedFileName = $"ProUSBMediaSuite_Logs_{DateTime.Now:yyyyMMdd_HHmmss}",
            FileTypeChoices = new[] {
                new FilePickerFileType("Text File") { Patterns = new[] { "*.txt" } },
                new FilePickerFileType("CSV File") { Patterns = new[] { "*.csv" } },
                new FilePickerFileType("JSON File") { Patterns = new[] { "*.json" } }
            }
        };

        var result = await w.StorageProvider.SaveFilePickerAsync(saveOptions);
        if(result == null) return;

        var path = result.Path.LocalPath;
        var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();

        try {
            switch(ext) {
                case ".txt":
                    await _logger.ExportToTextAsync(path);
                    break;
                case ".csv":
                    await _logger.ExportToCsvAsync(path);
                    break;
                case ".json":
                    await _logger.ExportToJsonAsync(path);
                    break;
                default:
                    await _logger.ExportToTextAsync(path + ".txt");
                    break;
            }

            Status = $"✅ Logs exported to {Path.GetFileName(path)}";
            Log($"Logs exported: {path}");
        } catch(Exception ex) {
            Status = $"❌ Export failed: {ex.Message}";
            Log($"Export error: {ex.Message}");
        }
    }

    [RelayCommand]
    public void ClearLogs() {
        _logger.ClearLogs();
        FilteredLogs.Clear();
        LogText = "";
        Log("Logs cleared");
    }

    [RelayCommand]
    public async Task Refresh() {
        IsRefreshing = true;
        Status = "Scanning...";
        Devs.Clear();
        try {
            var l = await _fac.EnumerateDevicesAsync(CancellationToken.None);
            Log($"Found {l.Count()} devices");
            foreach(var d in l) {
                Devs.Add(d);
                Log($"Device: {d.FriendlyName} ({d.DeviceId})");
            }
            Status = Devs.Count > 0 ? $"Found {Devs.Count} device(s)." : "No devices found.";
        } catch(Exception e) {
            Log($"Refresh Error: {e.Message}");
            Status = $"Error: {e.Message}";
        } finally {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    public async Task Browse() {
        Log("Browse called");
        var w = GetMainWindow();
        if(w == null) {
            Log("MainWindow is null");
            return;
        }

        var options = new FilePickerOpenOptions {
            AllowMultiple = false,
            Title = "Select ISO Image",
            FileTypeFilter = new[] {
                new FilePickerFileType("ISO Images") { Patterns = new[] { "*.iso" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
            }
        };

        var f = await w.StorageProvider.OpenFilePickerAsync(options);
        if(f.Count > 0) {
            Iso = f[0].Path.LocalPath;
            Status = "ISO selected.";
            Log($"ISO selected: {Iso}");
        }
    }

    [RelayCommand]
    public void SelectAll() {
        SelectedDevices.Clear();
        foreach(var dev in Devs) {
            SelectedDevices.Add(dev);
        }
        UpdateSelectionSummary();
        Log($"Selected all {SelectedDevices.Count} devices");
    }

    [RelayCommand]
    public void ClearAll() {
        SelectedDevices.Clear();
        UpdateSelectionSummary();
        Log("Cleared all device selections");
    }

    [RelayCommand]
    public async Task Start() {
        Log("Start called");

        if(SelectedDevices.Count == 0 && SelDev != null) {
            SelectedDevices.Add(SelDev);
            UpdateSelectionSummary();
        }

        if(SelectedDevices.Count == 0) {
            Log("Start aborted: No devices selected");
            Status = "Please select at least one device.";
            return;
        }

        if(string.IsNullOrEmpty(Iso) || !File.Exists(Iso)) {
            Log("Start aborted: ISO not found");
            Status = "Please select a valid ISO file.";
            return;
        }

        var confirmMsg = SelectedDevices.Count == 1
            ? $"{SelectedDevices[0].FriendlyName} ({SelectedDevices[0].DeviceId})"
            : $"{SelectedDevices.Count} devices:\n" +
              string.Join("\n", SelectedDevices.Select(d => $"  • {d.FriendlyName}"));

        CancelCommand.NotifyCanExecuteChanged();
        _cts = new();
        Prog = 0;

        DeviceProgress.Clear();
        foreach(var dev in SelectedDevices) {
            DeviceProgress.Add(new DeviceBurnProgress {
                DeviceName = dev.FriendlyName,
                DeviceId = dev.DeviceId,
                Progress = 0,
                Status = "Queued"
            });
        }

        Log($"Starting multi-device burn for {SelectedDevices.Count} device(s)");
        Status = $"Preparing {SelectedDevices.Count} device(s)...";

        string pt = PartitionSchemeIndex switch {
            0 => "gpt",
            1 => "mbr",
            2 => "hybrid",
            3 => "superfloppy",
            _ => "gpt"
        };

        string fs = FileSystemIndex switch {
            0 => "fat32",
            1 => "ntfs",
            2 => "exfat",
            3 => "udf",
            4 => "refs",
            _ => "fat32"
        };

        int cs = ClusterSizeIndex switch {
            1 => 512,
            2 => 1024,
            3 => 2048,
            4 => 4096,
            5 => 8192,
            6 => 16384,
            7 => 32768,
            8 => 65536,
            _ => 0
        };

        try {
            var baseConfig = new DeploymentConfiguration {
                JobName = "MultiDeviceBurn",
                TargetDevice = SelectedDevices[0],
                SourceIso = new IsoMetadata { FilePath = Iso },
                Strategy = IsRaw ? BurnStrategy.RawSectorWrite : BurnStrategy.FileSystemCopy,
                PartitionScheme = pt,
                FileSystem = fs,
                ClusterSize = cs,
                QuickFormat = QuickFormat,
                BypassWin11 = BypassWin11,
                PersistenceSize = PersistenceSize
            };

            await _multiDeviceBurner.BurnMultipleAsync(
                SelectedDevices.ToList(),
                Iso,
                baseConfig,
                UpdateDeviceProgress,
                _cts.Token
            );

            var succeeded = DeviceProgress.Count(d => d.Status.Contains("Complete"));
            var failed = DeviceProgress.Count(d => d.Status.Contains("Failed"));

            Status = succeeded == SelectedDevices.Count
                ? "✅ ALL DONE - All devices ready to boot!"
                : $"⚠️ Completed: {succeeded} succeeded, {failed} failed";

            Prog = 100;
            Log("Multi-device burn completed");
            Log($"Summary: {succeeded} succeeded, {failed} failed");
            Log($"Format: {fs.ToUpper()} on {pt.ToUpper()}");
            if(PersistenceSize > 0) Log($"Persistence: {PersistenceSize} MB");

            if(succeeded > 0) {
                Log("Auto-verifying burned devices...");
                foreach(var dev in SelectedDevices) {
                    try {
                        var verifyResult = await _verifier.VerifyDeviceAsync(dev, _cts.Token);
                        Log($"  {dev.FriendlyName}: {verifyResult.GetSummary()}");
                    } catch {
                        Log($"  {dev.FriendlyName}: Verification skipped");
                    }
                }
            }
        } catch(OperationCanceledException) {
            Status = "⚠️ Operation cancelled by user";
            Log("User cancelled multi-device burn");
            Prog = 0;
        } catch(Exception ex) {
            Status = $"❌ Error: {ex.Message}";
            Prog = 0;
            Log($"Multi-device burn error: {ex.Message}");
        } finally {
            IsBusy = false;
            CanCancel = false;
            CancelCommand.NotifyCanExecuteChanged();
            _cts?.Dispose();
            _cts = null;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    public void Cancel() {
        if(_cts == null) return;

        Log("Cancel requested by user");
        Status = "Cancelling all operations...";
        _cts.Cancel();
        CanCancel = false;
        CancelCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    public async Task VerifyDevice() {
        if(SelectedDevices.Count == 0 && SelDev == null) {
            Status = "Please select a device to verify.";
            Log("Verify aborted: No device selected");
            return;
        }

        var deviceToVerify = SelectedDevices.Count > 0 ? SelectedDevices[0] : SelDev!;

        IsVerifying = true;
        Log($"Verifying bootability of {deviceToVerify.FriendlyName}...");
        Status = "Verifying...";

        try {
            var result = await _verifier.VerifyDeviceAsync(deviceToVerify, CancellationToken.None);

            VerificationResult = result.GetSummary();
            Log(VerificationResult);

            foreach(var detail in result.Details) {
                Log($"  {detail}");
            }

            foreach(var warning in result.Warnings) {
                Log($"  ⚠️ {warning}");
            }

            Status = result.IsBootable ? "✅ Drive is bootable" : "❌ Drive is NOT bootable";
        } catch(Exception ex) {
            Log($"Verification error: {ex.Message}");
            VerificationResult = $"❌ Verification failed: {ex.Message}";
            Status = "Verification failed";
        } finally {
            IsVerifying = false;
        }
    }

    [RelayCommand]
    public async Task CheckHealth() {
        if(SelDev == null) {
            Status = "Please select a device to check health.";
            return;
        }

        IsCheckingHealth = true;
        Status = "Checking drive health...";
        Log($"Checking health for {SelDev.FriendlyName}...");

        try {
            HealthReport = await _healthChecker.CheckDriveHealthAsync(SelDev.PhysicalIndex, CancellationToken.None);
            
            Log($"Health Check: {HealthReport.Status} (Score: {HealthReport.HealthScore})");
            Log($"  {HealthReport.Message}");
            if(HealthReport.Temperature > 0) Log($"  Temperature: {HealthReport.Temperature}C");
            
            Status = $"Health: {HealthReport.Status} - {HealthReport.Message}";
        } catch(Exception ex) {
            Log($"Health check failed: {ex.Message}");
            Status = "Health check failed.";
        } finally {
            IsCheckingHealth = false;
        }
    }

    [RelayCommand]
    public async Task CreateIso() {
        if(SelDev == null) {
            Status = "Please select a device to create ISO from.";
            Log("CreateISO aborted: No device selected");
            return;
        }

        Log($"Starting ISO creation from {SelDev.FriendlyName}");

        var w = GetMainWindow();
        if(w == null) {
            Log("MainWindow is null");
            return;
        }

        var saveOptions = new FilePickerSaveOptions {
            Title = "Save ISO Image",
            SuggestedFileName = $"{SelDev.FriendlyName.Replace(" ", "_")}.iso",
            FileTypeChoices = new[] {
                new FilePickerFileType("ISO Image") { Patterns = new[] { "*.iso" } }
            },
            DefaultExtension = "iso"
        };

        var result = await w.StorageProvider.SaveFilePickerAsync(saveOptions);
        if(result == null) {
            Log("ISO creation cancelled by user");
            return;
        }

        var outputPath = result.Path.LocalPath;

        IsCreatingIso = true;
        IsBusy = true;
        CanCancel = true;
        CancelCommand.NotifyCanExecuteChanged();
        _cts = new();
        Prog = 0;

        try {
            var progress = new Progress<IsoCreationProgress>(p => {
                Prog = p.PercentComplete;
                Status = p.Message;
            });

            var createResult = await _isoCreator.CreateIsoFromDeviceAsync(
                SelDev,
                outputPath,
                IsoCreationMode.RawCopy,
                progress,
                _cts.Token
            );

            if(createResult.Success) {
                Status = $"✅ ISO created successfully";
                Log($"ISO created: {outputPath}");
                Log($"File size: {FormatFileSize(createResult.FileSizeBytes)}");
                Log($"Creation time: {createResult.Duration.TotalSeconds:F1}s");
                Prog = 100;
            } else {
                Status = $"❌ ISO creation failed: {createResult.ErrorMessage}";
                Log($"ISO creation failed: {createResult.ErrorMessage}");
                Prog = 0;
            }
        } catch(OperationCanceledException) {
            Status = "⚠️ ISO creation cancelled";
            Log("User cancelled ISO creation");
            Prog = 0;
        } catch(Exception ex) {
            Status = $"❌ Error: {ex.Message}";
            Log($"ISO creation error: {ex.Message}");
            Prog = 0;
        } finally {
            IsCreatingIso = false;
            IsBusy = false;
            CanCancel = false;
            CancelCommand.NotifyCanExecuteChanged();
            _cts?.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    public void LoadProfile() {
        if(SelectedProfile == null) return;

        Log($"Loading profile: {SelectedProfile.Name}");

        PartitionSchemeIndex = SelectedProfile.PartitionScheme switch {
            "gpt" => 0,
            "mbr" => 1,
            "hybrid" => 2,
            "superfloppy" => 3,
            _ => 0
        };

        FileSystemIndex = SelectedProfile.FileSystem switch {
            "fat32" => 0,
            "ntfs" => 1,
            "exfat" => 2,
            "udf" => 3,
            "refs" => 4,
            _ => 0
        };

        ClusterSizeIndex = SelectedProfile.ClusterSize switch {
            512 => 1,
            1024 => 2,
            2048 => 3,
            4096 => 4,
            8192 => 5,
            16384 => 6,
            32768 => 7,
            65536 => 8,
            _ => 0
        };

        QuickFormat = SelectedProfile.QuickFormat;
        BypassWin11 = SelectedProfile.BypassWin11;
        IsRaw = SelectedProfile.IsRaw;
        PersistenceSize = SelectedProfile.PersistenceSize;

        Status = $"Loaded profile: {SelectedProfile.Name}";
        Log($"Profile loaded successfully");
    }

    [RelayCommand]
    public void SaveProfile() {
        if(string.IsNullOrWhiteSpace(NewProfileName)) {
            Status = "Please enter a profile name.";
            return;
        }

        var pt = PartitionSchemeIndex switch {
            0 => "gpt",
            1 => "mbr",
            2 => "hybrid",
            3 => "superfloppy",
            _ => "gpt"
        };

        var fs = FileSystemIndex switch {
            0 => "fat32",
            1 => "ntfs",
            2 => "exfat",
            3 => "udf",
            4 => "refs",
            _ => "fat32"
        };

        var cs = ClusterSizeIndex switch {
            1 => 512,
            2 => 1024,
            3 => 2048,
            4 => 4096,
            5 => 8192,
            6 => 16384,
            7 => 32768,
            8 => 65536,
            _ => 0
        };

        var profile = new BurnProfile {
            Name = NewProfileName,
            PartitionScheme = pt,
            FileSystem = fs,
            ClusterSize = cs,
            QuickFormat = QuickFormat,
            BypassWin11 = BypassWin11,
            IsRaw = IsRaw,
            PersistenceSize = PersistenceSize
        };

        _profileManager.SaveProfile(profile);
        RefreshProfiles();

        Status = $"Profile '{NewProfileName}' saved successfully";
        Log($"Saved profile: {NewProfileName}");
        NewProfileName = "";
    }

    [RelayCommand]
    public void DeleteProfile() {
        if(SelectedProfile == null || SelectedProfile.IsDefault) {
            Status = "Cannot delete default profiles.";
            return;
        }

        var profileName = SelectedProfile.Name;
        _profileManager.DeleteProfile(profileName);
        RefreshProfiles();

        Status = $"Profile '{profileName}' deleted";
        Log($"Deleted profile: {profileName}");
        SelectedProfile = null;
    }
}


