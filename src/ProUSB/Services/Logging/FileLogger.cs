using System;
using System.Threading;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using ProUSB.Domain;
using ProUSB.Infrastructure;

namespace ProUSB.Services.Logging;

public class FileLogger {
    private readonly string _logPath;
    private readonly Lock _lock = new();
    private readonly long _maxBytes = 10 * 1024 * 1024;
    private readonly ObservableCollection<LogEntry> _logEntries = new();
    private readonly int _maxLogEntries = 10000;

    public ReadOnlyObservableCollection<LogEntry> LogEntries { get; }

    private LogLevel _minLevel = LogLevel.Info;
    public LogLevel MinLevel {
        get => _minLevel;
        set => _minLevel = value >= LogLevel.Debug && value <= LogLevel.Error ? value : LogLevel.Info;
    }

    public FileLogger(PortablePathManager pathManager) {
        string logDir = pathManager.GetLogsDirectory();
        Directory.CreateDirectory(logDir);
        _logPath = Path.Combine(logDir, "burn_debug.log");
        LogEntries = new ReadOnlyObservableCollection<LogEntry>(_logEntries);
        Info($"=== ProUSBMediaSuite Logging Started (Portable: {pathManager.IsPortableMode()}) ===");
    }

    private void Write(LogLevel level, string message, Exception? ex = null) {
        if(level < MinLevel) return;

        var entry = new LogEntry {
            Timestamp = DateTime.Now,
            Level = level,
            Message = ex != null ? $"{message}\n    Exception: {ex.GetType().Name}: {ex.Message}" : message,
            Source = "ProUSBMediaSuite"
        };

        _logEntries.Add(entry);

        if(_logEntries.Count > _maxLogEntries) {
            _logEntries.RemoveAt(0);
        }

        WriteToFile(entry, ex);
    }

    private void WriteToFile(LogEntry entry, Exception? ex = null) {
        lock(_lock) {
            try {
                if(File.Exists(_logPath) && new FileInfo(_logPath).Length > _maxBytes) {
                    var archived = Path.Combine(
                        Path.GetDirectoryName(Path.GetFullPath(_logPath))!,
                        Path.GetFileNameWithoutExtension(_logPath) + $"-{DateTime.UtcNow:yyyyMMddHHmmss}.log"
                    );
                    File.Move(_logPath, archived);
                }

                var logLine = entry.GetFormattedEntry();
                if(ex != null) {
                    logLine += $"\n    StackTrace: {ex.StackTrace}";
                }
                File.AppendAllText(_logPath, logLine + Environment.NewLine);
            } catch { }
        }
    }

    public void Debug(string message) => Write(LogLevel.Debug, message);
    public void Info(string message) => Write(LogLevel.Info, message);
    public void Warn(string message) => Write(LogLevel.Warn, message);
    public void Error(string message, Exception? ex = null) => Write(LogLevel.Error, message, ex);

    public List<LogEntry> FilterByLevel(LogLevel level) {
        return _logEntries.Where(e => e.Level == level).ToList();
    }

    public List<LogEntry> FilterByLevels(bool showInfo, bool showWarn, bool showError, bool showDebug) {
        return _logEntries
            .Where(e =>
                (showInfo && e.Level == LogLevel.Info) ||
                (showWarn && e.Level == LogLevel.Warn) ||
                (showError && e.Level == LogLevel.Error) ||
                (showDebug && e.Level == LogLevel.Debug)
            )
            .ToList();
    }

    public List<LogEntry> Search(string query) {
        if(string.IsNullOrWhiteSpace(query)) return _logEntries.ToList();

        var lowerQuery = query.ToLowerInvariant();
        return _logEntries
            .Where(e => e.Message.ToLowerInvariant().Contains(lowerQuery))
            .ToList();
    }

    public List<LogEntry> FilterAndSearch(bool showInfo, bool showWarn, bool showError, bool showDebug, string query) {
        var filtered = FilterByLevels(showInfo, showWarn, showError, showDebug);

        if(string.IsNullOrWhiteSpace(query)) return filtered;

        var lowerQuery = query.ToLowerInvariant();
        return filtered
            .Where(e => e.Message.ToLowerInvariant().Contains(lowerQuery))
            .ToList();
    }

    public async Task ExportToTextAsync(string filePath) {
        var lines = _logEntries.Select(e => e.GetFormattedEntry());
        await File.WriteAllLinesAsync(filePath, lines);
    }

    public async Task ExportToCsvAsync(string filePath) {
        List<string> lines = ["Timestamp,Level,Message"];
        lines.AddRange(_logEntries.Select(e => e.GetCsvEntry()));
        await File.WriteAllLinesAsync(filePath, lines);
    }

    public async Task ExportToJsonAsync(string filePath) {
        var options = new JsonSerializerOptions { WriteIndented = true };
        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, _logEntries, options);
    }

    public void ClearLogs() {
        _logEntries.Clear();
    }
}


