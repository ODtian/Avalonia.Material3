using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Runtime.InteropServices;
using Xunit;

namespace Avalonia.Material3.Tests;

public class NavigationMatrixScenarioTests
{
    [AvaloniaTheory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(5, false)]
    [InlineData(5, true)]
    public void Navigation_selection_and_hover_use_circular_full_corners_with_a_straight_capsule_middle(int recipe, bool hover)
    {
        using var host = new NavigationHost(Create(recipe));
        host.Theme.LightColorScheme = MaterialColorScheme.Light with {
            SecondaryContainer = hover ? Colors.Gray : Colors.Magenta, OnSecondaryContainer = Colors.Magenta };
        host.Theme.States = new MaterialStates { HoverStateLayerOpacity = 1, FocusStateLayerOpacity = 0 };
        host.First.Content = ""; host.First.Icon = null; host.Second.Content = ""; host.Second.Icon = null;
        var item = hover ? host.Second : host.First;
        host.Capture();
        if (hover) { host.Window.MouseMove(host.Center(item)); host.Capture(); }
        using var bitmap = new RenderTargetBitmap(new PixelSize((int)item.Bounds.Width, (int)item.Bounds.Height), new Vector(96, 96));
        using var pixels = new WriteableBitmap(bitmap.PixelSize, new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        bitmap.Render(item);
        using var frame = pixels.Lock(); bitmap.CopyPixels(frame);
        bool IsInk(int x, int y)
        {
            var offset = y * frame.RowBytes + x * 4;
            return Marshal.ReadByte(frame.Address, offset) == 255 && Marshal.ReadByte(frame.Address, offset + 1) == 0
                && Marshal.ReadByte(frame.Address, offset + 2) == 255 && Marshal.ReadByte(frame.Address, offset + 3) == 255;
        }
        var points = (from y in Enumerable.Range(0, frame.Size.Height)
                      from x in Enumerable.Range(0, frame.Size.Width) where IsInk(x, y) select new PixelPoint(x, y)).ToArray();
        Assert.NotEmpty(points);
        var left = points.Min(point => point.X); var right = points.Max(point => point.X);
        var top = points.Min(point => point.Y); var bottom = points.Max(point => point.Y);
        var width = right - left + 1; var height = bottom - top + 1;
        var nearTop = Enumerable.Range(left, width).Count(x => IsInk(x, top + 1));
        // Native 50% CornerSize resolves against the shorter edge: the top retains a straight
        // middle plus circular shoulder ink. An ellipse has only a narrow curved apex.
        Assert.True(nearTop >= width - height + 10, $"Capsule {width}×{height}: second ink row spans {nearTop} pixels.");
    }

    public static MaterialNavigation Create(int recipe) => recipe switch
    {
        0 => new MaterialNavigationBar(),
        1 => new MaterialNavigationBar { ItemLayout = MaterialNavigationItemLayout.Horizontal },
        2 => new MaterialNavigationRail(),
        3 => new MaterialNavigationRail { ItemLayout = MaterialNavigationItemLayout.Horizontal },
        4 => new MaterialNavigationRail { IsExpanded = true },
        5 => new MaterialNavigationRail { IsExpanded = true, ItemLayout = MaterialNavigationItemLayout.Horizontal },
        6 => new MaterialTabs(),
        7 => new MaterialTabs { Layout = MaterialTabLayout.Scrollable },
        8 => new MaterialTabs { Variant = MaterialTabVariant.Secondary },
        9 => new MaterialTabs { Variant = MaterialTabVariant.Secondary, Layout = MaterialTabLayout.Scrollable },
        _ => throw new ArgumentOutOfRangeException(nameof(recipe))
    };

    [AvaloniaTheory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    [InlineData(5)] [InlineData(6)] [InlineData(7)] [InlineData(8)] [InlineData(9)]
    public void Every_recipe_supports_touch_edge_target_mouse_cancel_keyboard_and_disabled_input(int recipe)
    {
        using var host = new NavigationHost(Create(recipe));
        var invocations = 0;
        host.Navigation.ItemInvoked += (_, _) => invocations++;
        Assert.True(host.Second.Bounds.Width >= 48);
        Assert.True(host.Second.Bounds.Height >= 48);
        var edge = host.Second.TranslatePoint(new Point(host.Second.Bounds.Width / 2, 1), host.Window)!.Value;
        using (var contact = host.Window.TouchBegin(edge)) host.Window.TouchEnd(contact, edge);
        Assert.Same(host.Second, host.Navigation.SelectedItem);
        Assert.Equal(1, invocations);
        host.Navigation.SelectedIndex = 0;
        var outside = new Point(host.Window.Bounds.Width - 1, host.Window.Bounds.Height / 2);
        host.Window.MouseDown(host.Center(host.Second), MouseButton.Left);
        host.Window.MouseMove(outside, RawInputModifiers.LeftMouseButton);
        host.Window.MouseUp(outside, MouseButton.Left);
        Assert.Same(host.First, host.Navigation.SelectedItem);
        host.Window.MouseDown(host.Center(host.Second), MouseButton.Right);
        host.Window.MouseUp(host.Center(host.Second), MouseButton.Right);
        Assert.Equal(1, invocations);
        host.Second.Focus(NavigationMethod.Tab);
        host.Key(PhysicalKey.Space);
        Assert.Same(host.Second, host.Navigation.SelectedItem);
        Assert.Equal(2, invocations);
        host.Key(PhysicalKey.Enter);
        Assert.Equal(3, invocations);
        host.Second.IsEnabled = false;
        host.Click(host.Second);
        Assert.Equal(3, invocations);
        Assert.Same(host.Second, host.Navigation.SelectedItem);
        Assert.False(ControlAutomationPeer.CreatePeerForElement(host.Second)!.IsEnabled());
    }

    [AvaloniaTheory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    [InlineData(5)] [InlineData(6)] [InlineData(7)] [InlineData(8)] [InlineData(9)]
    public void Every_recipe_updates_live_theme_and_wrapping_fonts_without_losing_selection_or_focus(int recipe)
    {
        using var host = new NavigationHost(Create(recipe));
        host.Resize(900);
        host.Click(host.Second);
        var light = host.Capture();
        host.Window.RequestedThemeVariant = ThemeVariant.Dark;
        var dark = host.Capture();
        Assert.NotEqual(light, dark);
        var expected = recipe < 6 ? "#CCC2DC" : recipe < 8 ? "#D0BCFF" : "#E6E0E9";
        Assert.Equal(Color.Parse(expected), ((ISolidColorBrush)host.Second.Foreground!).Color);
        host.Second.Content = "收藏与资料 / A very long bilingual collection label for layout adaptation";
        host.Theme.Typography = new MaterialTypography { FontFamily = new FontFamily("Arial"), Scale = 2 };
        var enlarged = host.Capture();
        Assert.NotEqual(dark, enlarged);
        Assert.Equal(new FontFamily("Arial"), host.Second.FontFamily);
        Assert.True(host.Second.FontSize >= 24);
        if (host.Navigation is MaterialNavigationRail rail) rail.IsExpanded = false;
        host.Resize(320);
        Assert.Same(host.Second, host.Navigation.SelectedItem);
        Assert.True(host.Second.IsFocused);
        Assert.InRange(host.Center(host.Second).X, 0, 320);
        Assert.True(host.Second.Bounds.Height >= 48);
        var label = host.Second.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text == (string)host.Second.Content);
        Assert.True(label.Bounds.Width > 0);
        Assert.True(label.Bounds.Height >= label.LineHeight);
        host.Window.RequestedThemeVariant = ThemeVariant.Light;
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = true };
        host.Capture();
        Assert.True(host.Second.TryFindResource("M3.StateLayerDuration", out var duration));
        Assert.Equal(TimeSpan.Zero, duration);
        host.Resize(1100);
        Assert.Same(host.Second.PageContent, host.Navigation.SelectedContent);
        Assert.True(host.Second.IsFocused);
    }

    [AvaloniaTheory]
    [InlineData(0, 80)] [InlineData(1, 64)] [InlineData(2, 64)] [InlineData(3, 56)] [InlineData(4, 64)]
    [InlineData(5, 56)] [InlineData(6, 64)] [InlineData(7, 64)] [InlineData(8, 48)] [InlineData(9, 48)]
    public void Default_public_targets_project_the_pinned_bar_rail_and_tab_geometry(int recipe, double height)
    {
        using var host = new NavigationHost(Create(recipe));
        if (recipe == 3) Assert.True(host.First.Bounds.Height >= height); // A collapsed inline bilingual label wraps and grows.
        else Assert.Equal(height, host.First.Bounds.Height);
        if (recipe is 2 or 3) Assert.Equal(96, host.First.Bounds.Width);
        if (recipe is 4 or 5) Assert.Equal(220, host.First.Bounds.Width);
    }

    [AvaloniaFact]
    public void A_host_control_label_is_presented_as_content_not_converted_to_a_string()
    {
        using var host = new NavigationHost(new MaterialNavigationBar());
        var label = new TextBlock { Text = "Control label 自定义", TextWrapping = TextWrapping.Wrap };
        host.Second.Content = label;
        host.Capture();
        Assert.True(label.IsEffectivelyVisible);
        Assert.True(label.Bounds.Width > 0);
        host.Click(host.Second);
        Assert.Same(host.Second.PageContent, host.Navigation.SelectedContent);
    }

    [AvaloniaFact]
    public void User_sees_hover_keyboard_focus_and_press_feedback_before_activation()
    {
        using var host = new NavigationHost(new MaterialNavigationBar());
        // The pinned default focus/press alphas are equal; distinct host inputs expose each layer recipe.
        host.Theme.States = new MaterialStates { FocusStateLayerOpacity = 0.2, PressedStateLayerOpacity = 0.35 };
        var idle = host.Capture();
        host.Window.MouseMove(host.Center(host.Second));
        var hovered = host.Capture();
        Assert.NotEqual(idle, hovered);
        host.Second.Focus(NavigationMethod.Tab);
        var focused = host.Capture();
        Assert.NotEqual(hovered, focused);
        host.Window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);
        Assert.True(host.Second.IsPressed);
        Assert.Same(host.First, host.Navigation.SelectedItem);
        Assert.NotEqual(focused, host.Capture());
        host.Window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None);
        Assert.Same(host.Second, host.Navigation.SelectedItem);
    }

    [AvaloniaFact]
    public void An_inactive_tab_icon_uses_the_pinned_OnSurface_hover_recipe()
    {
        using var host = new NavigationHost(new MaterialTabs());
        var icon = new TextBlock { Text = "☆" };
        host.Second.Icon = icon;
        host.Capture();
        Assert.Equal(Color.Parse("#49454F"), ((ISolidColorBrush)icon.Foreground!).Color);
        host.Window.MouseMove(host.Center(host.Second));
        host.Capture();
        Assert.Equal(Color.Parse("#1D1B20"), ((ISolidColorBrush)icon.Foreground!).Color);
    }

    [AvaloniaFact]
    public void Public_content_templates_and_two_way_selection_binding_survive_user_input()
    {
        using var host = new NavigationHost(new MaterialTabs());
        var model = new NavigationSelectionModel();
        host.Navigation.Bind(MaterialNavigation.SelectedIndexProperty, new Binding(nameof(model.Index)) { Source = model, Mode = BindingMode.TwoWay });
        var label = new TextBlock { Text = "Custom 资料 label", TextWrapping = TextWrapping.Wrap };
        var page = new TextBlock();
        host.Second.ContentTemplate = new FuncDataTemplate<string>((_, _) => label);
        host.Second.PageContent = "Templated page";
        host.Second.PageContentTemplate = new FuncDataTemplate<string>((text, _) => { page.Text = text; return page; });
        host.Click(host.Second);
        host.Capture();
        Assert.Equal(1, model.Index);
        Assert.True(label.IsEffectivelyVisible);
        Assert.Equal("Templated page", page.Text);
        Assert.True(page.Bounds.Width > 0);
        model.Index = 0;
        Assert.Same(host.First, host.Navigation.SelectedItem);
        host.Second.PageContent = "Updated page";
        host.Second.Template = new FuncControlTemplate<MaterialNavigationItem>((_, _) => new Border { Background = Brushes.Transparent, Child = new TextBlock { Text = "Replacement template" } });
        host.Capture();
        host.Click(host.Second);
        host.Capture();
        Assert.Equal(1, model.Index);
        Assert.Equal("Updated page", page.Text);
        Assert.Same(host.Second.PageContentTemplate, host.Navigation.SelectedContentTemplate);
    }

    [AvaloniaFact]
    public void Rail_uses_up_down_and_horizontal_headers_follow_RTL_arrows_with_page_tab_navigation()
    {
        using var rail = new NavigationHost(new MaterialNavigationRail());
        rail.First.Focus(NavigationMethod.Tab);
        rail.Key(PhysicalKey.ArrowDown);
        Assert.True(rail.Second.IsFocused);
        Assert.Same(rail.Second, rail.Navigation.SelectedItem);
        using var tabs = new NavigationHost(new MaterialTabs { FlowDirection = FlowDirection.RightToLeft });
        tabs.First.Focus(NavigationMethod.Tab);
        tabs.Key(PhysicalKey.ArrowLeft);
        Assert.True(tabs.Second.IsFocused);
        var pageAction = new MaterialButton { Content = "Page action" };
        tabs.Second.PageContent = pageAction;
        tabs.Capture();
        tabs.Key(PhysicalKey.Tab);
        Assert.True(pageAction.IsFocused);
        tabs.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.Shift);
        tabs.Window.KeyReleaseQwerty(PhysicalKey.Tab, RawInputModifiers.Shift);
        Assert.True(tabs.Second.IsFocused);
    }

    [AvaloniaFact]
    public void Touch_drag_in_scrollable_tabs_scrolls_without_invoking_the_dragged_item()
    {
        using var host = new NavigationHost(new MaterialTabs { Layout = MaterialTabLayout.Scrollable });
        for (var i = 0; i < 8; i++) host.Navigation.Items.Add(new MaterialNavigationItem { Content = $"Collection {i}" });
        host.Resize(320);
        var invocations = 0;
        host.Navigation.ItemInvoked += (_, _) => invocations++;
        var start = host.Center(host.Second);
        using var contact = host.Window.TouchBegin(start);
        host.Window.TouchMove(contact, start + new Vector(-100, 0));
        host.Window.TouchEnd(contact, start + new Vector(-100, 0));
        Assert.Equal(0, invocations);
        Assert.Same(host.First, host.Navigation.SelectedItem);
        Assert.True(host.Navigation.GetVisualDescendants().OfType<ScrollViewer>().Single().Offset.X > 0);
    }
}

internal sealed class NavigationSelectionModel : AvaloniaObject
{
    public static readonly StyledProperty<int> IndexProperty = AvaloniaProperty.Register<NavigationSelectionModel, int>(nameof(Index));
    public int Index { get => GetValue(IndexProperty); set => SetValue(IndexProperty, value); }
}
