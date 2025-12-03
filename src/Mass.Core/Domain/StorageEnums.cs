namespace Mass.Core.Domain;

public enum FileSystem
{
    FAT32,
    NTFS,
    exFAT,
    ext4,
    APFS,
    HFS
}

public enum PartitionScheme
{
    MBR,
    GPT,
    Hybrid
}

public static class FileSystemExtensions
{
    public static int GetMaxFileSizeBytes(this FileSystem fs) => fs switch
    {
        FileSystem.FAT32 => int.MaxValue,
        FileSystem.NTFS => int.MaxValue,
        FileSystem.exFAT => int.MaxValue,
        _ => int.MaxValue
    };

    public static bool SupportsLargeFiles(this FileSystem fs) => fs switch
    {
        FileSystem.FAT32 => false,
        _ => true
    };

    public static string ToFormatString(this FileSystem fs) => fs switch
    {
        FileSystem.FAT32 => "FAT32",
        FileSystem.NTFS => "NTFS",
        FileSystem.exFAT => "exFAT",
        FileSystem.ext4 => "ext4",
        _ => fs.ToString()
    };

    public static ReadOnlySpan<int> GetValidClusterSizes(this FileSystem fs) => fs switch
    {
        FileSystem.FAT32 => [512, 1024, 2048, 4096, 8192, 16384, 32768],
        FileSystem.NTFS => [512, 1024, 2048, 4096, 8192, 16384, 32768, 65536],
        FileSystem.exFAT => [512, 1024, 2048, 4096, 8192, 16384, 32768, 65536, 131072],
        _ => [4096]
    };

    public static bool TryParse(ReadOnlySpan<char> value, out FileSystem result)
    {
        if (value.Equals("FAT32", StringComparison.OrdinalIgnoreCase)) { result = FileSystem.FAT32; return true; }
        if (value.Equals("NTFS", StringComparison.OrdinalIgnoreCase)) { result = FileSystem.NTFS; return true; }
        if (value.Equals("exFAT", StringComparison.OrdinalIgnoreCase)) { result = FileSystem.exFAT; return true; }
        if (value.Equals("ext4", StringComparison.OrdinalIgnoreCase)) { result = FileSystem.ext4; return true; }
        result = default;
        return false;
    }
}

public static class PartitionSchemeExtensions
{
    public static bool SupportsUefi(this PartitionScheme scheme) => scheme switch
    {
        PartitionScheme.GPT => true,
        PartitionScheme.Hybrid => true,
        PartitionScheme.MBR => false,
        _ => false
    };

    public static bool SupportsLegacyBios(this PartitionScheme scheme) => scheme switch
    {
        PartitionScheme.MBR => true,
        PartitionScheme.Hybrid => true,
        PartitionScheme.GPT => false,
        _ => false
    };

    public static long GetMaxDiskSizeBytes(this PartitionScheme scheme) => scheme switch
    {
        PartitionScheme.MBR => 2L * 1024 * 1024 * 1024 * 1024,
        PartitionScheme.GPT => long.MaxValue,
        PartitionScheme.Hybrid => 2L * 1024 * 1024 * 1024 * 1024,
        _ => long.MaxValue
    };

    public static bool TryParse(ReadOnlySpan<char> value, out PartitionScheme result)
    {
        if (value.Equals("MBR", StringComparison.OrdinalIgnoreCase)) { result = PartitionScheme.MBR; return true; }
        if (value.Equals("GPT", StringComparison.OrdinalIgnoreCase)) { result = PartitionScheme.GPT; return true; }
        if (value.Equals("Hybrid", StringComparison.OrdinalIgnoreCase)) { result = PartitionScheme.Hybrid; return true; }
        result = default;
        return false;
    }
}
