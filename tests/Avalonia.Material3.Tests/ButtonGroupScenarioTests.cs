using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Xunit;

namespace Avalonia.Material3.Tests;

public class ButtonGroupScenarioTests
{
    [AvaloniaFact]
    public void Single_selection_is_committed_before_action_and_cannot_be_cleared_by_repeat_activation()
    {
        using var host = new ButtonHost();
        var first = new MaterialGroupButton { Content = "Day" };
        var second = new MaterialGroupButton { Content = "Week" };
        var group = new MaterialButtonGroup { SelectionMode = MaterialGroupSelectionMode.Single, Children = { first, second } };
        host.Window.Content = group;
        host.Capture();
        var result = "";
        second.Click += (_, _) => result = group.SelectedItem?.Content?.ToString();
        Click(host, second);
        Assert.Equal("Week", result);
        Assert.Same(second, group.SelectedItem);
        Assert.False(first.IsChecked);
        Click(host, second);
        Assert.True(second.IsChecked);
        Click(host, first);
        Assert.Same(first, group.SelectedItem);
        Assert.False(second.IsChecked);
    }

    [AvaloniaFact]
    public void Segmented_multiple_selection_has_container_relationship_and_arrows_move_focus_without_changing_options()
    {
        using var host = new ButtonHost();
        var first = new MaterialGroupButton { Content = "Photos" };
        var disabled = new MaterialGroupButton { Content = "Unavailable", IsEnabled = false };
        var last = new MaterialGroupButton { Content = "Videos" };
        var group = new MaterialSegmentedButtonGroup { SelectionMode = MaterialGroupSelectionMode.Multiple, Children = { first, disabled, last } };
        host.Window.Content = group;
        host.Capture();
        first.Focus(NavigationMethod.Tab);
        host.Window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
        Assert.True(last.IsFocused);
        Assert.Empty(group.SelectedItems);
        host.Window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        host.Window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        Assert.Equal(new[] { last }, group.SelectedItems);
        var container = ControlAutomationPeer.CreatePeerForElement(group)!;
        var provider = container.GetProvider<ISelectionProvider>()!;
        Assert.True(provider.CanSelectMultiple);
        Assert.False(provider.IsSelectionRequired);
        var item = ControlAutomationPeer.CreatePeerForElement(first)!.GetProvider<ISelectionItemProvider>()!;
        Assert.Same(container, item.SelectionContainer);
        item.AddToSelection();
        Assert.Equal(new[] { first, last }, group.SelectedItems);
        item.RemoveFromSelection();
        Assert.False(item.IsSelected);
        Assert.Single(provider.GetSelection());
    }

    [AvaloniaFact]
    public void Connected_options_use_pinned_asymmetric_selected_pressed_shapes_and_two_DIP_visual_gap()
    {
        using var host = new ButtonHost();
        var first = new MaterialGroupButton { Content = "Day" };
        var middle = new MaterialGroupButton { Content = "Week" };
        var last = new MaterialGroupButton { Content = "Month" };
        var group = new MaterialButtonGroup { Variant = MaterialButtonGroupVariant.Connected, SelectionMode = MaterialGroupSelectionMode.Single, AllowEmptySelection = true, Children = { first, middle, last } };
        host.Window.Content = group;
        host.Capture();
        Assert.Equal(new CornerRadius(9999, 8, 8, 9999), first.GroupCornerRadius);
        Assert.Equal(new CornerRadius(8), middle.GroupCornerRadius);
        Assert.Equal(2, middle.Bounds.Left - first.Bounds.Right);
        Click(host, middle);
        Assert.Equal(new CornerRadius(9999), middle.GroupCornerRadius);
        var p = first.TranslatePoint(new Point(first.Bounds.Width / 2, 20), host.Window)!.Value;
        host.Window.MouseDown(p, MouseButton.Left);
        Assert.Equal(new CornerRadius(9999, 4, 4, 9999), first.GroupCornerRadius);
        host.Window.MouseUp(p, MouseButton.Left);
        host.Capture();
        group.FlowDirection = Avalonia.Media.FlowDirection.RightToLeft;
        first.IsChecked = false;
        host.Capture();
        Assert.Equal(new CornerRadius(9999, 8, 8, 9999), first.GroupCornerRadius); // Logical target; Avalonia mirrors it visually.
    }

    [AvaloniaFact]
    public void Long_dynamic_connected_content_reflows_at_narrow_width_without_losing_selection_or_targets()
    {
        using var host = new ButtonHost();
        var first = new MaterialGroupButton { Content = "Mixed 中文 label that wraps into a readable option" };
        var second = new MaterialGroupButton { Content = "Other" };
        var group = new MaterialButtonGroup { Variant = MaterialButtonGroupVariant.Connected, SelectionMode = MaterialGroupSelectionMode.Multiple, Children = { first, second } };
        host.Window.Width = 180;
        host.Window.Height = 600;
        host.Window.Content = group;
        host.Capture();
        Assert.InRange(first.Bounds.Right, 48, 180);
        Assert.True(second.Bounds.Top >= first.Bounds.Bottom);
        Assert.True(first.Bounds.Height >= 48 && second.Bounds.Height >= 48);
        Click(host, first);
        Assert.True(first.IsChecked);
        second.Content = "Changed second label is also quite long";
        host.Theme.Typography = host.Theme.Typography with { Scale = 2 };
        host.Capture();
        Assert.True(first.IsChecked);
        Assert.InRange(second.Bounds.Right, 48, 180);
        group.Children.Remove(first);
        host.Capture();
        Assert.Empty(group.SelectedItems);
        Assert.Equal(new CornerRadius(20), second.GroupCornerRadius);
    }

    [AvaloniaFact]
    public void Unconnected_press_expands_fifteen_percent_and_compresses_neighbors_without_moving_group_edges()
    {
        using var host = new ButtonHost();
        var first = new MaterialGroupButton { Content = "First action" };
        var middle = new MaterialGroupButton { Content = "Middle action" };
        var last = new MaterialGroupButton { Content = "Last action" };
        var group = new MaterialButtonGroup { UseLayoutRounding = false, Children = { first, middle, last } };
        host.Window.Width = 800;
        host.Window.Content = group;
        host.Capture();
        var width = middle.Bounds.Width;
        var firstWidth = first.Bounds.Width;
        var lastWidth = last.Bounds.Width;
        var start = first.Bounds.Left;
        var end = last.Bounds.Right;
        Assert.Equal(12, middle.Bounds.Left - first.Bounds.Right);
        var p = middle.TranslatePoint(new Point(width / 2, 20), host.Window)!.Value;
        host.Window.MouseDown(p, MouseButton.Left);
        host.Capture();
        Assert.Equal(width * 1.15, middle.Bounds.Width, 4);
        Assert.Equal(firstWidth - width * .075, first.Bounds.Width, 4);
        Assert.Equal(lastWidth - width * .075, last.Bounds.Width, 4);
        Assert.Equal(start, first.Bounds.Left);
        Assert.Equal(end, last.Bounds.Right, 4);
        host.Window.MouseUp(p, MouseButton.Left);
        host.Capture();
        Assert.Equal(width, middle.Bounds.Width, 4);
    }

    [AvaloniaFact]
    public void Unconnected_overflow_keeps_actions_reachable_and_returns_focus_after_escape()
    {
        using var host = new ButtonHost();
        var first = new MaterialGroupButton { Content = "First action" };
        var last = new MaterialGroupButton { Content = "Last action" };
        var group = new MaterialButtonGroup { Children = { first, last } };
        var result = "waiting";
        last.Click += (_, _) => result = "last action completed";
        host.Window.Content = group;
        host.Window.Width = 180;
        host.Capture();
        Assert.Contains(last, group.OverflowItems);
        Click(host, group.OverflowButton);
        Assert.True(group.IsOverflowOpen);
        var entry = group.OverflowEntries.Last();
        ControlAutomationPeer.CreatePeerForElement(entry)!.GetProvider<IInvokeProvider>()!.Invoke();
        host.Capture();
        Assert.Equal("last action completed", result);
        Assert.False(group.IsOverflowOpen);
        group.OpenOverflow();
        host.Capture();
        group.OverflowEntries[0].Focus(NavigationMethod.Tab);
        host.Window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        host.Capture();
        Assert.False(group.IsOverflowOpen);
        Assert.True(group.OverflowButton.IsFocused);
        host.Window.Width = 800;
        host.Capture();
        Assert.Empty(group.OverflowItems);
        Assert.True(last.IsVisible);
    }

    [AvaloniaFact]
    public void Split_main_action_and_secondary_expansion_are_independent_and_escape_returns_secondary_focus()
    {
        using var host = new ButtonHost();
        var split = new MaterialSplitButton();
        split.MainButton.Content = "Save";
        var result = "waiting";
        split.MainButton.Click += (_, _) => result = "saved";
        var secondary = false;
        split.SecondaryButton.Click += (_, _) => secondary = split.IsExpanded;
        host.Window.Content = split;
        host.Capture();
        Click(host, split.SecondaryButton);
        Assert.True(secondary);
        Assert.True(split.IsExpanded);
        Assert.Equal("waiting", result);
        Click(host, split.MainButton);
        Assert.Equal("saved", result);
        Assert.True(split.IsExpanded);
        var peer = ControlAutomationPeer.CreatePeerForElement(split.SecondaryButton)!;
        var expand = peer.GetProvider<IExpandCollapseProvider>()!;
        Assert.Equal(Avalonia.Automation.ExpandCollapseState.Expanded, expand.ExpandCollapseState);
        split.MainButton.Focus(NavigationMethod.Tab);
        host.Window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        Assert.False(split.IsExpanded);
        Assert.True(split.SecondaryButton.IsFocused);
        split.SecondaryIsToggle = false;
        Click(host, split.SecondaryButton);
        Assert.False(split.IsExpanded);
        Assert.Null(peer.GetProvider<IExpandCollapseProvider>());
        split.IsEnabled = false;
        Assert.Throws<Avalonia.Automation.ElementNotEnabledException>(() => peer.GetProvider<IInvokeProvider>()!.Invoke());
    }

    [AvaloniaTheory]
    [InlineData(MaterialButtonSize.ExtraSmall, 32, 12, 10, 13, 22, 4, 8)]
    [InlineData(MaterialButtonSize.Small, 40, 16, 12, 13, 22, 4, 12)]
    [InlineData(MaterialButtonSize.Medium, 56, 24, 24, 15, 26, 4, 12)]
    [InlineData(MaterialButtonSize.Large, 96, 48, 48, 29, 38, 8, 20)]
    [InlineData(MaterialButtonSize.ExtraLarge, 136, 64, 64, 43, 50, 12, 20)]
    public void Every_split_size_uses_its_pinned_padding_icon_height_and_inner_shape(MaterialButtonSize size,
        double height, double leading, double trailing, double secondaryPadding, double icon, double inner, double pressed)
    {
        using var host = new ButtonHost();
        var split = new MaterialSplitButton { Size = size };
        split.MainButton.Content = "Save";
        host.Window.Width = 1000;
        host.Window.Content = new StackPanel { Children = { split } };
        host.Capture();
        Assert.Equal(height, split.MainButton.ContainerHeight);
        Assert.Equal(leading, split.MainButton.Padding.Left);
        Assert.Equal(trailing, split.MainButton.Padding.Right);
        Assert.Equal(secondaryPadding, split.SecondaryButton.Padding.Left);
        Assert.Equal(icon, split.SecondaryButton.SecondaryContentSize);
        Assert.Equal(new CornerRadius(9999, inner, inner, 9999), split.MainButton.SplitCornerRadius);
        Assert.Equal(2, split.SecondaryButton.Bounds.Left - split.MainButton.Bounds.Right);
        Click(host, split.SecondaryButton);
        Assert.Equal(new CornerRadius(9999), split.SecondaryButton.SplitCornerRadius);
        var p = split.MainButton.TranslatePoint(new Point(split.MainButton.Bounds.Width / 2, 20), host.Window)!.Value;
        host.Window.MouseDown(p, MouseButton.Left);
        Assert.Equal(new CornerRadius(9999, pressed, pressed, 9999), split.MainButton.SplitCornerRadius);
        host.Window.MouseUp(p, MouseButton.Left);
    }

    [AvaloniaFact]
    public void Split_long_main_and_secondary_text_share_growing_visual_height_and_remain_separate_at_two_hundred_percent()
    {
        using var host = new ButtonHost();
        var split = new MaterialSplitButton { Size = MaterialButtonSize.ExtraLarge };
        split.MainButton.Content = "Save changed document 中文 to the selected destination";
        split.SecondaryButton.UseIconContent = false;
        split.SecondaryButton.Content = "Options";
        host.Theme.Typography = host.Theme.Typography with { Scale = 2 };
        host.Window.Width = 320;
        host.Window.Height = 1200;
        host.Window.Content = new StackPanel { Children = { split } };
        host.Capture();
        Assert.InRange(split.MainButton.Bounds.Right, 48, 320);
        Assert.InRange(split.SecondaryButton.Bounds.Right, 48, 320);
        Assert.True(split.SecondaryButton.Bounds.Width >= 48);
        Assert.Equal(split.MainButton.SharedContainerHeight, split.SecondaryButton.SharedContainerHeight);
        Assert.True(split.SecondaryButton.SharedContainerHeight > 136);
        Assert.False(split.MainButton.Bounds.Intersects(split.SecondaryButton.Bounds));
    }

    private static void Click(ButtonHost host, Control button)
    {
        host.Capture();
        var point = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), host.Window)!.Value;
        host.Window.MouseDown(point, MouseButton.Left);
        host.Window.MouseUp(point, MouseButton.Left);
        host.Capture();
    }
}
