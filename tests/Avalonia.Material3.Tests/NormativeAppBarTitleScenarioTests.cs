using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Material3.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Material3.Tests;

public class NormativeAppBarTitleScenarioTests
{
    [AvaloniaFact]
    public void Two_row_titles_crossfade_in_fixed_typography_lanes_across_half_collapse()
    {
        var bar = new MaterialTopAppBar { Title = "Collection", Variant = MaterialTopAppBarVariant.Large,
            ScrollBehavior = MaterialAppBarScrollBehavior.EnterAlways, Foreground = Brushes.Black,
            NavigationContent = new MaterialIconButton { Content = new MaterialSymbol { Symbol = "arrow_back" } } };
        using var host = new GeometryHost(bar, 400, 240);
        var titles = bar.GetVisualDescendants().OfType<TextBlock>().Where(t => t.Text == bar.Title).ToArray();
        var small = Assert.Single(titles, t => t.FontSize == 22);
        var large = Assert.Single(titles, t => t.FontSize == 28);
        var range = bar.ExpandedHeight - bar.CollapsedHeight;
        bar.ApplyScrollDelta(range * .49, 40); host.Render();
        var smallBefore = GeometryHost.Box(small, host.Window);
        var largeBefore = GeometryHost.Box(large, host.Window);
        bar.ApplyScrollDelta(range * .02, 42); host.Render();
        Assert.Equal(22, small.FontSize); Assert.Equal(28, large.FontSize);
        Assert.Equal(smallBefore, GeometryHost.Box(small, host.Window));
        Assert.InRange(Math.Abs(GeometryHost.Box(large, host.Window).Top - largeBefore.Top), 0, 3);
        bar.ApplyScrollDelta(-range * .01, 41); host.Render();
        Assert.Equal(.5, bar.CollapsedFraction, 6);
        Assert.Equal(Color.Parse("#FAF4FC"), ((ISolidColorBrush)bar.CurrentBackground!).Color);
        // The source exposes both painted lanes at half collapse. Their actual glyph
        // ink differs in contrast because top alpha is(.8,0,.8,.15), bottom alpha=.5.
        var pixels = host.Offscreen(1);
        var top = GeometryHost.Box(small, host.Window);
        var bottom = GeometryHost.Box(large, host.Window);
        var topInk = 0; var bottomInk = 0;
        for (var y = 0; y < pixels.GetLength(1); y++)
        for (var x = 0; x < pixels.GetLength(0); x++)
        {
            if (top.Contains(new Point(x, y)) && pixels[x, y].R is > 225 and < 245) topInk++;
            if (bottom.Contains(new Point(x, y)) && y >= bar.CollapsedHeight && pixels[x, y].R < 180) bottomInk++;
        }
        Assert.True(topInk > 10); Assert.True(bottomInk > 10);
    }
}
