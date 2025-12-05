namespace Mass.CLI;

public static class ExitCodes
{
    public const int Success = 0;
    public const int GeneralError = 1;
    public const int DriveNotFound = 10;
    public const int InsufficientSpace = 11;
    public const int AccessDenied = 12;
    public const int InvalidFile = 13;
    public const int DriveLocked = 14;
    public const int WorkflowError = 20;
    public const int WorkflowValidationError = 21;
    public const int ConfigError = 30;
    public const int NetworkError = 40;
    public const int PluginError = 50;
    public const int TimeoutError = 60;
    public const int CancellationError = 70;
    
    public static int FromException(Exception ex) => ex switch
    {
        UnauthorizedAccessException => AccessDenied,
        FileNotFoundException => InvalidFile,
        DirectoryNotFoundException => InvalidFile,
        IOException ioEx when ioEx.Message.Contains("space") => InsufficientSpace,
        IOException ioEx when ioEx.Message.Contains("lock") => DriveLocked,
        TimeoutException => TimeoutError,
        OperationCanceledException => CancellationError,
        ArgumentException => GeneralError,
        _ => GeneralError
    };
}
