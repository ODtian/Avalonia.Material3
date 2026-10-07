using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Material3.Controls;

namespace Avalonia.Material3.Themes;

// Template geometry only: keep font-scaled editor padding and centre the notch on the actual supporting-role line box.
// Typography itself comes from the canonical BodyLarge/BodySmall resources, not a second type scale.
internal sealed class TextFieldMetricsConverter : IValueConverter, IMultiValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var size = value is double fontSize ? fontSize : 16;
        return new Thickness(16, size * 0.5, 16, size * 0.5);
    }

    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is "Envelope" && values.Count >= 4 && values[0] is Thickness padding)
        {
            var labelledFilled = values[1] is MaterialTextFieldVariant.Filled && values[2] is string label && !string.IsNullOrWhiteSpace(label);
            var height = values[3] is double lineHeight ? lineHeight : 16;
            return new Thickness(padding.Left, padding.Top + (labelledFilled ? height : 0), padding.Right, padding.Bottom);
        }
        return default(Thickness);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
