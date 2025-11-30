using Mass.Core.Services;
using System.ComponentModel;

namespace Mass.Launcher.Services;

public class Localizer : INotifyPropertyChanged
{
    private static Localizer? _instance;
    public static Localizer Instance => _instance ??= new Localizer();

    private ILocalizationService? _service;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Initialize(ILocalizationService service)
    {
        _service = service;
        _service.PropertyChanged += (s, e) => PropertyChanged?.Invoke(this, e);
    }

    public string this[string key] => _service?[key] ?? $"[{key}]";
}
