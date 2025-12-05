namespace ProUSB.Services;

public class OperationException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
