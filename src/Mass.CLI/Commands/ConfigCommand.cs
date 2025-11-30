using System.CommandLine;
using Spectre.Console;

namespace Mass.CLI.Commands;

public class ConfigCommand : Command
{
    public ConfigCommand() : base("config", "Manage application configuration")
    {
        AddCommand(CreateListCommand());
        AddCommand(CreateGetCommand());
        AddCommand(CreateSetCommand());
    }

    private Command CreateListCommand()
    {
        var command = new Command("list", "List all configuration values");
        command.SetHandler(ListConfig);
        return command;
    }

    private Command CreateGetCommand()
    {
        var command = new Command("get", "Get a configuration value");
        var keyArgument = new Argument<string>("key", "Configuration key");
        command.AddArgument(keyArgument);
        command.SetHandler(GetConfig, keyArgument);
        return command;
    }

    private Command CreateSetCommand()
    {
        var command = new Command("set", "Set a configuration value");
        var keyArgument = new Argument<string>("key", "Configuration key");
        var valueArgument = new Argument<string>("value", "Configuration value");
        command.AddArgument(keyArgument);
        command.AddArgument(valueArgument);
        command.SetHandler(SetConfig, keyArgument, valueArgument);
        return command;
    }

    private void ListConfig()
    {
        var table = new Table();
        table.AddColumn("Key");
        table.AddColumn("Value");

        // Mock data for now
        table.AddRow("Theme", "Dark");
        table.AddRow("Language", "en-US");
        table.AddRow("UpdateChannel", "Stable");

        AnsiConsole.Write(table);
    }

    private void GetConfig(string key)
    {
        AnsiConsole.MarkupLine($"[bold]{key}:[/] [blue]Value[/]");
    }

    private void SetConfig(string key, string value)
    {
        AnsiConsole.MarkupLine($"[green]Configuration updated:[/]");
        AnsiConsole.MarkupLine($"[bold]{key}[/] = [blue]{value}[/]");
    }
}
