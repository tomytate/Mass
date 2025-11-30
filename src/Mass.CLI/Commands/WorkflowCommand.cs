using System.CommandLine;
using Spectre.Console;
using Mass.Core.Orchestration;

namespace Mass.CLI.Commands;

public class WorkflowCommand : Command
{
    public WorkflowCommand() : base("workflow", "Manage and execute workflows")
    {
        AddCommand(CreateRunCommand());
        AddCommand(CreateListCommand());
    }

    private Command CreateRunCommand()
    {
        var command = new Command("run", "Execute a workflow file");
        
        var fileOption = new Option<string>(
            aliases: new[] { "--file", "-f" },
            description: "Path to the workflow file (.yaml, .yml, .json)")
        {
            IsRequired = true
        };

        command.AddOption(fileOption);

        command.SetHandler(async (filePath) =>
        {
            await ExecuteWorkflow(filePath);
        }, fileOption);

        return command;
    }

    private Command CreateListCommand()
    {
        var command = new Command("list", "List available workflows");
        
        command.SetHandler(() =>
        {
            ListWorkflows();
        });

        return command;
    }

    private async Task ExecuteWorkflow(string filePath)
    {
        if (!File.Exists(filePath))
        {
            AnsiConsole.MarkupLine($"[red]Error: Workflow file not found: {filePath}[/]");
            return;
        }

        try
        {
            var parser = new WorkflowParser();
            var workflow = parser.ParseFromFile(filePath);
            var executor = new WorkflowExecutor();

            AnsiConsole.MarkupLine($"[bold green]Starting workflow: {workflow.Name}[/]");
            if (!string.IsNullOrEmpty(workflow.Description))
            {
                AnsiConsole.MarkupLine($"[dim]{workflow.Description}[/]");
            }
            AnsiConsole.WriteLine();

            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Executing workflow...", async ctx =>
                {
                    var result = await executor.ExecuteAsync(workflow);

                    if (result.Success)
                    {
                        AnsiConsole.MarkupLine($"[bold green]Workflow completed successfully![/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[bold red]Workflow failed: {result.Message}[/]");
                    }

                    if (result.Context != null && result.Context.Logs.Any())
                    {
                        AnsiConsole.WriteLine();
                        AnsiConsole.Write(new Rule("[yellow]Execution Log[/]"));
                        foreach (var log in result.Context.Logs)
                        {
                            AnsiConsole.MarkupLine($"[dim]{log}[/]");
                        }
                    }
                });
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
        }
    }

    private void ListWorkflows()
    {
        var workflowDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MassSuite", "workflows");
        
        if (!Directory.Exists(workflowDir))
        {
            AnsiConsole.MarkupLine($"[yellow]No workflows directory found at {workflowDir}[/]");
            return;
        }

        var files = Directory.GetFiles(workflowDir, "*.yaml")
            .Concat(Directory.GetFiles(workflowDir, "*.yml"))
            .Concat(Directory.GetFiles(workflowDir, "*.json"));

        var table = new Table();
        table.AddColumn("Name");
        table.AddColumn("Version");
        table.AddColumn("Steps");
        table.AddColumn("File");

        var parser = new WorkflowParser();

        foreach (var file in files)
        {
            try
            {
                var workflow = parser.ParseFromFile(file);
                table.AddRow(
                    workflow.Name, 
                    workflow.Version, 
                    workflow.Steps.Count.ToString(), 
                    Path.GetFileName(file));
            }
            catch
            {
                table.AddRow(
                    $"[red]Invalid: {Path.GetFileName(file)}[/]", 
                    "-", 
                    "-", 
                    Path.GetFileName(file));
            }
        }

        AnsiConsole.Write(table);
    }
}
