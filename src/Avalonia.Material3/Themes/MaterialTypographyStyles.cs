using System.Text;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Material3.Tokens;
using Avalonia.Styling;

namespace Avalonia.Material3.Themes;

internal static class MaterialTypographyStyles
{
    public static Styles Create()
    {
        var styles = new Styles();
        foreach (var role in Enum.GetValues<MaterialTypeRole>())
        {
            var name = new StringBuilder("m3");
            foreach (var character in role.ToString())
            {
                if (char.IsUpper(character)) name.Append('-');
                name.Append(char.ToLowerInvariant(character));
            }
            var className = name.ToString();
            styles.Add(new Style(selector => selector.OfType<TextBlock>().Class(className))
            {
                Setters =
                {
                    new Setter(TextBlock.FontFamilyProperty, new DynamicResourceExtension($"M3.{role}FontFamily")),
                    new Setter(TextBlock.FontSizeProperty, new DynamicResourceExtension($"M3.{role}FontSize")),
                    new Setter(TextBlock.FontWeightProperty, new DynamicResourceExtension($"M3.{role}FontWeight")),
                    new Setter(TextBlock.LineHeightProperty, new DynamicResourceExtension($"M3.{role}LineHeight")),
                    new Setter(TextBlock.LetterSpacingProperty, new DynamicResourceExtension($"M3.{role}LetterSpacing")),
                    new Setter(TextBlock.ForegroundProperty, new DynamicResourceExtension("M3.OnSurfaceBrush"))
                }
            });
        }
        return styles;
    }
}
