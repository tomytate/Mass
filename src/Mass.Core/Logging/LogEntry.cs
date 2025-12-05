using Mass.Spec.Contracts.Logging;

namespace Mass.Core.Logging;

public class CoreLogEntry : LogEntry
{
    public string Category
    {
        get => Source;
        set => Source = value;
    }

    public Dictionary<string, object> Properties { get; set; } = new();
}
