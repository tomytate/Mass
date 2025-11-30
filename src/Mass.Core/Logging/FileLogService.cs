using System.Collections.Concurrent;
using System.Text.Json;

namespace Mass.Core.Logging;

public class FileLogService : ILogService
{
    private readonly string _logDirectory;
    private readonly string _currentLogFile;
    private readonly ConcurrentQueue<LogEntry> _logBuffer = new();
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private const int MaxLogFiles = 30;
    private const long MaxLogFileSize = 10 * 1024 * 1024; // 10MB

    public FileLogService()
    {
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MassSuite");
        _logDirectory = Path.Combine(appData, "logs");
        Directory.CreateDirectory(_logDirectory);
        
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        _currentLogFile = Path.Combine(_logDirectory, $"mass_{today}.log");
        
        CleanupOldLogs();
    }

    public void Log(LogLevel level, string category, string message, Exception? exception = null, Dictionary<string, object>? properties = null)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = level,
            Category = category,
            Message = message,
            Exception = exception?.ToString(),
            Properties = properties ?? new Dictionary<string, object>()
        };

        _logBuffer.Enqueue(entry);
        WriteToFile(entry);
    }

    public void LogTrace(string message, string category = "Application") 
        => Log(LogLevel.Trace, category, message);

    public void LogDebug(string message, string category = "Application") 
        => Log(LogLevel.Debug, category, message);

    public void LogInformation(string message, string category = "Application") 
        => Log(LogLevel.Information, category, message);

    public void LogWarning(string message, string category = "Application") 
        => Log(LogLevel.Warning, category, message);

    public void LogError(string message, Exception? exception = null, string category = "Application") 
        => Log(LogLevel.Error, category, message, exception);

    public void LogCritical(string message, Exception? exception = null, string category = "Application") 
        => Log(LogLevel.Critical, category, message, exception);

    public IEnumerable<LogEntry> GetLogs(int maxCount = 1000)
    {
        return LoadLogsFromFile().Take(maxCount);
    }

    public IEnumerable<LogEntry> GetLogsByLevel(LogLevel level, int maxCount = 1000)
    {
        return LoadLogsFromFile().Where(l => l.Level == level).Take(maxCount);
    }

    public IEnumerable<LogEntry> SearchLogs(string searchTerm, int maxCount = 1000)
    {
        var lowerSearch = searchTerm.ToLowerInvariant();
        return LoadLogsFromFile()
            .Where(l => l.Message.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                       l.Category.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .Take(maxCount);
    }

    public void ClearLogs()
    {
        _logBuffer.Clear();
        try
        {
            if (File.Exists(_currentLogFile))
            {
                File.Delete(_currentLogFile);
            }
        }
        catch { }
    }

    private void WriteToFile(LogEntry entry)
    {
        _ = Task.Run(async () =>
        {
            await _writeLock.WaitAsync();
            try
            {
                CheckLogRotation();
                var line = $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{entry.Level}] [{entry.Category}] {entry.Message}";
                if (!string.IsNullOrEmpty(entry.Exception))
                {
                    line += $"\n{entry.Exception}";
                }
                await File.AppendAllTextAsync(_currentLogFile, line + Environment.NewLine);
            }
            catch { }
            finally
            {
                _writeLock.Release();
            }
        });
    }

    private void CheckLogRotation()
    {
        if (File.Exists(_currentLogFile))
        {
            var fileInfo = new FileInfo(_currentLogFile);
            if (fileInfo.Length > MaxLogFileSize)
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
                var archiveName = Path.Combine(_logDirectory, $"mass_{timestamp}_archived.log");
                File.Move(_currentLogFile, archiveName);
                CleanupOldLogs();
            }
        }
    }

    private void CleanupOldLogs()
    {
        try
        {
            var logFiles = Directory.GetFiles(_logDirectory, "*.log")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .ToList();

            foreach (var file in logFiles.Skip(MaxLogFiles))
            {
                file.Delete();
            }
        }
        catch { }
    }

    private List<LogEntry> LoadLogsFromFile()
    {
        var entries = new List<LogEntry>();
        
        try
        {
            if (!File.Exists(_currentLogFile))
                return entries;

            var lines = File.ReadAllLines(_currentLogFile);
            foreach (var line in lines)
            {
                if (TryParseLogLine(line, out var entry))
                {
                    entries.Add(entry);
                }
            }
        }
        catch { }

        return entries.OrderByDescending(e => e.Timestamp).ToList();
    }

    private bool TryParseLogLine(string line, out LogEntry entry)
    {
        entry = new LogEntry();
        
        try
        {
            var parts = line.Split(new[] { "] [", "] " }, StringSplitOptions.None);
            if (parts.Length < 4) return false;

            var timestampStr = parts[0].TrimStart('[');
            entry.Timestamp = DateTime.Parse(timestampStr);
            entry.Level = Enum.Parse<LogLevel>(parts[1]);
            entry.Category = parts[2];
            entry.Message = string.Join("] ", parts.Skip(3));
            
            return true;
        }
        catch
        {
            return false;
        }
    }
}
