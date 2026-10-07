using System.Runtime.InteropServices;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Material3.Tests;

// Oracles: coverage pin AndroidX 11ece46a, Slider.kt 2428–2669.
// Native device DPI is recorded separately; this fixture honestly asserts its actual RenderScaling.
public class GeometryQualityScenarioTests
{
    [AvaloniaTheory]
    [InlineData(9, 16, false)]
    [InlineData(9, 16, true)]
    [InlineData(7, 24, false)]
    [InlineData(9, 10, false)]
    public void Pinned_date_range_literal_endpoint_and_week_edge_masks_match_the_selected_dates(int first, int last, bool rtl)
    {
        var picker = new MaterialDatePicker { Width = 360, SelectionMode = MaterialDateSelectionMode.Range,
            Culture = System.Globalization.CultureInfo.GetCultureInfo("en-US"), DisplayMonth = new(2024, 2, 1),
            SelectedDate = new(2024, 2, first), RangeEnd = new(2024, 2, last),
            FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        using var host = new GeometryHost(picker, 360, 640);
        var days = picker.GetVisualDescendants().OfType<MaterialCalendarDay>().ToArray();
        Rect Day(int value) => GeometryHost.Box(days.Single(d => d.Date.Day == value), host.Window);
        var start = Day(first); var end = Day(last);
        var band = Color.Parse("#E8DEF8"); var surface = Color.Parse("#ECE6F0");
        // February2024 starts Thursday:9..16 begins at(264,52), ends(264,100)
        // in the336×288 grid. Rectangle widths72/264 join40-DIP circles at their centers.
        Assert.Equal(band, host.Pixel(start.Left + (rtl ? 14 : 34), start.Top + 5));
        Assert.Equal(surface, host.Pixel(start.Left + (rtl ? 34 : 14), start.Top + 5));
        Assert.Equal(band, host.Pixel(end.Left + (rtl ? 34 : 14), end.Top + 5));
        Assert.Equal(surface, host.Pixel(end.Left + (rtl ? 14 : 34), end.Top + 5));
        if (last > 10)
        {
            var saturday = Day(10); var sunday = Day(11);
            Assert.Equal(band, host.Pixel(saturday.Left + (rtl ? 1 : 47), saturday.Top + 5));
            Assert.Equal(band, host.Pixel(sunday.Left + (rtl ? 47 : 1), sunday.Top + 5));
            Assert.Equal(surface, host.Pixel(saturday.Left + (rtl ? 1 : 47), saturday.Top + 2));
            Assert.Equal(surface, host.Pixel(sunday.Left + (rtl ? 47 : 1), sunday.Top + 2));
        }
    }

    [AvaloniaFact]
    public async Task Updated_time_period_toggle_uses_primary_roles_a_four_dip_visual_gap_and_fast_spatial_morph()
    {
        var picker = new MaterialTimePicker { SelectedTime = new(7, 7) };
        using var host = new GeometryHost(picker, 400, 680);
        var periods = picker.GetVisualDescendants().OfType<MaterialTimePeriodButton>().ToArray();
        var am = GeometryHost.Box(periods[0], host.Window);
        var pm = GeometryHost.Box(periods[1], host.Window);
        // TimePicker updated=true:52×38 faces within80, visual gap4; native targets48.
        Assert.Equal(48, am.Height); Assert.Equal(48, pm.Height);
        Assert.Equal(42, pm.Top - am.Top);
        Assert.Equal(Color.Parse("#EADDFF"), host.Pixel(am.Left + 8, am.Top + 13));
        Assert.Equal(Color.Parse("#ECE6F0"), host.Pixel(am.Center.X, am.Top + 44));
        Assert.Equal(FontWeight.Bold, periods[0].FontWeight);
        Assert.Equal(new CornerRadius(12), periods[0].CornerRadius);
        // A public FastSpatial override lengthens the same spring for reliable raster sampling.
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with { FastSpatial = new(.6, 64) } }; host.Render();
        picker.SetPeriod(true);
        Assert.Equal(new TimeOnly(19, 7), picker.SelectedTime);
        Assert.Equal(FontWeight.Bold, periods[1].FontWeight);
        // A checked face changes color immediately; its19→12-DIP corners morph in fixed bounds.
        var firstFrame = host.Pixel(pm.Left + 3, pm.Top + 10);
        Assert.Equal(Color.Parse("#ECE6F0"), firstFrame);
        var frames = new HashSet<Color> { firstFrame };
        for (var frame = 0; frame < 35; frame++)
        {
            await Task.Delay(16);
            Assert.Equal(pm, GeometryHost.Box(periods[1], host.Window));
            frames.Add(host.Pixel(pm.Left + 3, pm.Top + 10));
        }
        Assert.Equal(Color.Parse("#EADDFF"), host.Pixel(pm.Left + 3, pm.Top + 10));
        Assert.True(frames.Count > 1);
    }

    [AvaloniaFact]
    public void Standard_slider_hover_focus_and_dense_press_paint_only_the_normative_44_dip_handle()
    {
        var slider = new MaterialSlider { Width = 320, Height = 64, Value = 50,
            ValueLabelVisibility = SliderValueLabelVisibility.Never };
        using var host = new GeometryHost(slider, 320, 64);
        // AndroidX ThumbContent: fixed4×44 layout; painted2×44 on Focus/Press/Drag.
        // The default opacity focus theme leaves the optional inset-ring branch inactive.
        var surface = Color.Parse("#FEF7FF");
        var primary = Color.Parse("#6750A4");
        Assert.Equal(primary, host.Pixel(158, 15));
        host.Window.MouseMove(new Point(160, 32)); host.Render();
        Assert.Equal(surface, host.Pixel(154, 32)); // untouched8-DIP track gap
        for (var x = 146; x < 157; x++)
        for (var y = 8; y < 23; y++) Assert.Equal(surface, host.Pixel(x, y));
        Assert.Equal(primary, host.Pixel(158, 15)); // hover keeps4-DIP width
        slider.Focus(NavigationMethod.Tab); host.Render();
        Assert.Equal(surface, host.Pixel(158, 15));
        Assert.Equal(primary, host.Pixel(159, 15));
        Assert.Equal(surface, host.Pixel(151, 15));
        Assert.Equal(surface, host.Pixel(167, 32)); // gap uses the fixed4-DIP layout
        Assert.Equal(Color.Parse("#E8DEF8"), host.Pixel(169, 32));
        for (var press = 0; press < 12; press++)
        {
            host.Window.MouseDown(new Point(160, 32), MouseButton.Left); host.Render();
            Assert.Equal(surface, host.Pixel(158, 15));
            Assert.Equal(primary, host.Pixel(159, 15));
            Assert.Equal(surface, host.Pixel(151, 15));
            Assert.Equal(surface, host.Pixel(167, 32));
            host.Window.MouseUp(new Point(160, 32), MouseButton.Left); host.Render();
            Assert.Equal(50, slider.Value);
        }
    }

    [AvaloniaFact]
    public void Discrete_slider_marks_thumbs_targets_and_pointer_values_share_the_inset_cap_domain()
    {
        var slider = new MaterialRangeSlider { Width = 320, Height = 64, Step = 25,
            LowerValue = 25, UpperValue = 75, ShowMarks = true, ValueLabelVisibility = SliderValueLabelVisibility.Never };
        using var host = new GeometryHost(slider, 320, 64);
        Assert.Equal(1, host.Window.RenderScaling);
        // Literal320-DIP fixture: outer edges24/296; cap centers32/288; interior thumbs96/224.
        Assert.Equal(Color.Parse("#6750A4"), host.Pixel(32, 32));
        Assert.Equal(Color.Parse("#6750A4"), host.Pixel(288, 32));
        Assert.Equal(Color.Parse("#6750A4"), host.Pixel(95, 15));
        Assert.Equal(Color.Parse("#6750A4"), host.Pixel(223, 15));
        Assert.Equal(Color.Parse("#E8DEF8"), host.Pixel(160, 32)); // active tick, not label foreground
        Assert.Equal(Color.Parse("#FEF7FF"), host.Pixel(297, 32)); // no orphan stop outside rounded cap
        var endpoints = ControlAutomationPeer.CreatePeerForElement(slider)!.GetChildren()!;
        Assert.Equal(96, Assert.IsAssignableFrom<ControlAutomationPeer>(endpoints[0]).Owner.Bounds.Center.X);
        Assert.Equal(224, Assert.IsAssignableFrom<ControlAutomationPeer>(endpoints[1]).Owner.Bounds.Center.X);
        host.Window.MouseDown(new Point(224, 32), MouseButton.Left);
        host.Window.MouseUp(new Point(224, 32), MouseButton.Left);
        Assert.Equal(75, slider.UpperValue);
        Assert.Equal(75, endpoints[1].GetProvider<IRangeValueProvider>()!.Value);
    }

    [AvaloniaTheory]
    [InlineData(1d)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    [InlineData(2d)]
    public void Offscreen_device_scale_raster_preserves_literal_slider_cap_and_thumb_centers(double scale)
    {
        var slider = new MaterialRangeSlider { Width = 320, Height = 64, Step = 25, LowerValue = 25, UpperValue = 75,
            ShowMarks = true, ValueLabelVisibility = SliderValueLabelVisibility.Never };
        using var host = new GeometryHost(slider, 320, 64);
        // A real DPI-aware offscreen renderer, NOT a native monitor/Window.RenderScaling claim.
        var pixels = host.Offscreen(scale);
        Assert.Equal((int)(320 * scale), pixels.GetLength(0));
        Assert.Equal((int)(64 * scale), pixels.GetLength(1));
        Color At(double x, double y) => pixels[(int)(x * scale), (int)(y * scale)];
        Assert.Equal(Color.Parse("#6750A4"), At(32, 32));
        Assert.Equal(Color.Parse("#6750A4"), At(288, 32));
        Assert.Equal(Color.Parse("#6750A4"), At(95, 15));
        Assert.Equal(Color.Parse("#6750A4"), At(223, 15));
        Assert.Equal(Color.Parse("#FEF7FF"), At(297, 32));
        Assert.Equal(Color.Parse("#E8DEF8"), At(160, 32));
    }

    [AvaloniaTheory]
    [InlineData(1d, false)]
    [InlineData(1.25, false)]
    [InlineData(1.5, false)]
    [InlineData(2d, false)]
    [InlineData(1d, true)]
    [InlineData(1.25, true)]
    [InlineData(1.5, true)]
    [InlineData(2d, true)]
    public void Offscreen_device_scale_calendar_preserves_joining_and_forbidden_half_masks(double scale, bool rtl)
    {
        var picker = new MaterialDatePicker { Width = 360, SelectionMode = MaterialDateSelectionMode.Range,
            Culture = System.Globalization.CultureInfo.GetCultureInfo("en-US"), DisplayMonth = new(2024, 2, 1),
            SelectedDate = new(2024, 2, 7), RangeEnd = new(2024, 2, 24),
            FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        using var host = new GeometryHost(picker, 360, 640);
        var start = GeometryHost.Box(picker.GetVisualDescendants().OfType<MaterialCalendarDay>().Single(d => d.Date.Day == 7), host.Window);
        var pixels = host.Offscreen(scale);
        Color At(double dx, double dy) => pixels[(int)((start.Left + dx) * scale), (int)((start.Top + dy) * scale)];
        Assert.Equal(Color.Parse("#E8DEF8"), At(rtl ? 14 : 34, 5));
        Assert.Equal(Color.Parse("#ECE6F0"), At(rtl ? 34 : 14, 5));
        Assert.Equal(Color.Parse("#6750A4"), At(12, 24)); // circle fill, deliberately away from numeral7 ink
        Assert.Equal(Color.Parse("#ECE6F0"), At(24, 2));
    }

    [AvaloniaTheory]
    [InlineData(1d)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    [InlineData(2d)]
    public void Offscreen_device_scale_clock_07_retains_true_bubble_angle_and_spatial_white_numeral_ink(double scale)
    {
        var dial = new MaterialClockDial { Value = 7, ActivePart = MaterialTimePickerPart.Minute };
        using var host = new GeometryHost(dial, 256, 256);
        var pixels = host.Offscreen(scale);
        Assert.Equal(Color.Parse("#6750A4"), pixels[(int)(209 * scale), (int)(53 * scale)]);
        Assert.Equal(Color.Parse("#E6E0E9"), pixels[(int)(177 * scale), (int)(20 * scale)]);
        var ink = 0;
        for (var y = (int)(27 * scale); y < 57 * scale; y++)
        for (var x = (int)(164 * scale); x < 195 * scale; x++)
        {
            var dx = x + .5 - 195.582191242245 * scale; var dy = y + .5 - 52.9423726267832 * scale;
            if (dx * dx + dy * dy < 22 * 22 * scale * scale && pixels[x, y] == Color.Parse("#FFFFFF")) ink++;
        }
        Assert.True(ink > 0, "Spatial selected numeral ink must actually render at this raster DPI.");
    }

    [AvaloniaFact]
    public void Dense_discrete_pointer_inverse_selects_the_value_at_its_painted_interior_thumb()
    {
        var slider = new MaterialSlider { Width = 320, Height = 64, Step = 1, Value = 10,
            ValueLabelVisibility = SliderValueLabelVisibility.Never };
        using var host = new GeometryHost(slider, 320, 64);
        host.Window.MouseDown(new Point(57.6, 32), MouseButton.Left);
        host.Window.MouseUp(new Point(57.6, 32), MouseButton.Left);
        Assert.Equal(10, slider.Value); // fixed oracle32 + .10*256, not control-position readback
    }

    [AvaloniaFact]
    public void Nonoverlapping_range_labels_on_the_same_side_remain_over_their_handles()
    {
        var slider = new MaterialRangeSlider { Width = 360, Height = 100, LowerValue = 20, UpperValue = 40,
            ValueLabelVisibility = SliderValueLabelVisibility.Always };
        using var host = new GeometryHost(slider, 360, 100);
        Assert.Equal(Color.Parse("#322F35"), host.Pixel(86.4, 30));
        Assert.Equal(Color.Parse("#322F35"), host.Pixel(148.8, 30));
        Assert.Equal(Color.Parse("#FEF7FF"), host.Pixel(196, 30));
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Date_range_joins_rectangular_week_bands_to_endpoint_circles_without_painting_the_forbidden_half(bool rtl)
    {
        var picker = new MaterialDatePicker { Width = 360, SelectionMode = MaterialDateSelectionMode.Range,
            Culture = System.Globalization.CultureInfo.GetCultureInfo("en-US"), DisplayMonth = new(2024, 2, 1),
            SelectedDate = new(2024, 2, 7), RangeEnd = new(2024, 2, 24),
            FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        using var host = new GeometryHost(picker, 360, 640);
        var days = picker.GetVisualDescendants().OfType<MaterialCalendarDay>().ToArray();
        var start = GeometryHost.Box(days.Single(d => d.Date.Day == 7), host.Window);
        var end = GeometryHost.Box(days.Single(d => d.Date.Day == 24), host.Window);
        Assert.Equal(new Size(48, 48), start.Size);
        Assert.Equal(Color.Parse("#E8DEF8"), host.Pixel(start.Left + (rtl ? 14 : 34), start.Top + 5));
        Assert.Equal(Color.Parse("#ECE6F0"), host.Pixel(start.Left + (rtl ? 34 : 14), start.Top + 5));
        Assert.Equal(Color.Parse("#E8DEF8"), host.Pixel(end.Left + (rtl ? 34 : 14), end.Top + 5));
        Assert.Equal(Color.Parse("#ECE6F0"), host.Pixel(end.Left + (rtl ? 14 : 34), end.Top + 5));
        // Weekly backing is deliberately rectangular; outside the40-high band is not selected.
        var middle = GeometryHost.Box(days.Single(d => d.Date.Day == 14), host.Window);
        Assert.Equal(Color.Parse("#E8DEF8"), host.Pixel(middle.Left + 1, middle.Top + 5));
        Assert.Equal(Color.Parse("#ECE6F0"), host.Pixel(middle.Left + 1, middle.Top + 2));
        picker.RangeEnd = picker.SelectedDate; host.Render();
        Assert.Equal(Color.Parse("#ECE6F0"), host.Pixel(start.Left + (rtl ? 14 : 34), start.Top + 5));
    }

    [AvaloniaFact]
    public void Date_range_font_growth_keeps_circular_endpoints_and_a_40_dip_joining_band()
    {
        var picker = new MaterialDatePicker { Width = 360, SelectionMode = MaterialDateSelectionMode.Range,
            Culture = System.Globalization.CultureInfo.GetCultureInfo("en-US"), DisplayMonth = new(2024, 2, 1),
            SelectedDate = new(2024, 2, 7), RangeEnd = new(2024, 2, 24) };
        using var host = new GeometryHost(picker, 360, 900);
        host.Theme.Typography = host.Theme.Typography with { Scale = 2 }; host.Render();
        var day = picker.GetVisualDescendants().OfType<MaterialCalendarDay>().Single(d => d.Date.Day == 7);
        var box = GeometryHost.Box(day, host.Window);
        // BodyLarge line48 requires a48 circle plus two4 gutters: readable56-square target.
        Assert.Equal(new Size(56, 56), box.Size);
        Assert.Equal(Color.Parse("#6750A4"), host.Pixel(box.Left + 5, box.Center.Y));
        Assert.Equal(Color.Parse("#E8DEF8"), host.Pixel(box.Left + 50, box.Top + 9));
        Assert.Equal(Color.Parse("#ECE6F0"), host.Pixel(box.Left + 6, box.Top + 9));
        Assert.Equal(Color.Parse("#ECE6F0"), host.Pixel(box.Right - 1, box.Top + 6));
    }

    [AvaloniaFact]
    public void Enlarged_month_caption_wraps_instead_of_cutting_off_the_year()
    {
        var picker = new MaterialDatePicker { Width = 360, Culture = System.Globalization.CultureInfo.GetCultureInfo("en-US"),
            DisplayMonth = new(2024, 2, 1), SelectedDate = new(2024, 2, 7) };
        using var host = new GeometryHost(picker, 360, 900);
        host.Theme.Typography = host.Theme.Typography with { Scale = 2 }; host.Render();
        var caption = picker.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text == "February 2024");
        Assert.True(caption.TextLayout.Width <= caption.Bounds.Width + .01,
            $"Month/year ink layout{caption.TextLayout.Width} exceeds its visible caption{caption.Bounds.Width}.");
        Assert.True(caption.TextLayout.TextLines.Count > 1, "The narrow enlarged caption must expose the complete year through wrapping.");
    }

    [AvaloniaFact]
    public void Side_handle_hover_and_focus_do_not_paint_a_full_width_header_bar()
    {
        var handle = new MaterialSheetDragHandle { Sheet = new MaterialSideSheet() };
        using var host = new GeometryHost(handle, 256, 56);
        var outside = host.Pixel(32, 28);
        host.Window.MouseMove(new Point(128, 28)); host.Render();
        Assert.Equal(outside, host.Pixel(32, 28));
        Assert.Equal(new Size(256, 56), handle.Bounds.Size); // wider transparent gesture allocation retained
        handle.Focus(NavigationMethod.Tab); host.Render();
        Assert.Equal(outside, host.Pixel(32, 28));
        Assert.NotEqual(outside, host.Pixel(128, 28)); // actual centered4×48 marker remains visible
    }

    [AvaloniaFact]
    public void Clock_selector_hover_preserves_its_80_dip_visual_and_numeric_content_center()
    {
        var picker = new MaterialTimePicker { Is24Hour = true, SelectedTime = new(19, 7) };
        using var host = new GeometryHost(picker, 400, 640);
        var selector = picker.GetVisualDescendants().OfType<MaterialTimeSelector>().First();
        var before = GeometryHost.Box(selector, host.Window);
        Assert.Equal(80, before.Height);
        host.Window.MouseMove(before.Center); host.Render();
        Assert.Equal(before, GeometryHost.Box(selector, host.Window));
    }

    [AvaloniaFact]
    public void Clock_numbers_have_an_independent_24_dip_separator_and_36_dip_dial_gap()
    {
        var picker = new MaterialTimePicker { Is24Hour = true, SelectedTime = new(19, 7) };
        using var host = new GeometryHost(picker, 400, 640);
        var selectors = picker.GetVisualDescendants().OfType<MaterialTimeSelector>().ToArray();
        var hour = GeometryHost.Box(selectors[0], host.Window);
        var minute = GeometryHost.Box(selectors[1], host.Window);
        Assert.Equal(24, minute.Left - hour.Right);
        var dial = GeometryHost.Box(picker.GetVisualDescendants().OfType<MaterialClockDial>().Single(), host.Window);
        Assert.Equal(36, dial.Top - hour.Bottom);
    }

    [AvaloniaFact]
    public void Legacy_tertiary_period_faces_join_under_one_outline_with_separate_48_dip_native_targets()
    {
        var picker = new MaterialTimePicker { SelectedTime = new(7, 7) };
        using var host = new GeometryHost(picker, 400, 680);
        var periods = picker.GetVisualDescendants().OfType<MaterialTimePeriodButton>().ToArray();
        var am = GeometryHost.Box(periods[0], host.Window);
        var pm = GeometryHost.Box(periods[1], host.Window);
        Assert.True(am.Height >= 48 && pm.Height >= 48);
        // Historical scenario identity retained; the complete updated toggle replaces its legacy oracle.
        Assert.Equal(42, pm.Top - am.Top);
        Assert.Equal(Color.Parse("#EADDFF"), host.Pixel(am.Left + 8, am.Top + 13));
        host.Window.MouseDown(pm.Center, MouseButton.Left); host.Window.MouseUp(pm.Center, MouseButton.Left);
        Assert.Equal(new TimeOnly(19, 7), picker.SelectedTime);
        host.Render();
        Assert.Equal(am, GeometryHost.Box(periods[0], host.Window));
        Assert.Equal(pm, GeometryHost.Box(periods[1], host.Window));
    }

    [AvaloniaFact]
    public void Minute_07_keeps_its_true_angle_and_recolors_only_the_numeral_ink_inside_the_selector()
    {
        var dial = new MaterialClockDial { Value = 0, ActivePart = MaterialTimePickerPart.Minute };
        using var host = new GeometryHost(dial, 256, 256);
        var ordinaryNumeral = host.Region(164, 27, 31, 30); //05 wholly outside the00 bubble
        dial.Value = 7; host.Render();
        Assert.Equal(Color.Parse("#6750A4"), host.Pixel(209, 53)); // inside07 bubble, outside05 bubble
        Assert.Equal(Color.Parse("#E6E0E9"), host.Pixel(177, 20)); // outside07 bubble (not snapped to05)
        var selectedInk = 0; var unselectedInk = 0;
        var pixels = host.Region(164, 27, 31, 30);
        for (var y = 27; y < 57; y++)
        for (var x = 164; x < 195; x++)
        {
            var dx = x + .5 - 195.582191242245; var dy = y + .5 - 52.9423726267832;
            var color = pixels[x - 164, y - 27];
            if (dx * dx + dy * dy < 22 * 22 && color == Color.Parse("#FFFFFF")) selectedInk++;
            if (dx * dx + dy * dy > 25 * 25 && ordinaryNumeral[x - 164, y - 27] == Color.Parse("#1D1B20"))
            {
                Assert.Equal(Color.Parse("#1D1B20"), color); // all outside numeral ink retains its normal role
                unselectedInk++;
            }
        }
        Assert.True(selectedInk > 4, $"No OnPrimary numeral ink within the07 selector ({selectedInk}).");
        Assert.True(unselectedInk > 0, "The ordinary numeral fixture must contain outside solid ink; exact area is font dependent.");
        int? chosen = null; dial.ValueSelected += (_, args) => chosen = args.Value;
        var five = dial.GetVisualDescendants().OfType<MaterialClockNumber>().Single(n => n.Value == 5);
        Assert.False(five.IsChecked); // overlap isn't semantic selection
        var point = GeometryHost.Box(five, host.Window).Center;
        host.Window.MouseDown(point, MouseButton.Left); host.Window.MouseUp(point, MouseButton.Left);
        Assert.Equal(5, chosen); // actual native action retained; overlap doesn't change its identity
    }

    [AvaloniaFact]
    public void Navigation_badges_use_the_icon_anchor_without_changing_its_baseline_and_stay_in_the_header()
    {
        var icon = new Border { Width = 24, Height = 24, Background = Brushes.Green };
        var item = new MaterialNavigationItem { Content = "Inbox", Icon = icon };
        var tabs = new MaterialTabs { Items = { item } };
        using var host = new GeometryHost(tabs, 240, 200);
        var anchor = GeometryHost.Box(icon, host.Window);
        var label = item.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text == "Inbox");
        var labelBox = GeometryHost.Box(label, host.Window);
        var dot = new MaterialBadge(); item.Badge = dot; host.Render();
        Assert.Equal(anchor, GeometryHost.Box(icon, host.Window));
        Assert.Equal(labelBox, GeometryHost.Box(label, host.Window));
        var dotBox = GeometryHost.Box(dot, host.Window);
        Assert.Equal(18, dotBox.Left - anchor.Left);
        Assert.Equal(anchor.Top, dotBox.Top);
        var count = new MaterialBadge { Count = 999 }; item.Badge = count; host.Render();
        Assert.Equal(anchor, GeometryHost.Box(icon, host.Window));
        Assert.Equal(labelBox, GeometryHost.Box(label, host.Window));
        var countBox = GeometryHost.Box(count, host.Window);
        var itemBox = GeometryHost.Box(item, host.Window);
        Assert.True(countBox.Top >= itemBox.Top && countBox.Right <= itemBox.Right);
        Assert.Equal(12, countBox.Left - anchor.Left);
    }

    [AvaloniaFact]
    public async Task Navigation_selection_commits_immediately_but_paints_intermediate_indicator_frames_in_a_fixed_header()
    {
        var first = new MaterialNavigationItem { Content = "First", PageContent = new TextBlock { Text = "First page" } };
        var second = new MaterialNavigationItem { Content = "Second", PageContent = new TextBlock { Text = "Second page" } };
        var tabs = new MaterialTabs { Items = { first, second } };
        using var host = new GeometryHost(tabs, 320, 200);
        host.Theme.Motion = new MaterialMotion { StateLayerDuration = TimeSpan.FromMilliseconds(400) };
        host.Render();
        var header = GeometryHost.Box(second, host.Window);
        tabs.SelectedIndex = 1;
        Assert.Same(second.PageContent, tabs.SelectedContent);
        Assert.True(second.IsSelected);
        var immediate = host.Pixel(header.Center.X, header.Bottom - 1);
        Assert.NotEqual(Color.Parse("#6750A4"), immediate);
        var intermediate = false;
        for (var i = 0; i < 35; i++)
        {
            await Task.Delay(16); host.Render();
            Assert.Equal(header, GeometryHost.Box(second, host.Window));
            var color = host.Pixel(header.Center.X, header.Bottom - 1);
            if (color != Color.Parse("#6750A4") && color != Color.Parse("#FEF7FF")) intermediate = true;
        }
        Assert.True(intermediate, "Selection must render intermediate underline alpha/width, not just final semantic state.");
        Assert.Equal(Color.Parse("#6750A4"), host.Pixel(header.Center.X, header.Bottom - 1));
        host.Theme.Motion = new MaterialMotion { ReduceMotion = true };
        tabs.SelectedIndex = 0; host.Render();
        Assert.Equal(Color.Parse("#FEF7FF"), host.Pixel(header.Center.X, header.Bottom - 1));
    }
}

internal sealed class GeometryHost : IDisposable
{
    public MaterialTheme Theme { get; } = new() { Motion = new MaterialMotion { ReduceMotion = true } };
    public Window Window { get; }
    private readonly Border _surface;
    public GeometryHost(Control content, double width, double height)
    {
        Application.Current!.Styles.Add(Theme);
        _surface = new Border { Background = new SolidColorBrush(Color.Parse("#FEF7FF")), Child = content };
        Window = new Window { Width = width, Height = height, RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light,
            Background = _surface.Background, Content = _surface };
        Window.Show(); Render();
    }
    public static Rect Box(Control control, Window window) => new Rect(control.Bounds.Size).TransformToAABB(control.TransformToVisual(window)!.Value);
    public void Render() { using var frame = Window.CaptureRenderedFrame(); }
    public Color Pixel(double x, double y)
    {
        using var bitmap = Window.CaptureRenderedFrame()!;
        using var frame = bitmap.Lock();
        var offset = (int)(y * Window.RenderScaling) * frame.RowBytes + (int)(x * Window.RenderScaling) * 4;
        var first = Marshal.ReadByte(frame.Address, offset);
        var green = Marshal.ReadByte(frame.Address, offset + 1);
        var third = Marshal.ReadByte(frame.Address, offset + 2);
        return frame.Format == PixelFormat.Bgra8888 ? Color.FromRgb(third, green, first) : Color.FromRgb(first, green, third);
    }
    public Color[,] Offscreen(double scale)
    {
        var width = (int)(_surface.Bounds.Width * scale); var height = (int)(_surface.Bounds.Height * scale);
        using var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize(width, height), new Vector(96 * scale, 96 * scale));
        bitmap.Render(_surface);
        using var storage = new Avalonia.Media.Imaging.WriteableBitmap(new PixelSize(width, height),
            new Vector(96 * scale, 96 * scale), PixelFormat.Bgra8888, AlphaFormat.Premul);
        using var frame = storage.Lock();
        bitmap.CopyPixels(frame);
        var result = new Color[width, height];
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var offset = y * frame.RowBytes + x * 4;
            var blue = Marshal.ReadByte(frame.Address, offset); var green = Marshal.ReadByte(frame.Address, offset + 1);
            var red = Marshal.ReadByte(frame.Address, offset + 2);
            result[x, y] = Color.FromRgb(red, green, blue);
        }
        return result;
    }
    public Color[,] Region(int left, int top, int width, int height)
    {
        using var bitmap = Window.CaptureRenderedFrame()!;
        using var frame = bitmap.Lock();
        var colors = new Color[width, height];
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var offset = (int)((top + y) * Window.RenderScaling) * frame.RowBytes + (int)((left + x) * Window.RenderScaling) * 4;
            var first = Marshal.ReadByte(frame.Address, offset); var green = Marshal.ReadByte(frame.Address, offset + 1);
            var third = Marshal.ReadByte(frame.Address, offset + 2);
            colors[x, y] = frame.Format == PixelFormat.Bgra8888 ? Color.FromRgb(third, green, first) : Color.FromRgb(first, green, third);
        }
        return colors;
    }
    public void Dispose() { Window.Close(); Application.Current!.Styles.Remove(Theme); }
}
