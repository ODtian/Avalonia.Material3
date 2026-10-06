using System.Globalization;
using System.Text;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;

namespace Avalonia.Material3.Controls;

internal static class MaterialPickerSupport
{
    public static void Resource(AvaloniaObject control, AvaloniaProperty property, string key) =>
        control.Bind(property, new DynamicResourceExtension("M3." + key));

    public static TextBlock Text(string role, string color = "OnSurface")
    {
        var text = new TextBlock { TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
        Resource(text, TextBlock.FontFamilyProperty, role + "FontFamily");
        Resource(text, TextBlock.FontSizeProperty, role + "FontSize");
        Resource(text, TextBlock.FontWeightProperty, role + "FontWeight");
        Resource(text, TextBlock.LineHeightProperty, role + "LineHeight");
        Resource(text, TextBlock.LetterSpacingProperty, role + "LetterSpacing");
        Resource(text, TextBlock.ForegroundProperty, color + "Brush");
        return text;
    }

    public static MaterialButton Action(string name, Action action)
    {
        var button = new MaterialButton { Content = name, Variant = MaterialButtonVariant.Text };
        AutomationProperties.SetName(button, name);
        button.Click += (_, _) => action();
        return button;
    }

    // Civil Gregorian date semantics are independent of a culture's default calendar.
    public static CultureInfo Gregorian(CultureInfo culture)
    {
        var clone = (CultureInfo)culture.Clone();
        clone.DateTimeFormat.Calendar = new GregorianCalendar();
        return clone;
    }

    public static string NormalizeDigits(string? text)
    {
        var result = new StringBuilder();
        foreach (var rune in (text ?? "").EnumerateRunes())
            result.Append(Rune.GetUnicodeCategory(rune) == UnicodeCategory.DecimalDigitNumber
                ? ((int)Rune.GetNumericValue(rune)).ToString(CultureInfo.InvariantCulture) : rune.ToString());
        return result.ToString();
    }
}
