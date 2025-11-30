using System;

namespace ProUSB.Services.Diagnostics;

public class SmartHealthException : Exception
{
    public SmartHealthException(string message) : base(message) { }
    public SmartHealthException(string message, Exception innerException) : base(message, innerException) { }
}

