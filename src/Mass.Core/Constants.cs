namespace Mass.Core;

public static class Constants
{
    public static readonly string AppDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        "MassSuite");
        
    public static readonly string ConfigPath = Path.Combine(AppDataPath, "config.json");
    
    public static readonly string LogsPath = Path.Combine(AppDataPath, "logs");
    public static readonly string PluginsPath = Path.Combine(AppDataPath, "plugins");
}
