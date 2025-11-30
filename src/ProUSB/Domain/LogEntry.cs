using System;

namespace ProUSB.Domain;

public record LogEntry {
    public DateTime Timestamp { get; init; }
    public LogLevel Level { get; init; }
    public string Message { get; init; } = "";
    public string Source { get; init; } = "";

    public string GetFormattedEntry() {
        return $"[{Timestamp:HH:mm:ss}] [{Level}] {Message}";
    }

    public string GetCsvEntry() {
        return $"\"{Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{Level}\",\"{Message.Replace("\"", "\"\"")}\"";
    }
}

