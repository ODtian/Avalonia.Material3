using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.Styling;
using Xunit;

namespace Avalonia.Material3.Tests;

public class ProgressScenarioTests
{
    [AvaloniaFact]
    public void Host_outcomes_replace_activity_with_visible_results_and_read_only_accessible_status()
    {
        var indicator = new MaterialLinearProgressIndicator { Value = .4, AnimationTime = TimeSpan.Zero };
        AutomationProperties.SetName(indicator, "Import documents");
        using var host = new ProgressHost(indicator);
        var peer = ControlAutomationPeer.CreatePeerForElement(indicator)!;
        Assert.Equal(AutomationControlType.ProgressBar, peer.GetAutomationControlType());
        Assert.Equal("Import documents", peer.GetName());
        Assert.Equal("Running, 40%", peer.GetItemStatus());
        var range = Assert.IsAssignableFrom<IRangeValueProvider>(peer.GetProvider<IRangeValueProvider>());
        Assert.True(range.IsReadOnly);
        Assert.Equal(.4, range.Value);
        Assert.Throws<InvalidOperationException>(() => range.SetValue(.5));
        var running = host.Capture();
        indicator.Status = MaterialProgressStatus.Paused;
        Assert.Equal("Paused, 40%", peer.GetItemStatus());
        Assert.NotEqual(running, host.Capture());
        indicator.ResultMessage = "12 documents imported";
        indicator.Status = MaterialProgressStatus.Completed;
        Assert.Equal("Completed: 12 documents imported", peer.GetItemStatus());
        Assert.Equal(1, range.Value);
        var completed = host.Capture();
        Assert.NotEqual(running, completed);
        indicator.ResultMessage = "Connection lost; retry available";
        indicator.Status = MaterialProgressStatus.Failed;
        Assert.Equal("Failed: Connection lost; retry available", peer.GetItemStatus());
        Assert.NotEqual(completed, host.Capture());
        Assert.False(indicator.Focusable);
    }

    [AvaloniaFact]
    public void Circular_activity_moves_with_controlled_time_freezes_on_pause_and_converges_to_host_result()
    {
        var indicator = new MaterialCircularProgressIndicator { Value = .25, AnimationTime = TimeSpan.Zero };
        using var host = new ProgressHost(indicator);
        Assert.Equal(new Size(40, 40), indicator.Bounds.Size);
        Assert.Equal(Color.Parse("#6750A4"), host.Pixel(37, 17));
        Assert.Equal(Color.Parse("#E8DEF8"), host.Pixel(20, 37));
        var determinate = host.Capture();
        indicator.IsIndeterminate = true;
        var peer = ControlAutomationPeer.CreatePeerForElement(indicator)!;
        Assert.Null(peer.GetProvider<IRangeValueProvider>());
        Assert.Equal("Running, indeterminate", peer.GetItemStatus());
        indicator.AnimationTime = TimeSpan.FromMilliseconds(700);
        var moving = host.Capture();
        Assert.NotEqual(determinate, moving);
        indicator.AnimationTime = TimeSpan.FromMilliseconds(1400);
        Assert.NotEqual(moving, host.Capture());
        indicator.Status = MaterialProgressStatus.Paused;
        var paused = host.Capture();
        indicator.AnimationTime = TimeSpan.FromSeconds(8);
        Assert.Equal(paused, host.Capture());
        indicator.Status = MaterialProgressStatus.Running;
        var resumed = host.Capture();
        indicator.AnimationTime = TimeSpan.FromMilliseconds(8250);
        Assert.NotEqual(resumed, host.Capture());
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = true };
        var reduced = host.Capture();
        indicator.AnimationTime = TimeSpan.FromSeconds(15);
        Assert.Equal(reduced, host.Capture());
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = false };
        indicator.AnimationTime = TimeSpan.FromMilliseconds(15700);
        Assert.NotEqual(reduced, host.Capture());
        indicator.Status = MaterialProgressStatus.Completed;
        var done = host.Capture();
        indicator.AnimationTime = TimeSpan.FromSeconds(25);
        Assert.Equal(done, host.Capture());
        Assert.Equal(1, peer.GetProvider<IRangeValueProvider>()!.Value);
        Assert.Equal("Completed", peer.GetItemStatus());
    }

    [AvaloniaFact]
    public void Expressive_linear_feedback_is_a_visible_travelling_wave_not_a_property_only_variant()
    {
        var indicator = new MaterialLinearProgressIndicator { Value = .5, IsExpressive = true, AnimationTime = TimeSpan.Zero };
        using var host = new ProgressHost(indicator);
        Assert.Equal(10, indicator.Bounds.Height); // pinned WaveHeight, thickness4, amplitude3, wavelength40
        Assert.Equal(Color.Parse("#6750A4"), host.Pixel(10, 8));
        Assert.Equal(Color.Parse("#6750A4"), host.Pixel(30, 2));
        Assert.Equal(Color.Parse("#FEF7FF"), host.Pixel(10, 2));
        var wave = host.Capture();
        indicator.AnimationTime = TimeSpan.FromMilliseconds(250);
        Assert.NotEqual(wave, host.Capture());
        indicator.Value = .95; // normative amplitude drops to zero at95%; allow Long2 amplitude tween
        indicator.AnimationTime = TimeSpan.FromMilliseconds(800);
        Assert.Equal(Color.Parse("#6750A4"), host.Pixel(10, 5));
        Assert.Equal(Color.Parse("#FEF7FF"), host.Pixel(10, 8));
        indicator.IsIndeterminate = true;
        indicator.AnimationTime = TimeSpan.FromMilliseconds(1200);
        var unknown = host.Capture();
        indicator.AnimationTime = TimeSpan.FromMilliseconds(1700);
        Assert.NotEqual(unknown, host.Capture());
    }

    [AvaloniaFact]
    public void Loading_feedback_morphs_through_the_seven_pinned_shapes_and_supports_contained_and_determinate_forms()
    {
        var indicator = new MaterialLoadingIndicator { AnimationTime = TimeSpan.Zero };
        using var host = new ProgressHost(indicator);
        Assert.Equal(new Size(48, 48), indicator.Bounds.Size);
        Assert.True(indicator.IsIndeterminate);
        var frames = new List<byte[]>();
        for (var i = 0; i < 7; i++)
        {
            indicator.AnimationTime = TimeSpan.FromMilliseconds(650 * i);
            var frame = host.Capture();
            Assert.DoesNotContain(frames, previous => previous.SequenceEqual(frame));
            frames.Add(frame);
        }
        indicator.IsContained = true;
        Assert.Equal(Color.Parse("#EADDFF"), host.Pixel(24, 2));
        Assert.Equal(Color.Parse("#21005D"), host.Pixel(24, 24));
        indicator.IsIndeterminate = false;
        indicator.Value = 0;
        var circle = host.Capture();
        indicator.Value = 1;
        Assert.NotEqual(circle, host.Capture()); // circle -> soft burst, not a spinning ellipse
        var peer = ControlAutomationPeer.CreatePeerForElement(indicator)!;
        Assert.Equal(1, peer.GetProvider<IRangeValueProvider>()!.Value);
        indicator.Status = MaterialProgressStatus.Failed;
        indicator.ResultMessage = "Server unavailable";
        Assert.Equal("Failed: Server unavailable", peer.GetItemStatus());
        Assert.NotEqual(frames[0], host.Capture());
    }

    [AvaloniaFact]
    public void Existing_feedback_consumes_live_semantic_roles_and_disabled_state_tokens()
    {
        var indicator = new MaterialLoadingIndicator { IsContained = true, AnimationTime = TimeSpan.Zero };
        using var host = new ProgressHost(indicator);
        var enabled = host.Capture();
        indicator.IsEnabled = false;
        Assert.NotEqual(enabled, host.Capture());
        var disabled = host.Capture();
        indicator.AnimationTime = TimeSpan.FromSeconds(2);
        Assert.Equal(disabled, host.Capture());
        indicator.IsEnabled = true;
        host.Theme.SeedColor = Color.Parse("#006C4C");
        Assert.NotEqual(enabled, host.Capture());
        Assert.Equal(host.Theme.LightColorScheme.Primary, MaterialColorScheme.Light.Primary); // explicit input is preserved
        host.Window.RequestedThemeVariant = ThemeVariant.Dark;
        Assert.Equal(MaterialColorScheme.FromSeed(Color.Parse("#006C4C"), true).OnPrimaryContainer, host.Pixel(24, 24));
        indicator.Status = MaterialProgressStatus.Failed;
        Assert.Equal(MaterialColorScheme.FromSeed(Color.Parse("#006C4C"), true).Error,
            Assert.IsAssignableFrom<ISolidColorBrush>(indicator.ErrorBrush).Color);
    }

    [AvaloniaFact]
    public void Compact_feedback_gives_paused_and_terminal_descriptions_room_without_enlarging_the_shape()
    {
        var indicator = new MaterialCircularProgressIndicator { Value = .5, AnimationTime = TimeSpan.Zero };
        using var host = new ProgressHost(indicator);
        Assert.Equal(40, indicator.Bounds.Width);
        indicator.Status = MaterialProgressStatus.Paused;
        var paused = host.Capture();
        Assert.Equal(240, indicator.Bounds.Width);
        Assert.Equal(Color.Parse("#6750A4"), host.Pixel(37, 20)); // ring remains40 DIP at the leading edge, not a centered240-DIP ring
        indicator.ResultMessage = "12 documents imported";
        indicator.Status = MaterialProgressStatus.Completed;
        var completed = host.Capture();
        Assert.Equal(240, indicator.Bounds.Width);
        Assert.True(indicator.Bounds.Height < 100); // readable short message, not one letter per line
        Assert.NotEqual(paused, completed);
    }

    [AvaloniaFact]
    public void Reduced_loading_snaps_to_the_same_known_first_shape_from_any_phase_or_zero_duration_override()
    {
        var indicator = new MaterialLoadingIndicator { AnimationTime = TimeSpan.Zero };
        Assert.Equal("Running, indeterminate", indicator.StatusDescription);
        using var host = new ProgressHost(indicator);
        var first = host.Capture();
        indicator.AnimationTime = TimeSpan.FromMilliseconds(900);
        Assert.NotEqual(first, host.Capture());
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = true };
        Assert.Equal(first, host.Capture());
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = false, DurationLong2 = TimeSpan.Zero };
        indicator.AnimationTime = TimeSpan.FromSeconds(8);
        Assert.Equal(first, host.Capture());
    }

    [AvaloniaFact]
    public void Host_progress_draws_a_real_linear_track_gap_and_stop_and_updates_in_place()
    {
        var indicator = new MaterialLinearProgressIndicator { Value = .25, AnimationTime = TimeSpan.Zero };
        using var host = new ProgressHost(indicator);
        Assert.Equal(240, indicator.Bounds.Width);
        Assert.Equal(4, indicator.Bounds.Height);
        Assert.Equal(Color.Parse("#6750A4"), host.Pixel(20, 2));
        Assert.Equal(Color.Parse("#FEF7FF"), host.Pixel(63, 2)); // actual 4-DIP separation
        Assert.Equal(Color.Parse("#E8DEF8"), host.Pixel(100, 2));
        Assert.Equal(Color.Parse("#6750A4"), host.Pixel(237, 2)); // contrast stop
        var quarter = host.Capture();
        indicator.Value = .75;
        Assert.NotEqual(quarter, host.Capture());
        Assert.Equal(Color.Parse("#6750A4"), host.Pixel(100, 2));
    }
}

internal sealed class ProgressHost : IDisposable
{
    public MaterialTheme Theme { get; } = new();
    public Window Window { get; }
    private readonly Control _indicator;
    public ProgressHost(Control indicator)
    {
        _indicator = indicator;
        Application.Current!.Styles.Add(Theme);
        Window = new Window { Width = 320, Height = 180, RequestedThemeVariant = ThemeVariant.Light,
            Content = new StackPanel { Margin = new Thickness(24), Children = { indicator } } };
        indicator.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
        Window.Show();
    }
    public byte[] Capture()
    {
        using var bitmap = Window.CaptureRenderedFrame()!;
        using var stream = new MemoryStream();
        bitmap.Save(stream, Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        return stream.ToArray();
    }
    public Color Pixel(double x, double y)
    {
        using var bitmap = Window.CaptureRenderedFrame()!;
        using var frame = bitmap.Lock();
        var point = _indicator.TranslatePoint(new Point(x, y), Window)!.Value;
        var offset = (int)(point.Y * Window.RenderScaling) * frame.RowBytes + (int)(point.X * Window.RenderScaling) * 4;
        var a = System.Runtime.InteropServices.Marshal.ReadByte(frame.Address, offset + 3);
        var b = System.Runtime.InteropServices.Marshal.ReadByte(frame.Address, offset);
        var g = System.Runtime.InteropServices.Marshal.ReadByte(frame.Address, offset + 1);
        var r = System.Runtime.InteropServices.Marshal.ReadByte(frame.Address, offset + 2);
        return frame.Format == Avalonia.Platform.PixelFormat.Bgra8888 ? Color.FromArgb(a, r, g, b) : Color.FromArgb(a, b, g, r);
    }
    public void Dispose() { Window.Close(); Application.Current!.Styles.Remove(Theme); }
}
