using System.Globalization;
using Avalonia.Data.Converters;

namespace Avalonia.Material3.Themes;

// Keeps the initial six-role typography fallback scalable without introducing another theme API.
internal sealed class ContentFontScaleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is double size ? size * 0.75 : 12d;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
