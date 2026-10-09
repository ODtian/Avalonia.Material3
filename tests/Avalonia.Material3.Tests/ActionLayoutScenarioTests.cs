using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Material3.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Material3.Tests;

public class ActionLayoutScenarioTests
{
    [AvaloniaFact]
    public void Icon_container_keeps_ordinary_border_template_styles()
    {
        var button = new MaterialIconButton { IconVariant = MaterialIconButtonVariant.Filled };
        button.Classes.Add("authored");
        button.Styles.Add(new Avalonia.Styling.Style(selector => selector.OfType<MaterialIconButton>().Class("authored").Template().OfType<Border>().Name("Container"))
        { Setters = { new Avalonia.Styling.Setter(Border.BackgroundProperty, Brushes.Red) } });
        using var host = new GeometryHost(button, 80, 80); host.Render();
        Assert.Equal(Colors.Red, host.Pixel(40, 30));
    }

    [AvaloniaTheory]
    [InlineData(MaterialIconButtonWidth.Default, 56)]
    [InlineData(MaterialIconButtonWidth.Wide, 72)]
    public void Overflow_intrinsic_measure_recovers_after_its_secondary_slot_was_constrained(MaterialIconButtonWidth widthMode, double nominal)
    {
        var group = new MaterialButtonGroup { Width = nominal + 174, HorizontalAlignment = HorizontalAlignment.Left };
        group.OverflowButton.Size = MaterialButtonSize.Medium; group.OverflowButton.WidthMode = widthMode;
        foreach (var item in new[] { ("First",100d), ("Second",60d), ("Third",400d) })
            group.Children.Add(new MaterialGroupButton { Content = item.Item1, Width = item.Item2 });
        using var host = new GeometryHost(group, 360, 100); host.Render();
        Assert.Equal(new[] { "Third" }, group.OverflowItems.Select(button => button.Content));
        for (var attempt = 0; attempt < 3; attempt++)
        {
            group.Width = nominal + 164; host.Render();
            Assert.Equal(new[] { "Second", "Third" }, group.OverflowItems.Select(button => button.Content));
            group.Width = nominal + 174; host.Render();
            Assert.Equal(new[] { "Third" }, group.OverflowItems.Select(button => button.Content));
        }
    }

    [AvaloniaFact]
    public void Hidden_action_icon_slot_changes_restore_its_current_intrinsic_size()
    {
        var group = new MaterialButtonGroup { Width = 1128 / 3.5, HorizontalAlignment = HorizontalAlignment.Left };
        group.OverflowButton.Width = 48;
        group.Children.Add(new MaterialGroupButton { Content = "Create", Width = 314 / 3.5 });
        group.Children.Add(new MaterialGroupButton { Content = "Edit", Width = 254 / 3.5 });
        var share = new MaterialGroupButton { Content = new Border { Width = 128 / 3.5, Height = 20 }, Padding = new Thickness(24, 8), LeadingIcon = new Border { Width = 20, Height = 20 } };
        group.Children.Add(share); group.Children.Add(new MaterialGroupButton { Content = "Disabled", Width = 400 / 3.5 });
        using var host = new GeometryHost(group, 360, 80); host.Window.SetRenderScaling(3.5); host.Render();
        Assert.Contains(share, group.OverflowItems);
        share.LeadingIcon = null; host.Render();
        Assert.DoesNotContain(share, group.OverflowItems); Assert.Contains(share, group.GetVisualDescendants());
        share.TrailingIcon = new Border { Width = 20, Height = 20 }; host.Render();
        Assert.Contains(share, group.OverflowItems);
        share.TrailingIcon = null; host.Render();
        Assert.DoesNotContain(share, group.OverflowItems); Assert.Contains(share, group.GetVisualDescendants());
    }

    [AvaloniaFact]
    public void Overflow_visual_container_uses_native_secondary_measure_width_while_touch_stays_48()
    {
        var group = new MaterialButtonGroup { Width = 1128 / 3.5, HorizontalAlignment = HorizontalAlignment.Left };
        group.OverflowButton.Width = 48;
        foreach (var item in new[] { ("Create",314), ("Edit",254), ("Share",296), ("Disabled",400) })
            group.Children.Add(new MaterialGroupButton { Content = item.Item1, Width = item.Item2 / 3.5 });
        using var host = new GeometryHost(group, 360, 80); host.Window.SetRenderScaling(3.5); host.Render();
        var box = GeometryHost.Box(group.OverflowButton, host.Window);
        Assert.Equal(168, box.Width * 3.5, precision: 6);
        Assert.Equal(Color.Parse("#FEF7FF"), host.Pixel(box.Left + 14d / 3.5, box.Center.Y));
        Assert.Equal(Color.Parse("#6750A4"), host.Pixel(box.Left + 15d / 3.5, box.Center.Y));
        Assert.Equal(Color.Parse("#FEF7FF"), host.Pixel(box.Right - 15d / 3.5, box.Center.Y));
    }

    [AvaloniaFact]
    public void Overflow_membership_reconciles_the_visual_tree_when_nested_content_remeasures()
    {
        var group = new MaterialButtonGroup { Width = 1128 / 3.5, HorizontalAlignment = HorizontalAlignment.Left };
        group.OverflowButton.Width = 48;
        var caption = new Border { Width = 400 / 3.5 - 48, Height = 20 };
        group.Children.Add(new MaterialGroupButton { Content = caption, Padding = new Thickness(24, 8) });
        foreach (var item in new[] { ("Edit",254), ("Share",296), ("Disabled",400) })
            group.Children.Add(new MaterialGroupButton { Content = item.Item1, Width = item.Item2 / 3.5 });
        var share = group.Children.OfType<MaterialGroupButton>().Single(button => Equals(button.Content, "Share"));
        using var host = new GeometryHost(group, 360, 80); host.Window.SetRenderScaling(3.5); host.Render();
        Assert.Equal(new[] { "Share", "Disabled" }, group.OverflowItems.Select(button => button.Content));
        caption.Width = 314 / 3.5 - 48; host.Render();
        Assert.Equal(new[] { "Disabled" }, group.OverflowItems.Select(button => button.Content));
        Assert.Contains(share, group.GetVisualDescendants());
        var box = GeometryHost.Box(share, host.Window);
        Assert.Equal(Color.Parse("#6750A4"), host.Pixel(box.Center.X, box.Top + 10));
        var clicked = 0; share.Click += (_, _) => clicked++;
        host.Window.MouseDown(box.Center, MouseButton.Left); host.Window.MouseUp(box.Center, MouseButton.Left);
        Assert.Equal(1, clicked);
    }

    [AvaloniaFact]
    public void Overflow_remeasurement_restores_the_actual_visible_action_after_initial_layout()
    {
        var group = new MaterialButtonGroup { Width = 1128 / 3.5, HorizontalAlignment = HorizontalAlignment.Left };
        group.OverflowButton.Width = 48;
        foreach (var item in new[] { ("Create",314), ("Edit",254), ("Share",400), ("Disabled",400) })
            group.Children.Add(new MaterialGroupButton { Content = item.Item1, Width = item.Item2 / 3.5 });
        var share = group.Children.OfType<MaterialGroupButton>().Single(button => Equals(button.Content, "Share"));
        using var host = new GeometryHost(group, 360, 80); host.Window.SetRenderScaling(3.5); host.Render();
        Assert.Equal(new[] { "Share", "Disabled" }, group.OverflowItems.Select(button => button.Content));
        share.Width = 296 / 3.5; host.Render();
        Assert.Equal(new[] { "Disabled" }, group.OverflowItems.Select(button => button.Content));
        Assert.Contains(share, group.GetVisualDescendants());
        var box = GeometryHost.Box(share, host.Window);
        Assert.Equal(Color.Parse("#6750A4"), host.Pixel(box.Center.X, box.Top + 10));
        share.Width = 400 / 3.5; host.Render();
        Assert.Equal(new[] { "Share", "Disabled" }, group.OverflowItems.Select(button => button.Content));
        share.Width = 296 / 3.5; host.Render();
        Assert.Contains(share, group.GetVisualDescendants());
    }

    [AvaloniaFact]
    public void Standalone_group_button_keeps_ordinary_measure_when_font_options_change()
    {
        var button = new MaterialGroupButton { Content = "Create" };
        using var host = new GeometryHost(button, 260, 80);
        host.Theme.Typography = host.Theme.Typography with
        { FontFamily = new FontFamily($"avares://{typeof(ActionLayoutScenarioTests).Assembly.GetName().Name}/ReferenceFonts#Roboto") };
        host.Window.SetRenderScaling(3.5); host.Render();
        var label = button.GetVisualDescendants().OfType<TextBlock>().Single(text => text.Text == "Create");
        Assert.Equal(147, label.Bounds.Width * 3.5, precision: 6);
        TextOptions.SetTextHintingMode(host.Window, TextHintingMode.None); host.Render();
        Assert.Equal(147, label.Bounds.Width * 3.5, precision: 6);
        TextOptions.SetTextHintingMode(host.Window, TextHintingMode.Strong); host.Render();
        Assert.Equal(147, label.Bounds.Width * 3.5, precision: 6);
    }

    [AvaloniaFact]
    public void Unconnected_overflow_uses_native_remaining_space_before_the_final_gap()
    {
        var group = new MaterialButtonGroup { Width = 1128 / 3.5, HorizontalAlignment = HorizontalAlignment.Left };
        group.OverflowButton.Width = 48;
        foreach (var item in new[] { ("Create",314), ("Edit",254), ("Share",296), ("Disabled",400) })
            group.Children.Add(new MaterialGroupButton { Content = item.Item1, Width = item.Item2 / 3.5 });
        using var host = new GeometryHost(group, 1128 / 3.5, 48); host.Window.SetRenderScaling(3.5); host.Render();
        Assert.Equal(48, group.OverflowButton.DesiredSize.Width, precision: 6);
        Assert.Equal(1128, group.Bounds.Width * 3.5, precision: 6);
        Assert.Equal(314, group.Children[0].DesiredSize.Width * 3.5, precision: 6);
        Assert.Equal(254, group.Children[1].DesiredSize.Width * 3.5, precision: 6);
        Assert.Equal(296, group.Children[2].DesiredSize.Width * 3.5, precision: 6);
        Assert.Equal(new[] { "Disabled" }, group.OverflowItems.Select(button => button.Content));
        Assert.Equal(-15, group.Children[0].Bounds.Left * 3.5, precision: 6);
        Assert.Equal(341, group.Children[1].Bounds.Left * 3.5, precision: 6);
        Assert.Equal(637, group.Children[2].Bounds.Left * 3.5, precision: 6);
    }

    [AvaloniaFact]
    public void Extended_fab_preserves_parent_data_templates_for_non_string_content()
    {
        var content = new ActionCaption("Authored action");
        var authored = new Border { Width = 80, Height = 20, Background = Brushes.Black };
        var fab = new MaterialExtendedFab { Content = content };
        using var host = new GeometryHost(fab, 220, 120);
        host.Window.DataTemplates.Add(new FuncDataTemplate<ActionCaption>((_, _) => authored));
        fab.Content = null; fab.Content = content; host.Render();
        Assert.Contains(authored, fab.GetVisualDescendants());
    }
    private sealed record ActionCaption(string Text);

    [AvaloniaFact]
    public void Extended_fab_preserves_parent_string_templates_and_explicit_caller_templates()
    {
        var parent = new Border { Width = 80, Height = 20, Background = Brushes.Black };
        var explicitContent = new Border { Width = 90, Height = 20, Background = Brushes.Blue };
        var fab = new MaterialExtendedFab { Content = "Create" };
        fab.DataTemplates.Add(new FuncDataTemplate<string>((_, _) => parent));
        using var host = new GeometryHost(fab, 220, 120);
        Assert.Contains(parent, fab.GetVisualDescendants());
        fab.ContentTemplate = new FuncDataTemplate<string>((_, _) => explicitContent); host.Render();
        Assert.Contains(explicitContent, fab.GetVisualDescendants());
    }

    [AvaloniaFact]
    public void Extended_default_text_keeps_framework_live_direction_and_inherited_text_style_rendering()
    {
        var fab = new MaterialExtendedFab { Content = "ABC אבג long wrapped caption", HorizontalAlignment = HorizontalAlignment.Stretch };
        fab.SetValue(TextBlock.MaxLinesProperty, 1);
        fab.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.End);
        fab.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
        using var host = new GeometryHost(fab, 160, 100);
        fab.FlowDirection = FlowDirection.RightToLeft; host.Render();
        using var original = host.Window.CaptureRenderedFrame()!;
        fab.ContentTemplate = new FuncDataTemplate<string>((text, _) => new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap }); host.Render();
        using var expected = host.Window.CaptureRenderedFrame()!;
        using var first = original.Lock(); using var second = expected.Lock();
        var differences = 0;
        for (var y = 0; y < original.PixelSize.Height; y++)
        for (var x = 0; x < original.PixelSize.Width * 4; x++)
            if (System.Runtime.InteropServices.Marshal.ReadByte(first.Address, y * first.RowBytes + x) !=
                System.Runtime.InteropServices.Marshal.ReadByte(second.Address, y * second.RowBytes + x)) differences++;
        Assert.Equal(0, differences);
    }

    [AvaloniaTheory]
    [InlineData(1.25)]
    [InlineData(3.5)]
    public void Settled_extended_fab_has_an_integer_native_content_extent(double density)
    {
        var fab = new MaterialExtendedFab { Size = MaterialFabSize.Small, Icon = new Border { Width = 24, Height = 24 },
            Content = "Extended FAB", VerticalAlignment = VerticalAlignment.Top };
        using var host = new GeometryHost(fab, 240, 120); host.Window.SetRenderScaling(density); host.Render();
        Assert.Equal(Math.Ceiling(fab.Bounds.Width * density), fab.Bounds.Width * density, precision: 6);
        var original = fab.Bounds.Width;
        fab.IsExpanded = false; host.Render(); fab.IsExpanded = true; host.Render();
        Assert.Equal(original, fab.Bounds.Width);
    }

    [AvaloniaTheory]
    [InlineData(false, 1.25, 51, 5)]
    [InlineData(true, 1.25, 51, 5)]
    [InlineData(false, 3.5, 140, 14)]
    public void Stock_action_padding_rounds_each_native_physical_edge(bool group, double density, int height, int top)
    {
        MaterialButton button = group ? new MaterialGroupButton() : new MaterialButton();
        button.Content = new Border { Width = 24, Height = 20 };
        button.Padding = new Thickness(16, 10); button.Background = Brushes.Black;
        button.VerticalAlignment = VerticalAlignment.Top;
        using var host = new GeometryHost(button, 160, 100); host.Window.SetRenderScaling(density); host.Render();
        using var bitmap = host.Window.CaptureRenderedFrame()!; using var pixels = bitmap.Lock();
        var x = (int)(button.Bounds.Center.X * density); var rows = new List<int>();
        for (var y = 0; y < bitmap.PixelSize.Height; y++)
            if (System.Runtime.InteropServices.Marshal.ReadByte(pixels.Address, y * pixels.RowBytes + x * 4 + 1) == 0) rows.Add(y);
        Assert.Equal(top, rows[0]); Assert.Equal(height, rows.Count);
        Assert.Equal(new Thickness(16, 10), button.Padding);
        Assert.Equal(48 * density, button.Bounds.Height * density, precision: 6);
    }

    [AvaloniaFact]
    public void Button_group_uses_its_native_available_width_without_a_shadow_measurement_inset()
    {
        var group = new MaterialButtonGroup { VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Left };
        var first = new MaterialGroupButton { Content = "First" }; var last = new MaterialGroupButton { Content = "Last" };
        group.Children.Add(first); group.Children.Add(last);
        using var host = new GeometryHost(group, 220, 120);
        Assert.Equal(0, first.Bounds.Left); Assert.Equal(group.Bounds.Width, last.Bounds.Right);
        Assert.Equal(48, group.Bounds.Height);
    }

    [AvaloniaFact]
    public void Split_small_actions_wrap_the_native_48_DIP_target_with_only_the_2_DIP_join_gap()
    {
        var split = new MaterialSplitButton { VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Left };
        split.MainButton.Content = "Save";
        using var host = new GeometryHost(split, 220, 120);
        Assert.Equal(48, split.Bounds.Height);
        Assert.Equal(0, split.MainButton.Bounds.Left);
        Assert.Equal(2, split.SecondaryButton.Bounds.Left - split.MainButton.Bounds.Right);
        Assert.Equal(split.Bounds.Width, split.SecondaryButton.Bounds.Right);
    }

    [AvaloniaFact]
    public void Small_button_has_a_48_DIP_target_and_a_centered_40_DIP_container_without_horizontal_shadow_padding()
    {
        var button = new MaterialButton { Content = "Save", VerticalAlignment = VerticalAlignment.Top };
        using var host = new GeometryHost(button, 220, 120);
        Assert.Equal(48, button.Bounds.Height);
        Assert.Equal(Color.Parse("#6750A4"), host.Pixel(button.Bounds.Center.X, 4));
        Assert.Equal(Color.Parse("#FEF7FF"), host.Pixel(button.Bounds.Center.X, 3));
        Assert.Equal(Color.Parse("#6750A4"), host.Pixel(1, 24));
    }

    [AvaloniaTheory]
    [InlineData(MaterialFabSize.Standard, 56, 1)]
    [InlineData(MaterialFabSize.Small, 48, 5)]
    public void Fab_container_and_interactive_target_follow_native_extents(MaterialFabSize size, double target, double paintedX)
    {
        var fab = new MaterialFab { Size = size, VerticalAlignment = VerticalAlignment.Top };
        using var host = new GeometryHost(fab, 220, 120);
        host.Theme.Elevation = host.Theme.Elevation with { Shadow3 = default }; host.Render();
        Assert.Equal(target, fab.Bounds.Width); Assert.Equal(target, fab.Bounds.Height);
        Assert.Equal(Color.Parse("#EADDFF"), host.Pixel(paintedX, target / 2));
    }
}
