using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Material3.Tokens;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Material3.Tests;

public class AppChromeScenarioTests
{
    [AvaloniaFact]
    public void Small_app_bar_exposes_title_and_real_navigation_and_action_input()
    {
        var navigation = new MaterialIconButton { Content = "←" };
        AutomationProperties.SetName(navigation, "Back to collection");
        var action = new MaterialIconButton { Content = "✓" };
        AutomationProperties.SetName(action, "Save page");
        var bar = new MaterialTopAppBar { Title = "Collection 收藏", NavigationContent = navigation, Actions = action };
        using var host = new ChromeHost(bar);
        var result = "";
        navigation.Click += (_, _) => result = "returned";
        action.Click += (_, _) => result = "saved";
        host.Click(action);
        Assert.Equal("saved", result);
        navigation.Focus(NavigationMethod.Tab);
        host.Key(PhysicalKey.Enter);
        Assert.Equal("returned", result);
        Assert.Equal(64, bar.Bounds.Height);
        Assert.Contains(bar.GetVisualDescendants().OfType<TextBlock>(), text => text.Text == "Collection 收藏");
    }

    [AvaloniaTheory]
    [InlineData(MaterialTopAppBarVariant.Small, null, 64, 22)]
    [InlineData(MaterialTopAppBarVariant.CenterAligned, null, 64, 22)]
    [InlineData(MaterialTopAppBarVariant.Medium, null, 112, 24)]
    [InlineData(MaterialTopAppBarVariant.Large, null, 152, 28)]
    [InlineData(MaterialTopAppBarVariant.MediumFlexible, null, 112, 28)]
    [InlineData(MaterialTopAppBarVariant.MediumFlexible, "Updated today", 136, 28)]
    [InlineData(MaterialTopAppBarVariant.LargeFlexible, null, 120, 36)]
    [InlineData(MaterialTopAppBarVariant.LargeFlexible, "Updated today", 152, 36)]
    public void Pinned_top_forms_use_independent_reference_heights_and_type_roles(MaterialTopAppBarVariant variant, string? subtitle, double height, double font)
    {
        var bar = new MaterialTopAppBar { Title = "Collection", Subtitle = subtitle, Variant = variant };
        using var host = new ChromeHost(bar);
        Assert.Equal(height, bar.Bounds.Height);
        Assert.Equal(font, bar.GetVisualDescendants().OfType<TextBlock>().Single(text => text.Text == "Collection" && text.IsEffectivelyVisible && text.GetVisualAncestors().All(parent => parent.Opacity > 0)).FontSize);
    }

    [AvaloniaFact]
    public void Bound_page_scroll_collapses_only_its_bar_and_replacement_detaches_old_scroll()
    {
        var scroll = new ScrollViewer { Content = new Border { Height = 2000 } };
        var bar = new MaterialTopAppBar { Title = "Collection", Variant = MaterialTopAppBarVariant.Medium, ScrollBehavior = MaterialAppBarScrollBehavior.ExitUntilCollapsed, ScrollSource = scroll };
        var layout = new DockPanel();
        DockPanel.SetDock(bar, Dock.Top);
        layout.Children.Add(bar);
        layout.Children.Add(scroll);
        using var host = new ChromeHost(layout);
        scroll.Offset = new Vector(0, 200);
        host.Layout();
        Assert.True(bar.Bounds.Height == 64, $"height={bar.Bounds.Height}, fraction={bar.CollapsedFraction}, offset={scroll.Offset}, extent={scroll.Extent}, viewport={scroll.Viewport}, source={bar.ScrollSource == scroll}");
        Assert.Equal(1, bar.CollapsedFraction);
        Assert.Equal(22, bar.GetVisualDescendants().OfType<TextBlock>().Single(text => text.Text == "Collection" && text.IsEffectivelyVisible && text.GetVisualAncestors().All(parent => parent.Opacity > 0)).FontSize);
        scroll.Offset = new Vector(0, 20);
        host.Layout();
        Assert.Equal(92, bar.Bounds.Height);
        var replacement = new ScrollViewer { Content = new Border { Height = 2000 } };
        bar.ScrollSource = replacement;
        scroll.Offset = new Vector(0, 500);
        host.Layout();
        Assert.Equal(112, bar.Bounds.Height);
        bar.ScrollBehavior = MaterialAppBarScrollBehavior.Pinned;
        bar.ApplyScrollDelta(100, 100);
        host.Layout();
        Assert.Equal(112, bar.Bounds.Height);
        Assert.True(bar.IsScrolled);
    }

    [AvaloniaFact]
    public void Bottom_bar_has_actual_actions_and_fab_without_becoming_a_navigation_selector()
    {
        var action = new MaterialIconButton { Content = "✓" };
        var fab = new MaterialFab { Content = "+" };
        var bar = new MaterialBottomAppBar { Actions = action, FloatingAction = fab };
        using var host = new ChromeHost(bar);
        var saved = 0;
        action.Click += (_, _) => saved++;
        fab.Click += (_, _) => saved += 10;
        host.Click(action);
        fab.Focus(NavigationMethod.Tab);
        host.Key(PhysicalKey.Space);
        Assert.Equal(11, saved);
        Assert.Equal(80, bar.Bounds.Height);
        Assert.Equal(AutomationControlType.ToolBar, ControlAutomationPeer.CreatePeerForElement(bar)!.GetAutomationControlType());
    }

    [AvaloniaFact]
    public void Standard_drawer_reserves_real_layout_space_and_focus_highlight_is_not_product_navigation()
    {
        var drawer = new MaterialNavigationDrawer { Title = "Collections" };
        var first = new MaterialNavigationItem { Content = "Home", Icon = "⌂", PageContent = "Home page" };
        var second = new MaterialNavigationItem { Content = "Library", Icon = "▤", PageContent = "Library page" };
        drawer.Items.Add(first);
        drawer.Items.Add(second);
        var page = new Border();
        var layout = new MaterialNavigationDrawerLayout { Drawer = drawer, Content = page };
        using var host = new ChromeHost(layout);
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = true };
        Assert.Equal(360, drawer.Bounds.Width);
        Assert.Equal(640, page.Bounds.Width);
        Assert.Equal(336, second.Bounds.Width);
        Assert.Equal(56, second.Bounds.Height);
        first.Focus(NavigationMethod.Tab);
        host.Key(PhysicalKey.ArrowDown);
        Assert.True(second.IsFocused);
        Assert.Same(first, drawer.SelectedItem);
        host.Key(PhysicalKey.Enter);
        Assert.Same(second, drawer.SelectedItem);
        Assert.Equal("Library page", drawer.SelectedContent);
        Assert.True(drawer.IsOpen);
        drawer.IsOpen = false;
        host.Layout();
        Assert.Equal(1000, page.Bounds.Width);
        Assert.DoesNotContain(second, host.Window.GetVisualDescendants());
    }

    [AvaloniaFact]
    public void Modal_drawer_closes_before_navigation_and_veto_preserves_page_selection_and_modality()
    {
        var drawer = new MaterialNavigationDrawer { Mode = MaterialNavigationDrawerMode.Modal, IsOpen = false };
        var first = new MaterialNavigationItem { Content = "Home" };
        var second = new MaterialNavigationItem { Content = "Library" };
        drawer.Items.Add(first); drawer.Items.Add(second);
        var opener = new MaterialButton { Content = "Open navigation" };
        var layout = new MaterialNavigationDrawerLayout { Drawer = drawer, Content = opener };
        var overlays = new MaterialOverlayHost { Content = layout };
        using var host = new ChromeHost(overlays);
        opener.Focus(NavigationMethod.Tab);
        drawer.IsOpen = true;
        host.Layout();
        var session = drawer.Session!;
        Assert.Equal(1, overlays.OpenCount);
        Assert.True(first.IsFocused);
        Assert.False(opener.IsEffectivelyEnabled);
        var veto = true;
        session.Closing += (_, e) => e.Cancel = veto;
        host.Click(second);
        Assert.Same(first, drawer.SelectedItem);
        Assert.True(session.IsOpen);
        Assert.True(drawer.IsOpen);
        veto = false;
        var navigatedWithoutModal = false;
        second.Click += (_, _) => navigatedWithoutModal = overlays.OpenCount == 0;
        host.Click(second);
        Assert.True(navigatedWithoutModal);
        Assert.Same(second, drawer.SelectedItem);
        Assert.False(drawer.IsOpen);
        Assert.True(opener.IsEffectivelyEnabled);
        Assert.True(opener.IsFocused);
        Assert.True(session.Completion.IsCompletedSuccessfully);
    }

    [AvaloniaFact]
    public void Adaptive_drawer_keeps_selection_and_automation_actions_across_modal_and_permanent_layouts()
    {
        var drawer = new MaterialNavigationDrawer { IsStandardDismissible = false };
        var first = new MaterialNavigationItem { Content = "Home" };
        var second = new MaterialNavigationItem { Content = "Library", BadgeDescription = "3 unread" };
        drawer.Items.Add(first); drawer.Items.Add(second);
        drawer.SelectedIndex = 1;
        var page = new MaterialButton { Content = "Page action" };
        var pageSurface = new Border { Child = page };
        var layout = new MaterialNavigationDrawerLayout { Drawer = drawer, Content = pageSurface };
        var overlays = new MaterialOverlayHost { Content = layout };
        using var host = new ChromeHost(overlays);
        Assert.False(drawer.Close());
        Assert.Equal(640, pageSurface.Bounds.Width);
        var peer = ControlAutomationPeer.CreatePeerForElement(drawer)!;
        var expand = Assert.IsAssignableFrom<IExpandCollapseProvider>(peer);
        Assert.Equal(ExpandCollapseState.Expanded, expand.ExpandCollapseState);
        Assert.Equal("Navigation menu", peer.GetName());
        Assert.Equal(AutomationControlType.List, peer.GetAutomationControlType());
        Assert.Equal("Library, 3 unread", ControlAutomationPeer.CreatePeerForElement(second)!.GetName());
        drawer.Mode = MaterialNavigationDrawerMode.Modal;
        host.Layout();
        Assert.Equal(1000, pageSurface.Bounds.Width);
        Assert.Equal(1, overlays.OpenCount);
        Assert.Same(second, drawer.SelectedItem);
        var lower = drawer.Session!;
        var dialog = new MaterialDialog { Title = "Confirm", Content = "Confirm current page" };
        var upper = dialog.Show(overlays);
        host.Layout();
        Assert.True(overlays.RequestBack());
        Assert.Equal(MaterialOverlayCloseReason.Back, upper.Completion.Result.Reason);
        Assert.True(lower.IsOpen);
        Assert.True(overlays.RequestBack());
        Assert.Equal(MaterialOverlayCloseReason.Back, lower.Completion.Result.Reason);
        host.Layout();
        Assert.Equal(ExpandCollapseState.Collapsed, expand.ExpandCollapseState);
        expand.Expand();
        host.Layout();
        Assert.Equal(1, overlays.OpenCount);
        drawer.Mode = MaterialNavigationDrawerMode.Standard;
        host.Layout();
        Assert.Equal(0, overlays.OpenCount);
        Assert.True(drawer.IsOpen);
        Assert.Equal(640, pageSurface.Bounds.Width);
        Assert.Same(second, drawer.SelectedItem);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Horizontal_touch_swipe_dismisses_drawer_without_invoking_a_destination_in_either_direction(bool rtl)
    {
        var drawer = new MaterialNavigationDrawer { Mode = MaterialNavigationDrawerMode.Modal, IsOpen = false };
        var first = new MaterialNavigationItem { Content = "Home" };
        var second = new MaterialNavigationItem { Content = "Library" };
        drawer.Items.Add(first); drawer.Items.Add(second);
        var layout = new MaterialNavigationDrawerLayout { Drawer = drawer, Content = new MaterialButton { Content = "Page" } };
        var overlays = new MaterialOverlayHost { Content = layout, FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        using var host = new ChromeHost(overlays);
        drawer.IsOpen = true; host.Layout();
        var session = drawer.Session!;
        var invocations = 0;
        drawer.ItemInvoked += (_, _) => invocations++;
        var start = host.Center(second);
        var end = start + new Vector(rtl ? 220 : -220, 0);
        using (var contact = host.Window.TouchBegin(start))
        {
            host.Window.TouchMove(contact, end);
            host.Window.TouchEnd(contact, end);
        }
        host.Layout();
        Assert.False(session.IsOpen);
        Assert.Equal(0, invocations);
        Assert.Same(first, drawer.SelectedItem);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Host_opted_in_edge_swipe_opens_and_gesture_disable_cancels_a_captured_close(bool rtl)
    {
        var drawer = new MaterialNavigationDrawer { Mode = MaterialNavigationDrawerMode.Modal, IsOpen = false };
        drawer.Items.Add(new MaterialNavigationItem { Content = "Home" });
        var layout = new MaterialNavigationDrawerLayout { Drawer = drawer, IsEdgeSwipeEnabled = true, Content = new Border { Background = Brushes.Transparent } };
        var overlays = new MaterialOverlayHost { Content = layout, FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        using var host = new ChromeHost(overlays);
        var edgeStart = new Point(rtl ? 998 : 2, 200);
        var edgeEnd = edgeStart + new Vector(rtl ? -218 : 218, 0);
        using (var contact = host.Window.TouchBegin(edgeStart))
        {
            host.Window.TouchMove(contact, edgeEnd);
            host.Window.TouchEnd(contact, edgeEnd);
        }
        host.Layout();
        Assert.True(drawer.IsOpen);
        var start = new Point(rtl ? 800 : 200, 200);
        var direction = rtl ? 1 : -1;
        using (var contact = host.Window.TouchBegin(start))
        {
            host.Window.TouchMove(contact, start + new Vector(direction * 100, 0));
            Assert.Equal(direction * 100, drawer.DragOffset);
            drawer.IsGestureEnabled = false;
            host.Window.TouchEnd(contact, start + new Vector(direction * 220, 0));
        }
        host.Layout();
        Assert.True(drawer.IsOpen);
        Assert.Equal(0, drawer.DragOffset);
    }

    [AvaloniaFact]
    public void Host_title_and_action_templates_are_real_slots_and_center_title_does_not_collide_with_unequal_actions()
    {
        var title = new TextBlock { Text = "Host custom title", TextWrapping = TextWrapping.Wrap };
        var action = new MaterialIconButton { Content = "✓" };
        var nav = new MaterialIconButton { Content = "←" };
        var bar = new MaterialTopAppBar
        {
            Title = "Collection", TitleTemplate = new FuncDataTemplate<string>((_, _) => title),
            Variant = MaterialTopAppBarVariant.CenterAligned, NavigationContent = nav,
            Actions = "action", ActionsTemplate = new FuncDataTemplate<string>((_, _) => action)
        };
        using var host = new ChromeHost(bar, 320);
        var result = false; action.Click += (_, _) => result = true;
        host.Click(action);
        Assert.True(result);
        Assert.True(title.IsEffectivelyVisible);
        var titleStart = title.TranslatePoint(default, host.Window)!.Value.X;
        var titleEnd = titleStart + title.Bounds.Width;
        var navEnd = nav.TranslatePoint(default, host.Window)!.Value.X + nav.Bounds.Width;
        var actionStart = action.TranslatePoint(default, host.Window)!.Value.X;
        Assert.True(titleStart >= navEnd);
        Assert.True(titleEnd <= actionStart);
        Assert.Equal("Collection", ControlAutomationPeer.CreatePeerForElement(bar)!.GetName());
    }

    [AvaloniaFact]
    public void Detached_scroll_source_no_longer_drives_a_live_bar_and_repeated_drawer_open_close_converges()
    {
        var scroll = new ScrollViewer { Content = new Border { Height = 2000 } };
        var bar = new MaterialTopAppBar { Title = "Collection", Variant = MaterialTopAppBarVariant.Large, ScrollBehavior = MaterialAppBarScrollBehavior.EnterAlways, ScrollSource = scroll };
        var body = new DockPanel(); DockPanel.SetDock(bar, Dock.Top); body.Children.Add(bar); body.Children.Add(scroll);
        var drawer = new MaterialNavigationDrawer { Mode = MaterialNavigationDrawerMode.Modal, IsOpen = false };
        drawer.Items.Add(new MaterialNavigationItem { Content = "Home" });
        var layout = new MaterialNavigationDrawerLayout { Drawer = drawer, Content = body };
        var overlays = new MaterialOverlayHost { Content = layout };
        using var host = new ChromeHost(overlays);
        scroll.Offset = new Vector(0, 200); host.Layout();
        Assert.Equal(1, bar.CollapsedFraction);
        body.Children.Remove(scroll); host.Layout();
        Assert.Equal(0, bar.CollapsedFraction);
        scroll.Offset = default; host.Layout();
        Assert.Equal(152, bar.Bounds.Height);
        for (var i = 0; i < 50; i++)
        {
            drawer.IsOpen = true; host.Layout();
            var session = drawer.Session!;
            Assert.Equal(1, overlays.OpenCount);
            Assert.True(overlays.RequestBack());
            host.Layout();
            Assert.True(session.Completion.IsCompletedSuccessfully);
            Assert.Null(drawer.Session);
            Assert.Equal(0, overlays.OpenCount);
        }
    }

    [AvaloniaFact]
    public void Two_row_scrolled_color_interpolates_semantic_endpoints_and_updates_with_live_theme()
    {
        var bar = new MaterialTopAppBar { Title = "Collection", Variant = MaterialTopAppBarVariant.Large, ScrollBehavior = MaterialAppBarScrollBehavior.EnterAlways };
        using var host = new ChromeHost(bar);
        bar.ApplyScrollDelta(44, 44); host.Layout();
        Assert.Equal(.5, bar.CollapsedFraction);
        Assert.Equal(Color.Parse("#FAF4FC"), ((ISolidColorBrush)bar.CurrentBackground!).Color);
        host.Window.RequestedThemeVariant = ThemeVariant.Dark; host.Layout();
        Assert.Equal(Color.Parse("#18161C"), ((ISolidColorBrush)bar.CurrentBackground!).Color);
    }

    [AvaloniaFact]
    public void Accessible_destination_selection_closes_modal_before_committing_and_honors_close_veto()
    {
        var drawer = new MaterialNavigationDrawer { Mode = MaterialNavigationDrawerMode.Modal, IsOpen = false };
        var first = new MaterialNavigationItem { Content = "Home" };
        var last = new MaterialNavigationItem { Content = "Library" };
        drawer.Items.Add(first); drawer.Items.Add(last);
        var overlays = new MaterialOverlayHost { Content = new MaterialNavigationDrawerLayout { Drawer = drawer, Content = new Border() } };
        using var host = new ChromeHost(overlays);
        drawer.IsOpen = true; host.Layout();
        var veto = true;
        drawer.Session!.Closing += (_, e) => e.Cancel = veto;
        var provider = (ISelectionItemProvider)ControlAutomationPeer.CreatePeerForElement(last)!;
        Assert.Throws<InvalidOperationException>(() => provider.Select());
        Assert.Same(first, drawer.SelectedItem);
        var invocations = 0; drawer.ItemInvoked += (_, _) => invocations++;
        veto = false;
        provider.Select(); host.Layout();
        Assert.Equal(0, overlays.OpenCount);
        Assert.Same(last, drawer.SelectedItem);
        Assert.Equal(0, invocations);
    }

    [AvaloniaFact]
    public void Modal_drawer_uses_pinned_default_zero_elevation_with_explicit_host_shadow_override()
    {
        var drawer = new MaterialNavigationDrawer { Mode = MaterialNavigationDrawerMode.Modal, IsOpen = false };
        drawer.Items.Add(new MaterialNavigationItem { Content = "Home" });
        var overlays = new MaterialOverlayHost { Content = new MaterialNavigationDrawerLayout { Drawer = drawer, Content = new Border() } };
        using var host = new ChromeHost(overlays);
        drawer.IsOpen = true; host.Layout();
        Assert.Equal(default(BoxShadows), drawer.BoxShadow);
        var shadow = BoxShadows.Parse("0 3 6 0 #40000000");
        drawer.BoxShadow = shadow; host.Layout();
        Assert.Equal(shadow, drawer.BoxShadow);
        drawer.Mode = MaterialNavigationDrawerMode.Standard; host.Layout();
        Assert.Equal(shadow, drawer.BoxShadow);
    }

    [AvaloniaTheory]
    [InlineData(MaterialTopAppBarVariant.Medium, 24)]
    [InlineData(MaterialTopAppBarVariant.MediumFlexible, 24)]
    [InlineData(MaterialTopAppBarVariant.Large, 28)]
    [InlineData(MaterialTopAppBarVariant.LargeFlexible, 28)]
    public void Expanded_title_uses_reference_last_baseline_bottom_padding_when_room_is_available(MaterialTopAppBarVariant variant, double padding)
    {
        var title = new TextBlock { Text = "Host title", FontSize = 16, LineHeight = 20 };
        var bar = new MaterialTopAppBar { Title = "Collection", Variant = variant, TitleTemplate = new FuncDataTemplate<string>((_, _) => title) };
        using var host = new ChromeHost(bar);
        var baseline = title.TranslatePoint(new Point(0, title.TextLayout.TextLines[0].Baseline), host.Window)!.Value.Y;
        // Avalonia's default layout rounding may shift a glyph baseline by up to half a DIP.
        Assert.InRange(bar.Bounds.Bottom - baseline, padding - .5, padding + .5);
    }

    [AvaloniaFact]
    public void Navigation_and_action_icons_use_distinct_pinned_roles_without_overwriting_host_local_color()
    {
        var navigation = new MaterialIconButton { Content = "←" };
        var action = new MaterialIconButton { Content = "✓" };
        var bar = new MaterialTopAppBar { Title = "Collection", NavigationContent = navigation, Actions = action };
        using var host = new ChromeHost(bar);
        Assert.Equal(Color.Parse("#1D1B20"), ((ISolidColorBrush)navigation.Foreground!).Color);
        Assert.Equal(Color.Parse("#49454F"), ((ISolidColorBrush)action.Foreground!).Color);
        host.Window.RequestedThemeVariant = ThemeVariant.Dark; host.Layout();
        Assert.Equal(Color.Parse("#E6E0E9"), ((ISolidColorBrush)navigation.Foreground!).Color);
        Assert.Equal(Color.Parse("#CAC4D0"), ((ISolidColorBrush)action.Foreground!).Color);
        navigation.Foreground = Brushes.Red; host.Layout();
        Assert.Equal(Colors.Red, ((ISolidColorBrush)navigation.Foreground!).Color);
    }

    [AvaloniaFact]
    public void Centered_flexible_form_centers_subtitle_glyphs_with_the_title()
    {
        var bar = new MaterialTopAppBar { Variant = MaterialTopAppBarVariant.LargeFlexible, Title = "Collection", Subtitle = "Updated today", CenterTitle = true };
        using var host = new ChromeHost(bar);
        foreach (var text in bar.GetVisualDescendants().OfType<TextBlock>().Where(text => (text.Text is "Collection" or "Updated today") && text.IsEffectivelyVisible && text.GetVisualAncestors().All(parent => parent.Opacity > 0)))
        {
            var glyphCenter = text.TranslatePoint(new Point(text.TextLayout.WidthIncludingTrailingWhitespace / 2, 0), host.Window)!.Value.X;
            // Integer slot placement plus rounded glyph extent can differ by up to one DIP.
            Assert.InRange(glyphCenter, 499, 501);
        }
    }

    [AvaloniaFact]
    public void Compound_navigation_slot_can_reuse_its_buttons_as_ordinary_actions_after_replacement()
    {
        var first = new MaterialIconButton { Content = "←" };
        var second = new MaterialIconButton { Content = "☰" };
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Children = { first, second } };
        var bar = new MaterialTopAppBar { Title = "Collection", NavigationContent = panel };
        using var host = new ChromeHost(bar);
        Assert.Equal(Color.Parse("#1D1B20"), ((ISolidColorBrush)first.Foreground!).Color);
        Assert.Equal(Color.Parse("#1D1B20"), ((ISolidColorBrush)second.Foreground!).Color);
        bar.NavigationContent = null; host.Layout();
        bar.Actions = panel; host.Layout();
        Assert.Equal(Color.Parse("#49454F"), ((ISolidColorBrush)first.Foreground!).Color);
        Assert.Equal(Color.Parse("#49454F"), ((ISolidColorBrush)second.Foreground!).Color);
    }

    [AvaloniaTheory]
    [InlineData(MaterialTopAppBarVariant.MediumFlexible)]
    [InlineData(MaterialTopAppBarVariant.LargeFlexible)]
    public void Flexible_subtitle_is_retained_when_collapsed_with_small_subtitle_typography(MaterialTopAppBarVariant variant)
    {
        var bar = new MaterialTopAppBar { Variant = variant, Title = "Collection", Subtitle = "Updated today", ScrollBehavior = MaterialAppBarScrollBehavior.EnterAlways };
        using var host = new ChromeHost(bar);
        bar.ApplyScrollDelta(500, 500); host.Layout();
        var subtitle = bar.GetVisualDescendants().OfType<TextBlock>().Single(text => text.Text == "Updated today" && text.IsEffectivelyVisible && text.GetVisualAncestors().All(parent => parent.Opacity > 0));
        Assert.True(subtitle.IsEffectivelyVisible);
        Assert.Equal(12, subtitle.FontSize);
        Assert.Equal(16, subtitle.LineHeight);
        Assert.Equal(.5, subtitle.LetterSpacing);
        Assert.Equal(64, bar.Bounds.Height);
    }

    [AvaloniaTheory]
    [InlineData(MaterialTopAppBarVariant.Small)]
    [InlineData(MaterialTopAppBarVariant.Medium)]
    public void Single_row_title_respects_four_dip_space_after_navigation_target(MaterialTopAppBarVariant variant)
    {
        var navigation = new MaterialIconButton { Content = "←" };
        var bar = new MaterialTopAppBar { Variant = variant, Title = "Collection", NavigationContent = navigation, ScrollBehavior = MaterialAppBarScrollBehavior.EnterAlways };
        using var host = new ChromeHost(bar);
        if (variant == MaterialTopAppBarVariant.Medium) { bar.ApplyScrollDelta(500, 500); host.Layout(); }
        var title = bar.GetVisualDescendants().OfType<TextBlock>().Single(text => text.Text == "Collection" && text.IsEffectivelyVisible && text.GetVisualAncestors().All(parent => parent.Opacity > 0));
        var navRight = navigation.TranslatePoint(default, host.Window)!.Value.X + navigation.Bounds.Width;
        var titleLeft = title.TranslatePoint(default, host.Window)!.Value.X;
        Assert.Equal(4, titleLeft - navRight);
    }
}

internal sealed class ChromeHost : IDisposable
{
    public MaterialTheme Theme { get; } = new();
    public Window Window { get; }
    public ChromeHost(Control content, double width = 1000, double height = 700)
    {
        Application.Current!.Styles.Add(Theme);
        Window = new Window { Width = width, Height = height, RequestedThemeVariant = ThemeVariant.Light, Content = content };
        Window.Show();
        Layout();
    }
    public void Layout()
    {
        Dispatcher.UIThread.RunJobs(); Window.UpdateLayout();
        using var frame = Window.CaptureRenderedFrame();
        Dispatcher.UIThread.RunJobs(); Window.UpdateLayout();
    }
    public Point Center(Control control) => control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), Window)!.Value;
    public void Click(Control control) { var p = Center(control); Window.MouseDown(p, MouseButton.Left); Window.MouseUp(p, MouseButton.Left); Layout(); }
    public void Key(PhysicalKey key) { Window.KeyPressQwerty(key, RawInputModifiers.None); Window.KeyReleaseQwerty(key, RawInputModifiers.None); Layout(); }
    public void Dispose() { Window.Close(); Application.Current!.Styles.Remove(Theme); }
}
