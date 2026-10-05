using System.ComponentModel;
using System.Windows.Input;
using Avalonia.Data;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Material3.Controls;
using Avalonia.Media;
using Avalonia.Input;
using Xunit;

namespace Avalonia.Material3.Tests;

public class ExpressiveButtonScenarioTests
{
    [AvaloniaTheory]
    [InlineData(MaterialButtonVariant.Filled, "#D0BCFF", "#381E72")]
    [InlineData(MaterialButtonVariant.Tonal, "#4A4458", "#E8DEF8")]
    [InlineData(MaterialButtonVariant.Elevated, "#1D1B20", "#D0BCFF")]
    [InlineData(MaterialButtonVariant.Outlined, "#00FFFFFF", "#CAC4D0")]
    [InlineData(MaterialButtonVariant.Text, "#00FFFFFF", "#D0BCFF")]
    public void Every_action_recipe_updates_existing_content_when_the_host_changes_light_dark_light(
        MaterialButtonVariant variant, string background, string foreground)
    {
        using var host = new ButtonHost();
        host.Button.Variant = variant;
        var light = host.Capture();
        host.Window.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
        Assert.NotEqual(light, host.Capture());
        Assert.Equal(Color.Parse(background), Assert.IsAssignableFrom<ISolidColorBrush>(host.Button.Background).Color);
        Assert.Equal(Color.Parse(foreground), Assert.IsAssignableFrom<ISolidColorBrush>(host.Button.Foreground).Color);
        host.Window.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light;
        Assert.Equal(light, host.Capture());
    }

    [AvaloniaFact]
    public void Host_semantic_resources_override_component_fallbacks_and_update_existing_tonal_buttons()
    {
        using var host = new ButtonHost();
        host.Button.Variant = MaterialButtonVariant.Tonal;
        host.Theme.Resources["M3.SecondaryContainerBrush"] = new SolidColorBrush(Color.Parse("#CCEEDD"));
        host.Theme.Resources["M3.OnSecondaryContainerBrush"] = new SolidColorBrush(Color.Parse("#003322"));
        host.Capture();
        Assert.Equal(Color.Parse("#CCEEDD"), Assert.IsAssignableFrom<ISolidColorBrush>(host.Button.Background).Color);
        Assert.Equal(Color.Parse("#003322"), Assert.IsAssignableFrom<ISolidColorBrush>(host.Button.Foreground).Color);
        host.Theme.Resources["M3.SecondaryContainerBrush"] = new SolidColorBrush(Color.Parse("#DDEEFF"));
        host.Capture();
        Assert.Equal(Color.Parse("#DDEEFF"), Assert.IsAssignableFrom<ISolidColorBrush>(host.Button.Background).Color);
    }

    [AvaloniaFact]
    public void Custom_template_keeps_real_input_selection_command_and_automation_contracts()
    {
        using var host = new ButtonHost();
        host.Button.IsToggle = true;
        host.Button.Template = new FuncControlTemplate<MaterialButton>((_, _) => new Border
        {
            Background = Brushes.Navy, Padding = new Thickness(20), Child = new TextBlock { Text = "Custom action", Foreground = Brushes.White }
        });
        host.Capture();
        host.Window.MouseDown(host.Center, MouseButton.Left);
        host.Window.MouseUp(host.Center, MouseButton.Left);
        Assert.True(host.Button.IsChecked);
        Assert.Equal("Action completed", host.Result.Text);
        var peer = ControlAutomationPeer.CreatePeerForElement(host.Button)!;
        Assert.Equal(ToggleState.On, peer.GetProvider<IToggleProvider>()!.ToggleState);
    }

    [AvaloniaFact]
    public void Native_automation_can_query_provider_availability_without_accessing_the_UI_thread()
    {
        using var host = new ButtonHost();
        var peer = ControlAutomationPeer.CreatePeerForElement(host.Button)!;
        Assert.Null(Task.Run(() => peer.GetProvider<IToggleProvider>()).GetAwaiter().GetResult());
        host.Button.IsToggle = true;
        Assert.NotNull(Task.Run(() => peer.GetProvider<IToggleProvider>()).GetAwaiter().GetResult());
        host.Button.IsToggle = false;
        Assert.Null(Task.Run(() => peer.GetProvider<IToggleProvider>()).GetAwaiter().GetResult());
    }

    [AvaloniaFact]
    public void Icon_button_does_not_inherit_the_action_buttons_hover_elevation()
    {
        using var host = new ButtonHost();
        var button = new MaterialIconButton { Content = "★", IconVariant = MaterialIconButtonVariant.Filled };
        host.Window.Content = new StackPanel { Margin = new Thickness(24), Children = { button } };
        host.Capture();
        var center = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), host.Window)!.Value;
        host.Window.MouseMove(center);
        var belowContainer = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height - 3), host.Window)!.Value;
        Assert.Equal(Color.Parse("#FEF7FF"), host.PixelAt(belowContainer));
    }

    [AvaloniaFact]
    public void Extra_small_visual_container_is_32_units_inside_the_larger_input_target()
    {
        using var host = new ButtonHost();
        host.Button.Size = MaterialButtonSize.ExtraSmall;
        host.Button.Content = string.Empty;
        host.Capture();
        var outsideVisual = host.Button.TranslatePoint(new Point(host.Button.Bounds.Width / 2, host.Button.Bounds.Height / 2 - 17), host.Window)!.Value;
        var insideVisual = host.Button.TranslatePoint(new Point(host.Button.Bounds.Width / 2, host.Button.Bounds.Height / 2 - 15), host.Window)!.Value;
        Assert.Equal(Color.Parse("#FEF7FF"), host.PixelAt(outsideVisual));
        Assert.Equal(Color.Parse("#6750A4"), host.PixelAt(insideVisual));
        using var contact = host.Window.TouchBegin(outsideVisual);
        host.Window.TouchEnd(contact, outsideVisual);
        Assert.Equal("Action completed", host.Result.Text);
    }

    [AvaloniaTheory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(false, 3)]
    [InlineData(false, 4)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    [InlineData(true, 3)]
    public void Every_recipe_preserves_focus_cancel_touch_keyboard_command_and_disabled_semantics(bool icon, int variant)
    {
        using var host = new ButtonHost();
        MaterialButton button = icon
            ? new MaterialIconButton { IconVariant = (MaterialIconButtonVariant)variant, Content = "★" }
            : new MaterialButton { Variant = (MaterialButtonVariant)variant, Content = "提交 / Continue" };
        button.IsToggle = true;
        host.Window.Content = new StackPanel { Margin = new Thickness(24), Children = { button } };
        var count = 0;
        var command = new ActionCommand(parameter => { Assert.Equal("accepted", parameter); count++; });
        button.Command = command;
        button.CommandParameter = "accepted";
        host.Capture();
        var idle = host.Capture();
        host.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        host.Window.KeyReleaseQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        Assert.True(button.IsFocused);
        Assert.NotEqual(idle, host.Capture());
        var center = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), host.Window)!.Value;
        host.Window.MouseMove(center);
        var hover = host.Capture();
        host.Window.MouseDown(center, MouseButton.Left);
        Assert.True(button.IsPressed);
        Assert.NotEqual(hover, host.Capture());
        host.Window.MouseMove(new Point(310, 230), RawInputModifiers.LeftMouseButton);
        host.Window.MouseUp(new Point(310, 230), MouseButton.Left);
        Assert.False(button.IsPressed);
        Assert.False(button.IsChecked);
        Assert.Equal(0, count);
        var edge = button.TranslatePoint(new Point(button.Bounds.Width / 2, 1), host.Window)!.Value;
        using var contact = host.Window.TouchBegin(edge);
        host.Window.TouchEnd(contact, edge);
        Assert.True(button.IsChecked);
        Assert.Equal(1, count);
        button.Focus();
        host.Window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);
        Assert.Equal(1, count);
        host.Window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None);
        Assert.False(button.IsChecked);
        Assert.Equal(2, count);
        host.Window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        host.Window.KeyReleaseQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Assert.True(button.IsChecked);
        Assert.Equal(3, count);
        var selected = host.Capture();
        command.Allowed = false;
        Assert.False(button.IsEffectivelyEnabled);
        Assert.NotEqual(selected, host.Capture());
        host.Window.MouseDown(center, MouseButton.Left);
        host.Window.MouseUp(center, MouseButton.Left);
        host.Window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        var peer = ControlAutomationPeer.CreatePeerForElement(button)!;
        Assert.False(peer.IsEnabled());
        Assert.Throws<ElementNotEnabledException>(() => peer.GetProvider<IToggleProvider>()!.Toggle());
        Assert.Equal(3, count);
        Assert.True(button.IsChecked);
    }

    [AvaloniaFact]
    public void Selection_binding_is_two_way_and_programmatic_state_does_not_activate_the_action()
    {
        using var host = new ButtonHost();
        var selection = new Selection();
        host.Button.IsToggle = true;
        host.Button.Bind(MaterialButton.IsCheckedProperty, new Binding(nameof(Selection.Selected)) { Source = selection });
        selection.Selected = true;
        Assert.True(host.Button.IsChecked);
        Assert.Equal("Waiting", host.Result.Text);
        host.Window.MouseDown(host.Center, MouseButton.Left);
        host.Window.MouseUp(host.Center, MouseButton.Left);
        Assert.False(selection.Selected);
        selection.Selected = true;
        Assert.True(host.Button.IsChecked);
    }

    [AvaloniaTheory]
    [InlineData(MaterialIconButtonVariant.Standard, "#00FFFFFF", "#49454F", "#00FFFFFF", "#6750A4", "#D0BCFF")]
    [InlineData(MaterialIconButtonVariant.Filled, "#6750A4", "#FFFFFF", "#6750A4", "#FFFFFF", "#381E72")]
    [InlineData(MaterialIconButtonVariant.Tonal, "#E8DEF8", "#1D192B", "#625B71", "#FFFFFF", "#332D41")]
    [InlineData(MaterialIconButtonVariant.Outlined, "#00FFFFFF", "#49454F", "#322F35", "#F5EFF7", "#322F35")]
    public void Icon_variants_have_distinct_pinned_recipes_in_light_dark_and_selected_states(
        MaterialIconButtonVariant variant, string background, string foreground, string selectedBackground, string selectedForeground, string darkForeground)
    {
        using var host = new ButtonHost();
        var button = new MaterialIconButton { Content = "★", IconVariant = variant };
        host.Window.Content = button;
        host.Capture();
        Assert.Equal(Color.Parse(background), Assert.IsAssignableFrom<ISolidColorBrush>(button.Background).Color);
        Assert.Equal(Color.Parse(foreground), Assert.IsAssignableFrom<ISolidColorBrush>(button.Foreground).Color);
        button.IsToggle = true;
        button.IsChecked = true;
        Assert.Equal(Color.Parse(selectedBackground), Assert.IsAssignableFrom<ISolidColorBrush>(button.Background).Color);
        Assert.Equal(Color.Parse(selectedForeground), Assert.IsAssignableFrom<ISolidColorBrush>(button.Foreground).Color);
        host.Window.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
        host.Capture();
        Assert.Equal(Color.Parse(darkForeground), Assert.IsAssignableFrom<ISolidColorBrush>(button.Foreground).Color);
        button.IsEnabled = false;
        host.Capture();
        Assert.Equal(Color.Parse("#E6E0E9"), Assert.IsAssignableFrom<ISolidColorBrush>(button.Foreground).Color);
    }

    [AvaloniaTheory]
    [InlineData(MaterialButtonSize.ExtraSmall, MaterialIconButtonWidth.Narrow, 28, 12)]
    [InlineData(MaterialButtonSize.ExtraSmall, MaterialIconButtonWidth.Default, 32, 12)]
    [InlineData(MaterialButtonSize.ExtraSmall, MaterialIconButtonWidth.Wide, 40, 12)]
    [InlineData(MaterialButtonSize.Small, MaterialIconButtonWidth.Narrow, 32, 12)]
    [InlineData(MaterialButtonSize.Small, MaterialIconButtonWidth.Default, 40, 12)]
    [InlineData(MaterialButtonSize.Small, MaterialIconButtonWidth.Wide, 52, 12)]
    [InlineData(MaterialButtonSize.Medium, MaterialIconButtonWidth.Narrow, 48, 16)]
    [InlineData(MaterialButtonSize.Medium, MaterialIconButtonWidth.Default, 56, 16)]
    [InlineData(MaterialButtonSize.Medium, MaterialIconButtonWidth.Wide, 72, 16)]
    [InlineData(MaterialButtonSize.Large, MaterialIconButtonWidth.Narrow, 64, 28)]
    [InlineData(MaterialButtonSize.Large, MaterialIconButtonWidth.Default, 96, 28)]
    [InlineData(MaterialButtonSize.Large, MaterialIconButtonWidth.Wide, 128, 28)]
    [InlineData(MaterialButtonSize.ExtraLarge, MaterialIconButtonWidth.Narrow, 104, 28)]
    [InlineData(MaterialButtonSize.ExtraLarge, MaterialIconButtonWidth.Default, 136, 28)]
    [InlineData(MaterialButtonSize.ExtraLarge, MaterialIconButtonWidth.Wide, 184, 28)]
    public void Icon_widths_and_selected_shapes_follow_pinned_tokens(
        MaterialButtonSize size, MaterialIconButtonWidth width, double containerWidth, double squareRadius)
    {
        using var host = new ButtonHost();
        var button = new MaterialIconButton { Content = "★", Size = size, WidthMode = width, IsToggle = true };
        host.Window.Content = button;
        host.Capture();
        Assert.Equal(containerWidth, button.ContainerWidth);
        Assert.True(button.Bounds.Width >= Math.Max(48, containerWidth + 10));
        button.IsChecked = true;
        Assert.Equal(new CornerRadius(squareRadius), button.CornerRadius);
        button.Shape = MaterialButtonShape.Square;
        Assert.Equal(new CornerRadius(button.ContainerHeight / 2), button.CornerRadius);
        button.IsChecked = false;
        Assert.Equal(new CornerRadius(squareRadius), button.CornerRadius);
        button.WidthMode = MaterialIconButtonWidth.Default;
        host.Capture();
        Assert.Equal(button.ContainerHeight, button.ContainerWidth);
    }

    [AvaloniaFact]
    public void Icon_button_mouse_touch_keyboard_and_automation_share_one_selection_action()
    {
        using var host = new ButtonHost();
        var button = new MaterialIconButton { Content = "★", IsToggle = true };
        AutomationProperties.SetName(button, "Favorite");
        ((StackPanel)host.Window.Content!).Children.Clear();
        host.Window.Content = new StackPanel { Margin = new Thickness(24), Children = { button, host.Result } };
        var count = 0;
        button.Click += (_, _) => host.Result.Text = $"{++count}: {button.IsChecked}";
        host.Capture();
        var center = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), host.Window)!.Value;
        var idle = host.Capture();
        host.Window.MouseDown(center, MouseButton.Left);
        Assert.True(button.IsPressed);
        Assert.NotEqual(idle, host.Capture());
        host.Window.MouseUp(center, MouseButton.Left);
        Assert.Equal("1: True", host.Result.Text);
        using var contact = host.Window.TouchBegin(center);
        host.Window.TouchEnd(contact, center);
        Assert.Equal("2: False", host.Result.Text);
        button.Focus();
        host.Window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);
        Assert.False(button.IsChecked);
        host.Window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None);
        Assert.Equal("3: True", host.Result.Text);
        var peer = ControlAutomationPeer.CreatePeerForElement(button)!;
        Assert.Equal("Favorite", peer.GetName());
        peer.GetProvider<IToggleProvider>()!.Toggle();
        Assert.Equal("4: False", host.Result.Text);
        Assert.True(button.Bounds.Width >= 48 && button.Bounds.Height >= 48);
    }

    [AvaloniaFact]
    public void Leading_trailing_and_label_templates_remain_visible_and_share_one_action()
    {
        using var host = new ButtonHost();
        var leading = new TextBlock { Text = "+" };
        var trailing = new TextBlock { Text = "→" };
        var label = new TextBlock { Text = "提交 / Continue", TextWrapping = TextWrapping.Wrap };
        host.Button.LeadingIcon = "leading";
        host.Button.LeadingIconTemplate = new FuncDataTemplate<string>((_, _) => leading);
        host.Button.TrailingIcon = "trailing";
        host.Button.TrailingIconTemplate = new FuncDataTemplate<string>((_, _) => trailing);
        host.Button.ContentTemplate = new FuncDataTemplate<string>((_, _) => label);
        host.Capture();
        Assert.True(leading.IsEffectivelyVisible && leading.Bounds.Width > 0);
        Assert.True(trailing.IsEffectivelyVisible && trailing.Bounds.Width > 0);
        Assert.True(label.IsEffectivelyVisible && label.Bounds.Width > 0);
        host.Window.MouseDown(host.Center, MouseButton.Left);
        host.Window.MouseUp(host.Center, MouseButton.Left);
        Assert.Equal("Action completed", host.Result.Text);
    }

    [AvaloniaTheory]
    [InlineData(MaterialButtonSize.ExtraSmall, 12, 14)]
    [InlineData(MaterialButtonSize.Small, 12, 14)]
    [InlineData(MaterialButtonSize.Medium, 16, 16)]
    [InlineData(MaterialButtonSize.Large, 28, 24)]
    [InlineData(MaterialButtonSize.ExtraLarge, 28, 32)]
    public void Square_buttons_keep_the_pinned_shape_and_size_typography_when_selected(
        MaterialButtonSize size, double radius, double fontSize)
    {
        using var host = new ButtonHost();
        host.Button.Size = size;
        host.Button.Shape = MaterialButtonShape.Square;
        host.Button.IsToggle = true;
        host.Button.IsChecked = true;
        host.Capture();
        Assert.Equal(new CornerRadius(radius), host.Button.CornerRadius);
        Assert.Equal(fontSize, host.Button.FontSize);
        Assert.Equal(size is MaterialButtonSize.Large or MaterialButtonSize.ExtraLarge ? FontWeight.Normal : FontWeight.Medium, host.Button.FontWeight);
        var tracking = size switch { MaterialButtonSize.Medium => 0.2, MaterialButtonSize.Large or MaterialButtonSize.ExtraLarge => 0, _ => 0.1 };
        var lineHeight = size switch { MaterialButtonSize.Medium => 24, MaterialButtonSize.Large => 32, MaterialButtonSize.ExtraLarge => 40, _ => 20 };
        Assert.Equal(tracking, host.Button.LetterSpacing);
        Assert.Equal(lineHeight, TextBlock.GetLineHeight(host.Button));
        host.Theme.Typography = new Avalonia.Material3.Tokens.MaterialTypography { Scale = 2 };
        host.Capture();
        Assert.Equal(fontSize * 2, host.Button.FontSize);
        Assert.Equal(tracking * 2, host.Button.LetterSpacing);
        Assert.Equal(lineHeight * 2, TextBlock.GetLineHeight(host.Button));
    }

    [AvaloniaTheory]
    [InlineData(MaterialButtonVariant.Filled, "#6750A4", "#FFFFFF", "#6750A4", "#FFFFFF")]
    [InlineData(MaterialButtonVariant.Tonal, "#E8DEF8", "#1D192B", "#625B71", "#FFFFFF")]
    [InlineData(MaterialButtonVariant.Elevated, "#F7F2FA", "#6750A4", "#6750A4", "#FFFFFF")]
    [InlineData(MaterialButtonVariant.Outlined, "#00FFFFFF", "#49454F", "#322F35", "#F5EFF7")]
    [InlineData(MaterialButtonVariant.Text, "#00FFFFFF", "#6750A4", "#00FFFFFF", "#6750A4")]
    public void Button_variants_follow_pinned_color_roles_and_toggle_selection(
        MaterialButtonVariant variant, string background, string foreground, string selectedBackground, string selectedForeground)
    {
        using var host = new ButtonHost();
        host.Button.Variant = variant;
        host.Capture();
        Assert.Equal(Color.Parse(background), Assert.IsAssignableFrom<ISolidColorBrush>(host.Button.Background).Color);
        Assert.Equal(Color.Parse(foreground), Assert.IsAssignableFrom<ISolidColorBrush>(host.Button.Foreground).Color);
        host.Button.IsToggle = true;
        host.Window.MouseDown(host.Center, MouseButton.Left);
        host.Window.MouseUp(host.Center, MouseButton.Left);
        Assert.Equal(Color.Parse(selectedBackground), Assert.IsAssignableFrom<ISolidColorBrush>(host.Button.Background).Color);
        Assert.Equal(Color.Parse(selectedForeground), Assert.IsAssignableFrom<ISolidColorBrush>(host.Button.Foreground).Color);
    }

    [AvaloniaTheory]
    [InlineData(MaterialButtonSize.ExtraSmall, 32, 20, 8, 16, 16)]
    [InlineData(MaterialButtonSize.Small, 40, 20, 8, 16, 20)]
    [InlineData(MaterialButtonSize.Medium, 56, 24, 8, 24, 28)]
    [InlineData(MaterialButtonSize.Large, 96, 32, 12, 48, 48)]
    [InlineData(MaterialButtonSize.ExtraLarge, 136, 40, 16, 64, 68)]
    public void Button_sizes_use_pinned_geometry_and_preserve_the_touch_target(
        MaterialButtonSize size, double height, double icon, double gap, double padding, double radius)
    {
        using var host = new ButtonHost();
        host.Button.Size = size;
        host.Capture();
        Assert.Equal(height, host.Button.ContainerHeight);
        Assert.Equal(icon, host.Button.IconSize);
        Assert.Equal(gap, host.Button.IconSpacing);
        Assert.Equal(padding, host.Button.Padding.Left);
        Assert.Equal(new CornerRadius(radius), host.Button.CornerRadius);
        Assert.True(host.Button.Bounds.Height >= Math.Max(48, height + 10));
        host.Window.MouseDown(host.Center, MouseButton.Left);
        Assert.Equal(new CornerRadius(size switch { MaterialButtonSize.Medium => 12, MaterialButtonSize.Large or MaterialButtonSize.ExtraLarge => 16, _ => 8 }), host.Button.CornerRadius);
        host.Window.MouseUp(host.Center, MouseButton.Left);
    }

    [AvaloniaFact]
    public void Automation_toggle_exposes_live_state_and_runs_the_same_action()
    {
        using var host = new ButtonHost();
        AutomationProperties.SetName(host.Button, "Pin item");
        var peer = ControlAutomationPeer.CreatePeerForElement(host.Button)!;
        Assert.Null(peer.GetProvider<IToggleProvider>());
        host.Button.IsToggle = true;
        var toggle = Assert.IsAssignableFrom<IToggleProvider>(peer.GetProvider<IToggleProvider>());
        Assert.Equal("Pin item", peer.GetName());
        Assert.Equal(AutomationControlType.Button, peer.GetAutomationControlType());
        Assert.Equal(ToggleState.Off, toggle.ToggleState);
        AutomationPropertyChangedEventArgs? stateChange = null;
        peer.PropertyChanged += (_, args) => { if (args.Property == TogglePatternIdentifiers.ToggleStateProperty) stateChange = args; };
        toggle.Toggle();
        Assert.NotNull(stateChange);
        Assert.Equal(ToggleState.Off, stateChange.OldValue);
        Assert.Equal(ToggleState.On, stateChange.NewValue);
        Assert.Equal(ToggleState.On, toggle.ToggleState);
        Assert.Equal("Action completed", host.Result.Text);
        host.Button.IsEnabled = false;
        Assert.Throws<ElementNotEnabledException>(() => toggle.Toggle());
        Assert.Equal(ToggleState.On, toggle.ToggleState);
        host.Button.IsToggle = false;
        Assert.Null(peer.GetProvider<IToggleProvider>());
    }

    [AvaloniaFact]
    public void Toggle_button_commits_selection_before_the_host_click_result()
    {
        using var host = new ButtonHost();
        host.Button.IsToggle = true;
        host.Button.Click += (_, _) => host.Result.Text = host.Button.IsChecked ? "Selected" : "Unselected";
        host.Window.MouseDown(host.Center, MouseButton.Left);
        Assert.False(host.Button.IsChecked);
        host.Window.MouseUp(host.Center, MouseButton.Left);
        Assert.True(host.Button.IsChecked);
        Assert.Equal("Selected", host.Result.Text);
        host.Window.MouseDown(host.Center, MouseButton.Left);
        host.Window.MouseUp(host.Center, MouseButton.Left);
        Assert.False(host.Button.IsChecked);
        Assert.Equal("Unselected", host.Result.Text);
    }

    private sealed class ActionCommand(Action<object?> execute) : ICommand
    {
        private bool _allowed = true;
        public bool Allowed { get => _allowed; set { _allowed = value; CanExecuteChanged?.Invoke(this, EventArgs.Empty); } }
        public bool CanExecute(object? parameter) => Allowed;
        public void Execute(object? parameter) => execute(parameter);
        public event EventHandler? CanExecuteChanged;
    }

    private sealed class Selection : INotifyPropertyChanged
    {
        private bool _selected;
        public bool Selected { get => _selected; set { _selected = value; PropertyChanged?.Invoke(this, new(nameof(Selected))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
