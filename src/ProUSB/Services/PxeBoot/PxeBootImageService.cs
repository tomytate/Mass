using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DiscUtils;
using DiscUtils.Iso9660;
using DiscUtils.Udf;
using ProUSB.Domain;
using ProUSB.Services.Logging;

namespace ProUSB.Services.PxeBoot;

public class PxeBootImageService {
    private readonly FileLogger _log;

    public PxeBootImageService(FileLogger log) {
        _log = log;
    }

    public async Task<PxeBootResult> CreatePxeBootImageAsync(
        PxeBootConfiguration config,
        IProgress<PxeCreationProgress> progress,
        CancellationToken ct
    ) {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var warnings = new List<string>();
        var filesCreated = new List<PxeFileMapping>();

        try {
            _log.Info($"Starting PXE boot image creation from {config.IsoPath}");
            progress.Report(new PxeCreationProgress {
                PercentComplete = 0,
                Message = "Opening ISO file...",
                CurrentFile = "",
                BytesProcessed = 0,
                TotalBytes = 0
            });

            if (!File.Exists(config.IsoPath)) {
                throw new FileNotFoundException($"ISO file not found: {config.IsoPath}");
            }

            Directory.CreateDirectory(config.OutputDirectory);

            using var isoStream = File.OpenRead(config.IsoPath);
            var fs = UdfReader.Detect(isoStream)
                ? (DiscFileSystem)new UdfReader(isoStream)
                : new CDReader(isoStream, true);

            var bootType = config.BootType == PxeBootType.Generic
                ? DetectBootType(fs)
                : config.BootType;

            _log.Info($"Detected boot type: {bootType}");

            progress.Report(new PxeCreationProgress {
                PercentComplete = 10,
                Message = $"Extracting {bootType} boot files...",
                CurrentFile = "",
                BytesProcessed = 0,
                TotalBytes = 0
            });

            if (bootType == PxeBootType.Windows) {
                filesCreated.AddRange(await ExtractWindowsPxeFilesAsync(fs, config, progress, ct));
            } else {
                filesCreated.AddRange(await ExtractLinuxPxeFilesAsync(fs, config, progress, ct));
            }

            if (config.GenerateBiosConfig) {
                await GeneratePxeLinuxConfigAsync(config.OutputDirectory, bootType, ct);
                _log.Info("Generated BIOS (PXELINUX) configuration");
            }

            if (config.GenerateUefiConfig && bootType == PxeBootType.Linux) {
                await GenerateGrubConfigAsync(config.OutputDirectory, ct);
                _log.Info("Generated UEFI (GRUB2) configuration");
            }

            await CopyPxeToolsAsync(config.OutputDirectory, bootType, ct);
            await GenerateIpxeConfigAsync(config.OutputDirectory, bootType, ct);

            progress.Report(new PxeCreationProgress {
                PercentComplete = 100,
                Message = "PXE boot image created successfully",
                CurrentFile = "",
                BytesProcessed = 0,
                TotalBytes = 0
            });

            sw.Stop();
            _log.Info($"PXE boot image creation completed in {sw.Elapsed.TotalSeconds:F1}s");

            return new PxeBootResult {
                Success = true,
                FilesCreated = filesCreated,
                Warnings = warnings,
                Duration = sw.Elapsed
            };

        } catch (Exception ex) {
            _log.Error($"PXE boot image creation failed: {ex.Message}", ex);
            return new PxeBootResult {
                Success = false,
                FilesCreated = filesCreated,
                Warnings = warnings,
                ErrorMessage = ex.Message,
                Duration = sw.Elapsed
            };
        }
    }

    private async Task CopyPxeToolsAsync(string outputDir, PxeBootType bootType, CancellationToken ct) {
        var toolsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "pxe-tools");
        if (!Directory.Exists(toolsDir)) {
            _log.Warn($"PXE tools directory not found at {toolsDir}. Skipping tool copy.");
            return;
        }

        
        if (bootType == PxeBootType.Windows) {
            var wimbootSrc = Path.Combine(toolsDir, "ipxe", "wimboot");
            if (File.Exists(wimbootSrc)) {
                File.Copy(wimbootSrc, Path.Combine(outputDir, "wimboot"), true);
                _log.Info("Copied wimboot");
            }
        }

        
        var ipxeSrc = Path.Combine(toolsDir, "ipxe", "ipxe.efi");
        if (File.Exists(ipxeSrc)) {
            File.Copy(ipxeSrc, Path.Combine(outputDir, "ipxe.efi"), true);
            _log.Info("Copied ipxe.efi");
        }
        
        var memtestSrc = Path.Combine(toolsDir, "efi", "memtest", "memtest.efi");
        if (File.Exists(memtestSrc)) {
            Directory.CreateDirectory(Path.Combine(outputDir, "tools"));
            File.Copy(memtestSrc, Path.Combine(outputDir, "tools", "memtest.efi"), true);
            _log.Info("Copied memtest.efi");
        }

        
        var biosDir = Path.Combine(toolsDir, "bios");
        if (Directory.Exists(biosDir)) {
            var biosFiles = new[] { "pxelinux.0", "ldlinux.c32", "menu.c32", "libutil.c32", "libmenu.c32" };
            foreach (var file in biosFiles) {
                var src = Path.Combine(biosDir, file);
                
                
                
                
                
                if (!File.Exists(src)) {
                    var found = Directory.GetFiles(toolsDir, file, SearchOption.AllDirectories).FirstOrDefault();
                    if (found != null) src = found;
                }

                if (File.Exists(src)) {
                    File.Copy(src, Path.Combine(outputDir, file), true);
                    _log.Info($"Copied {file}");
                } else {
                    _log.Warn($"Could not find BIOS tool: {file}");
                }
            }
        }
    }

    private async Task GenerateIpxeConfigAsync(string outputDir, PxeBootType bootType, CancellationToken ct) {
        var ipxePath = Path.Combine(outputDir, "boot.ipxe");
        string content;

        if (bootType == PxeBootType.Windows) {
            content = @"#!ipxe
kernel wimboot
initrd windows/bootmgr.efi bootmgr.efi
initrd windows/Boot/BCD BCD
initrd windows/Boot/boot.sdi boot.sdi
initrd windows/sources/boot.wim boot.wim
boot";
        } else {
            content = @"#!ipxe
kernel linux/vmlinuz boot=live quiet splash
initrd linux/initrd.img
boot";
        }

        await File.WriteAllTextAsync(ipxePath, content, ct);
        _log.Info($"Generated iPXE config: {ipxePath}");
    }

    private PxeBootType DetectBootType(DiscFileSystem fs) {
        if (fs.FileExists("\\sources\\boot.wim") || fs.FileExists("\\bootmgr.efi")) {
            return PxeBootType.Windows;
        }

        if (fs.DirectoryExists("\\casper") || fs.DirectoryExists("\\live") ||
            fs.FileExists("\\isolinux\\isolinux.bin")) {
            return PxeBootType.Linux;
        }

        _log.Warn("Could not detect boot type, defaulting to Linux");
        return PxeBootType.Linux;
    }

    private async Task<List<PxeFileMapping>> ExtractWindowsPxeFilesAsync(
        DiscFileSystem fs,
        PxeBootConfiguration config,
        IProgress<PxeCreationProgress> progress,
        CancellationToken ct
    ) {
        var files = new List<PxeFileMapping>();
        var windowsDir = Path.Combine(config.OutputDirectory, "windows");
        Directory.CreateDirectory(windowsDir);

        var filesToExtract = new Dictionary<string, string> {
            [@"\sources\boot.wim"] = Path.Combine(windowsDir, "sources", "boot.wim"),
            [@"\bootmgr.efi"] = Path.Combine(windowsDir, "bootmgr.efi"),
            [@"\Boot\BCD"] = Path.Combine(windowsDir, "Boot", "BCD"),
            [@"\Boot\boot.sdi"] = Path.Combine(windowsDir, "Boot", "boot.sdi")
        };

        int current = 0;
        foreach (var (isoPath, destPath) in filesToExtract) {
            ct.ThrowIfCancellationRequested();

            if (!fs.FileExists(isoPath)) {
                _log.Warn($"Windows PXE file not found in ISO: {isoPath}");
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

            var fileInfo = fs.GetFileInfo(isoPath);
            _log.Info($"Extracting {isoPath} ({fileInfo.Length} bytes)");

            progress.Report(new PxeCreationProgress {
                PercentComplete = 10 + (current * 60 / filesToExtract.Count),
                Message = $"Extracting {Path.GetFileName(isoPath)}...",
                CurrentFile = isoPath,
                BytesProcessed = 0,
                TotalBytes = fileInfo.Length
            });

            using var sourceStream = fileInfo.OpenRead();
            using var destStream = File.Create(destPath);
            await sourceStream.CopyToAsync(destStream, ct);

            files.Add(new PxeFileMapping {
                SourcePath = isoPath,
                DestinationPath = destPath,
                Description = $"Windows boot file: {Path.GetFileName(isoPath)}",
                FileSize = fileInfo.Length
            });

            current++;
        }

        return files;
    }

    private async Task<List<PxeFileMapping>> ExtractLinuxPxeFilesAsync(
        DiscFileSystem fs,
        PxeBootConfiguration config,
        IProgress<PxeCreationProgress> progress,
        CancellationToken ct
    ) {
        var files = new List<PxeFileMapping>();
        var linuxDir = Path.Combine(config.OutputDirectory, "linux");
        Directory.CreateDirectory(linuxDir);

        var kernelPaths = new[] {
            @"\casper\vmlinuz",
            @"\live\vmlinuz",
            @"\isolinux\vmlinuz",
            @"\boot\vmlinuz"
        };

        var initrdPaths = new[] {
            @"\casper\initrd",
            @"\casper\initrd.lz",
            @"\live\initrd.img",
            @"\isolinux\initrd.img",
            @"\boot\initrd.img"
        };

        string? kernelPath = kernelPaths.FirstOrDefault(fs.FileExists);
        string? initrdPath = initrdPaths.FirstOrDefault(fs.FileExists);

        if (kernelPath == null) {
            throw new FileNotFoundException("Could not find Linux kernel (vmlinuz) in ISO");
        }

        if (initrdPath == null) {
            throw new FileNotFoundException("Could not find Linux initrd in ISO");
        }

        await ExtractFileAsync(fs, kernelPath, Path.Combine(linuxDir, "vmlinuz"), files, progress, ct);
        await ExtractFileAsync(fs, initrdPath, Path.Combine(linuxDir, "initrd.img"), files, progress, ct);

        return files;
    }

    private async Task ExtractFileAsync(
        DiscFileSystem fs,
        string sourcePath,
        string destPath,
        List<PxeFileMapping> files,
        IProgress<PxeCreationProgress> progress,
        CancellationToken ct
    ) {
        var fileInfo = fs.GetFileInfo(sourcePath);
        _log.Info($"Extracting {sourcePath} ({fileInfo.Length} bytes)");

        progress.Report(new PxeCreationProgress {
            PercentComplete = 50,
            Message = $"Extracting {Path.GetFileName(sourcePath)}...",
            CurrentFile = sourcePath,
            BytesProcessed = 0,
            TotalBytes = fileInfo.Length
        });

        using var sourceStream = fileInfo.OpenRead();
        using var destStream = File.Create(destPath);
        await sourceStream.CopyToAsync(destStream, ct);

        files.Add(new PxeFileMapping {
            SourcePath = sourcePath,
            DestinationPath = destPath,
            Description = $"Linux boot file: {Path.GetFileName(sourcePath)}",
            FileSize = fileInfo.Length
        });
    }

    private async Task GeneratePxeLinuxConfigAsync(string outputDir, PxeBootType bootType, CancellationToken ct) {
        var pxelinuxCfgDir = Path.Combine(outputDir, "pxelinux.cfg");
        Directory.CreateDirectory(pxelinuxCfgDir);

        var defaultConfigPath = Path.Combine(pxelinuxCfgDir, "default");

        string configContent = bootType == PxeBootType.Windows
            ? GenerateWindowsPxeLinuxConfig()
            : GenerateLinuxPxeLinuxConfig();

        await File.WriteAllTextAsync(defaultConfigPath, configContent, ct);
        _log.Info($"Generated PXELINUX config: {defaultConfigPath}");
    }

    private string GenerateWindowsPxeLinuxConfig() {
        return @"DEFAULT menu.c32
PROMPT 0
TIMEOUT 300

MENU TITLE Windows PXE Boot Menu

LABEL windows
    MENU LABEL Boot Windows PE
    KERNEL pxeboot.0
    APPEND bootfile=bootmgr.efi
";
    }

    private string GenerateLinuxPxeLinuxConfig() {
        return @"DEFAULT menu.c32
PROMPT 0
TIMEOUT 300

MENU TITLE Linux PXE Boot Menu

LABEL linux
    MENU LABEL Boot Linux
    KERNEL linux/vmlinuz
    APPEND initrd=linux/initrd.img boot=live quiet splash
";
    }

    private async Task GenerateGrubConfigAsync(string outputDir, CancellationToken ct) {
        var grubDir = Path.Combine(outputDir, "grub");
        Directory.CreateDirectory(grubDir);

        var grubCfgPath = Path.Combine(grubDir, "grub.cfg");

        string grubConfig = @"set timeout=30
set default=0

menuentry ""Boot Linux (UEFI)"" {
    linux /linux/vmlinuz boot=live quiet splash
    initrd /linux/initrd.img
}
";

        await File.WriteAllTextAsync(grubCfgPath, grubConfig, ct);
        _log.Info($"Generated GRUB2 config: {grubCfgPath}");
    }
}



