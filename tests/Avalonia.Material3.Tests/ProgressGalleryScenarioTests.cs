using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Tokens;
using Avalonia.Threading;
using Gallery.Pages;
using Xunit;

namespace Avalonia.Material3.Tests;

public class ProgressGalleryScenarioTests
{
    [AvaloniaFact]
    public async Task Gallery_user_starts_a_controllable_task_and_sees_progress_pause_completion_and_failure()
    {
        using var host = new ProgressHost(new StackPanel());
        var outcome = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        IProgress<double>? progress = null;
        var page = new ProgressFeedbackPage(host.Theme, (reporter, _) => { progress = reporter; return outcome.Task; });
        host.Window.Width = 1000;
        host.Window.Height = 900;
        host.Window.Content = new ScrollViewer { Content = page };
        Click(host.Window, page.StartButton);
        Assert.All(page.Indicators, indicator => Assert.Equal(MaterialProgressStatus.Running, indicator.Status));
        progress!.Report(.4);
        Dispatcher.UIThread.RunJobs();
        Assert.Contains("40%", page.Result.Text);
        Assert.False(page.StartButton.IsEnabled);
        Click(host.Window, page.PauseButton);
        Assert.All(page.Indicators, indicator => Assert.Equal(MaterialProgressStatus.Paused, indicator.Status));
        Click(host.Window, page.PauseButton);
        outcome.SetResult();
        await page.CurrentOperation;
        Assert.Contains("Completed", page.Result.Text);
        Assert.All(page.Indicators, indicator => Assert.Equal("Completed: 12 documents imported", ControlAutomationPeer.CreatePeerForElement(indicator)!.GetItemStatus()));
        Assert.True(page.StartButton.IsEnabled);
        outcome = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Click(host.Window, page.StartButton);
        outcome.SetException(new InvalidOperationException("Connection lost"));
        await page.CurrentOperation;
        Assert.Contains("Connection lost", page.Result.Text);
        Assert.All(page.Indicators, indicator => Assert.Equal(MaterialProgressStatus.Failed, indicator.Status));
        Assert.All(page.Indicators, indicator => Assert.Equal("Failed: Connection lost", ControlAutomationPeer.CreatePeerForElement(indicator)!.GetItemStatus()));
    }
    [AvaloniaFact]
    public void Narrow_gallery_at_200_percent_keeps_every_host_action_inside_the_viewport()
    {
        using var host = new ProgressHost(new StackPanel());
        host.Theme.Typography = host.Theme.Typography with { Scale = 2 };
        host.Window.Height = 900;
        var page = new ProgressFeedbackPage(host.Theme);
        host.Window.Content = new ScrollViewer { Content = page };
        using var frame = host.Window.CaptureRenderedFrame();
        foreach (var action in new[] { page.StartButton, page.PauseButton, page.FailureButton, page.MotionButton, page.ClockButton, page.StepButton, page.ThemeButton, page.FontButton })
            Assert.InRange(action.Bounds.Width, 48, 272);
    }

    private static void Click(Window window, Control button)
    {
        using var frame = window.CaptureRenderedFrame();
        var point = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)!.Value;
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
    }
}
