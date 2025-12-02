using CommunityToolkit.Mvvm.Input;
using Mass.Core.Workflows;
using Mass.Core.Services;
using Mass.Core.UI;
using System.Collections.ObjectModel;

namespace Mass.Launcher.ViewModels;

public partial class WorkflowsViewModel : ViewModelBase
{
    private readonly WorkflowParser _parser = new();
    private readonly WorkflowExecutor _executor = new();
    private readonly IActivityService _activityService;
    private string _workflowDirectory = string.Empty;

    public ObservableCollection<WorkflowInfo> Workflows { get; } = new();
    public ObservableCollection<string> ExecutionLogs { get; } = new();

    public WorkflowsViewModel(IActivityService activityService)
    {
        _activityService = activityService;
        Title = "Workflows";
        _workflowDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MassSuite", "workflows");
        Directory.CreateDirectory(_workflowDirectory);
        LoadWorkflows();
    }

    [RelayCommand]
    private void LoadWorkflows()
    {
        Workflows.Clear();
        
        if (!Directory.Exists(_workflowDirectory))
            return;

        var files = Directory.GetFiles(_workflowDirectory, "*.yaml")
            .Concat(Directory.GetFiles(_workflowDirectory, "*.yml"))
            .Concat(Directory.GetFiles(_workflowDirectory, "*.json"));

        foreach (var file in files)
        {
            try
            {
                var workflow = _parser.ParseFromFile(file);
                var id = $"workflow:{Path.GetFileNameWithoutExtension(file)}";
                
                Workflows.Add(new WorkflowInfo
                {
                    Id = id,
                    FilePath = file,
                    Name = workflow.Name,
                    Description = workflow.Description,
                    Version = workflow.Version,
                    StepCount = workflow.Steps.Count,
                    Status = "Ready",
                    IsFavorite = _activityService.IsFavorite(id)
                });
            }
            catch (Exception ex)
            {
                Workflows.Add(new WorkflowInfo
                {
                    FilePath = file,
                    Name = Path.GetFileName(file),
                    Description = $"Error: {ex.Message}",
                    Status = "Invalid"
                });
            }
        }
    }

    [RelayCommand]
    private async Task ExecuteWorkflowAsync(WorkflowInfo workflowInfo)
    {
        if (workflowInfo == null) return;

        ExecutionLogs.Clear();
        workflowInfo.Status = "Running";
        
        _activityService.AddActivity("Executed Workflow", $"Ran workflow: {workflowInfo.Name}", "⚙️");

        try
        {
            var workflow = _parser.ParseFromFile(workflowInfo.FilePath);
            var result = await _executor.ExecuteAsync(workflow);

            if (result.Context != null)
            {
                foreach (var log in result.Context.Logs)
                {
                    ExecutionLogs.Add(log);
                }
            }

            workflowInfo.Status = result.Success ? "Completed" : "Failed";
        }
        catch (Exception ex)
        {
            ExecutionLogs.Add($"[ERROR] {ex.Message}");
            workflowInfo.Status = "Failed";
        }
    }

    [RelayCommand]
    private void ToggleFavorite(WorkflowInfo workflow)
    {
        if (workflow.IsFavorite)
        {
            _activityService.RemoveFavorite(workflow.Id);
            workflow.IsFavorite = false;
        }
        else
        {
            _activityService.AddFavorite(workflow.Id, "Workflow", workflow.Name, "⚙️", "Workflows");
            workflow.IsFavorite = true;
        }
    }

    [RelayCommand]
    private void OpenWorkflowDirectory()
    {
        System.Diagnostics.Process.Start("explorer.exe", _workflowDirectory);
    }
}

public partial class WorkflowInfo : ViewModelBase
{
    public string Id { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public int StepCount { get; set; }
    
    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string _status = string.Empty;
    
    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private bool _isFavorite;
}
