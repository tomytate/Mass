using System.CommandLine;
using Spectre.Console;
using Mass.Core.Workflows;

namespace Mass.CLI.Commands;

public class WorkflowCommand : Command
{
    public WorkflowCommand() : base("workflow", "Manage and execute workflows")
    {
        AddCommand(CreateRunCommand());
        AddCommand(CreateListCommand());
        AddCommand(CreateValidateCommand());
        AddCommand(CreateTemplateCommand());
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

    private Command CreateValidateCommand()
    {
        var command = new Command("validate", "Validate a workflow file");
        
        var fileArgument = new Argument<string>("file", "Path to the workflow file to validate");
        command.AddArgument(fileArgument);

        command.SetHandler(async (filePath) =>
        {
            await ValidateWorkflow(filePath);
        }, fileArgument);

        return command;
    }

    private Command CreateTemplateCommand()
    {
        var command = new Command("template", "Manage workflow templates");
        
        var listCmd = new Command("list", "List available workflow templates");
        listCmd.SetHandler(ListTemplates);
        
        var createCmd = new Command("create", "Create workflow from template");
        var templateArg = new Argument<string>("template", "Template name (e.g., burn-iso)");
        var outputOpt = new Option<string>(
            aliases: ["--output", "-o"],
            description: "Output file path",
            getDefaultValue: () => "workflow.yaml");
        createCmd.AddArgument(templateArg);
        createCmd.AddOption(outputOpt);
        createCmd.SetHandler(CreateFromTemplate, templateArg, outputOpt);
        
        command.AddCommand(listCmd);
        command.AddCommand(createCmd);
        
        return command;
    }

    private async Task ValidateWorkflow(string filePath)
    {
        if (!File.Exists(filePath))
        {
            AnsiConsole.MarkupLine($"[red]Error: Workflow file not found: {filePath}[/]");
            return;
        }

        try
        {
            var workflowsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MassSuite", "workflows");
            var engine = new WorkflowEngine(workflowsPath);
            
            var parser = new WorkflowParser();
            var workflow = parser.ParseFromFile(filePath);
            
            var validation = await engine.ValidateWorkflowAsync(workflow.Id);
            
            if (validation.IsValid)
            {
                AnsiConsole.MarkupLine($"[green]✓ Workflow is valid![/]");
                AnsiConsole.MarkupLine($"[dim]Name:[/] {workflow.Name}");
                AnsiConsole.MarkupLine($"[dim]Steps:[/] {workflow.Steps.Count}");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]✗ Workflow validation failed:[/]");
                foreach (var error in validation.Errors)
                {
                    AnsiConsole.MarkupLine($"  [red]• {error}[/]");
                }
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
        }
    }

    private void ListTemplates()
    {
        var templatesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MassSuite", "workflows", "templates");
        
        if (!Directory.Exists(templatesDir))
        {
            AnsiConsole.MarkupLine("[yellow]No templates directory found.[/]");
            return;
        }

        var templates = Directory.GetFiles(templatesDir, "*.yaml")
            .Concat(Directory.GetFiles(templatesDir, "*.yml"));

        if (!templates.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No templates found.[/]");
            return;
        }

        var table = new Table();
        table.AddColumn("Template");
        table.AddColumn("Description");
        table.AddColumn("File");

        var parser = new WorkflowParser();
        foreach (var file in templates)
        {
            try
            {
                var workflow = parser.ParseFromFile(file);
                table.AddRow(
                    workflow.Name,
                    workflow.Description,
                    Path.GetFileName(file));
            }
            catch
            {
                table.AddRow(
                    $"[red]{Path.GetFileName(file)}[/]",
                    "[red]Invalid[/]",
                    Path.GetFileName(file));
            }
        }

        AnsiConsole.Write(table);
    }

    private void CreateFromTemplate(string templateName, string outputPath)
    {
        var templatesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MassSuite", "workflows", "templates");
        var templatePath = Path.Combine(templatesDir, $"{templateName}.yaml");

        if (!File.Exists(templatePath))
        {
            // Try .yml extension
            templatePath = Path.Combine(templatesDir, $"{templateName}.yml");
            if (!File.Exists(templatePath))
            {
                AnsiConsole.MarkupLine($"[red]Error: Template '{templateName}' not found.[/]");
                return;
            }
        }

        try
        {
            File.Copy(templatePath, outputPath, overwrite: false);
            AnsiConsole.MarkupLine($"[green]✓ Created workflow from template: {outputPath}[/]");
            AnsiConsole.MarkupLine($"[dim]Edit the file to customize parameters before running.[/]");
        }
        catch (IOException)
        {
            AnsiConsole.MarkupLine($"[red]Error: File '{outputPath}' already exists.[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
        }
    }
}
