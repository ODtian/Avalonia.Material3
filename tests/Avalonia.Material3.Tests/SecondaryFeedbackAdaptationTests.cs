using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Material3.Tests;

public class SecondaryFeedbackAdaptationTests
{
    [AvaloniaTheory]
    [InlineData(MaterialMenuVariant.Standard, false, "#FFD8E4", "#31111D")]
    [InlineData(MaterialMenuVariant.Vibrant, false, "#7D5260", "#FFFFFF")]
    [InlineData(MaterialMenuVariant.LegacyDropdown, false, "#E8DEF8", "#1D192B")]
    [InlineData(MaterialMenuVariant.Standard, true, "#633B48", "#FFD8E4")]
    [InlineData(MaterialMenuVariant.Vibrant, true, "#EFB8C8", "#492532")]
    [InlineData(MaterialMenuVariant.LegacyDropdown, true, "#4A4458", "#E8DEF8")]
    public void Pinned_variants_update_live_theme_roles_and_resize_with_long_localized_content(MaterialMenuVariant variant, bool dark, string background, string foreground)
    {
        using var host = new FeedbackHost(320, 480);
        var selected = new MaterialMenuItem { Content = "A very long translated operation / 长操作名称", IsChecked = true, SupportingText = "Supporting details / 辅助信息", TrailingText = "⌘P", Value = "done" };
        var menu = new MaterialMenu { Variant = variant, IsSegmented = variant != MaterialMenuVariant.LegacyDropdown };
        for (var i = 0; i < 15; i++) menu.Items.Add(new MaterialMenuItem { Content = $"Operation {i}", Value = i });
        menu.Items.Add(selected);
        menu.Show(host.Overlay, host.Entry);
        host.Window.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
        host.Theme.Typography = host.Theme.Typography with { Scale = 2 };
        host.Overlay.FlowDirection = FlowDirection.RightToLeft;
        host.Render();
        Assert.Equal(Color.Parse(background), ((ISolidColorBrush)selected.Background!).Color);
        Assert.Equal(Color.Parse(foreground), ((ISolidColorBrush)selected.Foreground!).Color);
        host.Key(Key.End); Assert.True(selected.IsFocused);
        var center = host.Center(selected);
        Assert.InRange(center.X, 8, 312); Assert.InRange(center.Y, 48, 432);
        Assert.True(selected.Bounds.Height >= 80);
        Save(host, $"menu-{variant}-{(dark ? "dark" : "light")}-320-font200-rtl.png");
        host.Window.Width = 1000; host.Window.Height = 700; host.Render();
        Assert.True(menu.Bounds.Width <= 280); Assert.True(menu.Bounds.Height <= 604);
        host.Key(Key.Enter);
        Assert.False(menu.IsOpen); Assert.True(host.Entry.IsFocused);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Long_tooltip_and_snackbar_text_actions_stay_reachable_in_short_scaled_windows(bool dark)
    {
        using var host = new FeedbackHost(320, 300);
        host.Window.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
        host.Theme.Typography = host.Theme.Typography with { Scale = 2 };
        var tip = new MaterialTooltip
        {
            Variant = MaterialTooltipVariant.Rich, Title = "A long heading / 长标题",
            Content = string.Join(" ", Enumerable.Repeat("Mixed supporting text / 多语言文字", 12)),
            ActionContent = "Read more information / 查看更多信息", ActionResult = "more"
        };
        var tipSession = tip.Show(host.Overlay, host.Entry); host.Render();
        var tipAction = tip.GetVisualDescendants().OfType<Button>().Single(button => button.Content as string == tip.ActionContent as string);
        tipAction.BringIntoView(); host.Render();
        Assert.InRange(host.Center(tipAction).Y, 8, 292);
        host.Click(tipAction); Assert.Equal("more", tipSession.Completion.Result.Value);
        var snackbar = new MaterialSnackbar
        {
            Content = "A long feedback message / 较长的反馈消息. No important text is truncated.",
            ActionContent = "Restore previous changes / 恢复以前的更改", ActionResult = "restored", ActionOnNewLine = true
        };
        var session = snackbar.Show(host.Overlay); host.Render();
        Assert.Equal(dark ? Color.Parse("#E6E0E9") : Color.Parse("#322F35"), ((ISolidColorBrush)snackbar.Background!).Color);
        var action = snackbar.GetVisualDescendants().OfType<Button>().Single(button => button.Content as string == snackbar.ActionContent as string);
        action.BringIntoView(); host.Render();
        Assert.InRange(host.Center(action).X, 12, 308); Assert.InRange(host.Center(action).Y, 12, 288);
        Save(host, $"snackbar-{(dark ? "dark" : "light")}-320-font200.png");
        host.Click(action); Assert.Equal("restored", session.Completion.Result.Value);
    }

    [AvaloniaFact]
    public void Touch_cancellation_never_invokes_a_row_but_a_released_target_returns_one_result()
    {
        using var host = new FeedbackHost();
        var row = new MaterialMenuItem { Content = "Touch action", Value = "touch" };
        var menu = new MaterialMenu { Items = { row } };
        var session = menu.Show(host.Overlay, host.Entry); host.Render();
        var point = row.TranslatePoint(new Point(row.Bounds.Width / 2, 1), host.Window)!.Value;
        var cancelled = host.Window.TouchBegin(point); cancelled.Dispose(); host.Render();
        Assert.True(session.IsOpen);
        using var touch = host.Window.TouchBegin(point);
        host.Window.TouchEnd(touch, point); host.Render();
        Assert.Equal("touch", session.Completion.Result.Value);
        Assert.False(menu.IsOpen);
    }

    [AvaloniaFact]
    public void Hover_and_keyboard_tooltips_have_different_lifetimes_and_disposal_preserves_explicit_HelpText()
    {
        using var host = new FeedbackHost();
        var other = new MaterialButton { Content = "Other focus" };
        ((StackPanel)host.Overlay.Content!).Children.Add(other); host.Render(); other.Focus();
        var clock = new FeedbackTimeProvider();
        var tip = new MaterialTooltip { Content = "Description" };
        AutomationProperties.SetHelpText(host.Entry, "Host description");
        using var attachment = tip.Attach(host.Overlay, host.Entry, clock);
        host.Window.MouseMove(host.Center(host.Entry)); host.Render();
        clock.Advance(TimeSpan.FromMilliseconds(399)); host.Render(); Assert.False(tip.IsOpen);
        clock.Advance(TimeSpan.FromMilliseconds(1)); host.Render(); Assert.True(tip.IsOpen);
        clock.Advance(TimeSpan.FromSeconds(10)); host.Render(); Assert.True(tip.IsOpen); Assert.True(other.IsFocused);
        host.Window.MouseMove(new Point(700, 400)); host.Render(); Assert.False(tip.IsOpen);
        host.Entry.Focus(); host.Render(); Assert.True(tip.IsOpen);
        clock.Advance(TimeSpan.FromSeconds(10)); host.Render(); Assert.True(tip.IsOpen);
        other.Focus(); host.Render(); Assert.False(tip.IsOpen);
        attachment.Dispose();
        Assert.Equal("Host description", ControlAutomationPeer.CreatePeerForElement(host.Entry).GetHelpText());
        host.Entry.Focus(); host.Render(); Assert.False(tip.IsOpen);
    }

    [AvaloniaFact]
    public void Cancellation_detach_and_timeout_races_finish_once_without_reusing_old_callbacks()
    {
        using var host = new FeedbackHost();
        var clock = new FeedbackTimeProvider();
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        var feedback = new MaterialSnackbar { Content = "Pending", Duration = TimeSpan.FromSeconds(10) };
        Assert.Throws<OperationCanceledException>(() => feedback.Show(host.Overlay, clock, cancelled.Token));
        Assert.Equal(0, host.Overlay.OpenCount);
        using var cancellation = new CancellationTokenSource();
        var first = feedback.Show(host.Overlay, clock, cancellation.Token); host.Render();
        var cover = host.Overlay.Show(new TextBlock { Text = "Other overlay" }, new MaterialOverlayOptions { IsModal = false }); host.Render();
        cancellation.Cancel(); host.Render(); Assert.True(first.IsOpen); Assert.False(feedback.CanInvokeAction);
        cover.Dismiss(); host.Render(); Assert.True(first.Completion.IsCompleted);
        var second = feedback.Show(host.Overlay, clock); host.Render();
        host.Window.Content = null; host.Render();
        Assert.Equal(MaterialOverlayCloseReason.HostDetached, second.Completion.Result.Reason);
        clock.Advance(TimeSpan.FromHours(1)); host.Render(); Assert.False(feedback.IsOpen); Assert.Equal(0, host.Overlay.OpenCount);
        host.Window.Content = host.Overlay; host.Render();
        var third = feedback.Show(host.Overlay, clock); host.Render();
        Assert.True(third.IsOpen); feedback.Dismiss();
    }

    [AvaloniaFact]
    public void Snackbar_actions_veto_and_CanExecute_have_real_enabled_semantics_and_command_faults_do_not_commit()
    {
        using var host = new FeedbackHost();
        var snackbar = new MaterialSnackbar { Content = "Undo?", ActionContent = "Undo", ActionCommand = new FeedbackCommand(() => throw new InvalidOperationException("Host refused"), false) };
        var session = snackbar.Show(host.Overlay); host.Render();
        var action = snackbar.GetVisualDescendants().OfType<Button>().Single(button => button.Content as string == "Undo");
        Assert.False(ControlAutomationPeer.CreatePeerForElement(action).IsEnabled());
        Assert.False(snackbar.InvokeAction());
        snackbar.ActionCommand = new FeedbackCommand(() => throw new InvalidOperationException("Host refused"));
        host.Render(); Assert.True(ControlAutomationPeer.CreatePeerForElement(action).IsEnabled());
        Assert.Throws<InvalidOperationException>(() => snackbar.InvokeAction()); Assert.True(session.IsOpen); Assert.False(session.Completion.IsCompleted);
        EventHandler<MaterialOverlayClosingEventArgs> veto = (_, args) => args.Cancel = true;
        session.Closing += veto; Assert.False(snackbar.InvokeAction()); Assert.True(session.IsOpen);
        session.Closing -= veto; snackbar.ActionCommand = null; Assert.True(snackbar.InvokeAction());
    }

    [AvaloniaFact]
    public void Sibling_submenus_converge_and_anchor_hide_closes_the_whole_chain_without_a_business_result()
    {
        using var host = new FeedbackHost();
        var one = new MaterialMenu { Items = { new MaterialMenuItem { Content = "First leaf" } } };
        var two = new MaterialMenu { Items = { new MaterialMenuItem { Content = "Second leaf" } } };
        var first = new MaterialMenuItem { Content = "First branch", Submenu = one };
        var second = new MaterialMenuItem { Content = "Second branch", Submenu = two };
        var menu = new MaterialMenu { Items = { first, second } };
        var root = menu.Show(host.Overlay, host.Entry); host.Render();
        ((IExpandCollapseProvider)ControlAutomationPeer.CreatePeerForElement(first)).Expand(); host.Render();
        ((IExpandCollapseProvider)ControlAutomationPeer.CreatePeerForElement(second)).Expand(); host.Render();
        Assert.False(one.IsOpen); Assert.True(two.IsOpen); Assert.Equal(2, host.Overlay.OpenCount);
        host.Entry.IsVisible = false; host.Render();
        Assert.False(two.IsOpen); Assert.False(menu.IsOpen); Assert.Null(root.Completion.Result.Value);
    }

    [AvaloniaFact]
    public void Public_content_and_replacement_item_template_keep_native_input_command_parameters_and_TwoWay_selection()
    {
        using var host = new FeedbackHost();
        var model = new CheckModel();
        var row = new MaterialMenuItem { Content = "Custom content", ToggleMode = MaterialMenuToggleMode.Check, StaysOpenOnClick = true, CommandParameter = "parameter" };
        row.Bind(MaterialMenuItem.IsCheckedProperty, new Avalonia.Data.Binding(nameof(CheckModel.Checked)) { Source = model, Mode = Avalonia.Data.BindingMode.TwoWay });
        row.Template = new FuncControlTemplate<MaterialMenuItem>((_, _) => new Border { Background = Brushes.LightGray, Padding = new Thickness(16), Child = new TextBlock { Text = "Rendered custom slot" } });
        object? observed = null; row.Click += (_, _) => observed = row.CommandParameter;
        var menu = new MaterialMenu { Items = { row } }; menu.Show(host.Overlay, host.Entry); host.Render();
        host.Click(row); Assert.Equal("parameter", observed); Assert.True(model.Checked);
        model.Checked = false; host.Render(); Assert.False(row.IsChecked);
        host.Key(Key.Escape); Assert.True(host.Entry.IsFocused);
    }

    private sealed class CheckModel : System.ComponentModel.INotifyPropertyChanged
    {
        private bool _checked;
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        public bool Checked { get => _checked; set { _checked = value; PropertyChanged?.Invoke(this, new(nameof(Checked))); } }
    }
    private static void Save(FeedbackHost host, string name)
    {
        var directory = Environment.GetEnvironmentVariable("M3_ISSUE15_SCREENSHOTS");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        using var bitmap = host.Window.CaptureRenderedFrame();
        bitmap?.Save(Path.Combine(directory, name), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
    }
}
