using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ProUSB.Domain;
using ProUSB.Infrastructure;

namespace ProUSB.Services.History;

public record BurnHistoryEntry {
    public required DateTime Timestamp { get; init; }
    public required string DeviceName { get; init; }
    public required string DeviceId { get; init; }
    public required string IsoPath { get; init; }
    public required string Strategy { get; init; }
    public required bool Success { get; init; }
    public required long DurationMs { get; init; }
    public string? ErrorMessage { get; init; }
    public long? BytesWritten { get; init; }
}

public record BurnStatistics {
    public int TotalBurns { get; init; }
    public int SuccessfulBurns { get; init; }
    public int FailedBurns { get; init; }
    public double SuccessRate => TotalBurns > 0 ? (double)SuccessfulBurns / TotalBurns * 100 : 0;
    public long TotalDataWritten { get; init; }
    public TimeSpan TotalTime { get; init; }
    public string MostUsedDevice { get; init; } = "None";
    public string MostUsedStrategy { get; init; } = "None";
}

public class BurnHistoryService {
    private readonly string _historyPath;
    private readonly List<BurnHistoryEntry> _history = [];
    private const int MaxHistoryEntries = 1000;

    public BurnHistoryService(PortablePathManager pathManager) {
        string historyDir = pathManager.GetDataDirectory();
        Directory.CreateDirectory(historyDir);
        _historyPath = Path.Combine(historyDir, "burn_history.json");
        _ = LoadHistoryAsync();
    }

    public async Task AddEntryAsync(BurnHistoryEntry entry) {
        _history.Insert(0, entry);
        
        if (_history.Count > MaxHistoryEntries) {
            _history.RemoveAt(_history.Count - 1);
        }
        
        await SaveHistoryAsync();
    }

    public List<BurnHistoryEntry> GetHistory(int limit = 100) => 
        _history.Take(limit).ToList();

    public List<BurnHistoryEntry> GetHistoryForDevice(string deviceId, int limit = 50) => 
        _history.Where(e => e.DeviceId == deviceId).Take(limit).ToList();

    public List<BurnHistoryEntry> GetHistoryForIso(string isoPath, int limit = 50) => 
        _history.Where(e => e.IsoPath.Equals(isoPath, StringComparison.OrdinalIgnoreCase))
                .Take(limit)
                .ToList();

    public BurnStatistics GetStatistics() {
        if (_history.Count == 0) {
            return new BurnStatistics {
                TotalBurns = 0,
                SuccessfulBurns = 0,
                FailedBurns = 0,
                TotalDataWritten = 0,
                TotalTime = TimeSpan.Zero
            };
        }

        var successful = _history.Count(h => h.Success);
        var deviceGroups = _history.GroupBy(h => h.DeviceName).OrderByDescending(g => g.Count());
        var strategyGroups = _history.GroupBy(h => h.Strategy).OrderByDescending(g => g.Count());

        return new BurnStatistics {
            TotalBurns = _history.Count,
            SuccessfulBurns = successful,
            FailedBurns = _history.Count - successful,
            TotalDataWritten = _history.Sum(h => h.BytesWritten ?? 0),
            TotalTime = TimeSpan.FromMilliseconds(_history.Sum(h => h.DurationMs)),
            MostUsedDevice = deviceGroups.FirstOrDefault()?.Key ?? "None",
            MostUsedStrategy = strategyGroups.FirstOrDefault()?.Key ?? "None"
        };
    }

    public BurnHistoryEntry? GetLastSuccessfulBurnForDevice(string deviceId) =>
        _history.FirstOrDefault(e => e.DeviceId == deviceId && e.Success);

    public async Task ClearHistoryAsync() {
        _history.Clear();
        await SaveHistoryAsync();
    }

    private async Task LoadHistoryAsync() {
        try {
            if (File.Exists(_historyPath)) {
                var json = await File.ReadAllTextAsync(_historyPath);
                var loaded = JsonSerializer.Deserialize<List<BurnHistoryEntry>>(json);
                if (loaded != null) {
                    _history.AddRange(loaded.Take(MaxHistoryEntries));
                }
            }
        } catch { }
    }

    private async Task SaveHistoryAsync() {
        try {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_history, options);
            await File.WriteAllTextAsync(_historyPath, json);
        } catch { }
    }
}


