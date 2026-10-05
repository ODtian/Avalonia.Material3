using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Material3.Tests;

public class FloatingActionScenarioTests
{
    [AvaloniaFact]
    public void Long_mixed_extended_labels_wrap_at_200_percent_in_a_narrow_host_without_shrinking_the_icon()
    {
        using var host = new ButtonHost();
        host.Window.Width = 320;
        host.Window.Height = 500;
        host.Theme.Typography = host.Theme.Typography with { Scale = 2 };
        var fab = new MaterialExtendedFab { Content = "Create a new document 新建文档 with a much longer label", Icon = "+", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch };
        host.Window.Content = new Grid { Children = { fab } };
        host.Capture();
        Assert.True(fab.Bounds.Height > 100);
        Assert.Equal(24, fab.IconSize);
        Assert.True(fab.Bounds.Width <= 320);
        Assert.Equal(28, fab.FontSize);
    }

    [AvaloniaFact]
    public void Text_only_extended_action_never_collapses_to_an_empty_named_box()
    {
        using var host = new ButtonHost();
        var fab = new MaterialExtendedFab { Content = "Compose", IsExpanded = false };
        host.Window.Content = new StackPanel { Children = { fab } };
        host.Capture();
        Assert.True(fab.IsExpanded);
        Assert.True(fab.Bounds.Width > 66);
        fab.Icon = "+";
        fab.IsExpanded = false;
        host.Capture();
        Assert.False(fab.IsExpanded);
        fab.Icon = null;
        host.Capture();
        Assert.True(fab.IsExpanded);
        Assert.True(fab.Bounds.Width > 66);
    }

    [AvaloniaFact]
    public void Docked_toolbar_uses_bounded_adaptive_spacing_instead_of_stretching_action_targets()
    {
        using var host = new ButtonHost();
        host.Window.Width = 800;
        var toolbar = new MaterialToolbar { Variant = MaterialToolbarVariant.Docked };
        var a = new MaterialIconButton { Content = "A" };
        var b = new MaterialIconButton { Content = "B" };
        var c = new MaterialIconButton { Content = "C" };
        toolbar.Items.AddRange(new Control[] { a, b, c });
        host.Window.Content = toolbar;
        host.Capture();
        var first = a.TranslatePoint(default, host.Window)!.Value;
        var second = b.TranslatePoint(default, host.Window)!.Value;
        Assert.Equal(32, second.X - first.X - a.Bounds.Width);
        Assert.Equal(50, a.Bounds.Width);
        host.Window.Width = 220;
        host.Capture();
        Assert.Equal(50, a.Bounds.Width);
    }

    [AvaloniaFact]
    public void Vibrant_toolbar_keeps_its_FAB_slot_in_bounds_and_keyboard_reaches_overflow_actions()
    {
        using var host = new ButtonHost();
        host.Window.Width = 320;
        var toolbar = new MaterialToolbar { Color = MaterialToolbarColor.Vibrant, Anchor = MaterialActionAnchor.BottomEnd, FloatingAction = new MaterialFab { Content = "+" } };
        for (var i = 0; i < 9; i++) toolbar.Items.Add(new MaterialIconButton { Content = i.ToString(), IsToggle = true });
        host.Window.Content = new Grid { Children = { toolbar } };
        host.Capture();
        Assert.Equal(Color.Parse("#EADDFF"), Assert.IsAssignableFrom<ISolidColorBrush>(toolbar.Background).Color);
        var first = (MaterialIconButton)toolbar.Items[0];
        Assert.Equal(Color.Parse("#21005D"), Assert.IsAssignableFrom<ISolidColorBrush>(first.Foreground).Color);
        first.IsChecked = true;
        Assert.Equal(Color.Parse("#F3EDF7"), Assert.IsAssignableFrom<ISolidColorBrush>(first.Background).Color);
        var fab = toolbar.FloatingAction!;
        var position = fab.TranslatePoint(default, host.Window)!.Value;
        Assert.InRange(position.X, 0, 320 - fab.Bounds.Width);
        first.Focus(NavigationMethod.Tab);
        host.Window.KeyPressQwerty(PhysicalKey.End, RawInputModifiers.None);
        host.Capture();
        var last = toolbar.Items[^1];
        Assert.True(last.IsFocused);
        var lastCenter = last.TranslatePoint(new Point(last.Bounds.Width / 2, last.Bounds.Height / 2), host.Window)!.Value;
        Assert.InRange(lastCenter.X, 16, position.X);
        toolbar.IsExpanded = false;
        host.Capture();
        Assert.True(fab.IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public async Task Expansion_really_animates_and_runtime_reduced_motion_snaps_and_can_be_reversed()
    {
        using var host = new ButtonHost();
        var fab = new MaterialExtendedFab { Content = "Create a new document", Icon = "+" };
        host.Window.Width = 600;
        host.Window.Content = new StackPanel { Children = { fab } };
        host.Theme.Motion = new Avalonia.Material3.Tokens.MaterialMotion
        {
            Springs = Avalonia.Material3.Tokens.MaterialSpringScheme.Expressive with
            {
                FastSpatial = new(1, 100), FastEffects = new(1, 100)
            }
        };
        host.Capture();
        var expanded = fab.Bounds.Width;
        fab.IsExpanded = false;
        await Task.Delay(60);
        host.Capture();
        Assert.InRange(fab.Bounds.Width, 66.01, expanded - 0.01);
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = true };
        host.Capture();
        Assert.Equal(66, fab.Bounds.Width);
        fab.IsExpanded = true;
        host.Capture();
        Assert.Equal(expanded, fab.Bounds.Width);
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = false };
        fab.IsExpanded = false;
        await Task.Delay(60);
        host.Capture();
        Assert.InRange(fab.Bounds.Width, 66.01, expanded - 0.01);
    }

    [AvaloniaTheory]
    [InlineData(MaterialToolbarVariant.Docked, Avalonia.Layout.Orientation.Horizontal)]
    [InlineData(MaterialToolbarVariant.Docked, Avalonia.Layout.Orientation.Vertical)]
    [InlineData(MaterialToolbarVariant.Floating, Avalonia.Layout.Orientation.Horizontal)]
    [InlineData(MaterialToolbarVariant.Floating, Avalonia.Layout.Orientation.Vertical)]
    public void Every_toolbar_layout_retains_core_actions_and_collapses_only_expansion_slots(MaterialToolbarVariant variant, Avalonia.Layout.Orientation orientation)
    {
        using var host = new ButtonHost();
        host.Window.Width = 600;
        host.Window.Height = 500;
        var toolbar = new MaterialToolbar { Variant = variant, Orientation = orientation };
        AutomationProperties.SetName(toolbar, "Editing tools");
        var primary = new MaterialIconButton { Content = "★" };
        var leading = new MaterialIconButton { Content = "+" };
        var trailing = new MaterialIconButton { Content = "▣" };
        toolbar.Items.Add(primary);
        toolbar.LeadingItems.Add(leading);
        toolbar.TrailingItems.Add(trailing);
        host.Window.Content = new Grid { Children = { toolbar } };
        host.Capture();
        Assert.True(toolbar.IsExpanded);
        Assert.True(leading.IsEffectivelyVisible && trailing.IsEffectivelyVisible);
        Assert.True(orientation == Avalonia.Layout.Orientation.Horizontal ? toolbar.Bounds.Height >= 64 : toolbar.Bounds.Width >= 64);
        var peer = ControlAutomationPeer.CreatePeerForElement(toolbar)!;
        Assert.Equal(AutomationControlType.ToolBar, peer.GetAutomationControlType());
        var expand = peer.GetProvider<IExpandCollapseProvider>()!;
        trailing.Focus(NavigationMethod.Tab);
        expand.Collapse();
        host.Capture();
        Assert.False(leading.IsEffectivelyVisible || trailing.IsEffectivelyVisible);
        Assert.True(primary.IsEffectivelyVisible);
        Assert.False(trailing.IsFocused);
        var count = 0;
        primary.Click += (_, _) => count++;
        primary.Focus(NavigationMethod.Tab);
        host.Window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Assert.Equal(1, count);
        expand.Expand();
        host.Capture();
        Assert.True(leading.IsEffectivelyVisible && trailing.IsEffectivelyVisible);
    }

    [AvaloniaTheory]
    [InlineData(MaterialActionAnchor.TopStart, false)]
    [InlineData(MaterialActionAnchor.TopEnd, false)]
    [InlineData(MaterialActionAnchor.BottomStart, false)]
    [InlineData(MaterialActionAnchor.BottomEnd, false)]
    [InlineData(MaterialActionAnchor.BottomStart, true)]
    [InlineData(MaterialActionAnchor.BottomEnd, true)]
    public void Menu_anchor_and_host_icon_slots_survive_expansion_resize_and_RTL(MaterialActionAnchor anchor, bool rtl)
    {
        using var host = new ButtonHost();
        host.Window.Width = 500;
        host.Window.Height = 400;
        var menu = new MaterialFabMenu { Anchor = anchor, FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, ToggleIcon = "OPEN", CloseIcon = "CLOSE", ExpandLabel = "Show creation choices", CollapseLabel = "Hide creation choices" };
        menu.Items.Add(new MaterialFabMenuItem { Content = "Create 新文档", LeadingIcon = "+" });
        host.Window.Content = new Grid { Children = { menu } };
        host.Capture();
        var toggle = Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(menu).OfType<MaterialFab>().Single();
        var before = toggle.TranslatePoint(default, host.Window)!.Value;
        Assert.Equal("OPEN", toggle.Content);
        menu.IsExpanded = true;
        host.Capture();
        Assert.Equal(before, toggle.TranslatePoint(default, host.Window)!.Value);
        Assert.Equal("CLOSE", toggle.Content);
        Assert.Equal("Hide creation choices", ControlAutomationPeer.CreatePeerForElement(toggle)!.GetName());
        var item = menu.Items[0].TranslatePoint(default, host.Window)!.Value;
        Assert.Equal(anchor is MaterialActionAnchor.TopStart or MaterialActionAnchor.TopEnd, item.Y > before.Y);
        host.Window.Width = 320;
        host.Window.Height = 240;
        host.Capture();
        var position = toggle.TranslatePoint(default, host.Window)!.Value;
        var otherEdge = toggle.TranslatePoint(new Point(toggle.Bounds.Width, 0), host.Window)!.Value;
        Assert.InRange(Math.Min(position.X, otherEdge.X), 0, 320 - toggle.Bounds.Width);
        Assert.InRange(position.Y, 0, 240 - toggle.Bounds.Height);
    }

    [AvaloniaFact]
    public void Menu_ExpandCollapse_keyboard_navigation_dismissal_and_disabled_state_are_observable()
    {
        using var host = new ButtonHost();
        var menu = new MaterialFabMenu();
        AutomationProperties.SetName(menu, "Create actions");
        var first = new MaterialFabMenuItem { Content = "Document" };
        var disabled = new MaterialFabMenuItem { Content = "Unavailable", IsEnabled = false };
        var last = new MaterialFabMenuItem { Content = "Folder" };
        menu.Items.AddRange(new[] { first, disabled, last });
        host.Window.Content = new Grid { Children = { menu } };
        host.Capture();
        var peer = ControlAutomationPeer.CreatePeerForElement(menu)!;
        Assert.Equal("Create actions", peer.GetName());
        Assert.Equal(AutomationControlType.Menu, peer.GetAutomationControlType());
        var expand = peer.GetProvider<IExpandCollapseProvider>()!;
        Assert.NotNull(expand);
        expand.Expand();
        host.Capture();
        Assert.Equal(ExpandCollapseState.Expanded, expand.ExpandCollapseState);
        Assert.True(first.IsFocused);
        host.Window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
        Assert.True(last.IsFocused);
        host.Window.KeyPressQwerty(PhysicalKey.Home, RawInputModifiers.None);
        Assert.True(first.IsFocused);
        host.Window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        host.Capture();
        Assert.Equal(ExpandCollapseState.Collapsed, expand.ExpandCollapseState);
        var toggle = Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(menu).OfType<MaterialFab>().Single();
        Assert.True(toggle.IsFocused);
        var togglePeer = ControlAutomationPeer.CreatePeerForElement(toggle)!;
        Assert.Equal("Expand actions", togglePeer.GetName());
        Assert.NotNull(togglePeer.GetProvider<IExpandCollapseProvider>());
        expand.Expand();
        host.Capture();
        host.Window.MouseDown(new Point(2, 2), MouseButton.Left);
        host.Window.MouseUp(new Point(2, 2), MouseButton.Left);
        Assert.False(menu.IsExpanded);
        expand.Expand();
        host.Capture();
        menu.IsEnabled = false;
        Assert.False(menu.IsExpanded);
        Assert.Throws<ElementNotEnabledException>(() => expand.Expand());
    }

    [AvaloniaFact]
    public void Fab_menu_opens_with_pointer_selects_an_action_and_returns_focus_to_its_anchor()
    {
        using var host = new ButtonHost();
        var menu = new MaterialFabMenu();
        var item = new MaterialFabMenuItem { Content = "New document", LeadingIcon = "+" };
        var count = 0;
        item.Click += (_, _) => count++;
        menu.Items.Add(item);
        menu.Items.Add(new MaterialFabMenuItem { Content = "New folder", LeadingIcon = "▣" });
        host.Window.Content = new Grid { Children = { menu } };
        host.Capture();
        Assert.False(menu.IsExpanded);
        var toggle = Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(menu).OfType<MaterialFab>().Single();
        var center = toggle.TranslatePoint(new Point(toggle.Bounds.Width / 2, toggle.Bounds.Height / 2), host.Window)!.Value;
        host.Window.MouseDown(center, MouseButton.Left);
        host.Window.MouseUp(center, MouseButton.Left);
        host.Capture();
        Assert.True(menu.IsExpanded);
        Assert.True(item.IsFocused);
        Assert.Equal(56, item.ContainerHeight);
        Assert.True(item.Bounds.Height >= 56);
        var action = item.TranslatePoint(new Point(item.Bounds.Width / 2, item.Bounds.Height / 2), host.Window)!.Value;
        host.Window.MouseDown(action, MouseButton.Left);
        host.Window.MouseUp(action, MouseButton.Left);
        host.Capture();
        Assert.Equal(1, count);
        Assert.False(menu.IsExpanded);
        Assert.True(toggle.IsFocused);
        Assert.False(item.IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public void Extended_fab_retains_label_icon_command_and_focus_when_host_collapses_it()
    {
        using var host = new ButtonHost();
        var fab = new MaterialExtendedFab { Icon = "+", Content = "Create 新文档", Size = MaterialFabSize.Medium };
        host.Window.Content = new StackPanel { Children = { fab } };
        var count = 0;
        fab.Click += (_, _) => count++;
        host.Capture();
        var expanded = fab.Bounds.Width;
        Assert.Equal(80, fab.ContainerSize);
        Assert.Equal(28, fab.IconSize);
        Assert.Equal(12, fab.IconSpacing);
        Assert.Equal(22, fab.FontSize);
        fab.Focus(NavigationMethod.Tab);
        fab.IsExpanded = false;
        host.Capture();
        Assert.True(fab.Bounds.Width < expanded);
        Assert.Equal(90, fab.Bounds.Width);
        Assert.True(fab.IsFocused);
        Assert.Equal("Create 新文档", ControlAutomationPeer.CreatePeerForElement(fab)!.GetName());
        host.Window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);
        host.Window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None);
        Assert.Equal(1, count);
        fab.IsExpanded = true;
        host.Capture();
        Assert.Equal(expanded, fab.Bounds.Width);
    }

    [AvaloniaTheory]
    [InlineData(MaterialFabSize.Small, 40, 24, 12)]
    [InlineData(MaterialFabSize.Standard, 56, 24, 16)]
    [InlineData(MaterialFabSize.Medium, 80, 28, 20)]
    [InlineData(MaterialFabSize.Large, 96, 36, 28)]
    public void Host_can_resize_the_same_fab_without_losing_action_or_minimum_target(MaterialFabSize size, double container, double icon, double radius)
    {
        using var host = new ButtonHost();
        var fab = new MaterialFab { Content = "+", Size = size };
        host.Window.Content = fab;
        host.Capture();
        Assert.Equal(container, fab.ContainerSize);
        Assert.Equal(icon, fab.IconSize);
        Assert.Equal(new CornerRadius(radius), fab.CornerRadius);
        Assert.True(fab.Bounds.Width >= 48 && fab.Bounds.Height >= 48);
        Assert.True(fab.Bounds.Width >= container && fab.Bounds.Height >= container);
        fab.Size = MaterialFabSize.Standard;
        host.Capture();
        Assert.Equal(56, fab.ContainerSize);
        Assert.Equal(new CornerRadius(16), fab.CornerRadius);
    }

    [AvaloniaFact]
    public void Standard_fab_is_a_themed_56_DIP_action_with_real_input_and_Invoke()
    {
        using var host = new ButtonHost();
        var fab = new MaterialFab { Content = "+" };
        AutomationProperties.SetName(fab, "Create document");
        host.Window.Content = new StackPanel { Margin = new Thickness(24), Children = { fab } };
        fab.Click += (_, _) => host.Result.Text = "Created";
        host.Capture();
        Assert.Equal(56, fab.ContainerSize);
        Assert.Equal(24, fab.IconSize);
        Assert.True(fab.Bounds.Width >= 56 && fab.Bounds.Height >= 56);
        Assert.Equal(Color.Parse("#EADDFF"), Assert.IsAssignableFrom<ISolidColorBrush>(fab.Background).Color);
        var center = fab.TranslatePoint(new Point(fab.Bounds.Width / 2, fab.Bounds.Height / 2), host.Window)!.Value;
        host.Window.MouseDown(center, MouseButton.Left);
        Assert.True(fab.IsPressed);
        host.Window.MouseUp(center, MouseButton.Left);
        Assert.Equal("Created", host.Result.Text);
        var peer = ControlAutomationPeer.CreatePeerForElement(fab)!;
        Assert.Equal("Create document", peer.GetName());
        Assert.Equal(AutomationControlType.Button, peer.GetAutomationControlType());
        peer.GetProvider<IInvokeProvider>()!.Invoke();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Assert.Equal("Created", host.Result.Text);
    }
}
