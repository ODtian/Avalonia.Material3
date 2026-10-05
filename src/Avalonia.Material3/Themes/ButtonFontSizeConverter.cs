using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Material3.Controls;

namespace Avalonia.Material3.Themes;

/// <summary>Adapts the theme's label scale to the pinned size-specific button text style.</summary>
internal sealed class ButtonFontSizeConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count != 2 || values[0] is not double label || values[1] is not MaterialButtonSize size)
            return AvaloniaProperty.UnsetValue;
        var metric = (parameter as string) switch
        {
            "LineHeight" => size switch { MaterialButtonSize.Medium => 24d, MaterialButtonSize.Large => 32d, MaterialButtonSize.ExtraLarge => 40d, _ => 20d },
            "Tracking" => size switch { MaterialButtonSize.Medium => 0.2, MaterialButtonSize.Large or MaterialButtonSize.ExtraLarge => 0, _ => 0.1 },
            _ => size switch { MaterialButtonSize.Medium => 16d, MaterialButtonSize.Large => 24d, MaterialButtonSize.ExtraLarge => 32d, _ => 14d }
        };
        return metric * (label / 14d);
    }
}
