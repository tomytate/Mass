namespace ProUSB.Domain;

public enum IsoCreationMode {
    RawCopy,
    SmartCopy
}

public record IsoCreationProgress {
    public double PercentComplete { get; init; }
    public long BytesCopied { get; init; }
    public long TotalBytes { get; init; }
    public double SpeedBytesPerSecond { get; init; }
    public string Message { get; init; } = "";
}

public record IsoCreationResult {
    public bool Success { get; init; }
    public string OutputPath { get; init; } = "";
    public long FileSizeBytes { get; init; }
    public IsoCreationMode CreationMode { get; init; }
    public TimeSpan Duration { get; init; }
    public string? ErrorMessage { get; init; }
}

