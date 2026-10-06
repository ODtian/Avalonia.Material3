using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Material3.Tests;

public class SecondaryFeedbackScenarioTests
{
    [AvaloniaFact]
    public void Controlled_long_press_suppresses_the_anchors_normal_click_and_cancelled_contacts_leave_no_tip()
    {
        using var host = new FeedbackHost();
        var clock = new FeedbackTimeProvider(); var clicks = 0;
        host.Entry.Click += (_, _) => clicks++;
        var tip = new MaterialTooltip { Content = "Long press information", LongPressDelay = TimeSpan.FromMilliseconds(500) };
        using var attachment = tip.Attach(host.Overlay, host.Entry, clock);
        var point = host.Center(host.Entry);
        using var touch = host.Window.TouchBegin(point);
        clock.Advance(TimeSpan.FromMilliseconds(499)); host.Render(); Assert.False(tip.IsOpen);
        clock.Advance(TimeSpan.FromMilliseconds(1)); host.Render(); Assert.True(tip.IsOpen);
        host.Window.TouchEnd(touch, point); host.Render(); Assert.Equal(0, clicks);
        clock.Advance(TimeSpan.FromMilliseconds(1500)); host.Render(); Assert.False(tip.IsOpen);
        var cancelled = host.Window.TouchBegin(point); cancelled.Dispose();
        clock.Advance(TimeSpan.FromSeconds(10)); host.Render(); Assert.False(tip.IsOpen); Assert.Equal(0, clicks);
    }

    [AvaloniaFact]
    public void Rich_tooltip_uses_pinned_first_baseline_distances_not_arbitrary_stack_spacing()
    {
        using var host = new FeedbackHost();
        var tip = new MaterialTooltip { Variant = MaterialTooltipVariant.Rich, Title = "Details", Content = "Supporting information", IsPersistent = true };
        tip.Show(host.Overlay, host.Entry); host.Render();
        var title = tip.GetVisualDescendants().OfType<TextBlock>().Single(text => text.Text == "Details");
        var body = tip.GetVisualDescendants().OfType<TextBlock>().Single(text => text.Text == "Supporting information");
        var titleBaseline = title.TranslatePoint(default, tip)!.Value.Y + title.TextLayout.TextLines[0].Baseline;
        var bodyBaseline = body.TranslatePoint(default, tip)!.Value.Y + body.TextLayout.TextLines[0].Baseline;
        Assert.Equal(28, titleBaseline, 2);
        Assert.Equal(24, bodyBaseline - titleBaseline, 2);
    }

    [AvaloniaFact]
    public void Tooltip_flips_below_the_top_edge_without_losing_the_four_DIP_anchor_gap()
    {
        using var host = new FeedbackHost(320, 480);
        var tip = new MaterialTooltip { Content = "Description" };
        tip.Show(host.Overlay, host.Entry); host.Render();
        var anchorBottom = host.Entry.TranslatePoint(new Point(0, host.Entry.Bounds.Height), host.Window)!.Value.Y;
        var tipTop = tip.TranslatePoint(default, host.Window)!.Value.Y;
        Assert.Equal(4, tipTop - anchorBottom, 3);
        Assert.InRange(tip.Bounds.Width, 40, 200);
    }

    [AvaloniaFact]
    public void Removing_a_focused_group_row_and_disabling_the_last_row_leave_a_safe_menu_focus()
    {
        using var host = new FeedbackHost();
        var first = new MaterialMenuItem { Content = "First" };
        var second = new MaterialMenuItem { Content = "Second" };
        var group = new MaterialMenuGroup { Items = { first, second } };
        var menu = new MaterialMenu { Items = { group } };
        menu.Show(host.Overlay, host.Entry); host.Render(); Assert.True(first.IsFocused);
        group.Items.Remove(first); host.Render(); Assert.True(second.IsFocused);
        second.IsEnabled = false; host.Render(); Assert.True(menu.IsFocused);
        host.Key(Key.Escape); Assert.False(menu.IsOpen);
        var empty = new MaterialMenu(); empty.Show(host.Overlay, host.Entry); host.Render();
        Assert.True(empty.IsFocused); host.Key(Key.Tab); Assert.False(empty.IsOpen); Assert.True(host.Entry.IsFocused);
    }

    [AvaloniaFact]
    public void Real_context_request_uses_target_local_point_and_hidden_anchor_converges_without_an_action()
    {
        using var host = new FeedbackHost(320, 480);
        var item = new MaterialMenuItem { Content = "Inspect", Value = "inspect" };
        var menu = new MaterialMenu { Items = { item } };
        using var attachment = menu.AttachContext(host.Overlay, host.Entry);
        var point = host.Entry.TranslatePoint(new Point(30, 30), host.Window)!.Value;
        host.Window.MouseDown(point, MouseButton.Right); host.Window.MouseUp(point, MouseButton.Right); host.Render();
        Assert.True(menu.IsOpen);
        Assert.Equal(new Point(30, 30), menu.Session!.Options.AnchorPoint);
        var result = menu.Session.Completion;
        host.Entry.IsVisible = false; host.Render();
        Assert.False(menu.IsOpen);
        Assert.Null(result.Result.Value);
        host.Entry.IsVisible = true; host.Render();
        host.Entry.Focus(); host.Key(Key.Apps);
        Assert.True(menu.IsOpen);
        host.Key(Key.Escape); Assert.False(menu.IsOpen);
    }

    [AvaloniaFact]
    public void Rich_keyboard_tooltip_stays_readable_and_Tab_reaches_its_action_then_returns_focus()
    {
        using var host = new FeedbackHost();
        var clock = new FeedbackTimeProvider();
        var result = 0;
        var tip = new MaterialTooltip
        {
            Variant = MaterialTooltipVariant.Rich, Title = "Backup",
            Content = "Keep a safe copy / 保存备份", ActionContent = "Learn more",
            ActionResult = "learn", ActionCommand = new FeedbackCommand(() => result++)
        };
        using var attachment = tip.Attach(host.Overlay, host.Entry, clock);
        host.Entry.Focus(); host.Render();
        var session = tip.Session!;
        Assert.True(tip.IsOpen);
        Assert.Equal(Color.Parse("#F3EDF7"), ((ISolidColorBrush)tip.Background!).Color);
        clock.Advance(TimeSpan.FromSeconds(10)); host.Render(); Assert.True(tip.IsOpen);
        host.Key(Key.Tab);
        Assert.NotSame(host.Entry, host.Window.FocusManager!.GetFocusedElement());
        host.Key(Key.Enter);
        Assert.Equal(1, result); Assert.Equal("learn", session.Completion.Result.Value);
        Assert.True(host.Entry.IsFocused); Assert.False(tip.IsOpen);
    }

    [AvaloniaFact]
    public void Plain_tooltip_manual_timeout_and_keyboard_description_do_not_steal_focus()
    {
        using var host = new FeedbackHost(); host.Entry.Focus();
        var clock = new FeedbackTimeProvider();
        var tip = new MaterialTooltip { Content = "Open secondary actions / 操作", EnableUserInput = false };
        using var attachment = tip.Attach(host.Overlay, host.Entry, clock);
        var session = tip.Show(host.Overlay, host.Entry, timeProvider: clock); host.Render();
        Assert.True(host.Entry.IsFocused);
        Assert.Equal(Color.Parse("#322F35"), ((ISolidColorBrush)tip.Background!).Color);
        Assert.Equal(Color.Parse("#F5EFF7"), ((ISolidColorBrush)tip.Foreground!).Color);
        Assert.Equal(AutomationControlType.ToolTip, ControlAutomationPeer.CreatePeerForElement(tip).GetAutomationControlType());
        Assert.Equal("Open secondary actions / 操作", ControlAutomationPeer.CreatePeerForElement(host.Entry).GetHelpText());
        clock.Advance(TimeSpan.FromMilliseconds(1499)); host.Render(); Assert.True(tip.IsOpen);
        clock.Advance(TimeSpan.FromMilliseconds(1)); host.Render(); Assert.False(tip.IsOpen);
        Assert.Equal(MaterialOverlayCloseReason.Cancelled, session.Completion.Result.Reason);
        host.Window.MouseMove(host.Center(host.Entry)); clock.Advance(TimeSpan.FromSeconds(5)); host.Render(); Assert.False(tip.IsOpen);
    }

    [AvaloniaFact]
    public void Snackbar_timeout_is_deferred_under_a_cover_and_old_actions_cannot_execute_or_close_a_new_generation()
    {
        using var host = new FeedbackHost(); host.Entry.Focus();
        var clock = new FeedbackTimeProvider();
        var actions = 0;
        var snackbar = new MaterialSnackbar { Content = "Saved / 已保存", ActionContent = "Undo", ActionCommand = new FeedbackCommand(() => actions++), Duration = TimeSpan.FromSeconds(4) };
        var session = snackbar.Show(host.Overlay, clock); host.Render();
        Assert.True(host.Entry.IsFocused);
        var cover = host.Overlay.Show(new MaterialButton { Content = "Covered feedback" }); host.Render();
        clock.Advance(TimeSpan.FromSeconds(4)); host.Render();
        Assert.True(session.IsOpen); Assert.False(snackbar.InvokeAction()); Assert.Equal(0, actions);
        cover.Dismiss(); host.Render();
        Assert.False(snackbar.IsOpen); Assert.Equal(MaterialOverlayCloseReason.Cancelled, session.Completion.Result.Reason);
        var next = snackbar.Show(host.Overlay, clock); host.Render();
        clock.Advance(TimeSpan.FromSeconds(3)); host.Render(); Assert.True(next.IsOpen);
        Assert.True(snackbar.InvokeAction()); Assert.Equal(1, actions);
        Assert.Equal(MaterialOverlayCloseReason.Confirmed, next.Completion.Result.Reason);
        clock.Advance(TimeSpan.FromSeconds(10)); host.Render(); Assert.Equal(1, actions);
    }

    [AvaloniaFact]
    public void Segmented_horizontal_groups_preserve_touch_targets_supporting_slots_and_radio_host_selection()
    {
        using var host = new FeedbackHost(320, 480);
        var left = new MaterialMenuItem { Content = "Small", ToggleMode = MaterialMenuToggleMode.Radio, GroupName = "size", StaysOpenOnClick = true, IsChecked = true, LeadingIcon = "A" };
        var right = new MaterialMenuItem { Content = "Large / 大号", ToggleMode = MaterialMenuToggleMode.Radio, GroupName = "size", StaysOpenOnClick = true, SupportingText = "For reading", TrailingText = "Aa" };
        var group = new MaterialMenuGroup { Orientation = Avalonia.Layout.Orientation.Horizontal, Items = { left, right } };
        var menu = new MaterialMenu { IsSegmented = true, Items = { group } };
        menu.Show(host.Overlay, host.Entry); host.Render();
        Assert.True(left.IsFocused);
        host.Key(Key.Right); Assert.True(right.IsFocused);
        host.Key(Key.Enter);
        Assert.True(right.IsChecked); Assert.False(left.IsChecked);
        Assert.True(left.Bounds.Height >= 48 && right.Bounds.Height >= 48);
        Assert.Equal(new CornerRadius(16), group.CornerRadius);
        host.Theme.Typography = host.Theme.Typography with { Scale = 2 };
        host.Render();
        Assert.True(right.Bounds.Height >= 80);
        Assert.True(menu.Bounds.Width <= 304);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Logical_submenu_navigation_returns_to_parent_then_commits_a_nested_result(bool rtl)
    {
        using var host = new FeedbackHost();
        host.Overlay.FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        var leaf = new MaterialMenuItem { Content = "Export PDF", Value = "pdf" };
        var child = new MaterialMenu { Items = { leaf } };
        var branch = new MaterialMenuItem { Content = "Export", Submenu = child };
        var menu = new MaterialMenu { Items = { branch } };
        var root = menu.Show(host.Overlay, host.Entry); host.Render();
        host.Key(rtl ? Key.Left : Key.Right);
        Assert.True(child.IsOpen); Assert.True(leaf.IsFocused);
        host.Key(Key.Escape);
        Assert.False(child.IsOpen); Assert.True(menu.IsOpen); Assert.True(branch.IsFocused);
        host.Key(rtl ? Key.Left : Key.Right);
        host.Key(Key.Enter);
        Assert.Equal("pdf", root.Completion.Result.Value);
        Assert.Equal(0, host.Overlay.OpenCount); Assert.True(host.Entry.IsFocused);
    }

    [AvaloniaFact]
    public void Menu_commands_cannot_run_when_covered_vetoed_or_faulted_and_selection_remains_host_owned()
    {
        using var host = new FeedbackHost();
        var commands = 0;
        var item = new MaterialMenuItem { Content = "Delete", Value = "deleted", Command = new FeedbackCommand(() => commands++) };
        var menu = new MaterialMenu { Items = { item } };
        var session = menu.Show(host.Overlay, host.Entry); host.Render();
        var cover = host.Overlay.Show(new MaterialButton { Content = "Cover" }, new MaterialOverlayOptions { IsModal = false }); host.Render();
        ((Avalonia.Automation.Provider.IInvokeProvider)ControlAutomationPeer.CreatePeerForElement(item)).Invoke();
        Assert.Equal(0, commands); Assert.True(session.IsOpen);
        cover.Dismiss(); host.Render();
        EventHandler<MaterialOverlayClosingEventArgs> veto = (_, args) => args.Cancel = true;
        session.Closing += veto;
        host.Key(Key.Enter);
        Assert.Equal(0, commands); Assert.True(menu.IsOpen);
        session.Closing -= veto;
        item.Command = new FeedbackCommand(() => throw new InvalidOperationException("Rejected by host"));
        Assert.Throws<InvalidOperationException>(() => ((Avalonia.Automation.Provider.IInvokeProvider)ControlAutomationPeer.CreatePeerForElement(item)).Invoke());
        Assert.True(menu.IsOpen); Assert.False(session.Completion.IsCompleted);
        item.Command = new FeedbackCommand(() => commands++);
        host.Key(Key.Enter);
        Assert.Equal(1, commands); Assert.Equal("deleted", session.Completion.Result.Value);
    }

    [AvaloniaFact]
    public void Checked_standard_and_vibrant_items_use_pinned_roles_and_stay_open_without_committing_highlight()
    {
        using var host = new FeedbackHost();
        var item = new MaterialMenuItem { Content = "Automatic backup", ToggleMode = MaterialMenuToggleMode.Check, StaysOpenOnClick = true };
        var menu = new MaterialMenu { IsSegmented = true, Items = { item } };
        menu.Show(host.Overlay, host.Entry); host.Render();
        Assert.False(item.IsChecked);
        host.Key(Key.Enter);
        Assert.True(item.IsChecked); Assert.True(menu.IsOpen);
        Assert.Equal(Color.Parse("#FFD8E4"), ((ISolidColorBrush)item.Background!).Color);
        Assert.Equal(Color.Parse("#31111D"), ((ISolidColorBrush)item.Foreground!).Color);
        menu.Variant = MaterialMenuVariant.Vibrant; host.Render();
        Assert.Equal(Color.Parse("#7D5260"), ((ISolidColorBrush)item.Background!).Color);
        Assert.Equal(Color.Parse("#FFFFFF"), ((ISolidColorBrush)item.Foreground!).Color);
        Assert.Equal(new CornerRadius(12), item.CornerRadius);
    }

    [AvaloniaFact]
    public void Menu_keyboard_skips_disabled_rows_and_commits_only_the_invoked_host_value()
    {
        using var host = new FeedbackHost();
        var first = new MaterialMenuItem { Content = "Copy", Value = "copied" };
        var disabled = new MaterialMenuItem { Content = "Unavailable", IsEnabled = false };
        var last = new MaterialMenuItem { Content = "Archive", Value = "archived" };
        var menu = new MaterialMenu { Items = { first, disabled, last } };
        host.Entry.Focus();
        var session = menu.Show(host.Overlay, host.Entry);
        host.Render();
        Assert.True(first.IsFocused);
        host.Key(Key.Down);
        Assert.True(last.IsFocused);
        host.Key(Key.Enter);
        Assert.Equal("archived", session.Completion.Result.Value);
        Assert.False(menu.IsOpen);
        Assert.True(host.Entry.IsFocused);
        Assert.Equal(AutomationControlType.MenuItem, ControlAutomationPeer.CreatePeerForElement(last).GetAutomationControlType());
    }
}

public sealed class FeedbackCommand(Action action, bool enabled = true) : System.Windows.Input.ICommand
{
    public event EventHandler? CanExecuteChanged { add { } remove { } }
    public bool CanExecute(object? parameter) => enabled;
    public void Execute(object? parameter) => action();
}

public sealed class FeedbackTimeProvider : TimeProvider
{
    private readonly List<FeedbackTimer> _timers = [];
    private long _ticks;
    public override DateTimeOffset GetUtcNow() => DateTimeOffset.UnixEpoch + TimeSpan.FromTicks(_ticks);
    public override long GetTimestamp() => _ticks;
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new FeedbackTimer(this, callback, state); _timers.Add(timer); timer.Change(dueTime, period); return timer;
    }
    public void Advance(TimeSpan elapsed)
    {
        _ticks += elapsed.Ticks;
        foreach (var timer in _timers.ToArray()) timer.Fire();
    }
    private sealed class FeedbackTimer(FeedbackTimeProvider clock, TimerCallback callback, object? state) : ITimer
    {
        private long _due = long.MaxValue;
        private TimeSpan _period;
        public bool Change(TimeSpan dueTime, TimeSpan period) { _due = dueTime == Timeout.InfiniteTimeSpan ? long.MaxValue : clock._ticks + dueTime.Ticks; _period = period; return true; }
        public void Fire()
        {
            if (_due > clock._ticks) return;
            _due = _period == Timeout.InfiniteTimeSpan ? long.MaxValue : clock._ticks + _period.Ticks;
            callback(state);
        }
        public void Dispose() { _due = long.MaxValue; clock._timers.Remove(this); }
        public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
    }
}

public sealed class FeedbackHost : IDisposable
{
    public MaterialTheme Theme { get; } = new();
    public Window Window { get; }
    public MaterialOverlayHost Overlay { get; }
    public MaterialButton Entry { get; } = new() { Content = "Secondary actions", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left };
    public FeedbackHost(double width = 800, double height = 600)
    {
        Application.Current!.Styles.Add(Theme);
        Overlay = new MaterialOverlayHost { Content = new StackPanel { Margin = new Thickness(20), Children = { Entry } } };
        Window = new Window { Width = width, Height = height, Content = Overlay, RequestedThemeVariant = ThemeVariant.Light };
        Window.Show();
        Render();
    }
    public void Render() { Window.UpdateLayout(); Avalonia.Threading.Dispatcher.UIThread.RunJobs(); Window.CaptureRenderedFrame()?.Dispose(); }
    public Point Center(Control control) => control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), Window)!.Value;
    public void Click(Control control) { var point = Center(control); Window.MouseMove(point); Window.MouseDown(point, MouseButton.Left); Window.MouseUp(point, MouseButton.Left); Render(); }
    public void Key(Key key) { Window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null); Window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null); Render(); }
    public void Dispose() { Window.Close(); Application.Current!.Styles.Remove(Theme); }
}
