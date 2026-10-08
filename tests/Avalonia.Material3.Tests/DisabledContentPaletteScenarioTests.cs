using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Material3.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Styling;
using Xunit;

namespace Avalonia.Material3.Tests;

public class DisabledContentPaletteScenarioTests
{
    [AvaloniaFact]
    public void Disabled_card_preserves_authored_slot_foreground_and_live_owner_color_bindings()
    {
        var ink = new ForegroundInk { Width = 24, Height = 24 };
        ink.Styles.Add(new Style(selector => selector.OfType<ForegroundInk>())
        { Setters = { new Setter(TextElement.ForegroundProperty, Brushes.Lime) } });
        var source = new Border { Background = Brushes.Blue };
        var card = new MaterialCard { IsEnabled = false, Content = ink, Width = 160, Height = 96 };
        using var binding = card.Bind(MaterialCard.BackgroundProperty, new Avalonia.Data.Binding(nameof(Border.Background)) { Source = source });
        using var host = new GeometryHost(card, 200, 160);
        Assert.Equal(Colors.Blue, host.Pixel(120, 48));
        var point = GeometryHost.Box(ink, host.Window).Center; Assert.Equal(Colors.Lime, host.Pixel(point.X, point.Y));
        source.Background = Brushes.Magenta; host.Render(); Assert.Equal(Colors.Magenta, host.Pixel(120, 48));
        Assert.False(ink.IsEffectivelyEnabled);
    }

    [AvaloniaTheory]
    [InlineData(MaterialCardVariant.Filled, 1)]
    [InlineData(MaterialCardVariant.Elevated, 1)]
    [InlineData(MaterialCardVariant.Outlined, 1)]
    [InlineData(MaterialCardVariant.Filled, .5)]
    public void Disabled_card_has_native_opaque_container_and_one_inherited_content_alpha(MaterialCardVariant variant, double authoredOpacity)
    {
        var ink = new ForegroundInk { Width = 24, Height = 24, Opacity = authoredOpacity,
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var card = new MaterialCard { Variant = variant, IsEnabled = false, Content = ink, Width = 160, Height = 96,
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        using var host = new GeometryHost(card, 220, 160);
        ((Border)host.Window.Content!).Background = Brushes.Black; host.Render();
        var background = variant == MaterialCardVariant.Filled ? Color.FromRgb(230, 224, 234) : Color.Parse("#FEF7FF");
        Assert.Equal(background, host.Pixel(120, 48));
        var point = GeometryHost.Box(ink, host.Window).Center;
        var actual = host.Pixel(point.X, point.Y);
        var alpha = 97 / 255d * authoredOpacity;
        byte Blend(byte foreground, byte surface) => (byte)Math.Round(foreground * alpha + surface * (1 - alpha));
        Assert.InRange(Math.Abs(actual.R - Blend(29, background.R)), 0, 1);
        Assert.InRange(Math.Abs(actual.G - Blend(27, background.G)), 0, 1);
        Assert.InRange(Math.Abs(actual.B - Blend(32, background.B)), 0, 1);
        Assert.Equal(authoredOpacity, ink.Opacity);
        Assert.False(ink.IsEffectivelyEnabled);
    }

    [AvaloniaTheory]
    [InlineData(MaterialCardVariant.Filled)]
    [InlineData(MaterialCardVariant.Elevated)]
    [InlineData(MaterialCardVariant.Outlined)]
    public void Disabled_card_generated_role_text_retains_the_same_native_copy_alpha_as_control_content(MaterialCardVariant variant)
    {
        var card = new MaterialCard { Variant = variant, IsEnabled = false, Title = "Native title", SupportingContent = "Native supporting", Width = 220,
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        using var host = new GeometryHost(card, 240, 160);
        var labels = card.GetVisualDescendants().OfType<TextBlock>().Where(text => text.Text is "Native title" or "Native supporting").ToArray();
        Assert.Equal(2, labels.Length);
        foreach (var text in labels)
        {
            Assert.Equal((byte)97, Assert.IsAssignableFrom<ISolidColorBrush>(text.Foreground).Color.A);
            Assert.Equal(1, text.Opacity);
        }
    }

    private sealed class ForegroundInk : Control
    {
        public override void Render(DrawingContext context) => context.DrawRectangle(GetValue(TextElement.ForegroundProperty), null, new Rect(Bounds.Size));
    }
}
