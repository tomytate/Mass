using Avalonia;
using Avalonia.Data.Converters;
using System.Globalization;

namespace Mass.Launcher.Converters;

public class BooleanToStarConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isFavorite && isFavorite)
        {
            return "⭐"; // Filled star
        }
        return "☆"; // Empty star
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return false;
    }
}
