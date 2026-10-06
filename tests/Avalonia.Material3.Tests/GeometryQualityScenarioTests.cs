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
}

internal sealed class GeometryHost : IDisposable
{
    public MaterialTheme Theme { get; } = new() { Motion = new MaterialMotion { ReduceMotion = true } };
    public Window Window { get; }
    public GeometryHost(Control content, double width, double height)
    {
        Application.Current!.Styles.Add(Theme);
        Window = new Window { Width = width, Height = height, RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light,
            Background = new SolidColorBrush(Color.Parse("#FEF7FF")), Content = content };
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
    public void Dispose() { Window.Close(); Application.Current!.Styles.Remove(Theme); }
}
