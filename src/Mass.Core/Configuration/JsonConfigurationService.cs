using System.Text.Json;
using Mass.Core.Interfaces;
using Mass.Spec.Config;
using Mass.Core.Logging;

namespace Mass.Core.Configuration;

/// <summary>
/// Implementation of IConfigurationService using JSON persistence.
/// </summary>
public class JsonConfigurationService : IConfigurationService
{
    private readonly string _configPath;
    private readonly ILogService _logger;
    private AppSettings _settings = new();
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the JsonConfigurationService.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configPath">Optional custom path for configuration file.</param>
    public JsonConfigurationService(
        ILogService logger,
        string? configPath = null)
    {
        _logger = logger;
        
        if (string.IsNullOrEmpty(configPath))
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var massSuiteDir = Path.Combine(appData, "MassSuite");
            Directory.CreateDirectory(massSuiteDir);
            _configPath = Path.Combine(massSuiteDir, "settings.json");
        }
        else
        {
            _configPath = configPath;
            var dir = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
    }

    /// <inheritdoc />
    public T Get<T>(string key, T fallback = default!)
    {
        if (string.IsNullOrEmpty(key)) return fallback;

        try
        {
            var parts = key.Split('.');
            object? currentObj = _settings;
            
            foreach (var part in parts)
            {
                if (currentObj == null) return fallback;
                
                var prop = currentObj.GetType().GetProperty(part);
                if (prop == null) return fallback;
                
                currentObj = prop.GetValue(currentObj);
            }

            if (currentObj is T typedValue)
            {
                return typedValue;
            }
            
            // Handle type conversion if needed (basic types)
            if (currentObj != null && typeof(T) != currentObj.GetType())
            {
                try 
                {
                    return (T)Convert.ChangeType(currentObj, typeof(T));
                }
                catch
                {
                    // Ignore conversion failure
                }
            }

            return fallback;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving configuration key: {key}", ex, "Configuration");
            return fallback;
        }
    }

    /// <inheritdoc />
    public void Set<T>(string key, T value)
    {
        try
        {
            var parts = key.Split('.');
            object? currentObj = _settings;
            
            // Traverse to parent object
            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (currentObj == null) return;
                
                var prop = currentObj.GetType().GetProperty(parts[i]);
                if (prop == null) return;
                
                currentObj = prop.GetValue(currentObj);
            }

            if (currentObj != null)
            {
                var lastPart = parts[^1];
                var prop = currentObj.GetType().GetProperty(lastPart);
                if (prop != null)
                {
                    prop.SetValue(currentObj, value);
                }
                else
                {
                    _logger.LogWarning($"Configuration key not found: {key}", "Configuration");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error setting configuration key: {key}", ex, "Configuration");
        }
    }

    /// <inheritdoc />
    public async Task LoadAsync()
    {
        if (!File.Exists(_configPath))
        {
            _logger.LogInformation($"Configuration file not found at {_configPath}, using defaults.", "Configuration");
            _settings = new AppSettings();
            await SaveAsync(); // Create default file
            return;
        }

        try
        {
            using var stream = File.OpenRead(_configPath);
            var loaded = await JsonSerializer.DeserializeAsync<AppSettings>(stream, _jsonOptions);
            if (loaded != null)
            {
                _settings = loaded;
                _logger.LogInformation($"Configuration loaded from {_configPath}", "Configuration");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to load configuration from {_configPath}", ex, "Configuration");
            // Keep default settings on failure
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync()
    {
        try
        {
            var tempPath = _configPath + ".tmp";
            
            await using (var stream = File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(stream, _settings, _jsonOptions);
            }

            // Atomic move
            File.Move(tempPath, _configPath, overwrite: true);
            
            _logger.LogInformation($"Configuration saved to {_configPath}", "Configuration");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to save configuration to {_configPath}", ex, "Configuration");
            throw;
        }
    }
}
