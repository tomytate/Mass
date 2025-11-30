using System;
using System.Collections.Generic;

namespace ProUSB.Domain;

public enum DownloadStatus {
    Queued,
    Downloading,
    Verifying,
    Complete,
    Failed,
    Cancelled
}

public record OsInfo {
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Vendor { get; init; }
    public required string Version { get; init; }
    public required string Architecture { get; init; }
    public required string Category { get; init; }
    public string? DirectDownloadUrl { get; init; }
    public string? DownloadPageUrl { get; init; }
    public long? FileSizeBytes { get; init; }
    public string? Sha256Checksum { get; init; }
    public DateTime? ReleaseDate { get; init; }
}

public record DownloadJob {
    public required OsInfo OS { get; init; }
    public required string OutputPath { get; init; }
    public DownloadStatus Status { get; set; }
    public long BytesDownloaded { get; set; }
    public long TotalBytes { get; set; }
    public double SpeedBytesPerSecond { get; set; }
    public TimeSpan? TimeRemaining { get; set; }
    public string? ErrorMessage { get; set; }
}

public record DownloadProgress {
    public required double PercentComplete { get; init; }
    public required long BytesDownloaded { get; init; }
    public required long TotalBytes { get; init; }
    public required double SpeedMBps { get; init; }
    public required TimeSpan TimeRemaining { get; init; }
    public required string Status { get; init; }
}

public record OsCatalog {
    public required List<OsInfo> OperatingSystems { get; init; }
}

