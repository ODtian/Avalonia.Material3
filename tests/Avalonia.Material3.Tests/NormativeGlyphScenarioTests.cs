using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Material3.Controls;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Material3.Tests;

public class NormativeGlyphScenarioTests
{
    [AvaloniaTheory]
    [InlineData(1d)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    [InlineData(2d)]
    public void Mixed_checkbox_uses_pinned_M3_canvas_and_centered_square_capped_mark(double scale)
    {
        var checkbox = new MaterialCheckBox { IsThreeState = true, IsChecked = null, Width = 48, Height = 48 };
        using var host = new GeometryHost(checkbox, 48, 48);
        var pixels = host.Offscreen(scale);
        Color At(double x, double y) => pixels[(int)(x * scale), (int)(y * scale)];
        // AndroidX11ece46a M3 stylingFix=true:18 canvas centered in48 target.
        // Endpoints19.5/28.5,y24 with2-DIP square caps:18.5..29.5×23..25.
        using var reference = new GeometryHost(new Grid { Children = {
            new Border { Width = 18, Height = 18, CornerRadius = new CornerRadius(2), Background = new SolidColorBrush(Color.Parse("#6750A4")) },
            new ReferenceMixedMark() } }, 48, 48);
        var expected = reference.Offscreen(scale);
        for (var y = (int)(21 * scale); y < 27 * scale; y++)
        for (var x = (int)(18 * scale); x < 30 * scale; x++)
            Assert.Equal(expected[x, y], pixels[x, y]);
        Assert.Equal(Color.Parse("#6750A4"), At(17.8, 21));
        Assert.Equal(Colors.White, At(20, 24));
        Assert.Equal(Colors.White, At(28, 24));
        Assert.Equal(Color.Parse("#6750A4"), At(24, 21));
        Assert.Equal(Color.Parse("#6750A4"), At(24, 27));
        Assert.Equal(new Size(48, 48), checkbox.Bounds.Size);
    }

    private sealed class ReferenceMixedMark : Control
    {
        public override void Render(DrawingContext context) =>
            context.DrawLine(new Pen(Brushes.White, 2, lineCap: PenLineCap.Square), new Point(19.5, 24), new Point(28.5, 24));
    }
}
