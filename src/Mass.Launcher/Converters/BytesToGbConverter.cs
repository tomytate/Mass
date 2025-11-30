using Avalonia.Data.Converters;
using System.Globalization;

namespace Mass.Launcher.Converters;

public class BytesToGbConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double bytes)
        {
            return (bytes / 1024.0 / 1024.0 / 1024.0).ToString("F1");
        }
        return "0.0";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double gb)
        {
            return (long)(gb * 1024 * 1024 * 1024);
        }
        return 0L;
    }
}
