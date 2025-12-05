namespace ProUSB.Services;

public class SafetyConfig
{
    public bool AllowRealWrites { get; set; } = false;
    public bool RequireElevation { get; set; } = true;
}
