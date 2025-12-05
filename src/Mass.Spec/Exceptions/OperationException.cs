using System.Text;

namespace Mass.Spec.Exceptions;

/// <summary>
/// Represents a structured operation failure in Mass Suite.
/// </summary>
public class OperationException : Exception
{
    public ErrorCode Error { get; }
    public Dictionary<string, object?> Context { get; }

    public OperationException(ErrorCode error, Dictionary<string, object?>? context = null, Exception? innerException = null)
        : base(FormatMessage(error, context), innerException)
    {
        Error = error;
        Context = context ?? new Dictionary<string, object?>();
    }

    private static string FormatMessage(ErrorCode error, Dictionary<string, object?>? context)
    {
        if (context == null || context.Count == 0)
        {
            return error.MessageTemplate;
        }

        var sb = new StringBuilder(error.MessageTemplate);
        foreach (var kvp in context)
        {
            sb.Replace($"{{{kvp.Key}}}", kvp.Value?.ToString() ?? "null");
        }
        return sb.ToString();
    }
}
