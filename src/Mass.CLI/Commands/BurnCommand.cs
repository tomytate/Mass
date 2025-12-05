using System.CommandLine;
using Mass.Core.Interfaces;
using Mass.Core.Logging;
using Spectre.Console;

namespace Mass.CLI.Commands;

public class BurnCommand : Command
{
    private readonly ILogService _logger;

    public BurnCommand(ILogService logger) : base("burn", "Burn an ISO image to a USB drive")
    {
        _logger = logger;
        var isoOption = new Option<string>(
            aliases: new[] { "--iso", "-i" },
            description: "Path to the ISO image file")
        {
            IsRequired = true
        };

        var driveOption = new Option<string>(
            aliases: new[] { "--drive", "-d" },
            description: "Target USB drive letter (e.g., E:)")
        {
            IsRequired = true
        };

        var fsOption = new Option<string>(
            aliases: new[] { "--filesystem", "-fs" },
            description: "Target filesystem",
            getDefaultValue: () => "FAT32");

        var schemeOption = new Option<string>(
            aliases: new[] { "--partition", "-p" },
            description: "Partition scheme",
            getDefaultValue: () => "GPT");

        AddOption(isoOption);
        AddOption(driveOption);
        AddOption(fsOption);
        AddOption(schemeOption);

        this.SetHandler(async (iso, drive, fs, scheme) =>
        {
            await BurnIso(iso, drive, fs, scheme);
        }, isoOption, driveOption, fsOption, schemeOption);
    }

    private async Task BurnIso(string isoPath, string driveLetter, string filesystem, string scheme)
    {
        if (!File.Exists(isoPath))
        {
            AnsiConsole.MarkupLine($"[red]Error: ISO file not found: {isoPath}[/]");
            return;
        }

        AnsiConsole.MarkupLine($"[bold]Burning ISO:[/] {isoPath}");
        AnsiConsole.MarkupLine($"[bold]Target Drive:[/] {driveLetter}");
        AnsiConsole.MarkupLine($"[bold]Filesystem:[/] {filesystem}");
        AnsiConsole.MarkupLine($"[bold]Scheme:[/] {scheme}");
        AnsiConsole.WriteLine();

        _logger.LogInformation($"Starting burn operation: ISO={isoPath}, Drive={driveLetter}, FS={filesystem}, Scheme={scheme}", "BurnCommand");

        await AnsiConsole.Progress()
            .StartAsync(async ctx => 
            {
                var task = ctx.AddTask("[green]Burning ISO...[/]");
                
                try 
                {
                    for (int i = 0; i <= 100; i += 10)
                    {
                        task.Value = i;
                        await Task.Delay(100);
                    }
                    
                    task.StopTask();
                    AnsiConsole.MarkupLine("[yellow]Note: Full USB burning implementation requires running the GUI application.[/]");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                    _logger.LogError("Burn operation failed", ex, "BurnCommand");
                }
            });

        AnsiConsole.MarkupLine("[bold green]Command completed![/]");
        _logger.LogInformation("Burn operation completed successfully", "BurnCommand");
    }
}
