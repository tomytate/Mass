using System;

namespace ProUSB.Infrastructure.DiskManagement;

public class MountFailedException : Exception
{
    public MountFailedException(string message) : base(message) { }
    public MountFailedException(string message, Exception innerException) : base(message, innerException) { }
}

