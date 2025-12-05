namespace ProUSB.Services;

/// <summary>
/// Safety configuration for hardware write operations.
/// </summary>
public class SafetyConfig
{
    /// <summary>
    /// Controls whether real hardware writes are allowed.
    /// Default: FALSE (disabled for safety)
    /// </summary>
    public bool AllowRealWrites { get; set; } = false;
    
    /// <summary>
    /// Requires administrator elevation for writes.
    /// Default: TRUE (always require elevation)
    /// </summary>
    public bool RequireElevation { get; set; } = true;
}
