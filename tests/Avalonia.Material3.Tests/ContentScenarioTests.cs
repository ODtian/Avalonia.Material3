using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using System.Windows.Input;
using System.Runtime.InteropServices;
using Avalonia.Platform;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Material3.Tests;

public class ContentScenarioTests
{
    [AvaloniaFact]
    public void Selecting_a_row_and_invoking_its_trailing_action_have_distinct_item_ownership()
    {
        using var host = new ContentHost();
        var action = new MaterialButton { Content = "Archive B" };
        var a = new MaterialListItem { Title = "甲 / A", IsSelectable = true };
        var b = new MaterialListItem { Title = "乙 / B", IsSelectable = true, Trailing = action };
        var result = "Waiting";
        a.Activated += (_, _) => result = "Selected A";
        b.Activated += (_, _) => result = "Selected B";
        action.Click += (_, _) => result = "Archived B";
        host.List.Children.Add(a);
        host.List.Children.Add(b);
        host.Render();
        host.Click(a, new Point(24, a.Bounds.Height / 2));
        Assert.True(a.IsSelected);
        Assert.False(b.IsSelected);
        Assert.Equal("Selected A", result);
        host.Click(action);
        Assert.Equal("Archived B", result);
        Assert.False(b.IsSelected);
        Assert.True(a.Bounds.Height >= 56);
    }

    [AvaloniaFact]
    public void Long_mixed_language_slots_grow_beyond_line_minima_and_follow_font_scale()
    {
        using var host = new ContentHost();
        var title = new TextBlock { Text = "标题 / A long mixed-language heading 中文 English", TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        var image = new Border { Width = 56, Height = 56, Background = Avalonia.Media.Brushes.Green };
        var row = new MaterialListItem { Title = title, Image = image, SupportingContent = string.Concat(Enumerable.Repeat("支持内容 Supporting text with variable height。", 8)), Lines = MaterialListLines.Three };
        var one = new MaterialListItem { Title = "One" };
        var two = new MaterialListItem { Title = "Two", SupportingContent = "Supporting", Lines = MaterialListLines.Two };
        host.List.Children.Add(one);
        host.List.Children.Add(two);
        host.List.Children.Add(row);
        host.Window.Width = 320;
        host.Render();
        Assert.Equal(56, one.Bounds.Height);
        Assert.Equal(72, two.Bounds.Height);
        Assert.True(row.Bounds.Height > 88);
        Assert.True(title.IsEffectivelyVisible && image.IsEffectivelyVisible);
        Assert.True(title.Bounds.Right <= row.Bounds.Width);
        var previous = row.Bounds.Height;
        host.Theme.Typography = new() { Scale = 1.5 };
        host.Render();
        Assert.Equal(24, row.FontSize);
        Assert.True(row.Bounds.Height > previous);
    }

    [AvaloniaFact]
    public void Card_variants_use_pinned_surfaces_and_an_interactive_card_owns_its_action()
    {
        using var host = new ContentHost();
        var filled = new MaterialCard { Title = "Filled" };
        var elevated = new MaterialCard { Title = "Elevated", Variant = MaterialCardVariant.Elevated };
        var outlined = new MaterialCard { Title = "Outlined", Variant = MaterialCardVariant.Outlined, IsInteractive = true, IsSelectable = true };
        host.List.Children.Add(filled);
        host.List.Children.Add(elevated);
        host.List.Children.Add(outlined);
        host.Render();
        Assert.Equal(Avalonia.Media.Color.Parse("#E6E0E9"), ((Avalonia.Media.ISolidColorBrush)filled.Background!).Color);
        Assert.Equal(Avalonia.Media.Color.Parse("#F7F2FA"), ((Avalonia.Media.ISolidColorBrush)elevated.Background!).Color);
        Assert.Equal(new CornerRadius(12), outlined.CornerRadius);
        Assert.Equal(new Thickness(1), outlined.BorderThickness);
        host.Click(filled);
        Assert.False(filled.IsSelected);
        host.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        host.Window.KeyReleaseQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        Assert.True(outlined.IsFocused);
        host.Window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);
        Assert.True(outlined.IsPressed);
        host.Window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None);
        Assert.True(outlined.IsSelected);
        outlined.IsEnabled = false;
        host.Click(outlined);
        Assert.True(outlined.IsSelected);
        host.Window.RequestedThemeVariant = ThemeVariant.Dark;
        host.Render();
        Assert.Equal(Avalonia.Media.Color.Parse("#36343B"), ((Avalonia.Media.ISolidColorBrush)filled.Background!).Color);
    }

    [AvaloniaFact]
    public void Keyboard_expansion_shows_the_owned_content_without_selecting_the_row()
    {
        using var host = new ContentHost();
        var detail = new TextBlock { Text = "展开的内容 / Expanded detail" };
        var row = new MaterialListItem { Title = "Folder", IsSelectable = true, IsExpandable = true, ExpandedContent = detail };
        host.List.Children.Add(row);
        host.Render();
        Assert.False(detail.GetVisualAncestors().Contains(host.Window) && detail.IsEffectivelyVisible);
        var expandButton = row.GetVisualDescendants().OfType<MaterialButton>().Single(b => AutomationProperties.GetName(b) == "Toggle expanded content");
        Assert.Equal("⌄", expandButton.Content);
        Assert.Equal(Color.Parse("#FEF7FF"), ((ISolidColorBrush)expandButton.Background!).Color);
        host.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        host.Window.KeyReleaseQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        host.Window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.Alt);
        host.Window.KeyReleaseQwerty(PhysicalKey.Enter, RawInputModifiers.Alt);
        host.Render();
        Assert.True(row.IsExpanded);
        Assert.True(detail.IsEffectivelyVisible);
        Assert.Equal("⌃", expandButton.Content);
        Assert.Equal(Color.Parse("#F3EDF7"), ((ISolidColorBrush)expandButton.Background!).Color);
        Assert.False(row.IsSelected);
        host.Window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.Alt);
        host.Window.KeyReleaseQwerty(PhysicalKey.Enter, RawInputModifiers.Alt);
        host.Render();
        Assert.False(row.IsExpanded);
        Assert.False(detail.GetVisualAncestors().Contains(host.Window) && detail.IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public void Touch_reveal_is_non_destructive_and_the_revealed_action_keeps_its_owner()
    {
        using var host = new ContentHost();
        var action = new MaterialButton { Content = "Delete B" };
        var row = new MaterialListItem { Title = "B", IsSelectable = true, IsRevealEnabled = true, RevealedActions = action };
        var result = "Waiting";
        row.Activated += (_, _) => result = "Selected B";
        action.Click += (_, _) => result = "Deleted B";
        host.List.Children.Add(row);
        host.Render();
        var start = host.At(row, new Point(180, 24));
        var end = start - new Vector(90, 0);
        using var contact = host.Window.TouchBegin(start);
        host.Window.TouchMove(contact, end);
        host.Window.TouchEnd(contact, end);
        host.Render();
        Assert.True(row.IsRevealed);
        Assert.False(row.IsSelected);
        Assert.Equal("Waiting", result);
        Assert.True(action.IsEffectivelyVisible && action.Bounds.Height >= 48);
        Assert.Equal(new CornerRadius(16), action.CornerRadius);
        host.Click(action);
        Assert.Equal("Deleted B", result);
        row.Focus();
        host.Window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        host.Window.KeyReleaseQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        Assert.False(row.IsRevealed);
        row.IsEnabled = false;
        host.Click(row);
        Assert.False(row.IsSelected);
    }

    [AvaloniaFact]
    public void Keyboard_reorder_preserves_item_identity_selection_and_reports_the_host_order()
    {
        using var host = new ContentHost();
        var a = new MaterialListItem { Title = "A", IsReorderEnabled = true, IsSelected = true };
        var b = new MaterialListItem { Title = "B", IsReorderEnabled = true };
        host.List.Children.Add(a);
        host.List.Children.Add(b);
        var result = "Waiting";
        host.List.ItemReordered += (_, e) => result = $"{e.Item.Title}: {e.OldIndex} → {e.NewIndex}";
        host.Render();
        host.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        host.Window.KeyReleaseQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        host.Window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.Alt);
        host.Window.KeyReleaseQwerty(PhysicalKey.ArrowDown, RawInputModifiers.Alt);
        host.Render();
        Assert.Same(a, host.List.Children[1]);
        Assert.Equal("A: 0 → 1", result);
        Assert.True(a.IsSelected && a.IsFocused);
        a.IsEnabled = false;
        Assert.False(host.List.MoveItem(a, 0));
        Assert.Same(a, host.List.Children[1]);
    }

    [AvaloniaFact]
    public void Pointer_drag_handle_reorders_variable_height_rows_without_activating_them()
    {
        using var host = new ContentHost();
        var a = new MaterialListItem { Title = "A", IsReorderEnabled = true, IsSelectable = true };
        var b = new MaterialListItem { Title = "B", SupportingContent = "Tall row\nwith several lines\nand another", Lines = MaterialListLines.Three };
        host.List.Children.Add(a);
        host.List.Children.Add(b);
        host.Render();
        var handle = a.GetVisualDescendants().OfType<MaterialButton>().Single(c => AutomationProperties.GetName(c) == "Reorder item");
        var start = host.At(handle);
        var end = host.At(b);
        host.Window.MouseDown(start, MouseButton.Left);
        Assert.True(a.IsReordering);
        host.Window.MouseMove(end, RawInputModifiers.LeftMouseButton);
        host.Window.MouseUp(end, MouseButton.Left);
        host.Render();
        Assert.Same(a, host.List.Children[1]);
        Assert.False(a.IsReordering || a.IsSelected);
    }

    [AvaloniaFact]
    public void Badges_render_dot_and_capped_count_but_automation_reports_the_full_count()
    {
        using var host = new ContentHost();
        var dot = new MaterialBadge();
        var count = new MaterialBadge { Count = 123 };
        host.List.Children.Add(dot);
        host.List.Children.Add(count);
        host.Render();
        Assert.Equal(6, dot.Bounds.Height);
        Assert.Equal(6, dot.Bounds.Width);
        Assert.Equal(16, count.Bounds.Height);
        Assert.Equal("99+", count.DisplayText);
        var peer = ControlAutomationPeer.CreatePeerForElement(count)!;
        Assert.Equal(AutomationControlType.Text, peer.GetAutomationControlType());
        Assert.Equal("123", peer.GetName());
        count.Count = 0;
        host.Render();
        Assert.False(count.IsVisible);
        count.ShowZero = true;
        host.Render();
        Assert.True(count.IsVisible);
        Assert.Equal("0", count.DisplayText);
        Assert.Throws<ArgumentException>(() => count.Count = -1);
    }

    [AvaloniaFact]
    public void Dividers_are_one_dip_decorative_lines_with_directional_insets()
    {
        using var host = new ContentHost();
        var horizontal = new MaterialDivider { InsetStart = 16, InsetEnd = 24 };
        var vertical = new MaterialDivider { Orientation = Avalonia.Layout.Orientation.Vertical, Height = 80, InsetStart = 8, InsetEnd = 12 };
        host.List.Children.Add(horizontal);
        host.List.Children.Add(vertical);
        host.Render();
        Assert.Equal(1, horizontal.Bounds.Height);
        Assert.Equal(new Thickness(16, 0, 24, 0), horizontal.LineInset);
        Assert.Equal(1, vertical.Bounds.Width);
        Assert.Equal(new Thickness(0, 8, 0, 12), vertical.LineInset);
        Assert.False(horizontal.Focusable);
        var peer = ControlAutomationPeer.CreatePeerForElement(horizontal)!;
        Assert.False(peer.IsControlElement());
        Assert.False(peer.IsContentElement());
        Assert.Throws<ArgumentException>(() => horizontal.InsetStart = -1);
    }

    [AvaloniaFact]
    public void Automation_observes_item_name_selection_expansion_and_independent_action_semantics()
    {
        using var host = new ContentHost();
        var action = new MaterialButton { Content = "Open attachment" };
        var row = new MaterialListItem { Title = "Inbox B", IsSelectable = true, IsExpandable = true, Trailing = action };
        var card = new MaterialCard { Title = "Static information" };
        host.List.Children.Add(row);
        host.List.Children.Add(card);
        host.Render();
        var result = "Waiting";
        row.Activated += (_, _) => result = "Selected Inbox B";
        var peer = ControlAutomationPeer.CreatePeerForElement(row)!;
        Assert.Equal("Inbox B", peer.GetName());
        Assert.Equal(AutomationControlType.ListItem, peer.GetAutomationControlType());
        Assert.Contains(AutomationDescendants(peer), child => child.GetName() == "Open attachment" && child.GetAutomationControlType() == AutomationControlType.Button);
        var toggle = Assert.IsAssignableFrom<IToggleProvider>(peer.GetProvider<IToggleProvider>());
        toggle.Toggle();
        Assert.Equal("Selected Inbox B", result);
        Assert.Equal(ToggleState.On, toggle.ToggleState);
        var expand = Assert.IsAssignableFrom<IExpandCollapseProvider>(peer.GetProvider<IExpandCollapseProvider>());
        expand.Expand();
        Assert.Equal(ExpandCollapseState.Expanded, expand.ExpandCollapseState);
        Assert.True(row.IsExpanded);
        Assert.Equal(AutomationControlType.Button, ControlAutomationPeer.CreatePeerForElement(action)!.GetAutomationControlType());
        var staticPeer = ControlAutomationPeer.CreatePeerForElement(card)!;
        Assert.Equal(AutomationControlType.Group, staticPeer.GetAutomationControlType());
        Assert.Null(staticPeer.GetProvider<IInvokeProvider>());
        Assert.Null(staticPeer.GetProvider<IToggleProvider>());
        row.IsEnabled = false;
        Assert.False(peer.IsEnabled());
        Assert.Throws<ElementNotEnabledException>(() => toggle.Toggle());
    }

    [AvaloniaFact]
    public void Scaled_badge_and_dark_theme_update_existing_content_without_clipping()
    {
        using var host = new ContentHost();
        var badge = new MaterialBadge { Count = 12 };
        var row = new MaterialListItem { Title = "Scale / 字体", Trailing = badge };
        host.List.Children.Add(row);
        host.Render();
        var initialHeight = badge.Bounds.Height;
        host.Theme.Typography = new() { Scale = 2 };
        host.Window.RequestedThemeVariant = ThemeVariant.Dark;
        host.Render();
        Assert.True(badge.Bounds.Height > initialHeight);
        Assert.Equal(Avalonia.Media.Color.Parse("#F2B8B5"), ((Avalonia.Media.ISolidColorBrush)badge.Background!).Color);
        Assert.Equal(32, row.FontSize);
        Assert.True(row.Bounds.Height >= badge.Bounds.Height);
    }

    [AvaloniaFact]
    public void Interaction_feedback_is_visible_but_static_cards_do_not_acquire_hover_feedback()
    {
        using var host = new ContentHost();
        var card = new MaterialCard { Title = "Static" };
        var row = new MaterialListItem { Title = "Interactive", IsSelectable = true };
        host.List.Children.Add(card);
        host.List.Children.Add(row);
        host.Render();
        var idle = host.Capture();
        host.Window.MouseMove(host.At(card));
        Assert.Equal(idle, host.Capture());
        host.Window.MouseMove(host.At(row));
        var hover = host.Capture();
        Assert.NotEqual(idle, hover);
        host.Window.MouseDown(host.At(row), MouseButton.Left);
        Assert.True(row.IsPressed);
        Assert.NotEqual(hover, host.Capture());
        host.Window.MouseMove(new Point(460, 600), RawInputModifiers.LeftMouseButton);
        host.Window.MouseUp(new Point(460, 600), MouseButton.Left);
        Assert.False(row.IsSelected || row.IsPressed);
        host.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        host.Window.KeyReleaseQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        Assert.True(row.IsFocused);
        Assert.NotEqual(idle, host.Capture());
    }

    [AvaloniaFact]
    public void Expressive_row_shapes_and_card_selection_remain_distinct_from_variant_surfaces()
    {
        using var host = new ContentHost();
        var row = new MaterialListItem { Title = "Expressive", IsExpressive = true, IsSelectable = true };
        var card = new MaterialCard { Title = "Elevated selection", Variant = MaterialCardVariant.Elevated, IsInteractive = true, IsSelectable = true };
        host.List.Children.Add(row);
        host.List.Children.Add(card);
        host.Render();
        Assert.Equal(new CornerRadius(4), row.CornerRadius);
        host.Window.MouseMove(host.At(row));
        Assert.Equal(new CornerRadius(12), row.CornerRadius);
        host.Window.MouseDown(host.At(row), MouseButton.Left);
        Assert.Equal(new CornerRadius(16), row.CornerRadius);
        host.Window.MouseUp(host.At(row), MouseButton.Left);
        Assert.Equal(new CornerRadius(16), row.CornerRadius);
        host.Click(card);
        Assert.Equal(Avalonia.Media.Color.Parse("#E8DEF8"), ((Avalonia.Media.ISolidColorBrush)card.Background!).Color);
        row.IsEnabled = false;
        host.Render();
        Assert.Equal(Avalonia.Media.Color.Parse("#1D1B20"), ((Avalonia.Media.ISolidColorBrush)row.Background!).Color);
        card.IsDragged = true;
        Assert.True(card.IsDragged);
    }

    [AvaloniaFact]
    public void Secondary_pointer_buttons_cannot_reveal_or_select_a_row()
    {
        using var host = new ContentHost();
        var row = new MaterialListItem { Title = "B", IsSelectable = true, IsRevealEnabled = true };
        host.List.Children.Add(row);
        host.Render();
        var start = host.At(row, new Point(180, 24));
        var end = start - new Vector(90, 0);
        host.Window.MouseDown(start, MouseButton.Right);
        host.Window.MouseMove(end, RawInputModifiers.RightMouseButton);
        host.Window.MouseUp(end, MouseButton.Right);
        Assert.False(row.IsRevealed || row.IsSelected);
    }

    [AvaloniaFact]
    public void Removing_or_disabling_a_dragged_row_cancels_capture_and_restores_its_transform()
    {
        using var host = new ContentHost();
        var transform = new Avalonia.Media.TranslateTransform(2, 3);
        var row = new MaterialListItem { Title = "A", IsReorderEnabled = true, RenderTransform = transform };
        host.List.Children.Add(row);
        host.Render();
        var handle = row.GetVisualDescendants().OfType<MaterialButton>().Single(c => AutomationProperties.GetName(c) == "Reorder item");
        host.Window.MouseDown(host.At(handle), MouseButton.Left);
        host.Window.MouseMove(new Point(160, 180), RawInputModifiers.LeftMouseButton);
        row.IsReorderEnabled = false;
        Assert.False(row.IsReordering);
        Assert.Same(transform, row.RenderTransform);
        host.Window.MouseUp(new Point(160, 180), MouseButton.Left);
        row.IsReorderEnabled = true;
        host.Render();
        host.Window.MouseDown(host.At(handle), MouseButton.Left);
        host.List.Children.Clear();
        Assert.False(row.IsReordering);
        Assert.Same(transform, row.RenderTransform);
        host.Window.MouseUp(new Point(160, 180), MouseButton.Left);
    }

    [AvaloniaFact]
    public void Reveal_exposes_a_trailing_action_underlay_without_changing_the_row_height()
    {
        using var host = new ContentHost();
        var action = new MaterialButton { Content = "Archive", MinWidth = 48 };
        var row = new MaterialListItem { Title = "Inbox", IsRevealEnabled = true, RevealedActions = action };
        host.List.Children.Add(row);
        host.Render();
        var height = row.Bounds.Height;
        row.Focus();
        host.Window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.Alt);
        host.Window.KeyReleaseQwerty(PhysicalKey.ArrowRight, RawInputModifiers.Alt);
        host.Render();
        Assert.True(row.IsRevealed);
        Assert.Equal(height, row.Bounds.Height);
        var actionPosition = action.TranslatePoint(default, row)!.Value;
        Assert.True(actionPosition.X >= 0 && actionPosition.X + action.Bounds.Width <= row.Bounds.Width);
        Assert.True(actionPosition.Y + action.Bounds.Height <= row.Bounds.Height);
    }

    [AvaloniaFact]
    public void Every_content_template_and_command_can_be_composed_without_stealing_item_activation()
    {
        using var host = new ContentHost();
        var result = "Waiting";
        var title = new TextBlock { Text = "长标题 / Mixed language title", TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        var support = new TextBlock { Text = "Supporting / 辅助内容", TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        var image = new Border { Width = 56, Height = 56, Background = Avalonia.Media.Brushes.Green };
        var leading = new MaterialButton { Content = "L", CommandParameter = "leading-B", Command = new ContentCommand(p => result = (string)p!) };
        var trailing = new MaterialButton { Content = "T", CommandParameter = "trailing-B", Command = new ContentCommand(p => result = (string)p!) };
        var row = new MaterialListItem
        {
            Title = "title", TitleTemplate = new FuncDataTemplate<string>((_, _) => title),
            SupportingContent = "support", SupportingContentTemplate = new FuncDataTemplate<string>((_, _) => support),
            Image = "image", ImageTemplate = new FuncDataTemplate<string>((_, _) => image),
            Leading = "leading", LeadingTemplate = new FuncDataTemplate<string>((_, _) => leading),
            Trailing = "trailing", TrailingTemplate = new FuncDataTemplate<string>((_, _) => trailing),
            Overline = "Section / 分类", IsSelectable = true, CommandParameter = "selected-B",
            Command = new ContentCommand(p => result = (string)p!)
        };
        AutomationProperties.SetName(row, "B / 内容条目");
        host.List.Children.Add(row);
        host.Render();
        Assert.All(new Control[] { title, support, image, leading, trailing }, c => Assert.True(c.IsEffectivelyVisible && c.Bounds.Width > 0));
        var leadingPoint = host.At(leading);
        host.Window.MouseDown(leadingPoint, MouseButton.Left);
        Assert.True(leading.IsPressed);
        host.Window.MouseUp(leadingPoint, MouseButton.Left);
        Assert.Equal("leading-B", result);
        Assert.False(row.IsSelected);
        host.Click(trailing);
        Assert.Equal("trailing-B", result);
        Assert.False(row.IsSelected);
        row.Focus();
        host.Window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        host.Window.KeyReleaseQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Assert.Equal("selected-B", result);
        Assert.True(row.IsSelected);
        Assert.Equal("B / 内容条目", ControlAutomationPeer.CreatePeerForElement(row)!.GetName());
    }

    [AvaloniaFact]
    public void Accessible_reorder_handle_exposes_named_move_actions_with_item_ownership()
    {
        using var host = new ContentHost();
        var a = new MaterialListItem { Title = "A", IsReorderEnabled = true };
        var b = new MaterialListItem { Title = "B" };
        host.List.Children.Add(a);
        host.List.Children.Add(b);
        host.Render();
        var handle = a.GetVisualDescendants().OfType<MaterialButton>().Single(c => AutomationProperties.GetName(c) == "Reorder item");
        ControlAutomationPeer.CreatePeerForElement(handle)!.GetProvider<IInvokeProvider>()!.Invoke();
        host.Render();
        var down = a.GetVisualDescendants().OfType<MaterialButton>().Single(c => AutomationProperties.GetName(c) == "Move item down" && c.IsEffectivelyVisible);
        ControlAutomationPeer.CreatePeerForElement(down)!.GetProvider<IInvokeProvider>()!.Invoke();
        host.Render();
        Assert.Same(a, host.List.Children[1]);
        Assert.False(a.IsSelected);
        a.Focus();
        host.Window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        host.Window.KeyReleaseQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        host.Render();
        Assert.False(down.IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public void Reveal_tracks_a_touch_drag_and_restores_the_closed_state_when_contact_is_cancelled()
    {
        using var host = new ContentHost();
        var action = new MaterialButton { Content = "Archive" };
        var row = new MaterialListItem { Title = "B", IsRevealEnabled = true, IsSelectable = true, RevealedActions = action };
        host.List.Children.Add(row);
        host.Render();
        var start = host.At(row, new Point(180, 24));
        var contact = host.Window.TouchBegin(start);
        host.Window.TouchMove(contact, start - new Vector(30, 0));
        host.Render();
        Assert.Equal(-30, row.RevealTranslation.Left);
        Assert.True(action.IsEffectivelyVisible && action.GetVisualAncestors().Contains(host.Window));
        Assert.False(row.IsRevealed || row.IsSelected);
        contact.Dispose();
        host.Render();
        Assert.Equal(default, row.RevealTranslation);
        Assert.False(row.IsRevealed || row.IsSelected || row.IsPressed);
    }

    [AvaloniaFact]
    public void Content_consumes_full_semantic_color_typography_shape_and_state_inputs_live()
    {
        using var host = new ContentHost();
        var badge = new MaterialBadge { Count = 7 };
        var row = new MaterialListItem { Title = "Title", SupportingContent = "Supporting", Overline = "Overline", Trailing = badge };
        var card = new MaterialCard { Title = "Card" };
        host.List.Children.Add(row);
        host.List.Children.Add(card);
        host.Theme.LightColorScheme = MaterialColorScheme.Light with { Error = Colors.Red, SurfaceContainerHighest = Colors.Blue };
        host.Theme.Shapes = new() { CornerMedium = 18 };
        host.Theme.Typography = new()
        {
            BodyMedium = new(18, 28, 0.3, FontWeight.Bold) { FontFamily = new FontFamily("Arial") },
            LabelSmall = new(13, 20, 0.7, FontWeight.Bold)
        };
        host.Render();
        Assert.Equal(Colors.Red, ((ISolidColorBrush)badge.Background!).Color);
        Assert.Equal(Colors.Blue, ((ISolidColorBrush)card.Background!).Color);
        Assert.Equal(new CornerRadius(18), card.CornerRadius);
        var supporting = row.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text == "Supporting");
        Assert.Equal(18, supporting.FontSize);
        Assert.Equal(28, supporting.LineHeight);
        Assert.Equal(0.3, supporting.LetterSpacing);
        Assert.Equal(FontWeight.Bold, supporting.FontWeight);
        Assert.Equal(new FontFamily("Arial"), supporting.FontFamily);
        Assert.Equal(13, badge.FontSize);
        host.Window.MouseMove(host.At(row));
        var hovered = host.Capture();
        host.Theme.States = new() { HoverStateLayerOpacity = 0.5 };
        Assert.NotEqual(hovered, host.Capture());
        host.Theme.SeedColor = Color.Parse("#006C4C");
        host.Render();
        Assert.NotEqual(Colors.Red, ((ISolidColorBrush)badge.Background!).Color);
        Assert.NotEqual(Colors.Blue, ((ISolidColorBrush)card.Background!).Color);
    }

    [AvaloniaFact]
    public void List_container_clips_its_rows_to_the_live_large_shape_without_an_extra_host_border()
    {
        using var host = new ContentHost();
        host.List.Margin = new Thickness(16);
        host.List.Children.Add(new MaterialListItem { Title = "Selected", IsSelected = true });
        host.Render();
        Assert.Equal(new CornerRadius(16), host.List.CornerRadius);
        Assert.Equal(Color.Parse("#FEF7FF"), host.PixelAt(host.At(host.List, new Point(1, 1))));
        host.Theme.Shapes = new() { CornerLarge = 24 };
        host.Render();
        Assert.Equal(new CornerRadius(24), host.List.CornerRadius);
        Assert.Equal(Color.Parse("#FEF7FF"), host.PixelAt(host.At(host.List, new Point(1, 1))));
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Revoking_primary_interaction_during_a_press_cancels_selection_and_clears_feedback(bool keyboard)
    {
        using var host = new ContentHost();
        var row = new MaterialListItem { Title = "A", IsSelectable = true };
        host.List.Children.Add(row);
        host.Render();
        if (keyboard)
        {
            row.Focus();
            host.Window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);
        }
        else host.Window.MouseDown(host.At(row), MouseButton.Left);
        Assert.True(row.IsPressed);
        row.IsInteractive = false;
        if (keyboard) host.Window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None);
        else host.Window.MouseUp(host.At(row), MouseButton.Left);
        Assert.False(row.IsSelected || row.IsPressed);
    }

    [AvaloniaFact]
    public void Tapping_the_reorder_handle_opens_named_actions_without_moving_or_selecting_the_item()
    {
        using var host = new ContentHost();
        var row = new MaterialListItem { Title = "A", IsReorderEnabled = true, IsSelectable = true };
        host.List.Children.Add(row);
        host.List.Children.Add(new MaterialListItem { Title = "B" });
        host.Render();
        var handle = row.GetVisualDescendants().OfType<MaterialButton>().Single(c => AutomationProperties.GetName(c) == "Reorder item");
        var point = host.At(handle);
        using var contact = host.Window.TouchBegin(point);
        host.Window.TouchEnd(contact, point);
        host.Render();
        Assert.True(row.IsReorderActionsVisible);
        Assert.False(row.IsReordering || row.IsSelected);
        Assert.Same(row, host.List.Children[0]);
        var down = row.GetVisualDescendants().OfType<MaterialButton>().Single(c => AutomationProperties.GetName(c) == "Move item down");
        host.Click(down);
        Assert.Same(row, host.List.Children[1]);
    }

    private static IEnumerable<AutomationPeer> AutomationDescendants(AutomationPeer peer)
    {
        foreach (var child in peer.GetChildren())
        {
            yield return child;
            foreach (var descendant in AutomationDescendants(child)) yield return descendant;
        }
    }

    private sealed class ContentCommand(Action<object?> execute) : ICommand
    {
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute(parameter);
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }

    private sealed class ContentHost : IDisposable
    {
        public MaterialTheme Theme { get; } = new() { Motion = new() { ReduceMotion = true } };
        public MaterialList List { get; } = new();
        public Window Window { get; }
        public ContentHost()
        {
            Application.Current!.Styles.Add(Theme);
            Window = new Window { Width = 440, Height = 700, RequestedThemeVariant = ThemeVariant.Light, Content = List };
            Window.Show();
        }
        public void Render()
        {
            using var frame = Window.CaptureRenderedFrame();
            Assert.NotNull(frame);
        }
        public byte[] Capture()
        {
            using var frame = Window.CaptureRenderedFrame()!;
            using var stream = new MemoryStream();
            frame.Save(stream, Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
            return stream.ToArray();
        }
        public Color PixelAt(Point point)
        {
            using var bitmap = Window.CaptureRenderedFrame()!;
            using var frame = bitmap.Lock();
            var offset = (int)(point.Y * Window.RenderScaling) * frame.RowBytes + (int)(point.X * Window.RenderScaling) * 4;
            var first = Marshal.ReadByte(frame.Address, offset);
            var green = Marshal.ReadByte(frame.Address, offset + 1);
            var third = Marshal.ReadByte(frame.Address, offset + 2);
            var alpha = Marshal.ReadByte(frame.Address, offset + 3);
            return frame.Format == PixelFormat.Bgra8888 ? Color.FromArgb(alpha, third, green, first) : Color.FromArgb(alpha, first, green, third);
        }
        public Point At(Control control, Point? point = null) => control.TranslatePoint(point ?? new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), Window)!.Value;
        public void Click(Control control, Point? point = null)
        {
            var p = At(control, point);
            Window.MouseDown(p, MouseButton.Left);
            Window.MouseUp(p, MouseButton.Left);
            Render();
        }
        public void Dispose()
        {
            Window.Close();
            Application.Current!.Styles.Remove(Theme);
        }
    }
}
