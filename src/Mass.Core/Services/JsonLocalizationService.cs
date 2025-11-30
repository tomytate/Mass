using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Mass.Core.Services;

public class JsonLocalizationService : ILocalizationService
{
    private readonly string _localesPath;
    private Dictionary<string, string> _currentStrings = new();
    private CultureInfo _currentCulture = new("en-US");

    public event PropertyChangedEventHandler? PropertyChanged;

    public CultureInfo CurrentCulture
    {
        get => _currentCulture;
        private set
        {
            if (_currentCulture != value)
            {
                _currentCulture = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentCulture)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            }
        }
    }

    public IEnumerable<CultureInfo> AvailableCultures { get; private set; } = new List<CultureInfo>();

    public string this[string key] => GetString(key);

    public JsonLocalizationService(string localesPath)
    {
        _localesPath = localesPath;
        LoadAvailableCultures();
        LoadLanguage("en-US"); // Default
    }

    public string GetString(string key)
    {
        if (_currentStrings.TryGetValue(key, out var value))
        {
            return value;
        }
        return $"[{key}]";
    }

    public void SetLanguage(string cultureCode)
    {
        LoadLanguage(cultureCode);
    }

    private void LoadAvailableCultures()
    {
        var cultures = new List<CultureInfo>();
        if (Directory.Exists(_localesPath))
        {
            foreach (var file in Directory.GetFiles(_localesPath, "*.json"))
            {
                try
                {
                    var code = Path.GetFileNameWithoutExtension(file);
                    cultures.Add(new CultureInfo(code));
                }
                catch { /* Ignore invalid filenames */ }
            }
        }
        
        if (!cultures.Any(c => c.Name == "en-US"))
        {
            cultures.Add(new CultureInfo("en-US"));
        }

        AvailableCultures = cultures;
    }

    private void LoadLanguage(string cultureCode)
    {
        var filePath = Path.Combine(_localesPath, $"{cultureCode}.json");
        if (File.Exists(filePath))
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var strings = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (strings != null)
                {
                    _currentStrings = strings;
                    CurrentCulture = new CultureInfo(cultureCode);
                    return;
                }
            }
            catch
            {
                // Log error
            }
        }

        // Fallback to empty if not found, or keep previous if failure
        if (_currentStrings.Count == 0)
        {
             _currentStrings = new Dictionary<string, string>();
             CurrentCulture = new CultureInfo(cultureCode);
        }
    }
}
