using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mass.Core.Interfaces;
using Mass.Core.Logging;
using Mass.Spec.Contracts.Logging;
using Mass.Core.UI;
using System.Collections.ObjectModel;

namespace Mass.Launcher.ViewModels;

public partial class LogsViewModel : ViewModelBase
{
    private readonly ILogService _logService;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedLevel = "All";

    public ObservableCollection<LogEntry> LogEntries { get; } = new();
    public List<string> LogLevels { get; } = new() { "All", "Trace", "Debug", "Information", "Warning", "Error", "Critical" };

    public LogsViewModel(ILogService logService)
    {
        _logService = logService;
        Title = "Logs";
        LoadLogs();
    }

    [RelayCommand]
    private void LoadLogs()
    {
        LogEntries.Clear();
        
        IEnumerable<LogEntry> entries;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            entries = _logService.SearchLogs(SearchText);
        }
        else if (SelectedLevel != "All" && Enum.TryParse<LogLevel>(SelectedLevel, out var level))
        {
            entries = _logService.GetLogsByLevel(level);
        }
        else
        {
            entries = _logService.GetLogs();
        }

        foreach (var entry in entries)
        {
            LogEntries.Add(entry);
        }
    }

    [RelayCommand]
    private void ClearLogs()
    {
        _logService.ClearLogs();
        LoadLogs();
    }

    [RelayCommand]
    private void ExportLogs()
    {
        try
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var exportPath = Path.Combine(desktop, $"mass_logs_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            
            var lines = LogEntries.Select(e => 
                $"[{e.Timestamp:yyyy-MM-dd HH:mm:ss}] [{e.Level}] [{e.Source}] {e.Message}");
            
            File.WriteAllLines(exportPath, lines);
            
            _logService.LogInformation($"Logs exported to: {exportPath}", "LogsView");
        }
        catch (Exception ex)
        {
            _logService.LogError("Failed to export logs", ex, "LogsView");
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        LoadLogs();
    }

    partial void OnSelectedLevelChanged(string value)
    {
        LoadLogs();
    }
}
