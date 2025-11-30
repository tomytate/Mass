using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mass.Core.UI;
using System.Collections.ObjectModel;

namespace Mass.Launcher.ViewModels;

public partial class CommandPaletteViewModel : ViewModelBase
{
    private readonly ICommandPaletteService _commandPalette;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isOpen;

    public ObservableCollection<CommandPaletteItem> Commands { get; } = new();
    public ObservableCollection<string> RecentCommands { get; } = new();

    public CommandPaletteViewModel(ICommandPaletteService commandPalette)
    {
        _commandPalette = commandPalette;
        Title = "Command Palette";
    }

    [RelayCommand]
    private void Open()
    {
        IsOpen = true;
        SearchQuery = string.Empty;
        LoadCommands();
        LoadRecentCommands();
    }

    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
        SearchQuery = string.Empty;
    }

    [RelayCommand]
    private void Search()
    {
        Commands.Clear();
        var results = _commandPalette.SearchCommands(SearchQuery);
        foreach (var cmd in results.Take(10))
        {
            Commands.Add(cmd);
        }
    }

    [RelayCommand]
    private void ExecuteCommand(CommandPaletteItem command)
    {
        _commandPalette.ExecuteCommand(command.Id);
        Close();
    }

    private void LoadCommands()
    {
        Commands.Clear();
        foreach (var cmd in _commandPalette.GetAllCommands().Take(10))
        {
            Commands.Add(cmd);
        }
    }

    private void LoadRecentCommands()
    {
        RecentCommands.Clear();
        foreach (var cmdId in _commandPalette.GetRecentCommands())
        {
            RecentCommands.Add(cmdId);
        }
    }

    partial void OnSearchQueryChanged(string value)
    {
        Search();
    }
}
