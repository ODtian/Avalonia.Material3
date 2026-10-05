using System.Globalization;
using Avalonia.Data.Converters;

namespace Avalonia.Material3.Themes;

// BodySmall/floating-label size is 12 against the editor's BodyLarge 16.
// Deriving from the editor also honors a host's explicit FontSize, not only theme scale.
internal sealed class TextFieldFontSizeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var size = value is double fontSize ? fontSize : 16;
        return (parameter as string) switch
        {
            "Notch" => new Thickness(0, size * 0.5, 0, 0),
            "Padding" => new Thickness(16, size * 0.5, 16, size * 0.5),
            _ => size * 0.75
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
