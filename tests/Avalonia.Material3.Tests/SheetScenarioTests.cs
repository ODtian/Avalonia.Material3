using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.VisualTree;
using Avalonia.Input;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Automation;
using Xunit;

namespace Avalonia.Material3.Tests;

public class SheetScenarioTests
{
    [AvaloniaFact]
    public void Ordinary_padding_and_content_alignment_are_consumable_without_replacing_the_template()
    {
        using var host = new SheetTestHost();
        var content = new Border();
        var sheet = new MaterialBottomSheet { ExpandedExtent = 600, Content = content, Padding = new Thickness(20) };
        sheet.Show(host.Overlay); host.Render();
        Assert.Equal(568, content.Bounds.Width);
        Assert.Equal(new Thickness(20), sheet.Padding);
    }

    [AvaloniaFact]
    public void Nondismissible_standard_side_does_not_invent_a_partial_drag_boundary()
    {
        using var host = new SheetTestHost();
        var sheet = new MaterialSideSheet { AllowDismiss = false };
        host.Overlay.Content = new MaterialSheetHost { Content = new Border(), Sheet = sheet };
        sheet.Expand(); host.Render();
        var handle = sheet.GetVisualDescendants().OfType<MaterialSheetDragHandle>().Single();
        var start = host.Center(handle);
        host.Window.MouseDown(start, MouseButton.Left); host.Window.MouseMove(start + new Vector(150, 0)); host.Render();
        Assert.Equal(256, sheet.VisibleExtent);
        host.Window.MouseUp(start + new Vector(150, 0), MouseButton.Left); host.Render();
        Assert.Equal(MaterialSheetState.Expanded, sheet.State);
    }

    [AvaloniaFact]
    public void Host_replacement_template_uses_the_public_layout_adapter_and_preserves_live_reservation()
    {
        using var host = new SheetTestHost();
        var main = new Border();
        var sheet = new MaterialSideSheet();
        var layout = new MaterialSheetHost { Content = main, Sheet = sheet };
        layout.Template = new Avalonia.Controls.Templates.FuncControlTemplate<MaterialSheetHost>((owner, _) =>
        {
            var content = new Avalonia.Controls.Presenters.ContentPresenter { HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Stretch, VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Stretch };
            content.Bind(Avalonia.Controls.Presenters.ContentPresenter.ContentProperty, new Avalonia.Data.Binding(nameof(owner.Content)) { Source = owner });
            var surface = new Avalonia.Controls.Presenters.ContentPresenter { HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Stretch, VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Stretch };
            surface.Bind(Avalonia.Controls.Presenters.ContentPresenter.ContentProperty, new Avalonia.Data.Binding(nameof(owner.Sheet)) { Source = owner });
            return new MaterialSheetHostPanel { Children = { content, surface } };
        });
        host.Overlay.Content = layout; sheet.Expand(); host.Render();
        Assert.Equal(744, main.Bounds.Width);
        layout.AvailableSize = new Size(600, 400); host.Render();
        Assert.Equal(344, main.Bounds.Width); Assert.Equal(400, sheet.Bounds.Height);
        sheet.Dismiss(); host.Render(); Assert.Equal(600, main.Bounds.Width);
    }

    [AvaloniaFact]
    public void Live_persistent_reservation_and_available_space_reflow_the_real_content_rectangle()
    {
        using var host = new SheetTestHost();
        var main = new Border();
        var sheet = new MaterialBottomSheet { ExpandedExtent = 600 };
        var layout = new MaterialSheetHost { Content = main, Sheet = sheet };
        host.Overlay.Content = layout; host.Render(); sheet.Expand(); host.Render();
        Assert.Equal(744, main.Bounds.Height);
        layout.ReserveVisibleExtent = true; host.Render();
        Assert.Equal(200, main.Bounds.Height);
        layout.AvailableSize = new Size(320, 400); host.Render();
        Assert.Equal(320, main.Bounds.Width); Assert.Equal(0, main.Bounds.Height);
        Assert.Equal(400, sheet.VisibleExtent);
    }

    [AvaloniaFact]
    public void Open_side_follows_runtime_RTL_but_future_edge_and_detached_configuration_wait_for_reopen()
    {
        using var host = new SheetTestHost();
        var sheet = new MaterialSideSheet();
        sheet.Show(host.Overlay); host.Render();
        sheet.Edge = MaterialSheetEdge.Start; sheet.IsDetached = true; host.Render();
        Assert.Equal(new CornerRadius(16, 0, 0, 16), sheet.CornerRadius);
        host.Overlay.FlowDirection = Avalonia.Media.FlowDirection.RightToLeft; host.Render();
        Assert.Equal(0, new Rect(sheet.Bounds.Size).TransformToAABB(sheet.TransformToVisual(host.Window)!.Value).Left);
        Assert.Equal(new CornerRadius(0, 16, 16, 0), sheet.CornerRadius);
        sheet.Dismiss();
        var session = sheet.Show(host.Overlay); host.Render();
        Assert.Equal(MaterialOverlayPlacement.Start, session.Options.Placement);
        Assert.Equal(new Thickness(16), session.Options.Margin);
        Assert.Equal(new CornerRadius(16), sheet.CornerRadius);
    }

    [AvaloniaFact]
    public void Normal_motion_changes_real_geometry_and_resize_cancels_a_stale_target()
    {
        using var host = new SheetTestHost();
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = false, Springs = host.Theme.Motion.Springs with { DefaultSpatial = new Tokens.MaterialSpring(1, 1) } };
        var sheet = new MaterialBottomSheet { ExpandedExtent = 600 };
        sheet.Show(host.Overlay); host.Render();
        var changes = 0; sheet.StateChanged += (_, _) => changes++;
        sheet.Expand(); Assert.True(sheet.IsSettling);
        Thread.Sleep(80); host.Render();
        Assert.InRange(sheet.VisibleExtent, 400.001, 599.999);
        host.Window.Height = 400; host.Render();
        Assert.False(sheet.IsSettling); Assert.Equal(400, sheet.VisibleExtent);
        Assert.Equal(0, sheet.Offset); Assert.Equal(1, changes);
    }

    [AvaloniaFact]
    public void Gesture_disabled_body_still_scrolls_natively_instead_of_expanding()
    {
        using var host = new SheetTestHost();
        var sheet = new MaterialBottomSheet { ExpandedExtent = 600, IsDraggable = false, Content = string.Join("\n", Enumerable.Repeat("Body", 100)) };
        sheet.Show(host.Overlay); host.Render();
        var scroll = sheet.GetVisualDescendants().OfType<ScrollViewer>().Single(viewer => viewer.Content is Avalonia.Controls.Presenters.ContentPresenter);
        scroll.IsScrollInertiaEnabled = false;
        var start = host.Center(scroll);
        using (var touch = host.Window.TouchBegin(start))
        {
            for (var i = 1; i <= 5; i++) { host.Window.TouchMove(touch, start - new Vector(0, i * 24)); host.Render(); }
            host.Window.TouchEnd(touch, start - new Vector(0, 120));
        }
        host.Render();
        Assert.True(scroll.Offset.Y > 0); Assert.Equal(400, sheet.VisibleExtent);
        Assert.False(sheet.IsDragging);
    }

    [AvaloniaFact]
    public void Replacement_template_and_host_slots_keep_editing_gestures_and_session_results()
    {
        using var host = new SheetTestHost();
        var sheet = new MaterialBottomSheet { ExpandedExtent = 600 };
        var editor = new MaterialTextField { Label = "Host template editor" };
        sheet.Template = new Avalonia.Controls.Templates.FuncControlTemplate<MaterialBottomSheet>((owner, scope) =>
        {
            var scroll = new ScrollViewer { Content = editor };
            scope.Register("PART_BodyScroll", scroll);
            var handle = new MaterialSheetDragHandle { Sheet = owner };
            return new StackPanel { Children = { handle, scroll } };
        });
        var session = sheet.Show(host.Overlay); host.Render();
        editor.Focus(); host.Window.KeyTextInput("host draft");
        Assert.Equal("host draft", editor.Text);
        var handle = sheet.GetVisualDescendants().OfType<MaterialSheetDragHandle>().Single();
        host.Click(handle); Assert.Equal(MaterialSheetState.Expanded, sheet.State);
        Assert.True(session.Close("Host explicit value"));
        Assert.Equal("Host explicit value", session.Completion.Result.Value);
    }

    [AvaloniaFact]
    public void Zero_peek_disabled_partial_and_conflicting_limits_converge_without_negative_geometry()
    {
        using var host = new SheetTestHost();
        var sheet = new MaterialBottomSheet { PeekExtent = 0, MinimumExtent = 0, ExpandedExtent = 0, IsPartialEnabled = false };
        var session = sheet.Show(host.Overlay); host.Render();
        Assert.Equal(MaterialSheetState.Expanded, sheet.State);
        Assert.Equal(0, sheet.VisibleExtent); Assert.Equal(800, sheet.Offset);
        Assert.False(sheet.Collapse());
        Assert.True(host.Overlay.RequestBack()); Assert.True(session.Completion.IsCompleted);
        sheet.MinimumExtent = 120; sheet.MaximumExtent = 80; sheet.ExpandedExtent = 200;
        sheet.Show(host.Overlay); host.Render();
        Assert.Equal(120, sheet.VisibleExtent);
        Assert.Throws<ArgumentException>(() => sheet.ExpandedExtent = double.PositiveInfinity);
        Assert.Throws<ArgumentException>(() => sheet.PeekExtent = -1);
    }

    [AvaloniaFact]
    public void Detached_standard_side_reserves_both_margins_without_claiming_overlay_layout()
    {
        using var host = new SheetTestHost();
        host.Overlay.Content = null; host.Render();
        var sheet = new MaterialSideSheet { IsDetached = true };
        host.Overlay.Content = new MaterialSheetHost { Content = host.Entry, Sheet = sheet };
        sheet.Expand(); host.Render();
        var bounds = new Rect(sheet.Bounds.Size).TransformToAABB(sheet.TransformToVisual(host.Window)!.Value);
        Assert.Equal(new Rect(728, 16, 256, 768), bounds);
        Assert.Equal(712, host.Entry.Bounds.Width);
        Assert.Equal(new CornerRadius(16), sheet.CornerRadius);
    }

    [AvaloniaFact]
    public void Coincident_partial_and_expanded_promote_to_expanded_when_content_grows()
    {
        using var host = new SheetTestHost();
        var sheet = new MaterialBottomSheet { ExpandedExtent = 300 };
        sheet.Show(host.Overlay); host.Render();
        Assert.Equal(300, sheet.VisibleExtent);
        sheet.ExpandedExtent = 600; host.Render();
        Assert.Equal(MaterialSheetState.Expanded, sheet.State);
        Assert.Equal(200, sheet.Offset);
    }

    [AvaloniaFact]
    public void Child_only_downward_panning_at_expanded_does_not_collapse_on_release()
    {
        using var host = new SheetTestHost();
        var sheet = new MaterialBottomSheet { ExpandedExtent = 600, Content = string.Join("\n", Enumerable.Repeat("Long body", 100)) };
        sheet.Show(host.Overlay); sheet.Expand(); host.Render();
        var scroll = sheet.GetVisualDescendants().OfType<ScrollViewer>().Single(viewer => viewer.Content is Avalonia.Controls.Presenters.ContentPresenter);
        scroll.Offset = new Vector(0, 400); scroll.IsScrollInertiaEnabled = false; host.Render();
        var start = host.Center(scroll);
        using (var touch = host.Window.TouchBegin(start))
        {
            host.Window.TouchMove(touch, start + new Vector(0, 200)); host.Render();
            host.Window.TouchEnd(touch, start + new Vector(0, 200)); host.Render();
        }
        Assert.Equal(200, scroll.Offset.Y);
        Assert.Equal(MaterialSheetState.Expanded, sheet.State);
        Assert.Equal(600, sheet.VisibleExtent);
    }

    [AvaloniaFact]
    public void Capture_steal_disabling_and_host_teardown_cancel_without_settling_or_confirmation()
    {
        using var host = new SheetTestHost();
        var sheet = new MaterialBottomSheet { ExpandedExtent = 600 };
        var session = sheet.Show(host.Overlay); host.Render();
        IPointer? pointer = null;
        sheet.AddHandler(InputElement.PointerPressedEvent, (_, e) => pointer = e.Pointer, Avalonia.Interactivity.RoutingStrategies.Bubble, true);
        var handle = sheet.GetVisualDescendants().OfType<MaterialSheetDragHandle>().Single();
        var start = host.Center(handle);
        host.Window.MouseDown(start, MouseButton.Left); host.Window.MouseMove(start - new Vector(0, 100)); host.Render();
        Assert.True(sheet.IsDragging);
        pointer!.Capture(host.Entry); host.Render();
        Assert.False(sheet.IsDragging); Assert.Equal(400, sheet.VisibleExtent);
        host.Window.MouseUp(start, MouseButton.Left);
        start = host.Center(handle);
        host.Window.MouseDown(start, MouseButton.Left); host.Window.MouseMove(start - new Vector(0, 100)); host.Render();
        sheet.IsDraggable = false; host.Render();
        Assert.False(sheet.IsDragging); Assert.Null(pointer.Captured);
        sheet.StateChanging += (_, e) => e.Cancel = true;
        host.Window.Content = null; host.Render();
        Assert.Equal(MaterialOverlayCloseReason.HostDetached, session.Completion.Result.Reason);
        Assert.Equal(MaterialSheetState.Hidden, sheet.State);
        Assert.Null(sheet.Session);
    }

    [AvaloniaFact]
    public void Covered_sessions_vetoed_dismissals_and_reopening_preserve_the_overlay_contract()
    {
        using var host = new SheetTestHost(); host.Entry.Focus();
        var sheet = new MaterialBottomSheet { ExpandedExtent = 600 };
        var session = sheet.Show(host.Overlay); host.Render();
        var nested = new MaterialDialog { Title = "Nested confirmation" }; nested.Show(host.Overlay); host.Render();
        Assert.False(sheet.Dismiss()); Assert.False(sheet.Expand());
        host.Key(Key.Escape); Assert.False(nested.IsOpen); Assert.True(session.IsOpen);
        EventHandler<MaterialSheetStateChangingEventArgs> veto = (_, e) => e.Cancel = e.State == MaterialSheetState.Hidden;
        sheet.StateChanging += veto;
        Assert.False(sheet.Dismiss()); Assert.True(session.IsOpen);
        sheet.StateChanging -= veto;
        for (var i = 0; i < 20; i++)
        {
            Assert.True(sheet.Dismiss()); Assert.False(session.Dismiss()); Assert.True(host.Entry.IsFocused);
            session = sheet.Show(host.Overlay); host.Render();
            Assert.Equal(MaterialSheetState.PartiallyExpanded, sheet.State);
        }
        Assert.True(sheet.Dismiss()); Assert.Equal(0, host.Overlay.OpenCount);
    }

    [AvaloniaTheory]
    [InlineData(false, 744)]
    [InlineData(true, 0)]
    public void Modal_side_uses_edge_corners_and_horizontal_drag_without_seizing_vertical_scroll(bool rtl, double left)
    {
        using var host = new SheetTestHost();
        host.Overlay.FlowDirection = rtl ? Avalonia.Media.FlowDirection.RightToLeft : Avalonia.Media.FlowDirection.LeftToRight;
        var sheet = new MaterialSideSheet { Title = "Modal side", Content = string.Join("\n", Enumerable.Repeat("Vertical information", 100)) };
        sheet.Show(host.Overlay); host.Render();
        Assert.Equal(256, sheet.VisibleExtent); Assert.Equal(1000, host.Entry.Bounds.Width);
        Assert.Equal(left, new Rect(sheet.Bounds.Size).TransformToAABB(sheet.TransformToVisual(host.Window)!.Value).Left);
        Assert.Equal(rtl ? new CornerRadius(0, 16, 16, 0) : new CornerRadius(16, 0, 0, 16), sheet.CornerRadius);
        var scroll = sheet.GetVisualDescendants().OfType<ScrollViewer>().Single(viewer => viewer.Content is Avalonia.Controls.Presenters.ContentPresenter);
        scroll.IsScrollInertiaEnabled = false;
        var start = host.Center(scroll);
        using (var touch = host.Window.TouchBegin(start))
        {
            for (var i = 1; i <= 5; i++) { host.Window.TouchMove(touch, start - new Vector(0, i * 32)); host.Render(); }
            host.Window.TouchEnd(touch, start - new Vector(0, 160));
        }
        host.Render(); Assert.True(scroll.Offset.Y > 0); Assert.Equal(256, sheet.VisibleExtent);
        var handle = sheet.GetVisualDescendants().OfType<MaterialSheetDragHandle>().Single();
        start = host.Center(handle);
        var end = start + new Vector(rtl ? -150 : 150, 0);
        host.Window.MouseDown(start, MouseButton.Left); host.Window.MouseMove(end); host.Render();
        Assert.Equal(106, sheet.VisibleExtent);
        host.Window.MouseUp(end, MouseButton.Left); host.Render(); Assert.Equal(0, host.Overlay.OpenCount);
    }

    [AvaloniaFact]
    public void Material_surface_tokens_slots_and_reduced_motion_update_an_already_open_sheet()
    {
        using var host = new SheetTestHost();
        var action = new MaterialButton { Content = "Apply host action" };
        var sheet = new MaterialBottomSheet { ExpandedExtent = 600, Title = "中文 / panel heading", Content = "Supporting information", Actions = action };
        sheet.Show(host.Overlay); host.Render();
        Assert.Equal(Avalonia.Media.Color.Parse("#F7F2FA"), ((Avalonia.Media.ISolidColorBrush)sheet.Background!).Color);
        Assert.Equal(new CornerRadius(28, 28, 0, 0), sheet.CornerRadius);
        Assert.Equal(16, sheet.FontSize);
        var invoked = 0; action.Click += (_, _) => invoked++; host.Click(action); Assert.Equal(1, invoked);
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = false };
        host.Render(); sheet.Expand();
        Assert.True(sheet.IsSettling);
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = true }; host.Render();
        Assert.False(sheet.IsSettling); Assert.Equal(600, sheet.VisibleExtent);
        host.Window.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
        host.Theme.Typography = host.Theme.Typography with { Scale = 2 }; host.Render();
        Assert.Equal(Avalonia.Media.Color.Parse("#1D1B20"), ((Avalonia.Media.ISolidColorBrush)sheet.Background!).Color);
        Assert.Equal(32, sheet.FontSize);
    }

    [AvaloniaFact]
    public void Keyboard_and_expandcollapse_automation_offer_reachable_actions_and_current_state()
    {
        using var host = new SheetTestHost();
        var sheet = new MaterialBottomSheet { ExpandedExtent = 600, Title = "Information panel", IsDraggable = false };
        sheet.Show(host.Overlay); host.Render();
        var handle = sheet.GetVisualDescendants().OfType<MaterialSheetDragHandle>().Single();
        var peer = ControlAutomationPeer.CreatePeerForElement(handle);
        Assert.Equal("Resize information panel", peer.GetName());
        var provider = Assert.IsAssignableFrom<IExpandCollapseProvider>(peer);
        Assert.Equal(ExpandCollapseState.PartiallyExpanded, provider.ExpandCollapseState);
        handle.Focus(); host.Key(Key.Up);
        Assert.Equal(MaterialSheetState.Expanded, sheet.State);
        Assert.Equal("expanded; modal", ControlAutomationPeer.CreatePeerForElement(sheet).GetItemStatus());
        provider.Collapse(); host.Render(); Assert.Equal(MaterialSheetState.PartiallyExpanded, sheet.State);
        provider.Expand(); host.Render(); Assert.Equal(MaterialSheetState.Expanded, sheet.State);
        host.Key(Key.Down);
        Assert.Equal(MaterialSheetState.PartiallyExpanded, sheet.State);
    }

    [AvaloniaFact]
    public void Child_button_editor_and_wheel_keep_their_native_input_and_do_not_drag_the_sheet()
    {
        using var host = new SheetTestHost();
        var button = new MaterialButton { Content = "Child action" };
        var editor = new MaterialTextField { Label = "Child editor", Text = "old" };
        var body = new StackPanel { Children = { button, editor, new TextBlock { Text = string.Join("\n", Enumerable.Repeat("Scrollable body", 100)) } } };
        var sheet = new MaterialBottomSheet { ExpandedExtent = 600, Content = body };
        sheet.Show(host.Overlay); host.Render();
        var count = 0; button.Click += (_, _) => count++;
        var point = host.Center(button);
        using (var touch = host.Window.TouchBegin(point)) host.Window.TouchEnd(touch, point);
        host.Render(); Assert.Equal(1, count); Assert.False(sheet.IsDragging);
        editor.Focus(); editor.SelectAll(); host.Window.KeyTextInput("edited"); host.Render();
        Assert.Equal("edited", editor.Text); Assert.Equal(MaterialSheetState.PartiallyExpanded, sheet.State);
        var scroll = sheet.GetVisualDescendants().OfType<ScrollViewer>().OrderByDescending(viewer => viewer.Extent.Height).First();
        host.Window.MouseWheel(host.Center(scroll), new Vector(0, -3)); host.Render();
        Assert.True(scroll.Offset.Y > 0); Assert.Equal(MaterialSheetState.PartiallyExpanded, sheet.State);
    }

    [AvaloniaFact]
    public void Touch_body_expands_before_scrolling_then_consumes_child_offset_before_collapsing()
    {
        using var host = new SheetTestHost();
        var body = new StackPanel();
        for (var i = 0; i < 100; i++) body.Children.Add(new TextBlock { Text = $"Information row {i}", Height = 24 });
        var sheet = new MaterialBottomSheet { ExpandedExtent = 600, Content = body };
        sheet.Show(host.Overlay); host.Render();
        var scroll = sheet.GetVisualDescendants().OfType<ScrollViewer>().Single(viewer => viewer.Content is Avalonia.Controls.Presenters.ContentPresenter);
        scroll.IsScrollInertiaEnabled = false;
        var start = host.Center(scroll);
        using var touch = host.Window.TouchBegin(start);
        host.Window.TouchMove(touch, start - new Vector(0, 100)); host.Render();
        Assert.Equal(500, sheet.VisibleExtent); Assert.Equal(0, scroll.Offset.Y);
        host.Window.TouchMove(touch, start - new Vector(0, 260)); host.Render();
        Assert.Equal(600, sheet.VisibleExtent); Assert.Equal(60, scroll.Offset.Y);
        host.Window.TouchMove(touch, start - new Vector(0, 230)); host.Render();
        Assert.Equal(600, sheet.VisibleExtent); Assert.Equal(30, scroll.Offset.Y);
        host.Window.TouchMove(touch, start - new Vector(0, 150)); host.Render();
        Assert.Equal(550, sheet.VisibleExtent); Assert.Equal(0, scroll.Offset.Y);
        host.Window.TouchEnd(touch, start - new Vector(0, 150)); host.Render();
        Assert.Equal(MaterialSheetState.Expanded, sheet.State);
    }

    [AvaloniaFact]
    public void Handle_pointer_drag_settles_and_capture_loss_restores_the_stable_detent()
    {
        using var host = new SheetTestHost();
        var sheet = new MaterialBottomSheet { ExpandedExtent = 600 };
        sheet.Show(host.Overlay); host.Render();
        var handle = sheet.GetVisualDescendants().OfType<MaterialSheetDragHandle>().Single();
        var start = host.Center(handle);
        host.Window.MouseDown(start, MouseButton.Left); host.Window.MouseMove(start - new Vector(0, 150)); host.Render();
        Assert.True(sheet.IsDragging); Assert.Equal(550, sheet.VisibleExtent);
        host.Window.MouseUp(start - new Vector(0, 150), MouseButton.Left); host.Render();
        Assert.Equal(MaterialSheetState.Expanded, sheet.State);
        start = host.Center(handle);
        host.Window.MouseDown(start, MouseButton.Left); host.Window.MouseMove(start + new Vector(0, 100)); host.Render();
        sheet.CancelDrag(); host.Render();
        Assert.False(sheet.IsDragging); Assert.Equal(600, sheet.VisibleExtent);
        Assert.Equal(MaterialSheetState.Expanded, sheet.State);
    }

    [AvaloniaTheory]
    [InlineData(false, 744)]
    [InlineData(true, 0)]
    public void Standard_side_is_coplanar_and_logical_end_changes_in_RTL(bool rtl, double sheetX)
    {
        using var host = new SheetTestHost();
        host.Overlay.Content = null; host.Render();
        var sheet = new MaterialSideSheet { Content = "Side information" };
        var layout = new MaterialSheetHost { Content = host.Entry, Sheet = sheet, FlowDirection = rtl ? Avalonia.Media.FlowDirection.RightToLeft : Avalonia.Media.FlowDirection.LeftToRight };
        host.Overlay.Content = layout; sheet.Expand(); host.Render();
        Assert.Equal(256, sheet.VisibleExtent);
        Assert.Equal(744, host.Entry.Bounds.Width);
        Assert.Equal(sheetX, new Rect(sheet.Bounds.Size).TransformToAABB(sheet.TransformToVisual(host.Window)!.Value).Left);
        Assert.False(sheet.Collapse());
        Assert.True(host.Entry.IsEffectivelyEnabled);
    }

    [AvaloniaFact]
    public void Declared_available_space_and_minimum_are_bounded_and_resize_reconciles_detents()
    {
        using var host = new SheetTestHost();
        var sheet = new MaterialBottomSheet { ExpandedExtent = 600, MinimumExtent = 56, AvailableSize = new Size(320, 400) };
        sheet.Show(host.Overlay); host.Render(); sheet.Expand(); host.Render();
        Assert.Equal(400, sheet.VisibleExtent); Assert.Equal(0, sheet.Offset);
        sheet.AvailableSize = new Size(320, 20); host.Render();
        Assert.Equal(20, sheet.VisibleExtent); Assert.Equal(0, sheet.Offset);
        Assert.Throws<ArgumentException>(() => sheet.MinimumExtent = double.NaN);
        Assert.Throws<ArgumentException>(() => sheet.AvailableSize = new Size(double.PositiveInfinity, 400));
    }

    [AvaloniaTheory]
    [InlineData(600, 400, 200, 800)]
    [InlineData(300, 500, 500, 800)]
    [InlineData(600, 200, 0, 400)]
    public void Modal_detents_use_the_pinned_deterministic_partial_anchor_and_back_returns_focus(double contentHeight, double partialOffset, double expandedOffset, int hostHeight)
    {
        using var host = new SheetTestHost(height: hostHeight); host.Entry.Focus();
        var sheet = new MaterialBottomSheet { ExpandedExtent = contentHeight, Content = "Modal information" };
        var session = sheet.Show(host.Overlay); host.Render();
        Assert.Equal(partialOffset, sheet.Offset);
        Assert.False(host.Entry.IsEffectivelyEnabled);
        Assert.True(sheet.Expand()); host.Render();
        Assert.Equal(expandedOffset, sheet.Offset);
        Assert.False(host.Overlay.RequestBack()); host.Render();
        Assert.Equal(MaterialSheetState.PartiallyExpanded, sheet.State);
        Assert.True(session.IsOpen);
        Assert.True(host.Overlay.RequestBack()); host.Render();
        Assert.Equal(MaterialOverlayCloseReason.Back, session.Completion.Result.Reason);
        Assert.True(host.Entry.IsFocused);
        Assert.Equal(MaterialSheetState.Hidden, sheet.State);
    }

    [AvaloniaFact]
    public void Standard_bottom_reserves_peek_space_and_expands_within_the_declared_host()
    {
        using var host = new SheetTestHost();
        host.Overlay.Content = null; host.Render();
        var sheet = new MaterialBottomSheet { ExpandedExtent = 600, Content = "Standard information" };
        var layout = new MaterialSheetHost { Content = host.Entry, Sheet = sheet };
        host.Overlay.Content = layout; host.Render();
        Assert.Equal(56, sheet.VisibleExtent);
        Assert.Equal(744, sheet.Offset);
        Assert.Equal(744, host.Entry.Bounds.Height);
        Assert.True(sheet.Expand()); host.Render();
        Assert.Equal(600, sheet.VisibleExtent);
        Assert.Equal(200, sheet.Offset);
        Assert.Equal(744, host.Entry.Bounds.Height);
        Assert.True(host.Entry.IsEffectivelyEnabled);
    }
}

internal sealed class SheetTestHost : IDisposable
{
    public MaterialTheme Theme { get; } = new();
    public MaterialButton Entry { get; } = new() { Content = "Open information", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch };
    public MaterialOverlayHost Overlay { get; } = new();
    public Window Window { get; }
    public SheetTestHost(double width = 1000, double height = 800)
    {
        Application.Current!.Styles.Add(Theme);
        Theme.Motion = Theme.Motion with { ReduceMotion = true };
        Overlay.Content = Entry;
        Window = new Window { Width = width, Height = height, Content = Overlay };
        Window.Show(); Render();
    }
    public void Render()
    {
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Window.UpdateLayout();
        Window.CaptureRenderedFrame()?.Dispose();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Window.UpdateLayout();
    }
    public Point Center(Control control) => control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), Window)!.Value;
    public void Click(Control control)
    {
        control.BringIntoView(); Render();
        var p = Center(control);
        Window.MouseDown(p, Avalonia.Input.MouseButton.Left);
        Window.MouseUp(p, Avalonia.Input.MouseButton.Left);
        Render();
    }
    public void Key(Key key) { Window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null); Window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null); Render(); }
    public void Dispose() { Window.Close(); Application.Current!.Styles.Remove(Theme); }
}
