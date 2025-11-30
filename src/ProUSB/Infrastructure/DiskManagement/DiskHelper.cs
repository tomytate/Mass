using System;

namespace ProUSB.Infrastructure.DiskManagement;

public static class DiskHelper
{
    public static int GetDataPartitionNumber(bool isGpt)
    {
        return isGpt ? DiskConstants.GptDataPartitionNumber : DiskConstants.MbrDataPartitionNumber;
    }

    public static long GetExpectedOffset(string partitionScheme)
    {
        if (string.IsNullOrWhiteSpace(partitionScheme))
            throw new ArgumentNullException(nameof(partitionScheme));

        return partitionScheme.Contains("GPT", StringComparison.OrdinalIgnoreCase)
            ? DiskConstants.GptDataOffset
            : DiskConstants.MbrDataOffset;
    }
}

