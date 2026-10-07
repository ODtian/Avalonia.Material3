using Avalonia.Controls;
using Avalonia.Automation;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;
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

    [AvaloniaFact]
    public void List_disclosure_uses_the_official_twenty_four_dip_artwork_in_both_states()
    {
        var row = new MaterialListItem { Title = "Entry", IsExpandable = true, ExpandedContent = "Details" };
        using var host = new GeometryHost(row, 320, 180);
        var action = row.GetVisualDescendants().OfType<MaterialButton>().Single(b => AutomationProperties.GetName(b) == "Toggle expanded content");
        action.Background = Brushes.White; action.Foreground = Brushes.Black;
        foreach (var symbol in new[] { "expand_more", "expand_less" })
        {
            host.Render();
            var center = action.TranslatePoint(new Point(action.Bounds.Width / 2, action.Bounds.Height / 2), host.Window)!.Value;
            var actual = host.Offscreen(1);
            using var reference = new GeometryHost(new MaterialSymbol { Symbol = symbol, Size = 24, Foreground = Brushes.Black }, 48, 48);
            var expected = reference.Offscreen(1);
            for (var y = 0; y < 24; y++)
            for (var x = 0; x < 24; x++)
                Assert.Equal(expected[x + 12, y + 12].R < 80, actual[(int)(center.X - 12) + x, (int)(center.Y - 12) + y].R < 80);
            if (symbol == "expand_more")
            {
                host.Window.MouseDown(center, MouseButton.Left); host.Window.MouseUp(center, MouseButton.Left);
                Assert.True(row.IsExpanded);
                host.Window.MouseMove(new Point(1, 1));
            }
        }
    }
}
