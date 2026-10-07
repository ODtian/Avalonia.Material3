using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Material3.Tests;

public class AppChromeMatrixScenarioTests
{
    [AvaloniaTheory]
    [InlineData(MaterialTopAppBarVariant.Small, 22, 28, 0)]
    [InlineData(MaterialTopAppBarVariant.CenterAligned, 22, 28, 0)]
    [InlineData(MaterialTopAppBarVariant.Medium, 24, 32, 0)]
    [InlineData(MaterialTopAppBarVariant.Large, 28, 36, 0)]
    [InlineData(MaterialTopAppBarVariant.MediumFlexible, 28, 36, 0)]
    [InlineData(MaterialTopAppBarVariant.LargeFlexible, 36, 44, 0)]
    public void Theme_scale_updates_full_title_metrics_and_pinned_surface_roles(MaterialTopAppBarVariant variant, double font, double lineHeight, double tracking)
    {
        var bar = new MaterialTopAppBar { Title = "Collection 收藏", Variant = variant };
        using var host = new ChromeHost(bar);
        Assert.Equal(Color.Parse("#FEF7FF"), ((ISolidColorBrush)bar.Background!).Color);
        host.Theme.Typography = new MaterialTypography { FontFamily = new FontFamily("Arial"), Scale = 2 };
        host.Layout();
        var title = bar.GetVisualDescendants().OfType<TextBlock>().Single(text => text.Text == bar.Title);
        Assert.Equal(font * 2, title.FontSize);
        Assert.Equal(lineHeight * 2, title.LineHeight);
        Assert.Equal(tracking * 2, title.LetterSpacing);
        Assert.Equal(new FontFamily("Arial"), title.FontFamily);
        host.Window.RequestedThemeVariant = ThemeVariant.Dark; host.Layout();
        Assert.Equal(Color.Parse("#141218"), ((ISolidColorBrush)bar.Background!).Color);
        bar.ApplyScrollDelta(20, 20); host.Layout();
        Assert.Equal(Color.Parse(variant is MaterialTopAppBarVariant.Small or MaterialTopAppBarVariant.CenterAligned ? "#211F26" : "#141218"), ((ISolidColorBrush)bar.CurrentBackground!).Color);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Modal_drawer_uses_logical_start_clamps_to_narrow_host_and_keeps_long_selected_labels(bool rtl)
    {
        var drawer = new MaterialNavigationDrawer { Mode = MaterialNavigationDrawerMode.Modal, IsOpen = false };
        var item = new MaterialNavigationItem { Content = "Library 资料 — very long bilingual collection label", Icon = "▤", Badge = "103", BadgeDescription = "103 unread" };
        drawer.Items.Add(item);
        var layout = new MaterialNavigationDrawerLayout { Drawer = drawer, Content = new Border { Background = Brushes.Transparent } };
        var overlays = new MaterialOverlayHost { Content = layout, FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        using var host = new ChromeHost(overlays, 200, 300);
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = true };
        host.Theme.Typography = new MaterialTypography { Scale = 2 };
        drawer.IsOpen = true; host.Layout();
        Assert.Equal(200, drawer.Bounds.Width);
        Assert.Equal(176, item.Bounds.Width);
        Assert.True(item.Bounds.Height > 56);
        Assert.Equal(0, new Rect(drawer.Bounds.Size).TransformToAABB(drawer.TransformToVisual(host.Window)!.Value).Left);
        Assert.Equal(Color.Parse("#F7F2FA"), ((ISolidColorBrush)drawer.Background!).Color);
        Assert.Equal(Color.Parse("#1D192B"), ((ISolidColorBrush)item.Foreground!).Color);
        Assert.Equal("Library 资料 — very long bilingual collection label, 103 unread", ControlAutomationPeer.CreatePeerForElement(item)!.GetName());
        host.Window.RequestedThemeVariant = ThemeVariant.Dark; host.Layout();
        Assert.Equal(Color.Parse("#1D1B20"), ((ISolidColorBrush)drawer.Background!).Color);
        Assert.Equal(Color.Parse("#E8DEF8"), ((ISolidColorBrush)item.Foreground!).Color);
        host.Key(PhysicalKey.Escape);
        Assert.False(drawer.IsOpen);
        Assert.Equal(0, overlays.OpenCount);
    }

    [AvaloniaFact]
    public void Real_page_wheel_and_touch_pan_drive_bound_scroll_without_collapsing_pinned_bar()
    {
        var scroll = new ScrollViewer { Content = new Border { Height = 2000, Background = Brushes.Transparent } };
        var bar = new MaterialTopAppBar { Title = "Collection", Variant = MaterialTopAppBarVariant.Large, ScrollSource = scroll };
        var panel = new DockPanel(); DockPanel.SetDock(bar, Dock.Top); panel.Children.Add(bar); panel.Children.Add(scroll);
        using var host = new ChromeHost(panel);
        host.Window.MouseWheel(host.Center(scroll), new Vector(0, -4)); host.Layout();
        Assert.True(scroll.Offset.Y > 0);
        Assert.True(bar.IsScrolled);
        Assert.Equal(152, bar.Bounds.Height);
        bar.ScrollBehavior = MaterialAppBarScrollBehavior.EnterAlways; host.Layout();
        var start = host.Center(scroll);
        using (var contact = host.Window.TouchBegin(start))
        {
            host.Window.TouchMove(contact, start + new Vector(0, -100));
            host.Window.TouchEnd(contact, start + new Vector(0, -100));
        }
        host.Layout();
        Assert.Equal(64, bar.Bounds.Height);
    }

    [AvaloniaFact]
    public void Modal_tab_focus_light_dismiss_and_detached_layout_complete_without_confirmation()
    {
        var drawer = new MaterialNavigationDrawer { Mode = MaterialNavigationDrawerMode.Modal, IsOpen = false };
        var first = new MaterialNavigationItem { Content = "Home" };
        var disabled = new MaterialNavigationItem { Content = "Disabled", IsEnabled = false };
        var last = new MaterialNavigationItem { Content = "Library" };
        drawer.Items.Add(first); drawer.Items.Add(disabled); drawer.Items.Add(last);
        var opener = new MaterialButton { Content = "Page action" };
        var layout = new MaterialNavigationDrawerLayout { Drawer = drawer, Content = opener };
        var overlays = new MaterialOverlayHost { Content = layout };
        using var host = new ChromeHost(overlays);
        opener.Focus(NavigationMethod.Tab);
        drawer.IsOpen = true; host.Layout();
        var session = drawer.Session!;
        host.Key(PhysicalKey.ArrowDown);
        Assert.True(last.IsFocused);
        Assert.Same(first, drawer.SelectedItem);
        host.Key(PhysicalKey.Tab);
        Assert.False(opener.IsFocused);
        Assert.False(disabled.IsFocused);
        var outside = new Point(800, 350);
        host.Window.MouseDown(outside, MouseButton.Left); host.Window.MouseUp(outside, MouseButton.Left); host.Layout();
        Assert.Equal(MaterialOverlayCloseReason.LightDismiss, session.Completion.Result.Reason);
        Assert.True(opener.IsFocused);
        drawer.IsOpen = true; host.Layout();
        var detached = drawer.Session!;
        detached.Closing += (_, e) => e.Cancel = true;
        overlays.Content = new Border(); host.Layout();
        Assert.Equal(MaterialOverlayCloseReason.AnchorDetached, detached.Completion.Result.Reason);
        Assert.False(drawer.IsOpen);
        Assert.Equal(0, overlays.OpenCount);
    }

    [AvaloniaFact]
    public void Resize_cancels_close_drag_and_focus_selection_survive_presentation_switch()
    {
        var drawer = new MaterialNavigationDrawer { Mode = MaterialNavigationDrawerMode.Modal, IsOpen = false };
        var item = new MaterialNavigationItem { Content = "Library" };
        drawer.Items.Add(item);
        var layout = new MaterialNavigationDrawerLayout { Drawer = drawer, Content = new Border() };
        var overlays = new MaterialOverlayHost { Content = layout };
        using var host = new ChromeHost(overlays);
        drawer.IsOpen = true; host.Layout();
        var start = host.Center(item);
        using (var contact = host.Window.TouchBegin(start))
        {
            host.Window.TouchMove(contact, start + new Vector(-100, 0));
            Assert.NotEqual(0, drawer.DragOffset);
            drawer.DrawerWidth = 280; host.Layout();
            host.Window.TouchEnd(contact, start + new Vector(-220, 0));
        }
        host.Layout();
        Assert.True(drawer.IsOpen);
        Assert.Equal(0, drawer.DragOffset);
        item.Focus(NavigationMethod.Tab);
        drawer.Mode = MaterialNavigationDrawerMode.Standard; host.Layout();
        Assert.True(item.IsFocused);
        Assert.True(item.IsSelected);
        drawer.Mode = MaterialNavigationDrawerMode.Modal; host.Layout();
        Assert.True(item.IsFocused);
        Assert.True(item.IsSelected);
        var peer = ControlAutomationPeer.CreatePeerForElement(item)!;
        item.IsEnabled = false;
        Assert.ThrowsAny<Exception>(() => ((IInvokeProvider)peer).Invoke());
        Assert.True(drawer.IsOpen);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Standard_drawer_and_appbar_navigation_occupy_logical_start_without_double_mirroring(bool rtl)
    {
        var nav = new MaterialIconButton { Content = "☰" };
        var action = new MaterialIconButton { Content = "✓" };
        var bar = new MaterialTopAppBar { Title = "Collection", NavigationContent = nav, Actions = action };
        var drawer = new MaterialNavigationDrawer();
        drawer.Items.Add(new MaterialNavigationItem { Content = "Home" });
        var page = new Border { Child = bar };
        var layout = new MaterialNavigationDrawerLayout { Drawer = drawer, Content = page, FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        using var host = new ChromeHost(layout);
        Rect Bounds(Control control) => new Rect(control.Bounds.Size).TransformToAABB(control.TransformToVisual(host.Window)!.Value);
        Assert.Equal(rtl ? 640 : 0, Bounds(drawer).Left);
        if (rtl) Assert.True(Bounds(nav).Left > Bounds(action).Right);
        else Assert.True(Bounds(nav).Right < Bounds(action).Left);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Rendered_close_drag_moves_toward_physical_start_edge_and_cancel_restores_the_surface(bool rtl)
    {
        var drawer = new MaterialNavigationDrawer { Mode = MaterialNavigationDrawerMode.Modal, IsOpen = false };
        var item = new MaterialNavigationItem { Content = "Home" };
        drawer.Items.Add(item);
        var overlays = new MaterialOverlayHost { Content = new MaterialNavigationDrawerLayout { Drawer = drawer, Content = new Border() }, FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        using var host = new ChromeHost(overlays);
        drawer.IsOpen = true; host.Layout();
        Rect Bounds() => new Rect(item.Bounds.Size).TransformToAABB(item.TransformToVisual(host.Window)!.Value);
        var initial = Bounds().Left;
        var start = host.Center(item);
        using (var contact = host.Window.TouchBegin(start))
        {
            host.Window.TouchMove(contact, start + new Vector(rtl ? 100 : -100, 0));
            host.Layout();
            Assert.Equal(initial + (rtl ? 100 : -100), Bounds().Left);
            drawer.IsGestureEnabled = false;
            host.Window.TouchEnd(contact, start);
        }
        host.Layout();
        Assert.Equal(initial, Bounds().Left);
        Assert.True(drawer.IsOpen);
    }
}
