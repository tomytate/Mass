using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DiscUtils;
using DiscUtils.Iso9660;
using DiscUtils.Udf;
using ProUSB.Services.Logging;

namespace ProUSB.Services.Iso;

public enum BootMode {
    Unknown,
    BiosOnly,
    UefiOnly,
    Hybrid
}

public enum OperatingSystem {
    Unknown,
    Windows,
    Linux,
    Other
}

public record IsoAnalysis {
    public BootMode BootMode { get; init; }
    public OperatingSystem OperatingSystem { get; init; }
    public string RecommendedPartitionScheme { get; init; } = "GPT";
    public string RecommendedFileSystem { get; init; } = "FAT32";
    public bool HasBootManager { get; init; }
    public bool HasEfiBootloader { get; init; }
    public bool IsBootable { get; init; }
    public string Details { get; init; } = "";
}

public class IsoAnalyzer {
    private readonly FileLogger _log;

    public IsoAnalyzer(FileLogger log) {
        _log = log;
    }

    public async Task<IsoAnalysis> AnalyzeIsoAsync(string isoPath, CancellationToken ct) {
        return await Task.Run(() => AnalyzeIso(isoPath), ct);
    }

    private IsoAnalysis AnalyzeIso(string isoPath) {
        _log.Info($"Analyzing ISO: {isoPath}");

        try {
            using var isoStream = File.OpenRead(isoPath);
            
            bool isUdf = UdfReader.Detect(isoStream);
            DiscFileSystem fs = isUdf 
                ? new UdfReader(isoStream) 
                : new CDReader(isoStream, true);

            bool hasBiosBootFiles = CheckBiosBootFiles(fs);
            bool hasUefiBootFiles = CheckUefiBootFiles(fs);
            var osType = DetectOperatingSystem(fs);

            var bootMode = DetermineBootMode(hasBiosBootFiles, hasUefiBootFiles);
            var isBootable = hasBiosBootFiles || hasUefiBootFiles;

            string recommendedScheme = bootMode switch {
                BootMode.UefiOnly => "GPT",
                BootMode.Hybrid => "GPT",
                _ => "MBR"
            };

            string recommendedFs = osType == OperatingSystem.Windows ? "NTFS" : "FAT32";

            string details = BuildAnalysisDetails(osType, bootMode, hasBiosBootFiles, hasUefiBootFiles);

            _log.Info($"ISO Analysis: OS={osType}, Boot={bootMode}, Scheme={recommendedScheme}");

            return new IsoAnalysis {
                BootMode = bootMode,
                OperatingSystem = osType,
                RecommendedPartitionScheme = recommendedScheme,
                RecommendedFileSystem = recommendedFs,
                HasBootManager = hasBiosBootFiles,
                HasEfiBootloader = hasUefiBootFiles,
                IsBootable = isBootable,
                Details = details
            };

        } catch (Exception ex) {
            _log.Error($"ISO analysis failed: {ex.Message}", ex);
            return new IsoAnalysis {
                BootMode = BootMode.Unknown,
                OperatingSystem = OperatingSystem.Unknown,
                Details = $"Analysis failed: {ex.Message}"
            };
        }
    }

    private bool CheckBiosBootFiles(DiscFileSystem fs) {
        string[] biosIndicators = {
            "\\bootmgr",
            "\\bootmgr.efi",
            "\\ntldr",
            "\\boot\\bcd",
            "\\isolinux\\isolinux.bin",
            "\\syslinux\\syslinux.bin",
            "\\boot\\grub\\i386-pc",
            "\\grldr"
        };

        foreach (var indicator in biosIndicators) {
            if (fs.FileExists(indicator) || fs.DirectoryExists(indicator)) {
                _log.Debug($"BIOS boot indicator found: {indicator}");
                return true;
            }
        }

        return false;
    }

    private bool CheckUefiBootFiles(DiscFileSystem fs) {
        string[] uefiIndicators = {
            "\\efi\\boot\\bootx64.efi",
            "\\efi\\boot\\bootia32.efi",
            "\\efi\\boot\\bootaa64.efi",
            "\\efi\\microsoft\\boot",
            "\\boot\\grub\\x86_64-efi"
        };

        foreach (var indicator in uefiIndicators) {
            if (fs.FileExists(indicator) || fs.DirectoryExists(indicator)) {
                _log.Debug($"UEFI boot indicator found: {indicator}");
                return true;
            }
        }

        return false;
    }

    private OperatingSystem DetectOperatingSystem(DiscFileSystem fs) {
        if (fs.FileExists("\\sources\\install.wim") || 
            fs.FileExists("\\sources\\install.esd") ||
            fs.FileExists("\\bootmgr")) {
            return OperatingSystem.Windows;
        }

        if (fs.FileExists("\\isolinux\\isolinux.bin") ||
            fs.FileExists("\\syslinux\\syslinux.bin") ||
            fs.DirectoryExists("\\casper") ||
            fs.DirectoryExists("\\live")) {
            return OperatingSystem.Linux;
        }

        return OperatingSystem.Other;
    }

    private BootMode DetermineBootMode(bool hasBios, bool hasUefi) {
        if (hasBios && hasUefi) return BootMode.Hybrid;
        if (hasUefi) return BootMode.UefiOnly;
        if (hasBios) return BootMode.BiosOnly;
        return BootMode.Unknown;
    }

    private string BuildAnalysisDetails(OperatingSystem os, BootMode boot, bool bios, bool uefi) {
        var parts = new System.Collections.Generic.List<string>();

        parts.Add($"Operating System: {os}");
        parts.Add($"Boot Mode: {boot}");
        
        if (bios) parts.Add("BIOS boot files detected");
        if (uefi) parts.Add("UEFI boot files detected");

        if (os == OperatingSystem.Windows) {
            parts.Add("Recommend NTFS for large file support");
        }

        if (boot == BootMode.Hybrid || boot == BootMode.UefiOnly) {
            parts.Add("GPT partition scheme required for UEFI");
        }

        return string.Join(". ", parts);
    }
}


