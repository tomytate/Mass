namespace Mass.Launcher.Services;

public static class ErrorMessageMapper
{
    public static string GetFriendlyMessage(Exception ex)
    {
        return ex switch
        {
            UnauthorizedAccessException => "Access denied. Please run the application as administrator.",
            FileNotFoundException => "The specified file could not be found.",
            DirectoryNotFoundException => "The specified directory could not be found.",
            IOException ioEx when ioEx.Message.Contains("space") => "There is not enough space on the drive for this operation.",
            IOException ioEx when ioEx.Message.Contains("lock") => "The drive is currently locked or in use by another process.",
            TimeoutException => "The operation timed out. Please try again.",
            OperationCanceledException => "The operation was cancelled.",
            ArgumentException => "Invalid input. Please check your inputs and try again.",
            _ => ex.Message
        };
    }

    public static string GetFriendlyTitle(Exception ex)
    {
        return ex switch
        {
            UnauthorizedAccessException => "Permission Error",
            FileNotFoundException or DirectoryNotFoundException => "File Error",
            IOException => "I/O Error",
            TimeoutException => "Timeout Error",
            OperationCanceledException => "Operation Cancelled",
            ArgumentException => "Validation Error",
            _ => "Error"
        };
    }
}
