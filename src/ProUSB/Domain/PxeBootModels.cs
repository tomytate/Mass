using System;
using System.Collections.Generic;

namespace ProUSB.Domain;

public enum PxeBootType {
    Windows,
    Linux,
    Generic
}

public record PxeBootConfiguration {
    public required string OutputDirectory { get; init; }
    public required string IsoPath { get; init; }
    public required PxeBootType BootType { get; init; }
    public bool GenerateUefiConfig { get; init; } = true;
    public bool GenerateBiosConfig { get; init; } = true;
}

public record PxeFileMapping {
    public required string SourcePath { get; init; }
    public required string DestinationPath { get; init; }
    public required string Description { get; init; }
    public long FileSize { get; init; }
}

public record PxeBootResult {
    public required bool Success { get; init; }
    public required List<PxeFileMapping> FilesCreated { get; init; }
    public required List<string> Warnings { get; init; }
    public string? ErrorMessage { get; init; }
    public TimeSpan Duration { get; init; }
}

public record PxeCreationProgress {
    public required double PercentComplete { get; init; }
    public required string Message { get; init; }
    public required string CurrentFile { get; init; }
    public long BytesProcessed { get; init; }
    public long TotalBytes { get; init; }
}

