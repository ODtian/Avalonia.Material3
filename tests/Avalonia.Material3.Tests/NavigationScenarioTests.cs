using System.Windows.Input;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Material3.Tokens;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Material3.Tests;

public class NavigationScenarioTests
{
    [AvaloniaFact]
    public void Host_switches_bar_item_layout_in_place_and_rail_expansion_keeps_the_focused_destination()
    {
        using var bar = new NavigationHost(new MaterialNavigationBar());
        bar.Navigation.ItemLayout = MaterialNavigationItemLayout.Horizontal;
        bar.Window.UpdateLayout();
        Assert.Equal(64, bar.First.Bounds.Height);
        using var rail = new NavigationHost(new MaterialNavigationRail());
        rail.Second.Focus(NavigationMethod.Tab);
        rail.Click(rail.Second);
        ((MaterialNavigationRail)rail.Navigation).IsExpanded = true;
        rail.Navigation.ItemLayout = MaterialNavigationItemLayout.Horizontal;
        rail.Window.Width = 1000;
        rail.Window.UpdateLayout();
        Assert.Same(rail.Second, rail.Navigation.SelectedItem);
        Assert.True(rail.Second.IsFocused);
        Assert.InRange(rail.First.Bounds.Width, 220, 360);
        Assert.Equal(56, rail.First.Bounds.Height);
    }

    [AvaloniaFact]
    public void Primary_and_secondary_tabs_switch_real_content_in_fixed_and_scrollable_layouts()
    {
        using var host = new NavigationHost(new MaterialTabs());
        host.Window.UpdateLayout();
        Assert.Equal(300, host.First.Bounds.Width);
        host.Click(host.Second);
        Assert.Same(host.Second.PageContent, host.Navigation.SelectedContent);
        var tabs = (MaterialTabs)host.Navigation;
        tabs.Variant = MaterialTabVariant.Secondary;
        tabs.Layout = MaterialTabLayout.Scrollable;
        for (var i = 0; i < 6; i++) tabs.Items.Add(new MaterialNavigationItem { Content = $"Collection 收藏 {i}", PageContent = new TextBlock { Text = $"Page {i}" } });
        host.Window.UpdateLayout();
        Assert.True(host.First.Bounds.Width >= 90);
        Assert.True(tabs.ItemsPanelRoot!.Bounds.Width > host.Window.Width);
        tabs.SelectedIndex = 7;
        host.Window.UpdateLayout();
        Assert.Equal("Page 5", ((TextBlock)tabs.SelectedContent!).Text);
    }

    [AvaloniaFact]
    public void Keyboard_user_has_one_header_tab_stop_skips_disabled_tabs_and_scrolls_to_end()
    {
        using var host = new NavigationHost(new MaterialTabs { Layout = MaterialTabLayout.Scrollable });
        host.Second.IsEnabled = false;
        var last = new MaterialNavigationItem { Content = "Last 最后一个 very long collection", PageContent = "Last page" };
        host.Navigation.Items.Add(last);
        host.Window.Width = 320;
        host.Window.UpdateLayout();
        host.Key(PhysicalKey.Tab);
        Assert.True(host.First.IsFocused);
        host.Key(PhysicalKey.ArrowRight);
        Assert.Same(last, host.Navigation.SelectedItem);
        Assert.True(last.IsFocused);
        Dispatcher.UIThread.RunJobs();
        host.Window.UpdateLayout();
        Assert.InRange(host.Center(last).X, 0, 320);
        Assert.True(host.Navigation.GetVisualDescendants().OfType<ScrollViewer>().Single().Offset.X > 0);
        host.Resize(1000);
        Assert.Same(last, host.Navigation.SelectedItem);
        Assert.True(last.IsFocused);
        host.Resize(320);
        Assert.Equal(320, host.Window.Bounds.Width);
        Assert.InRange(host.Center(last).X, 0, 320);
        host.Key(PhysicalKey.Home);
        Assert.True(host.First.IsFocused);
        Assert.Same(host.First, host.Navigation.SelectedItem);
    }

    [AvaloniaFact]
    public void Badge_and_repeat_invocation_are_observable_without_faking_a_selection_change()
    {
        using var host = new NavigationHost(new MaterialNavigationBar());
        var badge = new MaterialBadge { Count = 103 };
        host.Second.Badge = badge;
        host.Second.BadgeDescription = "103 unread items";
        host.Second.SelectedIcon = "▣";
        var changes = 0;
        var invocations = 0;
        var commands = 0;
        host.Navigation.SelectionChanged += (_, _) => changes++;
        host.Navigation.ItemInvoked += (_, e) => { Assert.Same(host.Second, e.Item); Assert.Same(host.Second.PageContent, host.Navigation.SelectedContent); invocations++; };
        host.Second.Command = new NavigationCommand(() => { Assert.Same(host.Second, host.Navigation.SelectedItem); commands++; });
        host.Click(host.Second);
        host.Click(host.Second);
        host.Window.UpdateLayout();
        Assert.Equal(1, changes);
        Assert.Equal(2, invocations);
        Assert.Equal(2, commands);
        Assert.Equal("99+", badge.DisplayText);
        Assert.True(badge.Bounds.Width > 0);
        Assert.Equal("▣", host.Second.DisplayIcon);
        host.Navigation.SelectedIndex = 0;
        Assert.Equal(2, invocations);
        Assert.Equal(2, commands);
        host.Second.IsEnabled = false;
        host.Click(host.Second);
        Assert.Same(host.First, host.Navigation.SelectedItem);
        Assert.Equal(2, invocations);
    }

    [AvaloniaFact]
    public void Automation_exposes_tab_selection_container_name_badge_and_disabled_actions()
    {
        using var host = new NavigationHost(new MaterialTabs());
        host.Second.BadgeDescription = "3 unread";
        var ownerPeer = ControlAutomationPeer.CreatePeerForElement(host.Navigation)!;
        var itemPeer = ControlAutomationPeer.CreatePeerForElement(host.Second)!;
        Assert.Equal(AutomationControlType.Tab, ownerPeer.GetAutomationControlType());
        Assert.Equal(AutomationControlType.TabItem, itemPeer.GetAutomationControlType());
        Assert.Equal("Library 资料, 3 unread", itemPeer.GetName());
        var owner = Assert.IsAssignableFrom<ISelectionProvider>(ownerPeer);
        var item = Assert.IsAssignableFrom<ISelectionItemProvider>(itemPeer);
        Assert.False(owner.CanSelectMultiple);
        Assert.True(owner.IsSelectionRequired);
        Assert.Same(owner, item.SelectionContainer);
        item.Select();
        Assert.True(item.IsSelected);
        Assert.Same(itemPeer, Assert.Single(owner.GetSelection()));
        Assert.Same(host.Second.PageContent, host.Navigation.SelectedContent);
        Assert.Throws<InvalidOperationException>(() => item.RemoveFromSelection());
        host.Second.IsEnabled = false;
        Assert.ThrowsAny<Exception>(() => item.Select());
        Assert.ThrowsAny<Exception>(() => ((IInvokeProvider)itemPeer).Invoke());
        Assert.True(item.IsSelected);
    }

    [AvaloniaFact]
    public void Collection_changes_retain_destination_identity_and_removed_items_lose_selection()
    {
        using var host = new NavigationHost(new MaterialTabs());
        host.Click(host.Second);
        host.Navigation.Items.Insert(0, new MaterialNavigationItem { Content = "Inserted", PageContent = "New page" });
        Assert.Same(host.Second, host.Navigation.SelectedItem);
        Assert.Equal(2, host.Navigation.SelectedIndex);
        Assert.True(host.Second.IsFocused);
        host.Navigation.Items.Remove(host.Second);
        Assert.False(host.Second.IsSelected);
        Assert.Same(host.First, host.Navigation.SelectedItem);
        host.Navigation.Items.Clear();
        Assert.Null(host.Navigation.SelectedItem);
        Assert.Null(host.Navigation.SelectedContent);
        Assert.Equal(-1, host.Navigation.SelectedIndex);
    }

    [AvaloniaFact]
    public void Expanded_rail_width_is_host_configurable_and_narrow_collapsed_width_is_available()
    {
        using var host = new NavigationHost(new MaterialNavigationRail { IsExpanded = true, ItemLayout = MaterialNavigationItemLayout.Horizontal });
        host.Window.Width = 320;
        host.Second.Content = "Long collection 很长的中英文混排资料 collection";
        host.Theme.Typography = new MaterialTypography { Scale = 2 };
        host.Window.UpdateLayout();
        Assert.Equal(220, host.Second.Bounds.Width);
        host.Click(host.Second);
        Assert.True(((Control)host.Second.PageContent!).Bounds.Width > 0);
        var rail = (MaterialNavigationRail)host.Navigation;
        rail.ExpandedWidth = 280;
        host.Window.UpdateLayout();
        Assert.Equal(280, host.Second.Bounds.Width);
        rail.IsExpanded = false;
        rail.UseNarrowWidth = true;
        host.Window.UpdateLayout();
        Assert.Equal(80, host.Second.Bounds.Width);
        Assert.Same(host.Second, host.Navigation.SelectedItem);
        Assert.Throws<ArgumentException>(() => rail.ExpandedWidth = 400);
    }

    [AvaloniaFact]
    public void Bar_pointer_selection_displays_the_selected_page_before_notifying_the_host()
    {
        using var host = new NavigationHost(new MaterialNavigationBar());
        object? observed = null;
        host.Navigation.SelectionChanged += (_, _) => observed = host.Navigation.SelectedContent;
        host.Click(host.Second);
        Assert.Same(host.Second, host.Navigation.SelectedItem);
        Assert.Same(host.Second.PageContent, host.Navigation.SelectedContent);
        Assert.Same(host.Second.PageContent, observed);
        Assert.True(host.Second.IsSelected);
        Assert.False(host.First.IsSelected);
        host.Window.UpdateLayout();
        Assert.True(((Control)host.Second.PageContent!).IsVisible);
        Assert.True(((Control)host.Second.PageContent!).Bounds.Width > 0);
    }
}

internal sealed class NavigationCommand(Action action) : ICommand
{
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => action();
    public event EventHandler? CanExecuteChanged { add { } remove { } }
}

internal sealed class NavigationHost : IDisposable
{
    public MaterialTheme Theme { get; } = new() { Motion = new MaterialMotion { ReduceMotion = true } };
    public MaterialNavigation Navigation { get; }
    public MaterialNavigationItem First { get; } = new() { Content = "Home 首页", Icon = "⌂", PageContent = new TextBlock { Text = "Home content" } };
    public MaterialNavigationItem Second { get; } = new() { Content = "Library 资料", Icon = "▤", PageContent = new TextBlock { Text = "Library content" } };
    public Window Window { get; }
    public NavigationHost(MaterialNavigation navigation)
    {
        Navigation = navigation;
        Navigation.Items.Add(First);
        Navigation.Items.Add(Second);
        Application.Current!.Styles.Add(Theme);
        Window = new Window { Width = 600, Height = 360, RequestedThemeVariant = ThemeVariant.Light, Content = Navigation };
        Window.Show();
        Window.UpdateLayout();
    }
    public void Resize(double width)
    {
        Window.Width = width;
        Window.Measure(new Size(width, Window.Height));
        Window.Arrange(new Rect(0, 0, width, Window.Height));
        Window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        Window.UpdateLayout();
        Capture();
    }
    public byte[] Capture()
    {
        using var bitmap = Window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No rendered frame.");
        using var stream = new MemoryStream();
        bitmap.Save(stream, Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        return stream.ToArray();
    }
    public Point Center(Control item) => item.TranslatePoint(new Point(item.Bounds.Width / 2, item.Bounds.Height / 2), Window)!.Value;
    public void Key(PhysicalKey key) { Window.KeyPressQwerty(key, RawInputModifiers.None); Window.KeyReleaseQwerty(key, RawInputModifiers.None); }
    public void Click(Control item) { Window.MouseDown(Center(item), MouseButton.Left); Window.MouseUp(Center(item), MouseButton.Left); }
    public void Dispose() { Window.Close(); Application.Current!.Styles.Remove(Theme); }
}
