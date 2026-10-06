using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Material3.Controls;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Material3.Tests;

public class ProgressMatrixScenarioTests
{
    [AvaloniaTheory]
    [InlineData(0, false, false)] [InlineData(0, false, true)]
    [InlineData(0, true, false)] [InlineData(0, true, true)]
    [InlineData(1, false, false)] [InlineData(1, false, true)]
    [InlineData(1, true, false)] [InlineData(1, true, true)]
    [InlineData(2, false, false)] [InlineData(2, false, true)]
    [InlineData(2, true, false)] [InlineData(2, true, true)]
    public void Every_pinned_form_has_visible_host_owned_activity_pause_motion_preferences_and_terminal_outcomes(int kind, bool expressiveOrContained, bool unknown)
    {
        MaterialProgressIndicator indicator = kind switch
        {
            0 => new MaterialLinearProgressIndicator { IsExpressive = expressiveOrContained },
            1 => new MaterialCircularProgressIndicator { IsExpressive = expressiveOrContained },
            _ => new MaterialLoadingIndicator { IsContained = expressiveOrContained }
        };
        indicator.AnimationTime = TimeSpan.Zero;
        indicator.IsIndeterminate = unknown;
        indicator.Status = MaterialProgressStatus.Idle;
        indicator.Value = .5;
        using var host = new ProgressHost(indicator);
        var idle = host.Capture();
        indicator.Status = MaterialProgressStatus.Running;
        var initial = host.Capture();
        Assert.NotEqual(idle, initial);
        indicator.AnimationTime = TimeSpan.FromMilliseconds(800);
        if (unknown || kind < 2 && expressiveOrContained) Assert.NotEqual(initial, host.Capture());
        else Assert.Equal(initial, host.Capture()); // normal determinate values are immediate and never silently animated
        var peer = ControlAutomationPeer.CreatePeerForElement(indicator)!;
        Assert.Equal(unknown ? "Running, indeterminate" : "Running, 50%", peer.GetItemStatus());
        Assert.Equal(unknown, peer.GetProvider<IRangeValueProvider>() is null);
        indicator.Status = MaterialProgressStatus.Paused;
        var paused = host.Capture();
        indicator.AnimationTime = TimeSpan.FromSeconds(10);
        Assert.Equal(paused, host.Capture());
        indicator.Status = MaterialProgressStatus.Running;
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = true };
        var reduced = host.Capture();
        indicator.AnimationTime = TimeSpan.FromSeconds(20);
        Assert.Equal(reduced, host.Capture());
        indicator.ResultMessage = "Imported";
        indicator.Status = MaterialProgressStatus.Completed;
        var completed = host.Capture();
        Assert.NotEqual(reduced, completed);
        Assert.Equal("Completed: Imported", peer.GetItemStatus());
        Assert.Equal(1, peer.GetProvider<IRangeValueProvider>()!.Value);
        indicator.Value = .1;
        indicator.AnimationTime = TimeSpan.FromSeconds(30);
        Assert.Equal(completed, host.Capture()); // late progress cannot revive a completed operation
        indicator.ResultMessage = "Retry required";
        indicator.Status = MaterialProgressStatus.Failed;
        Assert.NotEqual(completed, host.Capture());
        Assert.Equal("Failed: Retry required", peer.GetItemStatus());
    }

    [AvaloniaFact]
    public void Host_values_are_coerced_but_never_complete_an_operation_and_template_replacement_preserves_semantics()
    {
        var indicator = new MaterialLinearProgressIndicator { AnimationTime = TimeSpan.Zero };
        using var host = new ProgressHost(indicator);
        var peer = ControlAutomationPeer.CreatePeerForElement(indicator)!;
        foreach (var (input, expected) in new[] { (double.NaN, 0d), (double.NegativeInfinity, 0d), (-1d, 0d), (double.PositiveInfinity, 1d), (2d, 1d) })
        {
            indicator.Value = input;
            Assert.Equal(expected, peer.GetProvider<IRangeValueProvider>()!.Value);
            Assert.Equal(MaterialProgressStatus.Running, indicator.Status);
        }
        Assert.Throws<ArgumentException>(() => indicator.AnimationTime = TimeSpan.FromMilliseconds(-1));
        Assert.Throws<ArgumentException>(() => indicator.Status = (MaterialProgressStatus)99);
        indicator.Template = new FuncControlTemplate<MaterialProgressIndicator>((owner, _) => new Border { Background = Brushes.Red, Height = 12 });
        Assert.Equal(Color.Parse("#FF0000"), host.Pixel(20, 2));
        indicator.Status = MaterialProgressStatus.Failed;
        indicator.ResultMessage = "Host template failure";
        Assert.Equal("Failed: Host template failure", peer.GetItemStatus());
    }

    [AvaloniaFact]
    public void RTL_linear_progress_mirrors_the_actual_active_track_gap_and_contrast_stop()
    {
        var indicator = new MaterialLinearProgressIndicator { Value = .25, FlowDirection = FlowDirection.RightToLeft, AnimationTime = TimeSpan.Zero };
        using var host = new ProgressHost(indicator);
        Assert.Equal(Color.Parse("#6750A4"), host.Pixel(220, 2));
        Assert.Equal(Color.Parse("#FEF7FF"), host.Pixel(176, 2));
        Assert.Equal(Color.Parse("#E8DEF8"), host.Pixel(100, 2));
        Assert.Equal(Color.Parse("#6750A4"), host.Pixel(2, 2));
    }

    [AvaloniaFact]
    public void Controlled_time_rewinds_and_detachment_excludes_unobserved_time()
    {
        var indicator = new MaterialCircularProgressIndicator { IsIndeterminate = true, AnimationTime = TimeSpan.Zero };
        using var host = new ProgressHost(indicator);
        var initial = host.Capture();
        indicator.AnimationTime = TimeSpan.FromSeconds(2);
        Assert.NotEqual(initial, host.Capture());
        indicator.AnimationTime = TimeSpan.Zero;
        Assert.Equal(initial, host.Capture());
        var content = host.Window.Content;
        host.Window.Content = null;
        indicator.AnimationTime = TimeSpan.FromSeconds(20);
        host.Window.Content = content;
        Assert.Equal(initial, host.Capture());
        indicator.AnimationTime = TimeSpan.FromMilliseconds(20700);
        Assert.NotEqual(initial, host.Capture());
    }
}
