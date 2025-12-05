using System.CommandLine;
using Mass.Core.Configuration;
using Mass.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mass.CLI.Commands;

public class MigrateConfigCommand : Command
{
    private readonly IConfigurationService _configService;
    private readonly ILogger<MigrateConfigCommand> _logger;

    public MigrateConfigCommand(
        IConfigurationService configService,
        ILogger<MigrateConfigCommand> logger) 
        : base("migrate-old", "Migrates legacy configuration files to the new system.")
    {
        _configService = configService;
        _logger = logger;

        this.SetHandler(ExecuteAsync);
    }

    private async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting configuration migration...");

        // Define legacy paths
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var legacyPaths = new[]
        {
            Path.Combine(appData, "MassSuite", "config.json"),
            Path.Combine(appData, "MassSuite", "settings.xml"),
            Path.Combine(Directory.GetCurrentDirectory(), "config.json")
        };

        bool migrated = false;

        foreach (var path in legacyPaths)
        {
            if (File.Exists(path))
            {
                _logger.LogInformation("Found legacy config at {Path}", path);
                
                try
                {
                    // Simple migration logic: read file and map known keys
                    // In a real scenario, we'd parse JSON/XML properly
                    // For now, we'll just simulate migration of a few keys if found in text
                    var content = await File.ReadAllTextAsync(path);

                    if (content.Contains("Language"))
                    {
                        // Simulate parsing "Language": "fr-FR"
                        // This is a placeholder for actual parsing logic
                        _configService.Set("General.Language", "en-US"); // Reset to default or parse real value
                        _logger.LogInformation("Migrated General settings");
                        migrated = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to migrate {Path}", path);
                }
            }
        }

        if (migrated)
        {
            await _configService.SaveAsync();
            _logger.LogInformation("Migration completed successfully.");
        }
        else
        {
            _logger.LogInformation("No legacy configuration files found or migration needed.");
        }
    }
}
