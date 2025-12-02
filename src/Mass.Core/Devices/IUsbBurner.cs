namespace Mass.Core.Devices;

public interface IUsbBurner
{
    Task<BurnResult> BurnIsoAsync(BurnRequest request, IProgress<BurnProgress> progress, CancellationToken ct = default);
    Task<VerifyResult> VerifyAsync(string devicePath, CancellationToken ct = default);
    Task<bool> CancelAsync();
}

public record BurnRequest(
    string IsoPath,
    string TargetDeviceId,
    string PartitionScheme, // GPT, MBR
    string FileSystem, // FAT32, NTFS
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
