using System.CommandLine;
using Mass.Core.Configuration;
using Mass.Core.Interfaces;
using Mass.Core.Logging;
using Mass.Spec.Config;
using Microsoft.Extensions.Logging.Abstractions;
using Spectre.Console;

namespace Mass.CLI.Commands;

public class ConfigCommand : Command
{
    private readonly IConfigurationService _configService;

    public ConfigCommand() : base("config", "Manage application configuration")
    {
        // Initialize JsonConfigurationService with FileLogService for CLI
        _configService = new JsonConfigurationService(new FileLogService());
        
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
        var keyArgument = new Argument<string>("key", "Configuration key (e.g. General.Theme)");
        command.AddArgument(keyArgument);
        command.SetHandler(GetConfig, keyArgument);
        return command;
    }

    private Command CreateSetCommand()
    {
        var command = new Command("set", "Set a configuration value");
        var keyArgument = new Argument<string>("key", "Configuration key (e.g. General.Theme)");
        var valueArgument = new Argument<string>("value", "Configuration value");
        command.AddArgument(keyArgument);
        command.AddArgument(valueArgument);
        command.SetHandler(SetConfig, keyArgument, valueArgument);
        return command;
    }

    private async Task ListConfig()
    {
        await _configService.LoadAsync();
        
        // We need to access the full object to list it. 
        // IConfigurationService doesn't expose the root object directly via interface, 
        // but we can get sections.
        // Or we can cast to JsonConfigurationService if we know the implementation (hacky but works for CLI).
        // Or better, use Get<AppSettings>("") if supported, or Get<GeneralSettings>("General").
        
        var general = _configService.Get<GeneralSettings>("General");
        var pxe = _configService.Get<PxeSettings>("Pxe");
        var usb = _configService.Get<UsbSettings>("Usb");

        var table = new Table();
        table.AddColumn("Section");
        table.AddColumn("Key");
        table.AddColumn("Value");

        // General Settings
        table.AddRow("General", "Theme", general.Theme);
        table.AddRow("General", "Language", general.Language);
        
        // PXE Settings
        table.AddRow("Pxe", "TftpRoot", pxe.TftpRoot);
        table.AddRow("Pxe", "EnableDhcp", pxe.EnableDhcp.ToString());

        // USB Settings
        table.AddRow("Usb", "VerifyWrites", usb.VerifyWrites.ToString());
        
        AnsiConsole.Write(table);
    }

    private async Task GetConfig(string key)
    {
        await _configService.LoadAsync();
        var value = _configService.Get<object>(key);
        AnsiConsole.MarkupLine($"[bold]{key}[/] = [blue]{value}[/]");
    }

    private async Task SetConfig(string key, string value)
    {
        await _configService.LoadAsync();
        
        // Basic type inference for CLI input
        object typedValue = value;
        if (bool.TryParse(value, out bool b)) typedValue = b;
        else if (int.TryParse(value, out int i)) typedValue = i;
        
        _configService.Set(key, typedValue);
        await _configService.SaveAsync();
        
        AnsiConsole.MarkupLine($"[green]Configuration updated:[/]");
        AnsiConsole.MarkupLine($"[bold]{key}[/] = [blue]{value}[/]");
    }
}
