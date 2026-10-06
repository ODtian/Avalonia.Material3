using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Material3.Tests;

// Confirmed seams: public control inputs, native host layout/input and rendered pixels.
// No private animation state, timer count or implementation collaborator assertions.
public class MotionQualityScenarioTests
{
    [AvaloniaFact]
    public async Task Frequent_host_value_reports_do_not_slow_the_visible_indeterminate_phase()
    {
        using var host = new ButtonHost();
        host.Theme.Motion = new MaterialMotion();
        var reported = new MaterialCircularProgressIndicator { IsIndeterminate = true, AnimationTime = TimeSpan.Zero };
        var quiet = new MaterialCircularProgressIndicator { IsIndeterminate = true, AnimationTime = TimeSpan.Zero };
        host.Window.Content = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 20, Children = { reported, quiet } };
        host.Capture();
        reported.AnimationTime = quiet.AnimationTime = TimeSpan.FromMilliseconds(900);
        host.Capture();
        reported.AnimationTime = quiet.AnimationTime = null;
        for (var i = 0; i < 80; i++)
        {
            await Task.Delay(5);
            reported.Value = i % 2 == 0 ? .2 : .8;
        }
        // Switching clock domains freezes the same actual phase, rather than synthesizing a final frame.
        reported.AnimationTime = quiet.AnimationTime = TimeSpan.Zero;
        host.Capture();
        var first = ActiveInkCenter(host, reported);
        var second = ActiveInkCenter(host, quiet);
        Assert.InRange(new Vector(first.X - second.X, first.Y - second.Y).Length, 0, 2);
    }

    private static Point ActiveInkCenter(ButtonHost host, Control graphic)
    {
        var origin = graphic.TranslatePoint(default, host.Window)!.Value;
        double x = 0, y = 0;
        var count = 0;
        for (var row = 0; row < 40; row++)
        for (var column = 0; column < 40; column++)
        {
            if (host.PixelAt(origin + new Vector(column, row)) != Color.Parse("#6750A4")) continue;
            x += column; y += row; count++;
        }
        Assert.True(count > 10, "The progress arc must actually paint primary ink.");
        return new Point(x / count, y / count);
    }
}
