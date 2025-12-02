namespace Mass.Core.Devices;

public interface IPxeGenerator
{
    Task<string> GeneratePxeBootAsync(PxeConfig config, CancellationToken ct = default);
    Task<bool> ValidateConfigAsync(PxeConfig config);
}

public record PxeConfig(
    string ServerUrl, 
    string BootMenuPath, 
    Dictionary<string, string> Options
);
