using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace Mass.Core.Services;

public interface ILocalizationService : INotifyPropertyChanged
{
    CultureInfo CurrentCulture { get; }
    string this[string key] { get; }
    string GetString(string key);
    void SetLanguage(string cultureCode);
    IEnumerable<CultureInfo> AvailableCultures { get; }
}
