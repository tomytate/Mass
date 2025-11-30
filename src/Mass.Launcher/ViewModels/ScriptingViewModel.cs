using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mass.Core.Scripting;
using Mass.Core.UI;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Mass.Launcher.ViewModels;

public partial class ScriptingViewModel : ViewModelBase
{
    private readonly IScriptingService _scriptingService;

    [ObservableProperty]
    private string _scriptInput = "";

    [ObservableProperty]
    private string _outputLog = "";

    public ScriptingViewModel(IScriptingService scriptingService)
    {
        _scriptingService = scriptingService;
        Title = "Scripting Console";
        
        // Register a print function
        _scriptingService.RegisterObject("print", new Action<object>(Print));
    }

    private void Print(object obj)
    {
        OutputLog += $"{obj}\n";
    }

    [RelayCommand]
    private async Task RunScriptAsync()
    {
        if (string.IsNullOrWhiteSpace(ScriptInput)) return;

        OutputLog += $"> {ScriptInput}\n";
        
        try
        {
            var result = await _scriptingService.ExecuteAsync(ScriptInput);
            if (result != null)
            {
                OutputLog += $"Result: {result}\n";
            }
        }
        catch (Exception ex)
        {
            OutputLog += $"Error: {ex.Message}\n";
        }
    }

    [RelayCommand]
    private void ClearLog()
    {
        OutputLog = "";
    }
}
