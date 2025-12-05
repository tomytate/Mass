namespace ProUSB.Services;

/// <summary>
/// Exception thrown when an operation fails due to safety or permission issues.
/// </summary>
public class OperationException : Exception
{
    /// <summary>
    /// Error code for programmatic handling.
    /// </summary>
    public string ErrorCode { get; }
    
    /// <summary>
    /// Initializes a new instance of the OperationException.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    /// <param name="message">The error message.</param>
    public OperationException(string errorCode, string message) 
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
