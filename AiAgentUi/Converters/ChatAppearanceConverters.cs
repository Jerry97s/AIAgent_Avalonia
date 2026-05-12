using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;

namespace AiAgentUi.Converters;

public sealed class BoolToHorizontalAlignmentConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? HorizontalAlignment.Right : HorizontalAlignment.Left;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class IsUserChatBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var app = Application.Current;
        if (app is null)
            return Brushes.White;
        var key = value is true ? "ChatUserBg" : "ChatAgentBg";
        return app.TryGetResource(key, ThemeVariant.Light, out var r) && r is IBrush b ? b : Brushes.White;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class IsUserChatBorderConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var app = Application.Current;
        if (app is null)
            return Brushes.LightGray;
        var key = value is true ? "ChatUserBorder" : "ChatAgentBorder";
        return app.TryGetResource(key, ThemeVariant.Light, out var r) && r is IBrush b ? b : Brushes.LightGray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class IsUserRoleForegroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var app = Application.Current;
        if (app is null)
            return Brushes.Gray;
        var key = value is true ? "Ink500" : "AccentStrong";
        return app.TryGetResource(key, ThemeVariant.Light, out var r) && r is IBrush b ? b : Brushes.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
