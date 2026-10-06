using Avalonia.Controls;
using Avalonia.Headless;
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

    [AvaloniaFact]
    public async Task Opening_menu_has_a_monotonic_reveal_and_a_fixed_trigger_edge_at_every_sample()
    {
        using var host = new ButtonHost();
        host.Theme.Motion = new MaterialMotion();
        host.Window.Width = host.Window.Height = 500;
        var menu = new MaterialFabMenu();
        for (var i = 0; i < 4; i++) menu.Items.Add(new MaterialFabMenuItem { Content = "Create document " + i });
        host.Window.Content = new Grid { Children = { menu } };
        host.Capture();
        var edge = menu.TranslatePoint(new Point(menu.Bounds.Width, menu.Bounds.Height), host.Window)!.Value;
        menu.IsExpanded = true;
        double previous = 0;
        for (var frame = 0; frame < 38; frame++)
        {
            await Task.Delay(12);
            host.Capture();
            Assert.True(menu.Bounds.Height + .01 >= previous, $"Reveal recoiled from {previous} to {menu.Bounds.Height}.");
            previous = menu.Bounds.Height;
            var next = menu.TranslatePoint(new Point(menu.Bounds.Width, menu.Bounds.Height), host.Window)!.Value;
            Assert.Equal(edge.Y, next.Y, 4);
        }
        Assert.True(previous > 200, "The test must expose actual expanded actions, not an invisible zero-size projection.");
    }

    [AvaloniaFact]
    public async Task Press_width_motion_does_not_snap_the_corner_before_its_effects_transition()
    {
        using var host = new ButtonHost();
        host.Window.Width = 600;
        var first = new MaterialGroupButton { Content = "First long action" };
        var group = new MaterialButtonGroup { Children = { first, new MaterialGroupButton { Content = "Second long action" } } };
        host.Window.Content = new StackPanel { Children = { group } };
        host.Capture();
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with { DefaultEffects = new(1, 100) } };
        var center = first.TranslatePoint(new Point(first.Bounds.Width / 2, first.Bounds.Height / 2), host.Window)!.Value;
        host.Window.MouseDown(center, Avalonia.Input.MouseButton.Left);
        await Task.Delay(65);
        host.Capture();
        // Independent round-corner mask: (2,3) is outside the original 20-DIP corner,
        // but inside the pressed 8-DIP corner. A slow effects curve must not already show the latter.
        var corner = first.TranslatePoint(new Point(2, 8), host.Window)!.Value;
        Assert.Equal(Color.Parse("#FEF7FF"), host.PixelAt(corner));
        host.Window.MouseUp(center, Avalonia.Input.MouseButton.Left);
    }

    [AvaloniaFact]
    public async Task Whole_toolbar_keeps_its_cross_axis_and_FAB_center_while_the_surface_closes()
    {
        using var host = new ButtonHost();
        host.Window.Width = 600;
        var fab = new MaterialFab { Content = "Action" };
        var toolbar = new MaterialToolbar { FloatingAction = fab, CollapseBehavior = MaterialToolbarCollapseBehavior.WholeToolbar };
        toolbar.Items.Add(new MaterialIconButton { Content = "Action" });
        host.Window.Content = new Grid { Children = { toolbar } };
        host.Capture();
        var cross = toolbar.Bounds.Height;
        var center = fab.TranslatePoint(new Point(fab.Bounds.Width / 2, fab.Bounds.Height / 2), host.Window)!.Value;
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with { FastSpatial = new(1, 100) } };
        toolbar.IsExpanded = false;
        for (var i = 0; i < 20; i++)
        {
            await Task.Delay(15);
            host.Capture();
            Assert.Equal(cross, toolbar.Bounds.Height, 4);
            var next = fab.TranslatePoint(new Point(fab.Bounds.Width / 2, fab.Bounds.Height / 2), host.Window)!.Value;
            Assert.InRange(Math.Abs(next.Y - center.Y), 0, .5);
        }
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
