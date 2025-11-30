using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ProUSB.Infrastructure;

namespace ProUSB.Services.DeviceManagement;

public record DeviceNameMapping {
    public required string DeviceId { get; init; }
    public required string CustomName { get; init; }
    public DateTime LastUsed { get; init; } = DateTime.Now;
    public int UseCount { get; init; } = 0;
}

public class DeviceNamingService {
    private readonly string _mappingsPath;
    private readonly Dictionary<string, DeviceNameMapping> _mappings = [];

    public DeviceNamingService(PortablePathManager pathManager) {
        string dataDir = pathManager.GetDataDirectory();
        Directory.CreateDirectory(dataDir);
        _mappingsPath = Path.Combine(dataDir, "device_names.json");
        _ = LoadMappingsAsync();
    }

    public string GetDeviceName(string deviceId, string defaultName) =>
        _mappings.TryGetValue(deviceId, out var mapping) ? mapping.CustomName : defaultName;

    public async Task SetDeviceNameAsync(string deviceId, string customName) {
        if (_mappings.TryGetValue(deviceId, out var existing)) {
            _mappings[deviceId] = existing with {
                CustomName = customName,
                LastUsed = DateTime.Now,
                UseCount = existing.UseCount + 1
            };
        } else {
            _mappings[deviceId] = new DeviceNameMapping {
                DeviceId = deviceId,
                CustomName = customName,
                LastUsed = DateTime.Now,
                UseCount = 1
            };
        }
        
        await SaveMappingsAsync();
    }

    public async Task RemoveDeviceNameAsync(string deviceId) {
        _mappings.Remove(deviceId);
        await SaveMappingsAsync();
    }

    public List<DeviceNameMapping> GetAllMappings() => [.. _mappings.Values];

    public List<DeviceNameMapping> GetRecentDevices(int limit = 10) =>
        [.. _mappings.Values.OrderByDescending(m => m.LastUsed).Take(limit)];

    private async Task LoadMappingsAsync() {
        try {
            if (File.Exists(_mappingsPath)) {
                var json = await File.ReadAllTextAsync(_mappingsPath);
                var loaded = JsonSerializer.Deserialize<List<DeviceNameMapping>>(json);
                if (loaded != null) {
                    foreach (var mapping in loaded) {
                        _mappings[mapping.DeviceId] = mapping;
                    }
                }
            }
        } catch { }
    }

    private async Task SaveMappingsAsync() {
        try {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_mappings.Values, options);
            await File.WriteAllTextAsync(_mappingsPath, json);
        } catch { }
    }
}


