using System.Text.Json;

namespace Mass.Core.Configuration;

public class ConfigurationManager : IConfigurationManager
{
    private readonly string _configPath;
    private readonly JsonSerializerOptions _jsonOptions;
    
    // C# 14: field keyword is available for properties if needed, 
    // but here we use a standard backing field for the singleton instance logic if we were doing that.
    // For now, standard implementation.
    
    public UnifiedConfiguration Current { get; private set; } = new();

    public ConfigurationManager(string configPath)
    {
        _configPath = configPath;
        _jsonOptions = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNameCaseInsensitive = true 
        };
    }

    public async Task LoadAsync()
    {
        if (!File.Exists(_configPath))
        {
            Current = new UnifiedConfiguration();
            await SaveAsync();
            return;
        }

        try 
        {
            var json = await File.ReadAllTextAsync(_configPath);
            Current = JsonSerializer.Deserialize<UnifiedConfiguration>(json, _jsonOptions) ?? new UnifiedConfiguration();
        }
        catch
        {
            // Fallback to default if corrupt
            Current = new UnifiedConfiguration();
        }
    }

    public async Task SaveAsync()
    {
        var dir = Path.GetDirectoryName(_configPath);
        if (dir != null) Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(Current, _jsonOptions);
        await File.WriteAllTextAsync(_configPath, json);
    }

    public void Update(Action<UnifiedConfiguration> updateAction)
    {
        updateAction(Current);
        // Fire and forget save, or user should call SaveAsync explicitly?
        // For safety, we'll let the caller decide when to persist, 
        // but this method implies a state change. 
        // Let's make it async in a real app, but interface said void. 
        // We'll leave it in memory.
    }
}
