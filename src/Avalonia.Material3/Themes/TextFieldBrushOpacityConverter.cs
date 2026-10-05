using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Avalonia.Material3.Themes;

internal sealed class TextFieldBrushOpacityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is ISolidColorBrush brush && double.TryParse(parameter?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var opacity)
            ? new ImmutableSolidColorBrush(brush.Color, brush.Opacity * opacity)
            : value;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
