using System.CommandLine;
using Mass.CLI.Commands;
using Spectre.Console;

var rootCommand = new RootCommand("Mass Suite CLI - Professional Deployment & Media Creation Tool");

rootCommand.AddCommand(new BurnCommand());
rootCommand.AddCommand(new WorkflowCommand());
rootCommand.AddCommand(new ConfigCommand());

// Add version info
AnsiConsole.Write(
    new FigletText("Mass Suite")
        .Color(Color.Blue));

return await rootCommand.InvokeAsync(args);
