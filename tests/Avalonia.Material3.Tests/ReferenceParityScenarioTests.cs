using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.VisualTree;
using Material3.ReferenceUi;
using Xunit;
using Avalonia.Media;
using Avalonia.Platform;
using System.Runtime.InteropServices;

namespace Avalonia.Material3.Tests;

public class ReferenceParityScenarioTests
{
    [AvaloniaFact]
    public void Reference_buttons_restore_visible_actions_after_small_viewport_and_live_density_initialization()
    {
        using var host = new ReferenceHost();
        host.Window.Width = 1; host.Window.Height = 1; host.Render();
        host.Shell.MaterialTheme.Typography = host.Shell.MaterialTheme.Typography with
        { FontFamily = new FontFamily($"avares://{typeof(ReferenceParityScenarioTests).Assembly.GetName().Name}/ReferenceFonts#Roboto") };
        host.Shell.Navigate("buttons"); host.Render();
        host.Window.SetRenderScaling(3.5); host.Window.Width = 354.285714; host.Window.Height = 744; host.Render();
        var group = host.Find<MaterialButtonGroup>("button-group");
        var share = group.Children.OfType<MaterialGroupButton>().Single(button => Equals(button.Content, "Share"));
        foreach (var mode in new[] { TextHintingMode.None, TextHintingMode.Strong, TextHintingMode.None, TextHintingMode.Strong })
        {
            TextOptions.SetTextHintingMode(host.Window, mode); host.Render();
            Assert.Equal(new[] { "Disabled" }, group.OverflowItems.Select(button => button.Content));
            Assert.Contains(share, group.GetVisualDescendants());
            using var bitmap = host.Window.CaptureRenderedFrame()!; using var pixels = bitmap.Lock();
            var box = GeometryHost.Box(share, host.Window);
            var x = (int)(box.Center.X * 3.5); var y = (int)((box.Top + 10) * 3.5);
            var offset = y * pixels.RowBytes + x * 4;
            var red = pixels.Format == PixelFormat.Rgba8888 ? 0 : 2;
            Assert.Equal(103, Marshal.ReadByte(pixels.Address, offset + red));
            Assert.Equal(80, Marshal.ReadByte(pixels.Address, offset + 1));
            Assert.Equal(164, Marshal.ReadByte(pixels.Address, offset + 2 - red));
        }
    }

    [AvaloniaTheory]
    [InlineData(1.25)]
    [InlineData(3.5)]
    public void Reference_unconnected_group_keeps_native_three_actions_before_overflow_at_phone_density(double density)
    {
        using var host = new ReferenceHost("buttons");
        host.Shell.MaterialTheme.Typography = host.Shell.MaterialTheme.Typography with
        { FontFamily = new FontFamily($"avares://{typeof(ReferenceParityScenarioTests).Assembly.GetName().Name}/ReferenceFonts#Roboto") };
        host.Window.SetRenderScaling(density); host.Render();
        var group = host.Find<MaterialButtonGroup>("button-group");
        Assert.Equal(new[] { "Disabled" }, group.OverflowItems.Select(button => button.Content));
    }

    [AvaloniaFact]
    public void Reference_group_captions_use_actual_native_integer_paragraph_widths()
    {
        using var host = new ReferenceHost("buttons");
        host.Shell.MaterialTheme.Typography = host.Shell.MaterialTheme.Typography with
        { FontFamily = new FontFamily($"avares://{typeof(ReferenceParityScenarioTests).Assembly.GetName().Name}/ReferenceFonts#Roboto") };
        host.Window.SetRenderScaling(3.5); host.Render();
        var group = host.Find<MaterialButtonGroup>("button-group");
        foreach (var item in new[] { ("Create",146), ("Edit",86), ("Share",128) })
        {
            var button = group.Children.OfType<MaterialGroupButton>().Single(button => Equals(button.Content, item.Item1));
            var label = button.GetVisualDescendants().OfType<TextBlock>().Single(text => text.Text == item.Item1);
            Assert.Equal(item.Item2, label.Bounds.Width * 3.5, precision: 6);
        }
    }

    [AvaloniaFact]
    public void Group_native_caption_preserves_runtime_hinting_and_parent_template_priority()
    {
        using var host = new ReferenceHost("buttons");
        host.Shell.MaterialTheme.Typography = host.Shell.MaterialTheme.Typography with
        { FontFamily = new FontFamily($"avares://{typeof(ReferenceParityScenarioTests).Assembly.GetName().Name}/ReferenceFonts#Roboto") };
        host.Window.SetRenderScaling(3.5); host.Render();
        var group = host.Find<MaterialButtonGroup>("button-group");
        var create = group.Children.OfType<MaterialGroupButton>().Single(button => Equals(button.Content, "Create"));
        TextBlock Caption() => create.GetVisualDescendants().OfType<TextBlock>().Single(text => text.Text == "Create");
        Assert.Equal(146, Caption().Bounds.Width * 3.5, precision: 6);
        TextOptions.SetTextHintingMode(host.Window, TextHintingMode.None); host.Render();
        Assert.InRange(Caption().Bounds.Width * 3.5, 146.17, 146.20);
        TextOptions.SetTextHintingMode(host.Window, TextHintingMode.Strong); host.Render();
        Assert.Equal(146, Caption().Bounds.Width * 3.5, precision: 6);
        var authored = new Border { Width = 60, Height = 20, Background = Brushes.Green };
        host.Window.DataTemplates.Add(new Avalonia.Controls.Templates.FuncDataTemplate<string>(text => text == "Edit", (_, _) => authored));
        var edit = group.Children.OfType<MaterialGroupButton>().Single(button => Equals(button.Content, "Edit"));
        edit.Content = null; edit.Content = "Edit"; host.Render();
        Assert.Contains(authored, edit.GetVisualDescendants());
    }

    [AvaloniaFact]
    public void Native_clock_selector_edge_uses_clear_xor_and_destination_over_colour()
    {
        using var host = new ReferenceHost("time");
        host.Shell.MaterialTheme.Typography = host.Shell.MaterialTheme.Typography with
        {
            FontFamily = new FontFamily($"avares://{typeof(ReferenceParityScenarioTests).Assembly.GetName().Name}/ReferenceFonts#Roboto")
        };
        host.Window.SetRenderScaling(3.5); host.Find<MaterialTimePicker>("time-picker").ActivePart = MaterialTimePickerPart.Minute; host.Render();
        var dial = host.Shell.GetVisualDescendants().OfType<MaterialClockDial>().Single();
        var box = GeometryHost.Box(dial, host.Window);
        // Native actual sample840,1124 relative to face172,1022:668,102px.
        using var bitmap = host.Window.CaptureRenderedFrame()!;
        using var pixels = bitmap.Lock();
        var x = (int)Math.Round(box.Left * 3.5) + 668; var y = (int)Math.Round(box.Top * 3.5) + 102;
        var offset = y * pixels.RowBytes + x * 4;
        var red = pixels.Format == PixelFormat.Rgba8888 ? 0 : 2;
        // This headless raster backend covers12/255 of this circle pixel; the
        // actual GPU's different coverage and175/170/179 remain physical gates.
        const double c = 12d / 255;
        double Blend(double b, double p) => b * (1 - c) * (1 - c) + p * c * c + 255 * 2 * c * c * (1 - c);
        Assert.InRange(Marshal.ReadByte(pixels.Address, offset + red), (int)Blend(230, 103), (int)Math.Ceiling(Blend(230, 103)));
        Assert.InRange(Marshal.ReadByte(pixels.Address, offset + 1), (int)Blend(224, 80), (int)Math.Ceiling(Blend(224, 80)));
        Assert.InRange(Marshal.ReadByte(pixels.Address, offset + 2 - red), (int)Blend(233, 164), (int)Math.Ceiling(Blend(233, 164)));
    }

    [AvaloniaTheory]
    [InlineData(1.25, 52, 142)]
    [InlineData(3.5, 146, 398)]
    public void Reference_extended_fab_wraps_the_actual_native_Create_paragraph(double density, double paragraphWidth, double surfaceWidth)
    {
        using var host = new ReferenceHost("fab");
        host.Shell.MaterialTheme.Typography = host.Shell.MaterialTheme.Typography with
        {
            FontFamily = new FontFamily($"avares://{typeof(ReferenceParityScenarioTests).Assembly.GetName().Name}/ReferenceFonts#Roboto")
        };
        host.Window.SetRenderScaling(density); host.Render();
        var fab = host.Shell.GetVisualDescendants().OfType<MaterialExtendedFab>().Single();
        var label = fab.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.ContentPresenter>()
            .Single(presenter => presenter.Name == "PART_ContentPresenter").Child!;
        Assert.Equal(paragraphWidth, label.Bounds.Width * density, precision: 5);
        Assert.Equal(surfaceWidth, fab.Bounds.Width * density, precision: 5);
    }

    [AvaloniaTheory]
    [InlineData(TextHintingMode.None)]
    [InlineData(TextHintingMode.Light)]
    public void Extended_native_label_keeps_caller_hinting_measure_and_paint_routes_coherent(TextHintingMode hint)
    {
        using var host = new ReferenceHost("fab");
        host.Shell.MaterialTheme.Typography = host.Shell.MaterialTheme.Typography with
        {
            FontFamily = new FontFamily($"avares://{typeof(ReferenceParityScenarioTests).Assembly.GetName().Name}/ReferenceFonts#Roboto")
        };
        host.Window.SetRenderScaling(3.5); host.Render();
        var fab = host.Shell.GetVisualDescendants().OfType<MaterialExtendedFab>().Single();
        Assert.Equal(398, fab.Bounds.Width * 3.5, precision: 5);
        TextOptions.SetTextHintingMode(host.Window, hint); host.Render();
        Assert.Equal(399, fab.Bounds.Width * 3.5, precision: 5);
        TextOptions.SetTextHintingMode(host.Window, TextHintingMode.Strong); host.Render();
        Assert.Equal(398, fab.Bounds.Width * 3.5, precision: 5);
    }

    [AvaloniaFact]
    public void Extended_label_preserves_an_explicit_caller_rendering_mode()
    {
        using var host = new ReferenceHost("fab");
        host.Shell.MaterialTheme.Typography = host.Shell.MaterialTheme.Typography with
        {
            FontFamily = new FontFamily($"avares://{typeof(ReferenceParityScenarioTests).Assembly.GetName().Name}/ReferenceFonts#Roboto")
        };
        host.Window.SetRenderScaling(3.5); host.Render();
        var fab = host.Shell.GetVisualDescendants().OfType<MaterialExtendedFab>().Single();
        Assert.Equal(398, fab.Bounds.Width * 3.5, precision: 5);
        TextOptions.SetTextRenderingMode(host.Window, TextRenderingMode.Alias); host.Render();
        Assert.Equal(399, fab.Bounds.Width * 3.5, precision: 5);
        TextOptions.SetTextRenderingMode(host.Window, TextRenderingMode.Antialias); host.Render();
        Assert.Equal(398, fab.Bounds.Width * 3.5, precision: 5);
    }

    [AvaloniaFact]
    public void Composite_action_glyphs_keep_the_framework_paragraph_path()
    {
        using var host = new ReferenceHost("fab");
        host.Shell.MaterialTheme.Typography = host.Shell.MaterialTheme.Typography with
        {
            FontFamily = new FontFamily($"avares://{typeof(ReferenceParityScenarioTests).Assembly.GetName().Name}/ReferenceFonts#Roboto")
        };
        host.Window.SetRenderScaling(3.5);
        var fab = host.Shell.GetVisualDescendants().OfType<MaterialExtendedFab>().Single();
        fab.Content = "Create:;"; host.Render();
        using var original = host.Window.CaptureRenderedFrame()!;
        fab.ContentTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<string>((text, _) => new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap }); host.Render();
        using var expected = host.Window.CaptureRenderedFrame()!;
        using var first = original.Lock(); using var second = expected.Lock();
        var different = 0;
        for (var y = 0; y < original.PixelSize.Height; y++)
        for (var x = 0; x < original.PixelSize.Width * 4; x++)
            if (Marshal.ReadByte(first.Address, y * first.RowBytes + x) != Marshal.ReadByte(second.Address, y * second.RowBytes + x)) different++;
        Assert.Equal(0, different);
    }

    [AvaloniaFact]
    public void Reference_ripple_buttons_use_native_touch_padding_and_the_51_pixel_surface_at_125_percent()
    {
        using var host = new ReferenceHost("ripple"); host.Window.SetRenderScaling(1.25); host.Render();
        var button = host.Find<MaterialButton>("ripple-elevated"); button.BringIntoView(); host.Render();
        Assert.Equal(new Thickness(16, 10), button.Padding);
        var box = GeometryHost.Box(button, host.Window);
        using var bitmap = host.Window.CaptureRenderedFrame()!; using var pixels = bitmap.Lock();
        var x = (int)((box.Left + 32) * 1.25); var filled = 0;
        for (var y = (int)(box.Top * 1.25); y < (int)(box.Bottom * 1.25); y++)
        {
            var offset = y * pixels.RowBytes + x * 4;
            var red = pixels.Format == PixelFormat.Rgba8888 ? 0 : 2;
            if (Marshal.ReadByte(pixels.Address, offset + red) == 247 && Marshal.ReadByte(pixels.Address, offset + 1) == 242
                && Marshal.ReadByte(pixels.Address, offset + 2 - red) == 250) filled++;
        }
        Assert.Equal(51, filled);
    }

    [AvaloniaTheory]
    [InlineData("open-dialog")]
    [InlineData("open-side-sheet")]
    public void Reference_overlays_apply_the_native_window_safe_area(string action)
    {
        using var host = new ReferenceHost("overlays");
        host.Window.Height = 792; host.Shell.SetSafeArea(new Thickness(0, 24, 0, 0)); host.Render();
        host.Click(action);
        Control surface = action == "open-dialog" ? host.Shell.Overlay.GetVisualDescendants().OfType<MaterialDialog>().Single()
            : host.Shell.Overlay.GetVisualDescendants().OfType<MaterialSideSheet>().Single();
        var box = GeometryHost.Box(surface, host.Window);
        if (action == "open-dialog") Assert.Equal(408, box.Center.Y, 3);
        else { Assert.Equal(24, box.Top, 3); Assert.Equal(792, box.Bottom, 3); }
    }

    [AvaloniaTheory]
    [InlineData("selection-disabled", 2)]
    [InlineData("selection-disabled-off", 0)]
    public void Disabled_selection_profile_keeps_both_radio_and_switch_states_visible(string scene, int selectedSwitches)
    {
        using var host = new ReferenceHost(scene);
        var radios = host.Shell.GetVisualDescendants().OfType<MaterialRadioButton>().ToArray();
        Assert.Equal(3, radios.Length); Assert.Single(radios, radio => radio.IsChecked == true);
        Assert.All(radios, radio => Assert.False(radio.IsEffectivelyEnabled));
        var switches = host.Shell.GetVisualDescendants().OfType<MaterialSwitch>().ToArray();
        Assert.Equal(4, switches.Length); Assert.Equal(selectedSwitches, switches.Count(control => control.IsChecked == true));
        Assert.All(switches, control => Assert.False(control.IsEffectivelyEnabled));
    }

    [AvaloniaFact]
    public void Reference_large_fab_keeps_the_native_24_DIP_content_icon_inside_its_standard_36_DIP_canvas()
    {
        using var host = new ReferenceHost("fab");
        var large = host.Shell.GetVisualDescendants().OfType<MaterialFab>().Single(fab => fab.Size == MaterialFabSize.Large);
        var icon = large.GetVisualDescendants().OfType<MaterialSymbol>().Single();
        Assert.Equal(36, large.IconSize);
        var box = GeometryHost.Box(icon, host.Window);
        Assert.Equal(new Size(24, 24), box.Size);
        Assert.Equal(GeometryHost.Box(large, host.Window).Center, box.Center);
    }

    [AvaloniaFact]
    public void Reference_elevated_content_lambda_keeps_native_24_DIP_icon_geometry_and_touch_padding()
    {
        using var host = new ReferenceHost("buttons");
        var button = host.Find<MaterialButton>("elevated-button");
        button.BringIntoView(); host.Render();
        var icon = button.GetVisualDescendants().OfType<MaterialSymbol>().Single();
        Assert.Equal(new Size(24, 24), GeometryHost.Box(icon, host.Window).Size);
        Assert.Equal(new Thickness(16, 10), button.Padding);
    }

    [AvaloniaTheory]
    [InlineData("button-group", 24, 8, 24)]
    [InlineData("button-group-single", 16, 8, 24)]
    [InlineData("connected-group", 16, 10, 16)]
    public void Reference_button_groups_preserve_the_exact_native_overload_padding(string id, double left, double vertical, double right)
    {
        using var host = new ReferenceHost("buttons");
        var buttons = host.Find<MaterialButtonGroup>(id).Children.OfType<MaterialGroupButton>().ToArray();
        Assert.All(buttons, button => Assert.Equal(new Thickness(left, vertical, right, vertical), button.Padding));
        if (id == "button-group-single") Assert.All(buttons, button => Assert.Equal(24, button.IconSize));
        if (id == "button-group") Assert.Equal(new[] { "Disabled" }, host.Find<MaterialButtonGroup>(id).OverflowItems.Select(button => button.Content));
    }

    [AvaloniaFact]
    public void Reference_fab_menu_retains_native_intrinsic_16_DIP_padding_inside_the_16_DIP_scene()
    {
        using var host = new ReferenceHost("fab");
        var trigger = host.Find<MaterialFab>("fab-menu-toggle");
        var box = GeometryHost.Box(trigger, host.Window);
        Assert.Equal(32, host.Shell.Overlay.Bounds.Width - box.Right, 3);
        Assert.Equal(32, host.Window.ClientSize.Height - box.Bottom, 3);
    }

    [AvaloniaFact]
    public void Empty_supporting_slot_uses_no_height_and_live_counter_or_error_keeps_the_native_20_DIP_feedback_row()
    {
        var field = new MaterialTextField { Label = "Amount" };
        using var host = new GeometryHost(field, 320, 200);
        field.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top; host.Render();
        Assert.Equal(56, field.Bounds.Height);
        field.ShowCounter = true; host.Render(); Assert.Equal(76, field.Bounds.Height);
        field.ShowCounter = false; host.Render(); Assert.Equal(56, field.Bounds.Height);
        field.ErrorText = "Invalid"; host.Render(); Assert.Equal(76, field.Bounds.Height);
        field.ErrorText = null; host.Render(); Assert.Equal(56, field.Bounds.Height);
        field.SupportingText = ""; host.Render(); Assert.Equal(76, field.Bounds.Height);
    }

    [AvaloniaFact]
    public void Button_group_overflow_uses_the_native_vertical_more_symbol()
    {
        var group = new MaterialButtonGroup();
        Assert.Equal("more_vert", Assert.IsType<MaterialSymbol>(group.OverflowButton.Content).Symbol);
    }

    [AvaloniaFact]
    public void Reference_single_calendar_wraps_to_the_native_512_DIP_surface()
    {
        using var host = new ReferenceHost("date-single");
        var picker = host.Find<MaterialDatePicker>("date-picker");
        Assert.Equal(512, picker.Bounds.Height, 3);
        var lastDay = picker.GetVisualDescendants().OfType<MaterialCalendarDay>().Single(day => day.Date == new DateOnly(2024, 2, 29));
        Assert.True(lastDay.IsEffectivelyVisible && GeometryHost.Box(lastDay, host.Window).Bottom <= GeometryHost.Box(picker, host.Window).Bottom);
    }

    [AvaloniaFact]
    public void Reference_toolbar_Escape_returns_focus_to_an_available_core_action()
    {
        using var host = new ReferenceHost("fab");
        host.Find<MaterialIconButton>("toolbar-menu").Focus();
        host.Window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None); host.Render();
        Assert.True(host.Find<MaterialIconButton>("toolbar-edit").IsKeyboardFocusWithin);
    }

    [AvaloniaFact]
    public void Reference_date_dialog_centers_inside_the_native_safe_area_while_scrim_covers_the_window()
    {
        using var host = new ReferenceHost("date-range-9-16");
        host.Window.Height = 792; host.Shell.SetSafeArea(new Thickness(0, 24, 0, 0)); host.Render();
        host.Click("open-date-dialog");
        var dialog = host.Shell.Overlay.GetVisualDescendants().OfType<MaterialDialog>().Single();
        var bounds = new Rect(dialog.Bounds.Size).TransformToAABB(dialog.TransformToVisual(host.Window)!.Value);
        Assert.Equal(408, bounds.Center.Y, 3);
        Assert.Equal(host.Shell.Overlay.Bounds.Width, bounds.Width, 3);
    }

    [AvaloniaFact]
    public void Reference_native_button_families_and_split_wrap_geometry_are_preserved()
    {
        using var host = new ReferenceHost("buttons");
        var group = host.Find<MaterialButtonGroup>("button-group");
        Assert.All(group.Children.OfType<MaterialGroupButton>(), button => Assert.Equal(MaterialButtonVariant.Filled, button.Variant));
        foreach (var id in new[] { "button-group-single", "connected-group" })
            Assert.All(host.Find<MaterialButtonGroup>(id).Children.OfType<MaterialGroupButton>(), button => Assert.Equal(MaterialButtonVariant.Filled, button.Variant));
        foreach (var label in new[] { "Extra-small", "Small", "Medium", "Large", "Extra-large" })
            Assert.Equal(Avalonia.Layout.HorizontalAlignment.Left, host.Find<MaterialSplitButton>("split-" + label).HorizontalAlignment);
    }

    [AvaloniaFact]
    public void Reference_floating_toolbar_exposes_only_four_native_actions_and_keeps_core_actions_when_collapsed()
    {
        using var host = new ReferenceHost("fab");
        var toolbar = host.Find<MaterialToolbar>("floating-toolbar");
        var visible = toolbar.GetVisualDescendants().OfType<Avalonia.Controls.Button>().Where(button => button.IsEffectivelyVisible).ToArray();
        Assert.Equal(4, visible.Length);
        host.Click("toolbar-toggle");
        Assert.False(toolbar.IsExpanded);
        Assert.True(host.Find<MaterialIconButton>("toolbar-edit").IsEffectivelyVisible);
        Assert.True(host.Find<MaterialIconButton>("toolbar-share").IsEffectivelyVisible);
        Assert.False(host.Find<MaterialIconButton>("toolbar-menu").IsEffectivelyVisible);
        Assert.False(host.Find<MaterialIconButton>("toolbar-more").IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public void Reference_compose_slider_track_reaches_the_native_thumb_core_inset()
    {
        using var host = new ReferenceHost("slider");
        var slider = host.Find<MaterialSlider>("slider");
        var point = slider.TranslatePoint(new Point(10, 24), host.Window)!.Value;
        using var bitmap = host.Window.CaptureRenderedFrame()!; using var pixels = bitmap.Lock();
        var offset = (int)point.Y * pixels.RowBytes + (int)point.X * 4;
        var first = Marshal.ReadByte(pixels.Address, offset); var green = Marshal.ReadByte(pixels.Address, offset + 1); var third = Marshal.ReadByte(pixels.Address, offset + 2);
        Assert.Equal(Color.Parse("#6750A4"), pixels.Format == PixelFormat.Bgra8888 ? Color.FromRgb(third, green, first) : Color.FromRgb(first, green, third));
        var end = slider.TranslatePoint(new Point(2, 24), host.Window)!.Value;
        host.Window.MouseDown(end, MouseButton.Left); host.Window.MouseUp(end, MouseButton.Left); host.Render();
        Assert.Equal(0, slider.Value);
    }

    [AvaloniaFact]
    public void Reference_range_dialog_keeps_the_native_rounded_surface_corners()
    {
        using var host = new ReferenceHost("date-range-9-16");
        host.Shell.MaterialTheme.Elevation = host.Shell.MaterialTheme.Elevation with { Shadow3 = default };
        using var before = host.Window.CaptureRenderedFrame()!;
        host.Click("open-date-dialog");
        var dialog = host.Shell.Overlay.GetVisualDescendants().OfType<MaterialDialog>().Single();
        Assert.Equal(host.Shell.Overlay.Bounds.Width, dialog.Bounds.Width, 3);
        var corner = dialog.TranslatePoint(new Point(1, 1), host.Window)!.Value;
        using var bitmap = host.Window.CaptureRenderedFrame()!; using var pixels = bitmap.Lock();
        var offset = (int)(corner.Y * host.Window.RenderScaling) * pixels.RowBytes + (int)(corner.X * host.Window.RenderScaling) * 4;
        var first = Marshal.ReadByte(pixels.Address, offset); var green = Marshal.ReadByte(pixels.Address, offset + 1); var third = Marshal.ReadByte(pixels.Address, offset + 2);
        var color = pixels.Format == PixelFormat.Bgra8888 ? Color.FromRgb(third, green, first) : Color.FromRgb(first, green, third);
        using var original = before.Lock();
        var oldFirst = Marshal.ReadByte(original.Address, offset); var oldGreen = Marshal.ReadByte(original.Address, offset + 1); var oldThird = Marshal.ReadByte(original.Address, offset + 2);
        var oldRed = original.Format == PixelFormat.Bgra8888 ? oldThird : oldFirst;
        var oldBlue = original.Format == PixelFormat.Bgra8888 ? oldFirst : oldThird;
        Assert.InRange(Math.Abs(color.R - Math.Round(oldRed * .68)), 0, 1);
        Assert.InRange(Math.Abs(color.G - Math.Round(oldGreen * .68)), 0, 1);
        Assert.InRange(Math.Abs(color.B - Math.Round(oldBlue * .68)), 0, 1);
    }

    [AvaloniaFact]
    public void Reference_Roboto_assets_match_400_500_and_700_without_font_simulation()
    {
        var family = new FontFamily($"avares://{typeof(ReferenceParityScenarioTests).Assembly.GetName().Name}/ReferenceFonts#Roboto");
        foreach (var weight in new[] { FontWeight.Normal, FontWeight.Medium, FontWeight.Bold })
        {
            Assert.True(FontManager.Current.TryGetGlyphTypeface(new Typeface(family, FontStyle.Normal, weight), out var face));
            Assert.Equal(weight, face.Weight); Assert.Equal(FontSimulations.None, face.FontSimulations);
            Assert.Equal("Roboto", face.FamilyName);
        }
    }

    [AvaloniaFact]
    public void Reference_navigation_detaches_the_departing_scene_before_releasing_its_tooltip_description()
    {
        using var host = new ReferenceHost("overlays");
        var oldScene = host.Shell.CurrentScene!;
        var anchor = host.Find<MaterialIconButton>("tooltip");
        Assert.Equal("Save draft", AutomationProperties.GetHelpText(anchor));
        string? descriptionAtDetach = null;
        oldScene.PropertyChanged += (_, change) =>
        {
            if (change.Property == StyledElement.ParentProperty && change.NewValue is null)
                descriptionAtDetach = AutomationProperties.GetHelpText(anchor);
        };
        Assert.True(host.Shell.Navigate("home")); host.Render();
        Assert.Equal("Save draft", descriptionAtDetach);
        Assert.Null(oldScene.Parent); Assert.Null(AutomationProperties.GetHelpText(anchor));
    }

    [AvaloniaFact]
    public void Reference_sliders_use_the_native_48_dip_no_value_label_recipe()
    {
        using var host = new ReferenceHost("slider");
        foreach (var id in new[] { "slider", "slider-discrete", "slider-range" })
        {
            var slider = host.Find<MaterialSlider>(id);
            Assert.Equal(48, slider.Bounds.Height);
            Assert.Equal(SliderValueLabelVisibility.Never, slider.ValueLabelVisibility);
        }
        var continuous = host.Find<MaterialSlider>("slider");
        var center = continuous.TranslatePoint(new Point(40, 24), host.Window)!.Value;
        using var bitmap = host.Window.CaptureRenderedFrame()!; using var pixels = bitmap.Lock();
        var offset = (int)(center.Y * host.Window.RenderScaling) * pixels.RowBytes + (int)(center.X * host.Window.RenderScaling) * 4;
        var first = Marshal.ReadByte(pixels.Address, offset); var green = Marshal.ReadByte(pixels.Address, offset + 1); var third = Marshal.ReadByte(pixels.Address, offset + 2);
        Assert.Equal(Color.Parse("#6750A4"), pixels.Format == PixelFormat.Bgra8888 ? Color.FromRgb(third, green, first) : Color.FromRgb(first, green, third));
    }

    [AvaloniaFact]
    public void Reference_topics_then_activity_tab_returns_both_navigation_controls_to_activity()
    {
        using var host = new ReferenceHost("navigation");
        host.Click("navigation-3");
        Assert.Equal(3, host.Find<MaterialNavigationBar>("navigation").SelectedIndex);
        var tabs = host.Find<MaterialTabs>("tabs");
        var activity = (MaterialNavigationItem)tabs.Items[2]!;
        var point = activity.TranslatePoint(new Point(activity.Bounds.Width / 2, activity.Bounds.Height / 2), host.Window)!.Value;
        host.Window.MouseDown(point, MouseButton.Left); host.Window.MouseUp(point, MouseButton.Left); host.Render();
        Assert.Equal(2, host.Find<MaterialNavigationBar>("navigation").SelectedIndex);
        Assert.Equal(2, tabs.SelectedIndex);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Reference_range_dialog_OK_closes_incomplete_or_invalid_shared_drafts(bool invalidInput)
    {
        using var host = new ReferenceHost("date-range-9-16"); host.Click("open-date-dialog");
        var picker = host.Find<MaterialDatePicker>("date-range-dialog-picker");
        if (invalidInput)
        {
            picker.Mode = MaterialDatePickerMode.Input; host.Render();
            picker.StartInput.Focus(); picker.StartInput.SelectAll(); host.Window.KeyTextInput("02312024"); host.Render();
        }
        else { picker.SelectedDate = new DateOnly(2024, 2, 10); picker.RangeEnd = null; host.Render(); }
        Assert.False(picker.IsValid);
        var confirm = host.Shell.Overlay.GetVisualDescendants().OfType<MaterialButton>().Single(button =>
            button.IsEffectivelyVisible && Avalonia.Automation.Peers.ControlAutomationPeer.CreatePeerForElement(button)!.GetName() == "OK");
        Assert.True(confirm.IsEffectivelyEnabled);
        var point = confirm.TranslatePoint(new Point(confirm.Bounds.Width / 2, confirm.Bounds.Height / 2), host.Window)!.Value;
        host.Window.MouseDown(point, MouseButton.Left); host.Window.MouseUp(point, MouseButton.Left); host.Render();
        Assert.Equal(0, host.Shell.Overlay.OpenCount);
        var inline = host.Find<MaterialDatePicker>("date-range-picker");
        if (!invalidInput) Assert.Null(inline.RangeEnd);
        else { Assert.Null(inline.SelectedDate); Assert.Null(inline.RangeEnd); Assert.Equal(MaterialDatePickerMode.Input, inline.Mode); }
    }

    [AvaloniaFact]
    public void Reference_scene_manifest_matches_the_seventeen_native_routes_and_home_navigation()
    {
        using var host = new ReferenceHost();
        Assert.Equal(new[] { "date-range", "date-range-7-24", "date-range-9-16", "date-single", "time", "selection", "selection-disabled", "selection-disabled-off", "slider", "fields", "buttons", "ripple", "fab", "progress", "carousel", "navigation", "overlays" }, ReferenceShell.SceneIds);
        foreach (var scene in ReferenceShell.SceneIds)
        {
            Assert.True(host.Shell.Navigate(scene)); host.Render();
            Assert.Equal(scene, host.Shell.Scene);
            Assert.Equal("M3 · " + scene, host.Shell.TopBar.Title);
        }
        host.Shell.Navigate("home"); host.Render(); host.Click("scene-selection");
        Assert.Equal("selection", host.Shell.Scene); host.Click("home"); Assert.Equal("home", host.Shell.Scene);
    }

    [AvaloniaFact]
    public void Reference_selection_preserves_native_initial_state_and_shared_switch_value_through_input()
    {
        using var host = new ReferenceHost("selection");
        Assert.False(host.Find<MaterialCheckBox>("checkbox").IsChecked);
        Assert.Null(host.Find<MaterialCheckBox>("checkbox-mixed").IsChecked);
        Assert.True(host.Find<MaterialRadioButton>("radio-0").IsChecked);
        Assert.True(host.Find<MaterialSwitch>("switch").IsChecked);
        host.Click("checkbox-mixed"); Assert.True(host.Find<MaterialCheckBox>("checkbox-mixed").IsChecked);
        host.Click("radio-1"); Assert.True(host.Find<MaterialRadioButton>("radio-1").IsChecked);
        Assert.False(host.Find<MaterialRadioButton>("radio-0").IsChecked);
        host.Click("switch"); Assert.False(host.Find<MaterialSwitch>("switch-icons").IsChecked);
    }

    [AvaloniaFact]
    public void Reference_fields_share_one_native_value_and_ripple_actions_increment_the_host_counter()
    {
        using var host = new ReferenceHost("fields");
        var outlined = host.Find<MaterialTextField>("field-outlined");
        outlined.Text = "Draft"; host.Render();
        Assert.Equal("Draft", host.Find<MaterialTextField>("field-amount").Text);
        Assert.Equal("Draft", host.Find<MaterialTextField>("field-filled").Text);
        host.Shell.Navigate("ripple"); host.Render(); host.Click("ripple-filled");
        Assert.Equal("Pressed 1", host.Find<TextBlock>("ripple-count").Text);
    }

    [AvaloniaFact]
    public void Reference_date_dialog_edits_the_same_native_state_and_cancel_retains_the_edit()
    {
        using var host = new ReferenceHost("date-range-9-16");
        var inline = host.Find<MaterialDatePicker>("date-range-picker");
        Assert.Equal(new DateOnly(2024, 2, 9), inline.SelectedDate); Assert.Equal(new DateOnly(2024, 2, 16), inline.RangeEnd);
        host.Click("open-date-dialog");
        var dialog = host.Find<MaterialDatePicker>("date-range-dialog-picker");
        dialog.SelectedDate = new DateOnly(2024, 2, 10); dialog.RangeEnd = new DateOnly(2024, 2, 12); host.Render();
        Assert.Equal(dialog.SelectedDate, inline.SelectedDate); Assert.Equal(dialog.RangeEnd, inline.RangeEnd);
        Assert.True(host.Shell.Overlay.RequestBack()); host.Render();
        Assert.Equal(new DateOnly(2024, 2, 10), inline.SelectedDate); Assert.Equal(new DateOnly(2024, 2, 12), inline.RangeEnd);
    }

    [AvaloniaFact]
    public void Reference_default_launch_configuration_matches_the_manual_native_comparison()
    {
        var defaults = new ReferenceConfiguration();
        Assert.Equal("home", defaults.Scene); Assert.Equal("classic", defaults.Palette); Assert.Equal("en-US", defaults.Locale);
        Assert.True(defaults.CheckboxM3); Assert.True(defaults.ExpressiveButtons); Assert.False(defaults.Dark);
        var custom = ReferenceConfiguration.FromArguments(["--scene=selection", "--dark=true", "--palette=expressive", "--locale=zh-CN", "--checkboxM3=false", "--expressiveButtons=false"]);
        Assert.Equal("selection", custom.Scene); Assert.True(custom.Dark); Assert.Equal("expressive", custom.Palette);
        Assert.Equal("zh-CN", custom.Locale); Assert.False(custom.CheckboxM3); Assert.False(custom.ExpressiveButtons);
        Assert.Equal(2, custom.UnsupportedVariants.Count); Assert.Empty(defaults.UnsupportedVariants);
        Assert.Throws<ArgumentException>(custom.Validate); defaults.Validate();
    }

    private sealed class ReferenceHost : IDisposable
    {
        private readonly MaterialTheme _theme = new();
        public ReferenceShell Shell { get; }
        public Window Window { get; }
        public ReferenceHost(string scene = "home")
        {
            _theme.Motion = _theme.Motion with { ReduceMotion = true };
            Application.Current!.Styles.Add(_theme);
            Shell = new ReferenceShell(_theme, new ReferenceConfiguration { Scene = scene, Palette = "classic", Locale = "en-US", CheckboxM3 = true });
            Window = new Window { Width = 354.285714, Height = 744, Content = Shell }; Window.Show(); Render();
        }
        public T Find<T>(string id) where T : Control => Shell.GetVisualDescendants().OfType<T>().Single(control => AutomationProperties.GetAutomationId(control) == id);
        public void Render() { Window.UpdateLayout(); Avalonia.Threading.Dispatcher.UIThread.RunJobs(); Window.CaptureRenderedFrame()?.Dispose(); }
        public void Click(string id)
        {
            var control = Find<Control>(id); control.BringIntoView(); Render();
            var point = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), Window)!.Value;
            Window.MouseDown(point, MouseButton.Left); Window.MouseUp(point, MouseButton.Left); Render();
        }
        public void Dispose() { Window.Close(); Application.Current!.Styles.Remove(_theme); }
    }
}
