using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Headless;
using Xunit;

namespace Avalonia.Material3.Tests;

public class NormativeOverlayMotionScenarioTests
{
    [AvaloniaFact]
    public void Drawer_swipe_close_starts_from_the_dragged_surface_and_retains_its_scrim_fraction()
    {
        using var host = new FeedbackHost();
        host.Theme.Motion = new MaterialMotion { ReduceMotion = true }; host.Render();
        var drawer = new MaterialNavigationDrawer { Mode = MaterialNavigationDrawerMode.Modal };
        drawer.Show(host.Overlay); host.Render();
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with {
            DefaultSpatial = new(.8, 10), FastEffects = new(1, 1) } }; host.Render();
        var start = new Point(180, 300);
        using var touch = host.Window.TouchBegin(start);
        host.Window.TouchMove(touch, start - new Vector(220, 0)); host.Render();
        host.Window.TouchEnd(touch, start - new Vector(220, 0)); host.Render();
        Assert.Equal(0, host.Overlay.OpenCount);
        // After220-DIP travel, the360-DIP surface ends at140;200 remains dimmed host body.
        Assert.InRange(host.PixelAt(new Point(200, 300)).G, (byte)0, (byte)230);
    }

    [AvaloniaFact]
    public void Closing_snackbar_keeps_its_captured_location_when_the_host_grows()
    {
        using var host = new FeedbackHost();
        host.Theme.Motion = new MaterialMotion { ReduceMotion = true }; host.Render();
        var snackbar = new MaterialSnackbar { Content = "Saved", Duration = Timeout.InfiniteTimeSpan };
        snackbar.Show(host.Overlay); host.Render();
        var point = host.Center(snackbar);
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with {
            FastSpatial = new(.6, 800) { IsInstant = true }, FastEffects = new(1, 1) } }; host.Render();
        snackbar.Dismiss(); host.Window.Height = 700; host.Render();
        // InverseSurface raster stays at the accepted close frame; the larger host exposes extra body.
        Assert.InRange(host.PixelAt(point).R, (byte)0, (byte)100);
    }

    [AvaloniaFact]
    public async Task Dismissible_standard_drawer_moves_content_with_its_opening_track_and_reduces_to_the_final_layout()
    {
        using var host = new FeedbackHost();
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with { DefaultSpatial = new(.8, 10) } };
        var drawer = new MaterialNavigationDrawer { IsOpen = false };
        var body = new Border();
        host.Overlay.Content = new MaterialNavigationDrawerLayout { Drawer = drawer, Content = body };
        host.Render(); Assert.Equal(800, body.Bounds.Width);
        drawer.IsOpen = true; host.Render();
        var widths = new List<double>();
        for (var frame = 0; frame < 20; frame++)
        {
            await Task.Delay(16); host.Render(); widths.Add(body.Bounds.Width);
        }
        Assert.Contains(widths, width => width > 440 && width < 800);
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = true }; host.Render();
        Assert.Equal(440, body.Bounds.Width);
        Assert.True(drawer.Close()); host.Render(); Assert.Equal(800, body.Bounds.Width);
    }

    [AvaloniaFact]
    public async Task Material_dialog_uses_the_platform_translate_fade_and_generic_host_content_keeps_its_presentation()
    {
        using var host = new FeedbackHost();
        var dialog = new MaterialDialog { Content = "Body", Title = "Dialog" };
        var session = dialog.Show(host.Overlay);
        // Android16 Material.Dialog platform recipe:20→0 Y/alpha0→1,220ms standard.
        Assert.Equal(20, dialog.TransformToVisual(host.Window)!.Value.M32);
        Assert.Equal(1, dialog.TransformToVisual(host.Window)!.Value.M11);
        Assert.Equal(0, EffectiveOpacity(dialog));
        host.Render();
        var restingTop = (host.Window.Bounds.Height - dialog.Bounds.Height) / 2;
        await Task.Delay(70); host.Render();
        Assert.InRange(dialog.TransformToVisual(host.Window)!.Value.M32, restingTop, restingTop + 20);
        await Task.Delay(220); host.Render();
        Assert.Equal(restingTop, dialog.TransformToVisual(host.Window)!.Value.M32, 3);
        session.Dismiss(); Assert.True(session.Completion.IsCompleted); Assert.Null(dialog.Parent);
        var custom = new Border { Width = 120, Height = 80, RenderTransform = new TranslateTransform(7, 11) };
        var customSession = host.Overlay.Show(custom); host.Render();
        Assert.Equal(1, EffectiveOpacity(custom));
        Assert.Equal(new Point(7, 11), new Point(custom.RenderTransform.Value.M31, custom.RenderTransform.Value.M32));
        customSession.Dismiss(); Assert.Null(custom.Parent);
    }

    [AvaloniaFact]
    public async Task Modal_drawer_uses_default_spatial_open_and_fast_effects_close_with_immediate_session_result()
    {
        using var host = new FeedbackHost();
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with {
            DefaultSpatial = new(.8, 10), FastEffects = new(1, 100) } }; host.Render();
        var drawer = new MaterialNavigationDrawer { Mode = MaterialNavigationDrawerMode.Modal };
        var session = drawer.Show(host.Overlay);
        Assert.Equal(-360, drawer.TransformToVisual(host.Window)!.Value.M31);
        host.Render(); var positions = new List<double>();
        for (var frame = 0; frame < 28; frame++)
        {
            await Task.Delay(16); host.Render(); positions.Add(drawer.TransformToVisual(host.Window)!.Value.M31);
        }
        Assert.Contains(positions, value => value > -360 && value < 0);
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = true }; host.Render();
        Assert.Equal(0, drawer.TransformToVisual(host.Window)!.Value.M31);
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = false }; host.Render();
        var pixel = host.PixelAt(new Point(180, 300));
        Assert.True(drawer.Close()); Assert.True(session.Completion.IsCompleted);
        Assert.Equal(0, host.Overlay.OpenCount); Assert.Null(drawer.Parent);
        await Task.Delay(180); host.Render();
        Assert.NotEqual(pixel, host.PixelAt(new Point(180, 300)));
    }

    [AvaloniaTheory]
    [InlineData("menu")]
    [InlineData("tooltip")]
    [InlineData("snackbar")]
    public async Task Standard_feedback_enters_from_point_eight_and_exits_visually_after_session_completion(string kind)
    {
        using var host = new FeedbackHost();
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with {
            FastSpatial = new(.6, 100), FastEffects = new(1, 100) } };
        host.Render(); host.Entry.Focus();
        Control surface = kind switch {
            "menu" => new MaterialMenu { Items = { new MaterialMenuItem { Content = "Operation" } } },
            "tooltip" => new MaterialTooltip { Content = "Description", IsPersistent = true },
            _ => new MaterialSnackbar { Content = "Saved", Duration = Timeout.InfiniteTimeSpan }
        };
        var session = surface switch {
            MaterialMenu menu => menu.Show(host.Overlay, host.Entry),
            MaterialTooltip tooltip => tooltip.Show(host.Overlay, host.Entry),
            MaterialSnackbar snackbar => snackbar.Show(host.Overlay),
            _ => throw new InvalidOperationException()
        };
        // Literal pinned recipes use.8→1 scale/FastSpatial and0→1 alpha/FastEffects.
        Assert.Equal(.8, surface.TransformToVisual(host.Window)!.Value.M11, 5);
        Assert.Equal(0, EffectiveOpacity(surface));
        var scales = new List<double>(); var alphas = new List<double>();
        var observed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        TimeSpan? started = null;
        void Sample(TimeSpan time)
        {
            started ??= time;
            scales.Add(surface.TransformToVisual(host.Window)!.Value.M11);
            alphas.Add(EffectiveOpacity(surface));
            if (time - started.Value >= TimeSpan.FromMilliseconds(700)) observed.TrySetResult();
            else host.Window.RequestAnimationFrame(Sample);
        }
        host.Window.RequestAnimationFrame(Sample);
        host.Render();
        await observed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Contains(scales, value => value > .8 && value < 1);
        Assert.Contains(alphas, value => value > 0 && value < 1);
        Assert.InRange(scales[^1], .99, 1.03);
        Assert.InRange(alphas[^1], .95, 1);
        var center = host.Center(surface); var painted = host.PixelAt(center);
        Assert.True(session.Dismiss());
        Assert.True(session.Completion.IsCompleted); Assert.Equal(0, host.Overlay.OpenCount);
        Assert.Null(surface.Parent); Assert.Null(surface.GetVisualParent());
        Assert.NotEqual(host.PixelAt(new Point(780, 100)), host.PixelAt(center)); // retained inert raster
        var closing = host.PixelAt(center);
        await Task.Delay(120); host.Render();
        Assert.NotEqual(closing, host.PixelAt(center));
        // The same public control can be reopened while its old visual exits.
        var reopened = surface switch {
            MaterialMenu menu => menu.Show(host.Overlay, host.Entry),
            MaterialTooltip tooltip => tooltip.Show(host.Overlay, host.Entry),
            MaterialSnackbar snackbar => snackbar.Show(host.Overlay),
            _ => throw new InvalidOperationException()
        };
        Assert.True(reopened.IsOpen);
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = true }; host.Render();
        Assert.Equal(1, surface.TransformToVisual(host.Window)!.Value.M11, 5);
        Assert.Equal(1, EffectiveOpacity(surface));
        reopened.Dismiss();
    }
    private static double EffectiveOpacity(Visual visual) => visual.GetVisualAncestors().Prepend(visual).Aggregate(1d, (alpha, node) => alpha * node.Opacity);
}
