using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AiAgentUi.Converters;

public sealed class PinFillConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var pinned = value is true;
        return pinned
            ? new SolidColorBrush(Color.Parse("#EA580C"))
            : new SolidColorBrush(Color.Parse("#A1A1AA"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
