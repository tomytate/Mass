using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mass.Core.UI;
using System.Collections.ObjectModel;

namespace Mass.Launcher.ViewModels;

public partial class OperationsConsoleViewModel : ViewModelBase
{
    private readonly IOperationsConsoleService _consoleService;

    public ObservableCollection<OperationLogEntry> Operations { get; } = new();

    [ObservableProperty]
    private OperationLogLevel _filterLevel = OperationLogLevel.Info;

    [ObservableProperty]
    private bool _autoScroll = true;

    public OperationsConsoleViewModel(IOperationsConsoleService consoleService)
    {
        _consoleService = consoleService;
        Title = "Operations Console";
        _consoleService.OperationLogged += OnOperationLogged;
        LoadOperations();
    }

    private void OnOperationLogged(object? sender, OperationLogEntry entry)
    {
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            Operations.Insert(0, entry);
            
            while (Operations.Count > 100)
            {
                Operations.RemoveAt(Operations.Count - 1);
            }
        });
    }

    private void LoadOperations()
    {
        foreach (var op in _consoleService.GetRecentOperations(100))
        {
            Operations.Add(op);
        }
    }

    [RelayCommand]
    private void Clear()
    {
        _consoleService.Clear();
        Operations.Clear();
    }

    [RelayCommand]
    private void SetFilter(string level)
    {
        if (Enum.TryParse<OperationLogLevel>(level, out var filterLevel))
        {
            FilterLevel = filterLevel;
        }
    }
}
