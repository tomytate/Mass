using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ProUSB.Domain;
using ProUSB.Infrastructure;
using ProUSB.Services.Iso;

namespace ProUSB.Services.IsoManagement;

public record IsoLibraryEntry {
    public required string FilePath { get; init; }
    public required string FileName { get; init; }
    public required long SizeBytes { get; init; }
    public required DateTime LastModified { get; init; }
    public DateTime LastScanned { get; init; } = DateTime.Now;
    public string? DetectedOs { get; init; }
    public string? DetectedVersion { get; init; }
    public bool IsBootable { get; init; }
    public string? Sha256Hash { get; init; }
    public List<string> Tags { get; init; } = [];
    public int UseCount { get; init; } = 0;
    public DateTime? LastUsed { get; init; }
}

public record QuickBurnPreset {
    public required string Name { get; init; }
    public required string IsoPath { get; init; }
    public required string ProfileName { get; init; }
    public string? DeviceId { get; init; }
}

public class IsoLibraryService {
    private readonly string _libraryPath;
    private readonly IsoIntegrityVerifier _verifier;
    private readonly List<IsoLibraryEntry> _library = [];
    private readonly List<QuickBurnPreset> _presets = [];

    public IsoLibraryService(PortablePathManager pathManager, IsoIntegrityVerifier verifier) {
        _verifier = verifier;
        string dataDir = pathManager.GetDataDirectory();
        Directory.CreateDirectory(dataDir);
        _libraryPath = Path.Combine(dataDir, "iso_library.json");
        _ = LoadLibraryAsync();
    }

    public async Task ScanDirectoryAsync(string directoryPath, CancellationToken ct = default) {
        if (!Directory.Exists(directoryPath)) return;

        var isoFiles = Directory.GetFiles(directoryPath, "*.iso", SearchOption.AllDirectories);

        foreach (var isoPath in isoFiles) {
            ct.ThrowIfCancellationRequested();
            
            try {
                var fileInfo = new FileInfo(isoPath);
                var existing = _library.FirstOrDefault(e => e.FilePath.Equals(isoPath, StringComparison.OrdinalIgnoreCase));

                if (existing != null && existing.LastModified == fileInfo.LastWriteTime) {
                    continue;
                }

                var isValid = await _verifier.IsStructureValid(isoPath);
                var (os, version) = DetectOsInfo(fileInfo.Name);

                var entry = new IsoLibraryEntry {
                    FilePath = isoPath,
                    FileName = fileInfo.Name,
                    SizeBytes = fileInfo.Length,
                    LastModified = fileInfo.LastWriteTime,
                    LastScanned = DateTime.Now,
                    DetectedOs = os,
                    DetectedVersion = version,
                    IsBootable = isValid,
                    Tags = GenerateTags(os),
                    UseCount = existing?.UseCount ?? 0,
                    LastUsed = existing?.LastUsed
                };

                if (existing != null) {
                    _library.Remove(existing);
                }
                _library.Add(entry);

            } catch { }
        }

        await SaveLibraryAsync();
    }

    public List<IsoLibraryEntry> GetAllIsos() => [.. _library.OrderByDescending(e => e.LastUsed ?? DateTime.MinValue)];

    public List<IsoLibraryEntry> SearchIsos(string query) {
        if (string.IsNullOrWhiteSpace(query)) return GetAllIsos();

        var lowerQuery = query.ToLowerInvariant();
        return _library
            .Where(e =>
                e.FileName.ToLowerInvariant().Contains(lowerQuery) ||
                e.DetectedOs?.ToLowerInvariant().Contains(lowerQuery) == true ||
                e.Tags.Any(t => t.ToLowerInvariant().Contains(lowerQuery)))
            .ToList();
    }

    public List<IsoLibraryEntry> FilterByTag(string tag) =>
        _library.Where(e => e.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)).ToList();

    public async Task UpdateUseCountAsync(string isoPath) {
        var entry = _library.FirstOrDefault(e => e.FilePath.Equals(isoPath, StringComparison.OrdinalIgnoreCase));
        if (entry != null) {
            _library.Remove(entry);
            _library.Add(entry with { UseCount = entry.UseCount + 1, LastUsed = DateTime.Now });
            await SaveLibraryAsync();
        }
    }

    public async Task<QuickBurnPreset?> AddPresetAsync(string name, string isoPath, string profileName, string? deviceId = null) {
        var preset = new QuickBurnPreset {
            Name = name,
            IsoPath = isoPath,
            ProfileName = profileName,
            DeviceId = deviceId
        };
        _presets.Add(preset);
        await SaveLibraryAsync();
        return preset;
    }

    public List<QuickBurnPreset> GetPresets() => [.. _presets];

    private (string? Os, string? Version) DetectOsInfo(string fileName) {
        var lower = fileName.ToLowerInvariant();

        if (lower.Contains("ubuntu")) return ("Ubuntu", ExtractVersion(fileName, "ubuntu"));
        if (lower.Contains("fedora")) return ("Fedora", ExtractVersion(fileName, "fedora"));
        if (lower.Contains("debian")) return ("Debian", ExtractVersion(fileName, "debian"));
        if (lower.Contains("arch")) return ("Arch Linux", null);
        if (lower.Contains("manjaro")) return ("Manjaro", null);
        if (lower.Contains("mint")) return ("Linux Mint", ExtractVersion(fileName, "mint"));
        if (lower.Contains("kali")) return ("Kali Linux", ExtractVersion(fileName, "kali"));
        if (lower.Contains("windows")) return ("Windows", ExtractWindowsVersion(fileName));
        if (lower.Contains("win11")) return ("Windows", "11");
        if (lower.Contains("win10")) return ("Windows", "10");

        return (null, null);
    }

    private string? ExtractVersion(string fileName, string osName) {
        var index = fileName.ToLowerInvariant().IndexOf(osName);
        if (index >= 0) {
            var afterOs = fileName.Substring(index + osName.Length);
            var versionMatch = System.Text.RegularExpressions.Regex.Match(afterOs, @"[\d\.]+");
            return versionMatch.Success ? versionMatch.Value : null;
        }
        return null;
    }

    private string? ExtractWindowsVersion(string fileName) {
        if (fileName.Contains("11")) return "11";
        if (fileName.Contains("10")) return "10";
        if (fileName.Contains("2022")) return "Server 2022";
        if (fileName.Contains("2019")) return "Server 2019";
        return null;
    }

    private List<string> GenerateTags(string? os) {
        List<string> tags = [];
        
        if (os != null) {
            tags.Add(os);
            
            if (os.Contains("Ubuntu") || os.Contains("Debian") || os.Contains("Mint")) {
                tags.Add("Debian-based");
            }
            if (os.Contains("Fedora") || os.Contains("RHEL")) {
                tags.Add("RPM-based");
            }
            if (os.Contains("Arch") || os.Contains("Manjaro")) {
                tags.Add("Arch-based");
            }
            if (os.Contains("Windows")) {
                tags.Add("Windows");
            } else {
                tags.Add("Linux");
            }
        }

        return tags;
    }

    private async Task LoadLibraryAsync() {
        try {
            if (File.Exists(_libraryPath)) {
                var json = await File.ReadAllTextAsync(_libraryPath);
                var data = JsonSerializer.Deserialize<LibraryData>(json);
                if (data != null) {
                    _library.AddRange(data.Entries ?? []);
                    _presets.AddRange(data.Presets ?? []);
                }
            }
        } catch { }
    }

    private async Task SaveLibraryAsync() {
        try {
            var data = new LibraryData {
                Entries = [.. _library],
                Presets = [.. _presets]
            };
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(data, options);
            await File.WriteAllTextAsync(_libraryPath, json);
        } catch { }
    }

    private record LibraryData {
        public List<IsoLibraryEntry> Entries { get; init; } = [];
        public List<QuickBurnPreset> Presets { get; init; } = [];
    }
}


