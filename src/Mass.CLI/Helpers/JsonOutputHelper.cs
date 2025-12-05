using System.Text.Json;

namespace Mass.CLI.Helpers;

public static class JsonOutputHelper
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    
    public static void WriteSuccess<T>(T result)
    {
        var output = new
        {
            success = true,
            data = result
        };
        Console.WriteLine(JsonSerializer.Serialize(output, Options));
    }
    
    public static void WriteError(string message, int exitCode)
    {
        var output = new
        {
            success = false,
            error = message,
            exitCode = exitCode
        };
        Console.WriteLine(JsonSerializer.Serialize(output, Options));
    }
    
    public static void WriteError(Exception ex, int exitCode)
    {
        WriteError(ex.Message, exitCode);
    }
}
