using System.Globalization;
using Avalonia.Data.Converters;

namespace Avalonia.Material3.Themes;

internal sealed class ButtonFocusCornerRadiusConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is CornerRadius radius
            ? new CornerRadius(radius.TopLeft + 5, radius.TopRight + 5, radius.BottomRight + 5, radius.BottomLeft + 5)
            : AvaloniaProperty.UnsetValue;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
