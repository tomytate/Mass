using ProUSB.Domain;

namespace ProUSB.Engine;

/// <summary>
/// Legacy types for BurnEngine compatibility.
/// </summary>
public record BurnRequest(
    string IsoPath,
    string TargetDeviceId,
    string PartitionScheme,
    string FileSystem,
    int PersistenceSizeMB = 0
);

public record BurnResult(bool Success, string Message, TimeSpan Duration);

public record BurnProgress(
    int Percentage,
    string CurrentOperation,
    long BytesProcessed,
    long TotalBytes
);

public record VerifyResult(bool Success, string Message, List<string> Errors);
