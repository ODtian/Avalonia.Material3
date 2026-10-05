using System.ComponentModel;
using System.Windows.Input;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Material3.Tests;

public class FloatingActionContractTests
{
    [AvaloniaTheory]
    [InlineData(MaterialFabSize.Standard, 56, 24, 12, 14, 16)]
    [InlineData(MaterialFabSize.Small, 56, 24, 8, 16, 16)]
    [InlineData(MaterialFabSize.Medium, 80, 28, 12, 22, 20)]
    [InlineData(MaterialFabSize.Large, 96, 32, 16, 24, 28)]
    public void Extended_size_recipes_use_pinned_geometry_and_live_theme_metrics(MaterialFabSize size, double container, double icon, double gap, double font, double corner)
    {
        using var host = new ButtonHost();
        var fab = new MaterialExtendedFab { Size = size, Icon = "+", Content = "Compose" };
        host.Window.Content = new StackPanel { Children = { fab } };
        host.Capture();
        Assert.Equal(container, fab.ContainerSize);
        Assert.Equal(icon, fab.IconSize);
        Assert.Equal(gap, fab.IconSpacing);
        Assert.Equal(font, fab.FontSize);
        Assert.Equal(new CornerRadius(corner), fab.CornerRadius);
        host.Theme.Typography = host.Theme.Typography with { Scale = 2 };
        host.Capture();
        Assert.Equal(font * 2, fab.FontSize);
    }

    [AvaloniaTheory]
    [InlineData(MaterialFabSize.Standard)]
    [InlineData(MaterialFabSize.Small)]
    [InlineData(MaterialFabSize.Medium)]
    [InlineData(MaterialFabSize.Large)]
    public void Every_fab_size_has_edge_touch_cancel_keyboard_visible_focus_and_disabled_Invoke(MaterialFabSize size)
    {
        using var host = new ButtonHost();
        var fab = new MaterialFab { Size = size, Content = "+" };
        var count = 0;
        fab.Click += (_, _) => count++;
        host.Window.Content = new StackPanel { Margin = new Thickness(24), Children = { fab } };
        var normal = host.Capture();
        fab.Focus(NavigationMethod.Tab);
        Assert.NotEqual(normal, host.Capture());
        var edge = fab.TranslatePoint(new Point(1, fab.Bounds.Height / 2), host.Window)!.Value;
        using var contact = host.Window.TouchBegin(edge);
        Assert.True(fab.IsPressed);
        Assert.NotEqual(normal, host.Capture());
        host.Window.TouchEnd(contact, edge);
        Assert.Equal(1, count);
        host.Window.MouseDown(edge, MouseButton.Left);
        host.Window.MouseMove(new Point(300, 210));
        host.Window.MouseUp(new Point(300, 210), MouseButton.Left);
        Assert.Equal(1, count);
        fab.Focus(NavigationMethod.Tab);
        host.Window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);
        Assert.True(fab.IsPressed);
        host.Window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None);
        host.Window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Assert.Equal(3, count);
        var peer = ControlAutomationPeer.CreatePeerForElement(fab)!;
        fab.IsEnabled = false;
        Assert.False(peer.IsEnabled());
        Assert.Throws<ElementNotEnabledException>(() => peer.GetProvider<IInvokeProvider>()!.Invoke());
        using var disabledContact = host.Window.TouchBegin(edge);
        host.Window.TouchEnd(disabledContact, edge);
        Assert.Equal(3, count);
    }

    [AvaloniaFact]
    public void Existing_fab_menu_item_and_toolbar_follow_full_semantic_mode_and_host_shape_inputs()
    {
        using var host = new ButtonHost();
        var fab = new MaterialFab { Content = "+" };
        var item = new MaterialFabMenuItem { Content = "Create" };
        var toolbar = new MaterialToolbar { Color = MaterialToolbarColor.Standard };
        toolbar.Items.Add(new MaterialIconButton { Content = "★" });
        host.Window.Content = new StackPanel { Children = { fab, item, toolbar } };
        host.Capture();
        Assert.Equal(Color.Parse("#EADDFF"), ((ISolidColorBrush)fab.Background!).Color);
        host.Window.RequestedThemeVariant = ThemeVariant.Dark;
        host.Capture();
        Assert.Equal(Color.Parse("#4F378B"), ((ISolidColorBrush)fab.Background!).Color);
        Assert.Equal(Color.Parse("#EADDFF"), ((ISolidColorBrush)item.Foreground!).Color);
        Assert.Equal(Color.Parse("#211F26"), ((ISolidColorBrush)toolbar.Background!).Color);
        host.Theme.Shapes = host.Theme.Shapes with { CornerLarge = 7 };
        host.Capture();
        Assert.Equal(new CornerRadius(7), fab.CornerRadius);
        host.Theme.DynamicColors = new(host.Theme.LightColorScheme with { PrimaryContainer = Colors.Green }, host.Theme.DarkColorScheme with { PrimaryContainer = Colors.Red });
        host.Capture();
        Assert.Equal(Colors.Red, ((ISolidColorBrush)fab.Background!).Color);
        Assert.Equal(Colors.Red, ((ISolidColorBrush)item.Background!).Color);
        host.Window.RequestedThemeVariant = ThemeVariant.Light;
        host.Capture();
        Assert.Equal(Colors.Green, ((ISolidColorBrush)fab.Background!).Color);
    }

    [AvaloniaFact]
    public void Host_templates_and_command_parameters_work_and_menu_command_observes_collapsed_state()
    {
        using var host = new ButtonHost();
        var menu = new MaterialFabMenu();
        var label = new TextBlock { Text = "Template label" };
        var icon = new TextBlock { Text = "Custom icon" };
        var item = new MaterialFabMenuItem
        {
            Content = new object(), LeadingIcon = new object(),
            ContentTemplate = new FuncDataTemplate<object>((_, _) => label),
            LeadingIconTemplate = new FuncDataTemplate<object>((_, _) => icon),
            CommandParameter = "document"
        };
        object? received = null;
        item.Command = new ActionCommand(value => { Assert.False(menu.IsExpanded); received = value; });
        menu.Items.Add(item);
        host.Window.Content = new Grid { Children = { menu } };
        menu.IsExpanded = true;
        host.Capture();
        Assert.True(label.IsEffectivelyVisible && icon.IsEffectivelyVisible);
        var point = item.TranslatePoint(new Point(item.Bounds.Width / 2, item.Bounds.Height / 2), host.Window)!.Value;
        host.Window.MouseDown(point, MouseButton.Left);
        host.Window.MouseUp(point, MouseButton.Left);
        Assert.Equal("document", received);
        var custom = new MaterialFab { Content = "Custom", Template = new FuncControlTemplate<MaterialFab>((_, _) => new Border { Background = Brushes.Red, Width = 64, Height = 64 }) };
        custom.Command = new ActionCommand(value => received = value);
        custom.CommandParameter = "custom";
        host.Window.Content = custom;
        host.Capture();
        ControlAutomationPeer.CreatePeerForElement(custom)!.GetProvider<IInvokeProvider>()!.Invoke();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Assert.Equal("custom", received);
    }

    [AvaloniaFact]
    public void TwoWay_expansion_binding_survives_pointer_provider_and_keyboard_changes()
    {
        using var host = new ButtonHost();
        var model = new ExpansionModel();
        var toolbar = new MaterialToolbar();
        toolbar.Bind(MaterialToolbar.IsExpandedProperty, new Binding(nameof(ExpansionModel.Expanded)) { Source = model, Mode = BindingMode.TwoWay });
        toolbar.Items.Add(new MaterialIconButton { Content = "★" });
        toolbar.LeadingItems.Add(new MaterialIconButton { Content = "+" });
        host.Window.Content = toolbar;
        host.Capture();
        var toggle = toolbar.GetVisualDescendants().OfType<MaterialFab>().Single();
        var point = toggle.TranslatePoint(new Point(toggle.Bounds.Width / 2, toggle.Bounds.Height / 2), host.Window)!.Value;
        host.Window.MouseDown(point, MouseButton.Left);
        host.Window.MouseUp(point, MouseButton.Left);
        Assert.False(model.Expanded);
        model.Expanded = true;
        host.Capture();
        Assert.True(toolbar.IsExpanded);
        ControlAutomationPeer.CreatePeerForElement(toolbar)!.GetProvider<IExpandCollapseProvider>()!.Collapse();
        Assert.False(model.Expanded);
        model.Expanded = true;
        toggle.Focus(NavigationMethod.Tab);
        host.Window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        Assert.False(model.Expanded);
        model.Expanded = true;
        Assert.True(toolbar.IsExpanded);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Toolbar_directional_navigation_and_FAB_position_follow_orientation_and_RTL(bool vertical)
    {
        using var host = new ButtonHost();
        host.Window.Width = 600;
        host.Window.Height = 600;
        var toolbar = new MaterialToolbar { Orientation = vertical ? Avalonia.Layout.Orientation.Vertical : Avalonia.Layout.Orientation.Horizontal, FlowDirection = FlowDirection.RightToLeft, FloatingActionPosition = MaterialToolbarFabPosition.Start, FloatingAction = new MaterialFab { Content = "+" } };
        var first = new MaterialIconButton { Content = "A" };
        var disabled = new MaterialIconButton { Content = "B", IsEnabled = false };
        var last = new MaterialIconButton { Content = "C" };
        toolbar.Items.AddRange(new Control[] { first, disabled, last });
        host.Window.Content = new Grid { Children = { toolbar } };
        host.Capture();
        first.Focus(NavigationMethod.Tab);
        host.Window.KeyPressQwerty(vertical ? PhysicalKey.ArrowDown : PhysicalKey.ArrowLeft, RawInputModifiers.None);
        Assert.True(last.IsFocused);
        host.Window.KeyPressQwerty(PhysicalKey.Home, RawInputModifiers.None);
        Assert.True(first.IsFocused);
        var fab = toolbar.FloatingAction;
        var fabCenter = fab.TranslatePoint(new Point(fab.Bounds.Width / 2, fab.Bounds.Height / 2), host.Window)!.Value;
        var firstCenter = first.TranslatePoint(new Point(first.Bounds.Width / 2, first.Bounds.Height / 2), host.Window)!.Value;
        Assert.True(vertical ? fabCenter.Y < firstCenter.Y : fabCenter.X > firstCenter.X);
    }

    [AvaloniaFact]
    public void Menu_Tab_can_leave_without_trapping_focus_and_detach_dismisses_without_stealing_host_focus()
    {
        using var host = new ButtonHost();
        var menu = new MaterialFabMenu();
        var item = new MaterialFabMenuItem { Content = "Create" };
        menu.Items.Add(item);
        var outside = new MaterialButton { Content = "Other host action" };
        var grid = new Grid { Children = { menu, outside } };
        host.Window.Content = grid;
        host.Capture();
        menu.IsExpanded = true;
        host.Capture();
        Assert.True(item.IsFocused);
        host.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        host.Window.KeyReleaseQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        host.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        host.Capture();
        Assert.True(outside.IsFocused);
        Assert.False(menu.IsExpanded);
        menu.IsExpanded = true;
        host.Capture();
        grid.Children.Remove(menu);
        Assert.False(menu.IsExpanded);
        outside.Focus(NavigationMethod.Tab);
        host.Window.MouseDown(new Point(310, 230), MouseButton.Left);
        Assert.True(outside.IsFocused);
    }

    [AvaloniaFact]
    public void Invalid_public_enum_and_accessible_label_inputs_are_rejected()
    {
        Assert.Throws<ArgumentException>(() => new MaterialFab { Size = (MaterialFabSize)99 });
        Assert.Throws<ArgumentException>(() => new MaterialFabMenu { Anchor = (MaterialActionAnchor)99 });
        Assert.Throws<ArgumentException>(() => new MaterialFabMenu { ExpandLabel = " " });
        Assert.Throws<ArgumentException>(() => new MaterialToolbar { Variant = (MaterialToolbarVariant)99 });
        Assert.Throws<ArgumentException>(() => new MaterialToolbar { Orientation = (Avalonia.Layout.Orientation)99 });
        Assert.Throws<ArgumentException>(() => new MaterialToolbar { Color = (MaterialToolbarColor)99 });
        Assert.Throws<ArgumentException>(() => new MaterialToolbar { FloatingActionPosition = (MaterialToolbarFabPosition)99 });
        Assert.Throws<ArgumentException>(() => new MaterialToolbar { CollapseLabel = "" });
    }

    private sealed class ActionCommand(Action<object?> execute) : ICommand
    {
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute(parameter);
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
    private sealed class ExpansionModel : INotifyPropertyChanged
    {
        private bool _expanded = true;
        public bool Expanded { get => _expanded; set { _expanded = value; PropertyChanged?.Invoke(this, new(nameof(Expanded))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
