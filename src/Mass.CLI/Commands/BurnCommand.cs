using System.CommandLine;
using Spectre.Console;

namespace Mass.CLI.Commands;

public class BurnCommand : Command
{
    public BurnCommand() : base("burn", "Burn an ISO image to a USB drive")
    {
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

        var burner = new ProUSB.Services.UsbBurnerService();
        var progress = new Progress<double>(p => 
        {
            // We'll handle progress in the AnsiConsole task if possible, 
            // but for now let's just let the spinner run or use a simple reporter
        });

        await AnsiConsole.Progress()
            .StartAsync(async ctx => 
            {
                var task = ctx.AddTask("[green]Burning ISO...[/]");
                
                var internalProgress = new Progress<double>(p => 
                {
                    task.Value = p;
                });

                try 
                {
                    await burner.BurnIsoAsync(isoPath, driveLetter, filesystem, scheme, internalProgress);
                    task.Value = 100;
                    task.StopTask();
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                }
            });

        AnsiConsole.MarkupLine("[bold green]Burn completed successfully![/]");
    }
}
