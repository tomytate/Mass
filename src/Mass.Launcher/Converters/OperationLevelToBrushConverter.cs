using Avalonia.Data.Converters;
using Avalonia.Media;
using Mass.Core.UI;
using System.Globalization;

namespace Mass.Launcher.Converters;

public class OperationLevelToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is OperationLogLevel level)
        {
            return level switch
            {
                OperationLogLevel.Info => new SolidColorBrush(Color.Parse("#3B82F6")),
                OperationLogLevel.Success => new SolidColorBrush(Color.Parse("#10B981")),
                OperationLogLevel.Warning => new SolidColorBrush(Color.Parse("#F59E0B")),
                OperationLogLevel.Error => new SolidColorBrush(Color.Parse("#EF4444")),
                _ => new SolidColorBrush(Color.Parse("#6B7280"))
            };
        }
        return new SolidColorBrush(Color.Parse("#6B7280"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return OperationLogLevel.Info;
    }
}
