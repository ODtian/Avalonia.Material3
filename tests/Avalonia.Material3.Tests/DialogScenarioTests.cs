using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Xunit;

namespace Avalonia.Material3.Tests;

public class DialogScenarioTests
{
    [AvaloniaFact]
    public void Teardown_completes_the_entire_stack_even_if_a_closed_observer_throws()
    {
        using var host = new DialogHost();
        var outer = host.Overlay.Show(new TextBlock { Text = "Outer" });
        var inner = host.Overlay.Show(new TextBlock { Text = "Inner" }); host.Render();
        inner.Closed += (_, _) => throw new InvalidOperationException("Host observer failed");
        Assert.ThrowsAny<Exception>(() => { host.Window.Content = null; host.Render(); });
        Assert.True(inner.Completion.IsCompleted);
        Assert.True(outer.Completion.IsCompleted);
        Assert.Equal(0, host.Overlay.OpenCount);
    }

    [AvaloniaFact]
    public void Host_replacement_template_uses_the_public_accessibility_scope_and_retains_modal_results()
    {
        using var host = new DialogHost();
        host.Overlay.Template = new FuncControlTemplate<MaterialOverlayHost>((owner, scope) =>
        {
            var background = new MaterialOverlayContentPresenter();
            background.Bind(MaterialOverlayContentPresenter.ContentProperty, new Avalonia.Data.Binding("Content") { Source = owner });
            var layer = new Grid();
            scope.Register("PART_ContentPresenter", background); scope.Register("PART_OverlayLayer", layer);
            return new Grid { Children = { background, layer } };
        });
        host.Render();
        var action = new MaterialButton { Content = "Custom host modal" };
        var session = host.Overlay.Show(action); host.Render();
        Assert.False(host.Entry.IsEffectivelyEnabled); Assert.True(action.IsFocused);
        action.Click += (_, _) => session.Close("Custom host result");
        host.Click(action); Assert.Equal("Custom host result", session.Completion.Result.Value);
        Assert.True(host.Entry.IsEffectivelyEnabled);
    }

    [AvaloniaFact]
    public void Removed_return_focus_and_nonfocusable_initial_focus_fall_back_to_usable_controls()
    {
        using var host = new DialogHost(); host.Entry.Focus();
        var nextEntry = new MaterialButton { Content = "Fallback entry" };
        var background = (StackPanel)host.Overlay.Content!; background.Children.Add(nextEntry); host.Render();
        var action = new MaterialButton { Content = "Action" };
        var content = new StackPanel { Children = { action } };
        var session = host.Overlay.Show(content, new MaterialOverlayOptions { InitialFocus = content }); host.Render();
        Assert.True(action.IsFocused);
        background.Children.Remove(host.Entry);
        session.Dismiss(); Assert.True(nextEntry.IsFocused);
    }

    [AvaloniaFact]
    public void Long_content_accepts_native_touch_scroll_gestures_without_activating_background()
    {
        using var host = new DialogHost();
        var dialog = new MaterialDialog { Title = "Touch scroll", Content = string.Join("\n", Enumerable.Repeat("Long content", 100)) };
        dialog.Show(host.Overlay); host.Render();
        var scroll = dialog.GetVisualDescendants().OfType<ScrollViewer>().OrderByDescending(viewer => viewer.Extent.Height).First();
        scroll.IsScrollInertiaEnabled = false; // Verify direct panning, not time-dependent post-release physics.
        host.Render();
        var start = host.Center(scroll) + new Vector(0, 80);
        using var touch = host.Window.TouchBegin(start);
        for (var move = 1; move <= 5; move++) { host.Window.TouchMove(touch, start - new Vector(0, move * 32)); host.Render(); }
        host.Window.TouchEnd(touch, start - new Vector(0, 160));
        host.Render();
        Assert.True(scroll.Offset.Y > 0);
        Assert.False(host.Entry.IsEffectivelyEnabled);
    }

    [AvaloniaFact]
    public void A_very_short_scaled_window_can_scroll_the_shell_to_reach_wrapped_actions()
    {
        using var host = new DialogHost(320, 300);
        host.Theme.Typography = host.Theme.Typography with { Scale = 2 };
        var dialog = new MaterialDialog { Title = "Very long translated heading / 长标题", Content = "Supporting text", ConfirmText = "Confirm translated changes", CancelText = "Discard translated changes" };
        var session = dialog.Show(host.Overlay); host.Render();
        var confirm = host.Button(dialog, "Confirm translated changes");
        confirm.BringIntoView(); host.Render();
        var center = host.Center(confirm);
        Assert.InRange(center.Y, 24, 276);
        host.Click(confirm);
        Assert.True(session.Completion.IsCompleted);
    }

    [AvaloniaTheory]
    [InlineData(MaterialDialogMode.Basic)]
    [InlineData(MaterialDialogMode.FullScreen)]
    public void Touch_cancel_has_no_value_and_disabled_confirmation_cannot_commit(MaterialDialogMode mode)
    {
        using var host = new DialogHost();
        host.Entry.Focus();
        var dialog = new MaterialDialog { Mode = mode, Title = "Confirm action", Content = "Review", IsConfirmEnabled = false, ConfirmResult = "Must not leak" };
        var session = dialog.Show(host.Overlay);
        host.Render();
        var confirm = host.Button(dialog, "OK");
        Assert.False(ControlAutomationPeer.CreatePeerForElement(confirm).IsEnabled());
        host.Click(confirm);
        Assert.False(dialog.Confirm());
        Assert.True(session.IsOpen);
        var cancel = mode == MaterialDialogMode.Basic ? host.Button(dialog, "Cancel") : host.Button(dialog, "×");
        Assert.Equal("Cancel", ControlAutomationPeer.CreatePeerForElement(cancel).GetName());
        var point = cancel.TranslatePoint(new Point(cancel.Bounds.Width / 2, 1), host.Window)!.Value;
        using var touch = host.Window.TouchBegin(point);
        host.Window.TouchEnd(touch, point);
        host.Render();
        Assert.Equal(new MaterialOverlayResult(MaterialOverlayCloseReason.Cancelled), session.Completion.Result);
        Assert.True(host.Entry.IsFocused);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Modal_scrim_blocks_background_and_only_opted_in_light_dismiss_completes(bool lightDismiss)
    {
        using var host = new DialogHost();
        var activated = 0; host.Entry.Click += (_, _) => activated++;
        var session = host.Overlay.Show(new MaterialButton { Content = "Overlay action" }, new MaterialOverlayOptions { CloseOnLightDismiss = lightDismiss });
        host.Render();
        host.Click(host.Entry);
        Assert.Equal(0, activated);
        Assert.Equal(!lightDismiss, session.IsOpen);
        if (lightDismiss) Assert.Equal(MaterialOverlayCloseReason.LightDismiss, session.Completion.Result.Reason);
    }

    [AvaloniaFact]
    public void Modeless_feedback_can_opt_out_of_focus_without_changing_the_hosts_enabled_binding()
    {
        using var host = new DialogHost(); host.Entry.Focus();
        var session = host.Overlay.Show(new TextBlock { Text = "Feedback" }, new MaterialOverlayOptions { IsModal = false, TakeFocus = false, RestoreFocus = false, Placement = MaterialOverlayPlacement.Bottom });
        host.Render(); Assert.True(host.Entry.IsFocused); Assert.True(host.Entry.IsEffectivelyEnabled);
        session.Dismiss(); Assert.True(host.Entry.IsFocused);
    }

    [AvaloniaTheory]
    [InlineData(MaterialOverlayPlacement.Bottom, false, 340, 520)]
    [InlineData(MaterialOverlayPlacement.Start, false, 20, 20)]
    [InlineData(MaterialOverlayPlacement.Start, true, 660, 20)]
    [InlineData(MaterialOverlayPlacement.End, false, 660, 20)]
    [InlineData(MaterialOverlayPlacement.End, true, 20, 20)]
    public void Sheet_and_drawer_consumers_get_logical_edge_placement(MaterialOverlayPlacement placement, bool rtl, double x, double y)
    {
        using var host = new DialogHost(); host.Overlay.FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        var content = new Border { Width = 120, MinHeight = 60, Background = Brushes.Red, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
        host.Overlay.Show(content, new MaterialOverlayOptions { Placement = placement, Margin = new Thickness(20) }); host.Render();
        var bounds = new Rect(content.Bounds.Size).TransformToAABB(content.TransformToVisual(host.Window)!.Value);
        Assert.Equal(new Point(x, y), bounds.TopLeft);
        if (placement != MaterialOverlayPlacement.Bottom) Assert.Equal(560, content.Bounds.Height);
    }

    [AvaloniaFact]
    public void Host_templates_slots_and_custom_actions_keep_native_editing_and_completion()
    {
        using var host = new DialogHost();
        var editor = new MaterialTextField { Label = "Custom slot", Text = "original" };
        var custom = new MaterialButton { Content = "Apply" };
        var dialog = new MaterialDialog { Title = "Custom slots", Content = "host model", ContentTemplate = new FuncDataTemplate<string>((_, _) => editor), Actions = custom, ConfirmResult = "custom value" };
        custom.Click += (_, _) => dialog.Confirm();
        var session = dialog.Show(host.Overlay, new MaterialOverlayOptions { InitialFocus = editor }); host.Render();
        Assert.True(editor.IsFocused); editor.SelectAll(); host.Window.KeyTextInput("edited");
        Assert.Equal("edited", editor.Text);
        Assert.DoesNotContain(dialog.GetVisualDescendants().OfType<MaterialButton>(), button => button.IsEffectivelyVisible && AutomationProperties.GetName(button) == "OK");
        host.Click(custom); Assert.Equal("custom value", session.Completion.Result.Value);
        // A public replacement template can preserve the interaction contract without private collaborators.
        var replacementAction = new MaterialButton { Content = "Host action" };
        dialog.Actions = null;
        dialog.Template = new FuncControlTemplate<MaterialDialog>((_, _) => replacementAction);
        replacementAction.Click += (_, _) => dialog.Cancel();
        var next = dialog.Show(host.Overlay); host.Render(); host.Click(replacementAction);
        Assert.Equal(MaterialOverlayCloseReason.Cancelled, next.Completion.Result.Reason);
    }

    [AvaloniaFact]
    public void Theme_mode_seed_and_typography_update_already_open_material_surfaces()
    {
        using var host = new DialogHost();
        var dialog = new MaterialDialog { Title = "Live theme", Content = "中文 / supporting content" };
        dialog.Show(host.Overlay); host.Render();
        Assert.Equal(Tokens.MaterialColorScheme.Light.SurfaceContainerHigh, ((ISolidColorBrush)dialog.Background!).Color);
        host.Window.RequestedThemeVariant = ThemeVariant.Dark; host.Render();
        Assert.Equal(Tokens.MaterialColorScheme.Dark.SurfaceContainerHigh, ((ISolidColorBrush)dialog.Background!).Color);
        host.Theme.SeedColor = Colors.Red; host.Theme.Typography = host.Theme.Typography with { Scale = 2 }; host.Render();
        Assert.Equal(Tokens.MaterialColorScheme.FromSeed(Colors.Red, true).SurfaceContainerHigh, ((ISolidColorBrush)dialog.Background!).Color);
        Assert.Equal(28, dialog.FontSize);
        Assert.Equal(new CornerRadius(28), dialog.CornerRadius);
    }

    [AvaloniaFact]
    public void Invalid_and_reused_presentations_fail_without_corrupting_the_host()
    {
        using var host = new DialogHost();
        Assert.Throws<ArgumentException>(() => host.Overlay.Show(new TextBlock(), new MaterialOverlayOptions { ScrimOpacity = double.NaN }));
        Assert.Throws<ArgumentException>(() => host.Overlay.Show(new TextBlock(), new MaterialOverlayOptions { Placement = MaterialOverlayPlacement.Anchor }));
        Assert.Equal(0, host.Overlay.OpenCount);
        var dialog = new MaterialDialog(); var session = dialog.Show(host.Overlay);
        Assert.Throws<InvalidOperationException>(() => dialog.Show(host.Overlay));
        Assert.Throws<InvalidOperationException>(() => host.Overlay.Show(dialog));
        Assert.Throws<ArgumentOutOfRangeException>(() => session.Dismiss(MaterialOverlayCloseReason.HostDetached));
        host.Render(); dialog.Cancel();
        Assert.False(session.Close("late")); Assert.Equal(0, host.Overlay.OpenCount);
    }

    [AvaloniaFact]
    public void Removing_an_anchor_forces_its_presentation_closed_even_when_dismissal_is_vetoed()
    {
        using var host = new DialogHost();
        var session = host.Overlay.Show(new MaterialButton { Content = "Transient action" }, new MaterialOverlayOptions { IsModal = false, Placement = MaterialOverlayPlacement.Anchor, Anchor = host.Entry });
        session.Closing += (_, args) => args.Cancel = true;
        host.Render();
        ((StackPanel)host.Overlay.Content!).Children.Remove(host.Entry);
        host.Render();
        Assert.True(session.Completion.IsCompleted);
        Assert.Equal(MaterialOverlayCloseReason.AnchorDetached, session.Completion.Result.Reason);
        Assert.Equal(0, host.Overlay.OpenCount);
    }

    [AvaloniaFact]
    public void Automation_navigation_exposes_only_the_active_modal_and_reconnects_background_after_cancel()
    {
        using var host = new DialogHost();
        var peer = ControlAutomationPeer.CreatePeerForElement(host.Overlay);
        Assert.Contains("Open dialog", Names(peer));
        var dialog = new MaterialDialog { Title = "Accessible confirmation", Content = "Review these changes" };
        var session = dialog.Show(host.Overlay);
        host.Render();
        Assert.DoesNotContain("Open dialog", Names(peer));
        Assert.Contains("Accessible confirmation", Names(peer));
        ControlAutomationPeer.CreatePeerForElement(host.Button(dialog, "Cancel")).GetProvider<Avalonia.Automation.Provider.IInvokeProvider>()!.Invoke();
        host.Render();
        Assert.True(session.Completion.IsCompleted);
        Assert.Equal(MaterialOverlayCloseReason.Cancelled, session.Completion.Result.Reason);
        Assert.Contains("Open dialog", Names(peer));
        static IEnumerable<string?> Names(AutomationPeer peer) => new[] { peer.GetName() }.Concat(peer.GetChildren().SelectMany(Names));
    }

    [AvaloniaFact]
    public void Reentrant_completion_is_rejected_and_closed_observers_can_open_the_next_presentation()
    {
        using var host = new DialogHost();
        var session = host.Overlay.Show(new MaterialButton { Content = "Complete once" });
        host.Render();
        var callbacks = 0;
        var reentered = false;
        session.Closing += (_, _) => { if (++callbacks == 1) reentered = session.Close("nested"); };
        MaterialOverlaySession? next = null;
        session.Closed += (_, _) => next = host.Overlay.Show(new MaterialButton { Content = "Next" });
        Assert.True(session.Close("outer"));
        Assert.False(reentered);
        Assert.Equal("outer", session.Completion.Result.Value);
        Assert.NotNull(next);
        Assert.Equal(1, host.Overlay.OpenCount);
    }

    [AvaloniaTheory]
    [InlineData(MaterialDialogMode.Basic)]
    [InlineData(MaterialDialogMode.FullScreen)]
    public void Long_mixed_content_scrolls_while_actions_stay_reachable_after_resize_and_font_scaling(MaterialDialogMode mode)
    {
        using var host = new DialogHost();
        var dialog = new MaterialDialog { Mode = mode, Title = "Long content / 长内容", Content = string.Join("\n", Enumerable.Repeat("中文 / A long piece of supporting content that wraps.", 80)), ConfirmText = "Save" };
        var session = dialog.Show(host.Overlay);
        host.Render();
        var position = dialog.TranslatePoint(default, host.Window)!.Value;
        if (mode == MaterialDialogMode.Basic) { Assert.InRange(dialog.Bounds.Width, 280, 560); Assert.True(position.X >= 24); }
        else { Assert.Equal(default, position); Assert.Equal(800, dialog.Bounds.Width); Assert.Equal(600, dialog.Bounds.Height); }
        host.Theme.Typography = host.Theme.Typography with { Scale = 2 };
        host.Window.Width = 320; host.Window.Height = 480;
        host.Render();
        var confirm = host.Button(dialog, "Save");
        // Tiny/scaled viewports can need the public BringIntoView/scroll fallback for the shell.
        confirm.BringIntoView(); host.Render();
        var point = confirm.TranslatePoint(default, host.Window)!.Value;
        Assert.InRange(point.X, 0, 320 - confirm.Bounds.Width);
        Assert.InRange(point.Y, 0, 480 - confirm.Bounds.Height);
        Assert.True(confirm.Bounds.Height >= 48);
        var scroll = dialog.GetVisualDescendants().OfType<ScrollViewer>().OrderByDescending(viewer => viewer.Extent.Height).First();
        Assert.True(scroll.Extent.Height > scroll.Viewport.Height);
        var old = scroll.Offset.Y;
        host.Window.MouseWheel(host.Center(scroll), new Vector(0, -3));
        host.Render();
        Assert.True(scroll.Offset.Y > old);
        host.Click(confirm);
        Assert.True(session.Completion.IsCompleted);
    }

    [AvaloniaFact]
    public void User_edits_a_basic_dialog_validation_keeps_it_open_then_confirm_returns_the_edited_result()
    {
        using var host = new DialogHost();
        host.Entry.Focus();
        var editor = new MaterialTextField { Label = "Display name", Text = "Old name" };
        var dialog = new MaterialDialog { Title = "Edit profile", Content = editor, ConfirmText = "Save", Icon = "☆" };
        dialog.Confirming += (_, args) => { args.Value = editor.Text; args.Cancel = string.IsNullOrWhiteSpace(editor.Text); };
        var session = dialog.Show(host.Overlay, new MaterialOverlayOptions { InitialFocus = editor });
        host.Render();
        Assert.True(editor.IsFocused);
        editor.SelectAll();
        host.Window.KeyTextInput("New name");
        host.Render();
        var peer = ControlAutomationPeer.CreatePeerForElement(dialog);
        Assert.Equal("Edit profile", peer.GetName());
        Assert.Equal(AutomationControlType.Window, peer.GetAutomationControlType());
        Assert.Equal("Modal dialog open", peer.GetItemStatus());
        editor.Text = "";
        host.Click(host.Button(dialog, "Save"));
        Assert.True(session.IsOpen);
        editor.Text = "New name";
        host.Click(host.Button(dialog, "Save"));
        Assert.Equal(new MaterialOverlayResult(MaterialOverlayCloseReason.Confirmed, "New name"), session.Completion.Result);
        Assert.True(host.Entry.IsFocused);
        Assert.False(dialog.IsOpen);
        Assert.Equal("Dialog closed", peer.GetItemStatus());
    }

    [AvaloniaFact]
    public void Reusable_modeless_anchor_reflows_in_a_small_host_and_light_dismisses_without_disabling_content()
    {
        using var host = new DialogHost(320, 240);
        var content = new MaterialButton { Content = "Anchored action", MinWidth = 150 };
        var session = host.Overlay.Show(content, new MaterialOverlayOptions
        {
            IsModal = false, Placement = MaterialOverlayPlacement.Anchor, Anchor = host.Entry,
            CloseOnLightDismiss = true, Margin = new Thickness(8)
        });
        host.Render();
        Assert.True(host.Entry.IsEffectivelyEnabled);
        var top = content.TranslatePoint(default, host.Window)!.Value;
        Assert.True(top.Y >= host.Entry.Bounds.Bottom);
        Assert.InRange(top.X, 8, 320 - content.Bounds.Width - 8);
        host.Window.Width = 200;
        host.Render();
        top = content.TranslatePoint(default, host.Window)!.Value;
        Assert.InRange(top.X, 8, 200 - content.Bounds.Width - 8);
        host.Window.MouseDown(new Point(190, 230), MouseButton.Left);
        host.Window.MouseUp(new Point(190, 230), MouseButton.Left);
        host.Render();
        Assert.True(session.Completion.IsCompleted);
        Assert.Equal(MaterialOverlayCloseReason.LightDismiss, session.Completion.Result.Reason);
    }

    [AvaloniaFact]
    public void Modal_contains_forward_reverse_and_programmatic_focus_and_detach_finishes_pending_work()
    {
        using var host = new DialogHost();
        var first = new MaterialButton { Content = "One" };
        var last = new MaterialButton { Content = "Two" };
        var session = host.Overlay.Show(new StackPanel { Children = { first, last } });
        host.Render();
        Assert.True(first.IsFocused);
        host.Key(PhysicalKey.Tab);
        Assert.True(last.IsFocused);
        host.Key(PhysicalKey.Tab);
        Assert.True(first.IsFocused);
        host.Key(PhysicalKey.Tab, RawInputModifiers.Shift);
        Assert.True(last.IsFocused);
        Assert.False(host.Entry.Focus());
        var outside = new MaterialButton { Content = "Sibling outside host" };
        host.Window.Content = null;
        host.Render();
        // Reparenting is a teardown, not a presentation that can stay pending forever.
        Assert.True(session.Completion.IsCompleted);
        Assert.Equal(MaterialOverlayCloseReason.HostDetached, session.Completion.Result.Reason);
        host.Window.Content = new Grid { Children = { host.Overlay, outside } };
        host.Render();
        var empty = host.Overlay.Show(new TextBlock { Text = "Focusable modal fallback" });
        host.Render();
        outside.Focus();
        Assert.False(outside.IsFocused);
        Assert.True(host.Overlay.RequestBack());
        Assert.True(empty.Completion.IsCompleted);
    }

    [AvaloniaFact]
    public void Nested_modal_cancels_only_the_top_and_back_policy_and_validation_are_respected()
    {
        using var host = new DialogHost();
        host.Entry.Focus();
        var first = new MaterialButton { Content = "First" };
        var outer = host.Overlay.Show(first);
        host.Render();
        var second = new MaterialButton { Content = "Second" };
        var inner = host.Overlay.Show(second, new MaterialOverlayOptions { CloseOnBack = false });
        host.Render();
        Assert.False(first.IsEffectivelyEnabled);
        Assert.False(outer.Close("too early"));
        Assert.False(host.Overlay.RequestBack());
        inner.Closing += RejectEscape;
        host.Key(PhysicalKey.Escape);
        Assert.True(inner.IsOpen);
        inner.Closing -= RejectEscape;
        host.Key(PhysicalKey.Escape);
        Assert.Equal(MaterialOverlayCloseReason.Escape, inner.Completion.Result.Reason);
        Assert.True(first.IsFocused);
        Assert.True(host.Overlay.RequestBack());
        Assert.Equal(MaterialOverlayCloseReason.Back, outer.Completion.Result.Reason);
        Assert.True(host.Entry.IsFocused);
        static void RejectEscape(object? sender, MaterialOverlayClosingEventArgs args) => args.Cancel = true;
    }

    [AvaloniaFact]
    public void Host_opens_a_generic_modal_returns_a_value_and_restores_the_entry_focus()
    {
        using var host = new DialogHost();
        host.Entry.Focus();
        var action = new MaterialButton { Content = "Accept" };
        var session = host.Overlay.Show(action);
        action.Click += (_, _) => session.Close("saved");
        host.Render();
        Assert.True(action.IsFocused);
        Assert.False(host.Entry.IsEffectivelyEnabled);
        host.Click(action);
        Assert.Equal(new MaterialOverlayResult(MaterialOverlayCloseReason.Confirmed, "saved"), session.Completion.Result);
        Assert.True(host.Entry.IsFocused);
        Assert.True(host.Entry.IsEffectivelyEnabled);
        Assert.Equal(0, host.Overlay.OpenCount);
    }
}

internal sealed class DialogHost : IDisposable
{
    public MaterialTheme Theme { get; } = new() { Motion = new Tokens.MaterialMotion { ReduceMotion = true } };
    public MaterialButton Entry { get; } = new() { Content = "Open dialog" };
    public MaterialOverlayHost Overlay { get; }
    public Window Window { get; }
    public DialogHost(double width = 800, double height = 600)
    {
        Application.Current!.Styles.Add(Theme);
        Overlay = new MaterialOverlayHost { Content = new StackPanel { Children = { Entry } } };
        Window = new Window { Width = width, Height = height, Content = Overlay };
        Window.Show();
        Render();
    }
    public void Render() { using var frame = Window.CaptureRenderedFrame(); }
    public MaterialButton Button(Control root, string label) => root.GetVisualDescendants().OfType<MaterialButton>().Single(button => button.IsEffectivelyVisible && (button.Content is TextBlock text ? text.Text : button.Content?.ToString()) == label);
    public Point Center(Control control) => control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), Window)!.Value;
    public void Click(Control control) { Render(); Window.MouseDown(Center(control), MouseButton.Left); Window.MouseUp(Center(control), MouseButton.Left); Render(); }
    public void Key(PhysicalKey key, RawInputModifiers modifiers = RawInputModifiers.None) { Window.KeyPressQwerty(key, modifiers); Window.KeyReleaseQwerty(key, modifiers); Render(); }
    public void Dispose() { Window.Close(); Application.Current!.Styles.Remove(Theme); }
}
