namespace ProUSB.Infrastructure.DiskManagement;

public static class DiskConstants {
    public const int DefaultMountDelayMs = 2000;
    public const long MbrDataOffset = 1048576;
    public const long GptDataOffset = 17825792;
    public const int MbrDataPartitionNumber = 1;
    public const int GptDataPartitionNumber = 2;
}

