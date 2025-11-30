using System;

namespace ProUSB.Infrastructure.DiskManagement;

public class FormatFailedException : Exception
{
    public FormatFailedException(string message) : base(message) { }
    public FormatFailedException(string message, Exception innerException) : base(message, innerException) { }
}

