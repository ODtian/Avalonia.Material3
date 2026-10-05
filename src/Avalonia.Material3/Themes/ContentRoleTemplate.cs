using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Material3.Tokens;
using Avalonia.Media;

namespace Avalonia.Material3.Themes;

// An explicit slot template must preserve Control content, not stringify an independently interactive action.
internal sealed class ContentRoleTemplate : IDataTemplate
{
    public MaterialTypeRole Role { get; set; } = MaterialTypeRole.BodyLarge;
    public bool Match(object? data) => true;
    public Control? Build(object? data)
    {
        if (data is Control control) return control;
        if (data is null) return null;
        var text = new TextBlock { Text = data.ToString(), TextWrapping = TextWrapping.Wrap };
        text.Bind(TextBlock.FontFamilyProperty, new DynamicResourceExtension($"M3.{Role}FontFamily"));
        text.Bind(TextBlock.FontSizeProperty, new DynamicResourceExtension($"M3.{Role}FontSize"));
        text.Bind(TextBlock.FontWeightProperty, new DynamicResourceExtension($"M3.{Role}FontWeight"));
        text.Bind(TextBlock.LineHeightProperty, new DynamicResourceExtension($"M3.{Role}LineHeight"));
        text.Bind(TextBlock.LetterSpacingProperty, new DynamicResourceExtension($"M3.{Role}LetterSpacing"));
        return text;
    }
}
