using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mass.Core.Abstractions;
using Mass.Core.Configuration;
using Mass.Core.Telemetry;
using Mass.Core.UI;

namespace Mass.Launcher.ViewModels;

public partial class ConsentDialogViewModel : ViewModelBase
{
    private readonly IConfigurationService _config;
    private readonly ITelemetryService _telemetry;

    public ConsentDialogViewModel(IConfigurationService config, ITelemetryService telemetry)
    {
        _config = config;
        _telemetry = telemetry;
        Title = "Help Improve Mass Suite";
    }

    [RelayCommand]
    private async Task AcceptAsync()
    {
        var settings = _config.Get<AppSettings>("AppSettings", new AppSettings());
        if (settings != null)
        {
            settings.Telemetry.Enabled = true;
            settings.Telemetry.ConsentDecisionMade = true;
            _config.Set("AppSettings", settings);
            await _config.SaveAsync();
            
            _telemetry.TrackEvent("TelemetryConsentGiven");
        }
        
        CloseDialog();
    }

    [RelayCommand]
    private async Task DeclineAsync()
    {
        var settings = _config.Get<AppSettings>("AppSettings", new AppSettings());
        if (settings != null)
        {
            settings.Telemetry.Enabled = false;
            settings.Telemetry.ConsentDecisionMade = true;
            _config.Set("AppSettings", settings);
            await _config.SaveAsync();
        }
        
        CloseDialog();
    }

    private void CloseDialog()
    {
        // Logic to close the dialog - this will be handled by the View or Shell
        OnRequestClose?.Invoke();
    }

    public event Action? OnRequestClose;
}
