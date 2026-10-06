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
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.Styling;
using Xunit;

namespace Avalonia.Material3.Tests;

public class ButtonGroupMatrixScenarioTests
{
    [AvaloniaFact]
    public void Switching_selection_to_action_mode_hides_the_selection_result_and_selection_provider()
    {
        using var host = new ButtonHost();
        var option = new MaterialGroupButton { Content = "Option" };
        var group = new MaterialButtonGroup { SelectionMode = MaterialGroupSelectionMode.Single, Children = { option } };
        host.Window.Content = group; host.Capture();
        var peer = ControlAutomationPeer.CreatePeerForElement(group)!;
        Assert.Single(group.SelectedItems);
        group.SelectionMode = MaterialGroupSelectionMode.None;
        Assert.Empty(group.SelectedItems);
        Assert.Null(group.SelectedItem);
        Assert.Null(peer.GetProvider<ISelectionProvider>());
    }

    [AvaloniaFact]
    public void Required_single_selection_automation_rejects_invalid_add_remove_but_select_commits_without_action()
    {
        using var host = new ButtonHost();
        var first = new MaterialGroupButton { Content = "A" };
        var second = new MaterialGroupButton { Content = "B" };
        var group = new MaterialSegmentedButtonGroup { Children = { first, second } };
        host.Window.Content = group; host.Capture();
        var firstItem = ControlAutomationPeer.CreatePeerForElement(first)!.GetProvider<ISelectionItemProvider>()!;
        var secondItem = ControlAutomationPeer.CreatePeerForElement(second)!.GetProvider<ISelectionItemProvider>()!;
        Assert.Throws<InvalidOperationException>(firstItem.RemoveFromSelection);
        Assert.Throws<InvalidOperationException>(secondItem.AddToSelection);
        var invoked = false; second.Click += (_, _) => invoked = true;
        secondItem.Select();
        Assert.False(invoked);
        Assert.Same(second, group.SelectedItem);
        Assert.Equal(AutomationControlType.RadioButton, ControlAutomationPeer.CreatePeerForElement(second)!.GetAutomationControlType());
        group.SelectionMode = MaterialGroupSelectionMode.Multiple;
        var peer = ControlAutomationPeer.CreatePeerForElement(second)!;
        Assert.Equal(AutomationControlType.CheckBox, peer.GetAutomationControlType());
        peer.GetProvider<IToggleProvider>()!.Toggle();
        Assert.False(second.IsChecked);
    }

    [AvaloniaFact]
    public void Bound_selection_survives_real_input_dynamic_removal_and_host_changes()
    {
        using var host = new ButtonHost();
        var first = new MaterialGroupButton { Content = "A" };
        var second = new MaterialGroupButton { Content = "B" };
        var model = new SelectionModel { Current = first };
        var group = new MaterialButtonGroup { SelectionMode = MaterialGroupSelectionMode.Single, Children = { first, second } };
        group.Bind(MaterialButtonGroup.SelectedItemProperty, new Binding(nameof(SelectionModel.Current)) { Source = model, Mode = BindingMode.TwoWay });
        host.Window.Content = group; host.Capture();
        Click(host, second); Assert.Same(second, model.Current);
        model.Current = first; host.Capture(); Assert.True(first.IsChecked); Assert.False(second.IsChecked);
        group.Children.Remove(first); host.Capture(); Assert.Same(second, model.Current);
        group.AllowEmptySelection = true; model.Current = null; host.Capture(); Assert.Empty(group.SelectedItems);
    }

    [AvaloniaTheory]
    [InlineData(MaterialButtonVariant.Filled, "#D0BCFF", "#381E72")]
    [InlineData(MaterialButtonVariant.Tonal, "#4A4458", "#E8DEF8")]
    [InlineData(MaterialButtonVariant.Elevated, "#1D1B20", "#D0BCFF")]
    [InlineData(MaterialButtonVariant.Outlined, "#00FFFFFF", "#CAC4D0")]
    public void Split_recipes_keep_their_color_when_secondary_is_selected_and_switch_modes_live(MaterialButtonVariant variant, string bg, string fg)
    {
        using var host = new ButtonHost();
        var split = new MaterialSplitButton { Variant = variant };
        split.MainButton.Content = "Save";
        host.Window.Content = split; host.Capture();
        var before = split.SecondaryButton.Background;
        Click(host, split.SecondaryButton);
        Assert.Equal(before, split.SecondaryButton.Background);
        host.Window.RequestedThemeVariant = ThemeVariant.Dark; host.Capture();
        Assert.Equal(Color.Parse(bg), ((ISolidColorBrush)split.SecondaryButton.Background!).Color);
        Assert.Equal(Color.Parse(fg), ((ISolidColorBrush)split.SecondaryButton.Foreground!).Color);
        split.Size = MaterialButtonSize.ExtraLarge;
        host.Capture(); Assert.Equal(50, split.SecondaryButton.SecondaryContentSize);
        Assert.Equal(32, split.MainButton.FontSize);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Segmented_recipe_has_outline_and_selected_secondary_colors_in_light_and_dark(bool dark)
    {
        using var host = new ButtonHost();
        var a = new MaterialGroupButton { Content = "Selected", LeadingIcon = "☆" };
        var b = new MaterialGroupButton { Content = "Unselected" };
        var group = new MaterialSegmentedButtonGroup { Children = { a, b } };
        host.Window.Content = group;
        host.Window.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light; host.Capture();
        Assert.Equal(Color.Parse(dark ? "#4A4458" : "#E8DEF8"), ((ISolidColorBrush)a.Background!).Color);
        Assert.Equal(Color.Parse(dark ? "#E8DEF8" : "#1D192B"), ((ISolidColorBrush)a.Foreground!).Color);
        Assert.Equal(Color.Parse(dark ? "#938F99" : "#79747E"), ((ISolidColorBrush)b.BorderBrush!).Color);
        Assert.Equal(new Thickness(1), b.BorderThickness);
        Assert.Equal(-1, b.Bounds.Left - a.Bounds.Right);
        Assert.NotEqual(host.Capture(), AfterSelection());
        byte[] AfterSelection() { Click(host, b); return host.Capture(); }
    }

    [AvaloniaTheory]
    [InlineData(MaterialButtonGroupVariant.Connected)]
    [InlineData(MaterialButtonGroupVariant.Unconnected)]
    public void Pointer_cancel_disabled_and_command_ineligible_options_do_not_mutate_selection(MaterialButtonGroupVariant variant)
    {
        using var host = new ButtonHost();
        var a = new MaterialGroupButton { Content = "Enabled" };
        var b = new MaterialGroupButton { Content = "Disabled", IsEnabled = false };
        var group = new MaterialButtonGroup { Variant = variant, SelectionMode = MaterialGroupSelectionMode.Multiple, Children = { a, b } };
        host.Window.Content = group; host.Capture();
        var p = a.TranslatePoint(new Point(20, 20), host.Window)!.Value;
        host.Window.MouseDown(p, MouseButton.Left); host.Window.MouseMove(new Point(0, 200)); host.Window.MouseUp(new Point(0, 200), MouseButton.Left);
        Assert.Empty(group.SelectedItems);
        Click(host, b); Assert.Empty(group.SelectedItems);
        Assert.Throws<ElementNotEnabledException>(() => ControlAutomationPeer.CreatePeerForElement(b)!.GetProvider<IInvokeProvider>()!.Invoke());
    }

    [AvaloniaFact]
    public void Host_template_and_content_slots_do_not_replace_option_input_or_selection_relationship()
    {
        using var host = new ButtonHost();
        var a = new MaterialGroupButton { Content = new { Label = "Custom option" }, LeadingIcon = "+", TrailingIcon = "!" };
        a.ContentTemplate = new FuncDataTemplate<object>((_, _) => new TextBlock { Text = "Custom option", TextWrapping = TextWrapping.Wrap });
        AutomationProperties.SetName(a, "Custom option");
        var group = new MaterialSegmentedButtonGroup { AllowEmptySelection = true, Children = { a } };
        host.Window.Content = group; host.Capture();
        a.Template = new FuncControlTemplate<MaterialGroupButton>((_, _) => new Border { Background = Brushes.Blue, Child = new TextBlock { Text = "Host template" }, Padding = new Thickness(20) });
        Click(host, a); Assert.True(a.IsChecked);
        Assert.Equal("Custom option", ControlAutomationPeer.CreatePeerForElement(a)!.GetName());
        Assert.Same(ControlAutomationPeer.CreatePeerForElement(group), ControlAutomationPeer.CreatePeerForElement(a)!.GetProvider<ISelectionItemProvider>()!.SelectionContainer);
    }

    [AvaloniaFact]
    public async Task Existing_group_width_motion_plays_and_runtime_reduced_motion_snaps_without_recreation()
    {
        using var host = new ButtonHost();
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with { FastSpatial = new MaterialSpring(1, 100) } };
        var a = new MaterialGroupButton { Content = "First action" };
        var b = new MaterialGroupButton { Content = "Second action" };
        var group = new MaterialButtonGroup { UseLayoutRounding = false, Children = { a, b } };
        host.Window.Width = 800; host.Window.Content = group; host.Capture();
        var width = a.Bounds.Width;
        var p = a.TranslatePoint(new Point(20, 20), host.Window)!.Value;
        host.Window.MouseDown(p, MouseButton.Left);
        await Task.Delay(50); host.Capture();
        Assert.InRange(a.Bounds.Width, width, width * 1.14);
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = true }; host.Capture();
        Assert.True(a.Bounds.Width > width);
        host.Window.MouseUp(p, MouseButton.Left); host.Capture(); Assert.Equal(width, a.Bounds.Width);
    }

    [AvaloniaFact]
    public void Vertical_and_RTL_navigation_skip_disabled_and_keep_shape_edges_logical()
    {
        using var host = new ButtonHost();
        var a = new MaterialGroupButton { Content = "Top" };
        var b = new MaterialGroupButton { Content = "Bottom" };
        var group = new MaterialButtonGroup { Orientation = Avalonia.Layout.Orientation.Vertical, Variant = MaterialButtonGroupVariant.Connected, Children = { a, b } };
        host.Window.Content = group; host.Capture();
        Assert.Equal(new CornerRadius(9999, 9999, 8, 8), a.GroupCornerRadius);
        Assert.Equal(new CornerRadius(8, 8, 9999, 9999), b.GroupCornerRadius);
        a.Focus(NavigationMethod.Tab); host.Window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null); Assert.True(b.IsFocused);
        group.Orientation = Avalonia.Layout.Orientation.Horizontal; group.FlowDirection = FlowDirection.RightToLeft; host.Capture();
        a.Focus(NavigationMethod.Tab); host.Window.KeyPress(Key.Left, RawInputModifiers.None, PhysicalKey.ArrowLeft, null); Assert.True(b.IsFocused);
    }

    [AvaloniaFact]
    public void Disabled_segmented_selection_stays_visible_and_uses_OnSurface_foreground_without_losing_state()
    {
        using var host = new ButtonHost();
        var a = new MaterialGroupButton { Content = "Selected" };
        var group = new MaterialSegmentedButtonGroup { Children = { a } };
        host.Window.Content = group; host.Capture();
        a.IsEnabled = false; host.Capture();
        Assert.True(a.IsChecked);
        Assert.Equal(Color.Parse("#E8DEF8"), ((ISolidColorBrush)a.Background!).Color);
        Assert.Equal(Color.Parse("#1D1B20"), ((ISolidColorBrush)a.Foreground!).Color);
    }

    [AvaloniaFact]
    public void Segmented_options_share_equal_width_and_selection_icon_does_not_move_row_edges()
    {
        using var host = new ButtonHost();
        var a = new MaterialGroupButton { Content = "A" };
        var b = new MaterialGroupButton { Content = "A longer label" };
        var group = new MaterialSegmentedButtonGroup { Children = { a, b } };
        host.Window.Content = group; host.Capture();
        var firstWidth = a.Bounds.Width;
        Assert.Equal(a.Bounds.Width, b.Bounds.Width);
        var end = b.Bounds.Right;
        Click(host, b);
        Assert.Equal(firstWidth, a.Bounds.Width);
        Assert.Equal(end, b.Bounds.Right);
    }

    [AvaloniaFact]
    public void Grown_long_label_shapes_keep_nominal_outer_corners_so_text_stays_inside_the_container()
    {
        using var host = new ButtonHost();
        var split = new MaterialSplitButton();
        split.MainButton.Content = "A long descriptive label that wraps over many readable lines";
        host.Window.Width = 180; host.Window.Height = 600; host.Window.Content = new StackPanel { Children = { split } }; host.Capture();
        Assert.True(split.MainButton.SharedContainerHeight > 40);
        Assert.Equal(20, split.MainButton.SplitCornerRadius.TopLeft);
    }

    [AvaloniaFact]
    public void Host_can_redirect_committed_selection_from_the_public_change_event()
    {
        using var host = new ButtonHost();
        var a = new MaterialGroupButton { Content = "A" };
        var b = new MaterialGroupButton { Content = "B" };
        var c = new MaterialGroupButton { Content = "C" };
        var group = new MaterialButtonGroup { SelectionMode = MaterialGroupSelectionMode.Single, Children = { a, b, c } };
        group.SelectionChanged += (_, _) => { if (group.SelectedItem == b) group.SelectedItem = c; };
        host.Window.Content = group; host.Capture(); Click(host, b);
        Assert.Same(c, group.SelectedItem);
        Assert.True(c.IsChecked); Assert.False(b.IsChecked);
    }

    [AvaloniaFact]
    public void Overflow_does_not_write_the_hosts_visibility_binding_when_content_moves_out_of_the_row()
    {
        using var host = new ButtonHost();
        var visibility = new Border { IsVisible = true };
        var a = new MaterialGroupButton { Content = "First action" };
        var b = new MaterialGroupButton { Content = "Second action" };
        b.Bind(Control.IsVisibleProperty, new Binding(nameof(Control.IsVisible)) { Source = visibility });
        var group = new MaterialButtonGroup { Children = { a, b } };
        host.Window.Width = 180; host.Window.Content = group; host.Capture();
        Assert.Contains(b, group.OverflowItems);
        Assert.True(b.IsVisible);
        visibility.IsVisible = false;
        host.Window.Width = 800; host.Capture();
        Assert.False(b.IsVisible);
    }

    [AvaloniaFact]
    public async Task Connected_inner_shape_animates_visibly_and_reduce_motion_snaps_the_existing_template()
    {
        using var host = new ButtonHost();
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with { DefaultEffects = new MaterialSpring(1, 100) } };
        var a = new MaterialGroupButton { Content = "A", Width = 80 };
        var b = new MaterialGroupButton { Content = "", Width = 80, Background = Brushes.Red, Foreground = Brushes.Red };
        var c = new MaterialGroupButton { Content = "C", Width = 80 };
        var group = new MaterialButtonGroup { Variant = MaterialButtonGroupVariant.Connected, Children = { a, b, c } };
        host.Window.Content = group; host.Capture();
        var corner = b.TranslatePoint(new Point(1, 7), host.Window)!.Value;
        var initial = host.PixelAt(corner);
        Assert.True(initial.G > 100); // Anti-aliased small corner is outside the opaque red fill.
        var p = b.TranslatePoint(new Point(40, 25), host.Window)!.Value;
        host.Window.MouseDown(p, MouseButton.Left); await Task.Delay(50);
        Assert.True(host.PixelAt(corner).G > 0); // Slow spring has not reached the pressed shape.
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = true };
        Assert.Equal(Colors.Red, host.PixelAt(corner));
        host.Window.MouseUp(p, MouseButton.Left);
        host.Window.MouseMove(new Point(0, 200)); // Remove hover elevation before comparing the released corner.
        Assert.Equal(initial, host.PixelAt(corner));
    }

    [AvaloniaFact]
    public void Group_and_split_commands_receive_committed_state_parameters_and_can_execute_prevents_mutation()
    {
        using var host = new ButtonHost();
        var a = new MaterialGroupButton { Content = "Choose", CommandParameter = "chosen" };
        var group = new MaterialButtonGroup { SelectionMode = MaterialGroupSelectionMode.Multiple, Children = { a } };
        var result = "waiting";
        var command = new HostCommand(parameter => result = $"{parameter}:{a.IsChecked}");
        a.Command = command; host.Window.Content = group; host.Capture();
        Click(host, a); Assert.Equal("chosen:True", result);
        command.Enabled = false; host.Capture(); Click(host, a); Assert.True(a.IsChecked); Assert.Equal("chosen:True", result);
        var split = new MaterialSplitButton();
        split.MainButton.Content = "Save"; split.SecondaryButton.CommandParameter = "options";
        split.SecondaryButton.Command = new HostCommand(parameter => result = $"{parameter}:{split.IsExpanded}");
        host.Window.Content = split; host.Capture(); Click(host, split.SecondaryButton); Assert.Equal("options:True", result);
        split.MainButton.Command = new HostCommand(_ => result = "unexpected");
        split.MainButton.Click += (_, e) => e.Handled = true;
        Click(host, split.MainButton); Assert.Equal("options:True", result);
    }

    private sealed class HostCommand(Action<object?> action) : ICommand
    {
        private bool _enabled = true;
        public bool Enabled { get => _enabled; set { _enabled = value; CanExecuteChanged?.Invoke(this, EventArgs.Empty); } }
        public bool CanExecute(object? parameter) => Enabled;
        public void Execute(object? parameter) => action(parameter);
        public event EventHandler? CanExecuteChanged;
    }

    [AvaloniaFact]
    public void Repeating_required_single_activation_does_not_publish_a_transient_unselected_state()
    {
        using var host = new ButtonHost();
        var a = new MaterialGroupButton { Content = "A" };
        var group = new MaterialSegmentedButtonGroup { Children = { a } };
        host.Window.Content = group; host.Capture();
        var changes = 0;
        a.PropertyChanged += (_, e) => { if (e.Property == MaterialButton.IsCheckedProperty) changes++; };
        Click(host, a);
        Assert.True(a.IsChecked);
        Assert.Equal(0, changes);
    }

    [AvaloniaFact]
    public void Overflow_stays_measured_across_repeated_nested_scrolling_layout_at_two_hundred_percent()
    {
        using var host = new ButtonHost();
        host.Theme.Typography = host.Theme.Typography with { Scale = 2 };
        var group = new MaterialButtonGroup { Children =
        {
            new MaterialGroupButton { Content = "Create a new document" },
            new MaterialGroupButton { Content = "Duplicate current document" },
            new MaterialGroupButton { Content = "Export mixed 中文 content" }
        } };
        host.Window.Content = new ScrollViewer { Content = new StackPanel { Margin = new Thickness(16), Children = { group } } };
        for (var i = 0; i < 5; i++) host.Capture();
        Assert.NotEmpty(group.OverflowItems);
        Assert.True(group.OverflowButton.IsVisible);
        Assert.True(group.Bounds.Height >= 48);
        Assert.True(group.OverflowButton.Bounds.Height >= 48);
    }

    [AvaloniaFact]
    public void Narrow_XL_split_preserves_space_for_a_readable_label_and_icons_without_overwriting_pinned_padding()
    {
        using var host = new ButtonHost();
        host.Theme.Typography = host.Theme.Typography with { Scale = 2 };
        var split = new MaterialSplitButton { Size = MaterialButtonSize.ExtraLarge };
        split.MainButton.Content = "XL"; split.MainButton.LeadingIcon = "+";
        host.Window.Content = new StackPanel { Margin = new Thickness(16), Children = { split } };
        host.Capture();
        Assert.Equal(64, split.MainButton.Padding.Left);
        var labelSpace = split.MainButton.Bounds.Width - split.MainButton.EffectivePadding.Left - split.MainButton.EffectivePadding.Right - split.MainButton.IconSize - split.MainButton.IconSpacing;
        Assert.True(labelSpace >= split.MainButton.FontSize - 1);
        var secondarySpace = split.SecondaryButton.Bounds.Width - split.SecondaryButton.EffectivePadding.Left - split.SecondaryButton.EffectivePadding.Right;
        Assert.True(secondarySpace >= split.SecondaryButton.SecondaryContentSize - 1);
        host.Window.Width = 1100; host.Capture(); Assert.Equal(split.MainButton.Padding, split.MainButton.EffectivePadding);
    }

    [AvaloniaFact]
    public void RTL_horizontal_arrows_follow_the_actual_visual_direction_across_three_options()
    {
        using var host = new ButtonHost();
        var a = new MaterialGroupButton { Content = "Start" };
        var b = new MaterialGroupButton { Content = "Middle" };
        var c = new MaterialGroupButton { Content = "End" };
        var group = new MaterialButtonGroup { Variant = MaterialButtonGroupVariant.Connected, FlowDirection = FlowDirection.RightToLeft, Children = { a, b, c } };
        host.Window.Content = group; host.Capture();
        Assert.True(a.TranslatePoint(new Point(0, 0), host.Window)!.Value.X > c.TranslatePoint(new Point(0, 0), host.Window)!.Value.X);
        a.Focus(NavigationMethod.Tab);
        host.Window.KeyPress(Key.Left, RawInputModifiers.None, PhysicalKey.ArrowLeft, null);
        Assert.True(b.IsFocused);
        host.Window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
        Assert.True(a.IsFocused);
        var split = new MaterialSplitButton { FlowDirection = FlowDirection.RightToLeft };
        split.MainButton.Content = "Save";
        var actions = 0; split.MainButton.Click += (_, _) => actions++;
        host.Window.Content = split; host.Capture();
        Assert.True(split.MainButton.TranslatePoint(new Point(0, 0), host.Window)!.Value.X > split.SecondaryButton.TranslatePoint(new Point(0, 0), host.Window)!.Value.X);
        Click(host, split.MainButton); Assert.Equal(1, actions);
        Click(host, split.SecondaryButton); Assert.True(split.IsExpanded); Assert.Equal(1, actions);
    }

    private static void Click(ButtonHost host, Control button)
    {
        host.Capture(); var p = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), host.Window)!.Value;
        host.Window.MouseDown(p, MouseButton.Left); host.Window.MouseUp(p, MouseButton.Left); host.Capture();
    }
    private sealed class SelectionModel : INotifyPropertyChanged
    {
        private MaterialGroupButton? _current;
        public MaterialGroupButton? Current { get => _current; set { _current = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Current))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
