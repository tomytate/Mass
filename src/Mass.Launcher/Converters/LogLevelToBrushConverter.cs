using Avalonia.Data.Converters;
using Avalonia.Media;
using Mass.Core.Logging;
using System.Globalization;

namespace Mass.Launcher.Converters;

public class LogLevelToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is LogLevel level)
        {
            return level switch
            {
                LogLevel.Trace => new SolidColorBrush(Color.Parse("#6B7280")),
                LogLevel.Debug => new SolidColorBrush(Color.Parse("#3B82F6")),
                LogLevel.Information => new SolidColorBrush(Color.Parse("#10B981")),
                LogLevel.Warning => new SolidColorBrush(Color.Parse("#F59E0B")),
                LogLevel.Error => new SolidColorBrush(Color.Parse("#EF4444")),
                LogLevel.Critical => new SolidColorBrush(Color.Parse("#DC2626")),
                _ => new SolidColorBrush(Color.Parse("#6B7280"))
            };
        }
        return new SolidColorBrush(Color.Parse("#6B7280"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return LogLevel.Information;
    }
}
