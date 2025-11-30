using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ProUSB.Domain;
using ProUSB.Services.Logging;
using ProUSB.Infrastructure;

namespace ProUSB.Services.Profiles;

public class ProfileManager {
    private readonly FileLogger _logger;
    private readonly string _profilesDirectory;
    private readonly JsonSerializerOptions _jsonOptions;
    
    public ProfileManager(FileLogger logger, PortablePathManager pathManager) {
        _logger = logger;
        
        _profilesDirectory = pathManager.GetProfilesDirectory();
        
        if(!Directory.Exists(_profilesDirectory)) {
            Directory.CreateDirectory(_profilesDirectory);
            _logger.Info($"Created profiles directory: {_profilesDirectory}");
        }
        
        _jsonOptions = new JsonSerializerOptions {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            AllowTrailingCommas = false
        };
        
        EnsureDefaultProfiles();
    }
    
    public async Task<List<BurnProfile>> GetAllProfilesAsync() {
        var profiles = new List<BurnProfile>();
        
        try {
            var files = Directory.GetFiles(_profilesDirectory, "*.json");
            
            foreach(var file in files) {
                try {
                    await using var fileStream = File.OpenRead(file);
                    
                    var profile = await JsonSerializer.DeserializeAsync<BurnProfile>(
                        fileStream,
                        _jsonOptions
                    );
                    
                    if(profile != null) {
                        profiles.Add(profile);
                    }
                }
                catch(JsonException ex) {
                    _logger.Warn($"Invalid JSON in {Path.GetFileName(file)}: {ex.Message}");
                }
            }
            
            _logger.Info($"Loaded {profiles.Count} profile(s)");
        }
        catch(Exception ex) {
            _logger.Error($"Error loading profiles: {ex.Message}");
        }
        
        return profiles.OrderBy(p => p.IsDefault ? 0 : 1).ThenBy(p => p.Name).ToList();
    }
    
    public List<BurnProfile> GetAllProfiles() {
        return GetAllProfilesAsync().GetAwaiter().GetResult();
    }
    
    public async Task SaveProfileAsync(BurnProfile profile) {
        try {
            var fileName = SanitizeFileName(profile.Name) + ".json";
            var filePath = Path.Combine(_profilesDirectory, fileName);
            
            await using var fileStream = File.Create(filePath);
            await JsonSerializer.SerializeAsync(fileStream, profile, _jsonOptions);
            
            _logger.Info($"Saved profile: {profile.Name}");
        }
        catch(Exception ex) {
            _logger.Error($"Failed to save profile {profile.Name}: {ex.Message}");
            throw;
        }
    }
    
    public void SaveProfile(BurnProfile profile) {
        SaveProfileAsync(profile).GetAwaiter().GetResult();
    }
    
    public void DeleteProfile(string profileName) {
        try {
            var fileName = SanitizeFileName(profileName) + ".json";
            var filePath = Path.Combine(_profilesDirectory, fileName);
            
            if(File.Exists(filePath)) {
                File.Delete(filePath);
                _logger.Info($"Deleted profile: {profileName}");
            }
        }
        catch(Exception ex) {
            _logger.Error($"Failed to delete profile {profileName}: {ex.Message}");
            throw;
        }
    }
    
    private void EnsureDefaultProfiles() {
        var profiles = GetAllProfiles();
        
        if(profiles.Any(p => p.IsDefault)) {
            return;
        }
        
        _logger.Info("Creating default profiles...");
        
        var defaults = new[] {
            new BurnProfile {
                Name = "Windows 11 UEFI (Small)",
                Description = "Windows 11 UEFI with install.wim < 4GB (GPT + FAT32)",
                PartitionScheme = "gpt",
                FileSystem = "fat32",
                ClusterSize = 0,
                QuickFormat = true,
                BypassWin11 = true,
                IsDefault = true
            },
            new BurnProfile {
                Name = "Windows 11 UEFI (Large)",
                Description = "Windows 11 UEFI with install.wim > 4GB (GPT + NTFS)",
                PartitionScheme = "gpt",
                FileSystem = "ntfs",
                ClusterSize = 0,
                QuickFormat = true,
                BypassWin11 = true,
                IsDefault = true
            },
            new BurnProfile {
                Name = "Windows 10 Legacy",
                Description = "Windows 10 for Legacy BIOS (MBR + NTFS)",
                PartitionScheme = "mbr",
                FileSystem = "ntfs",
                ClusterSize = 0,
                QuickFormat = true,
                BypassWin11 = false,
                IsDefault = true
            },
            new BurnProfile {
                Name = "Linux Live USB",
                Description = "Generic Linux live USB (GPT + FAT32)",
                PartitionScheme = "gpt",
                FileSystem = "fat32",
                ClusterSize = 0,
                QuickFormat = true,
                IsDefault = true
            },
            new BurnProfile {
                Name = "Generic FAT32",
                Description = "Universal compatibility (MBR + FAT32)",
                PartitionScheme = "mbr",
                FileSystem = "fat32",
                ClusterSize = 0,
                QuickFormat = true,
                IsDefault = true
            }
        };
        
        foreach(var profile in defaults) {
            SaveProfile(profile);
        }
        
        _logger.Info($"Created {defaults.Length} default profiles");
    }
    
    private string SanitizeFileName(string name) {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }
}


