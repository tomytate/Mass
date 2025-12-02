using System.CommandLine;
using Mass.Core;
using Mass.Core.Configuration;
using Spectre.Console;

namespace Mass.CLI.Commands;

public class ConfigCommand : Command
{
    private readonly IConfigurationManager _configManager;

    public ConfigCommand() : base("config", "Manage application configuration")
    {
        // In a real DI scenario, this would be injected.
        // For CLI, we initialize it here or pass it in.
        _configManager = new ConfigurationManager(Constants.ConfigPath);
        
        AddCommand(CreateListCommand());
        AddCommand(CreateGetCommand());
        AddCommand(CreateSetCommand());
    }

    private Command CreateListCommand()
    {
        var command = new Command("list", "List all configuration values");
        command.SetHandler(async () => await ListConfig());
        return command;
    }

    private Command CreateGetCommand()
    {
        var command = new Command("get", "Get a configuration value");
        var keyArgument = new Argument<string>("key", "Configuration key (e.g. App.Theme)");
        command.AddArgument(keyArgument);
        command.SetHandler(GetConfig, keyArgument);
        return command;
    }

    private Command CreateSetCommand()
    {
        var command = new Command("set", "Set a configuration value");
        var keyArgument = new Argument<string>("key", "Configuration key (e.g. App.Theme)");
        var valueArgument = new Argument<string>("value", "Configuration value");
        command.AddArgument(keyArgument);
        command.AddArgument(valueArgument);
        command.SetHandler(SetConfig, keyArgument, valueArgument);
        return command;
    }

    private async Task ListConfig()
    {
        await _configManager.LoadAsync();
        var config = _configManager.Current;

        var table = new Table();
        table.AddColumn("Section");
        table.AddColumn("Key");
        table.AddColumn("Value");

        // App Settings
        table.AddRow("App", "Theme", config.App.Theme);
        table.AddRow("App", "Language", config.App.Language);
        
        // PXE Settings
        table.AddRow("Pxe", "RootPath", config.Pxe.RootPath);
        table.AddRow("Pxe", "EnableDhcp", config.Pxe.EnableDhcp.ToString());

        // USB Settings
        table.AddRow("Usb", "VerifyAfterBurn", config.Usb.VerifyAfterBurn.ToString());
        
        AnsiConsole.Write(table);
    }

    private async Task GetConfig(string key)
    {
        await _configManager.LoadAsync();
        // Simple reflection or property traversal could go here.
        // For now, just printing that we need to implement the traversal logic.
        AnsiConsole.MarkupLine($"[yellow]Get logic for '{key}' not fully implemented in this refactor step yet.[/]");
    }

    private async Task SetConfig(string key, string value)
    {
        await _configManager.LoadAsync();
        
        // Update logic would go here.
        // For now, just acknowledging.
        
        AnsiConsole.MarkupLine($"[green]Configuration updated (simulation):[/]");
        AnsiConsole.MarkupLine($"[bold]{key}[/] = [blue]{value}[/]");
        
        // await _configManager.SaveAsync();
    }
}
