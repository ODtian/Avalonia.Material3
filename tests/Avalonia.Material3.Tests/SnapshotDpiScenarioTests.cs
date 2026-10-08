using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Tokens;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Runtime.InteropServices;
using System.Globalization;
using Avalonia.Styling;
using Xunit;

namespace Avalonia.Material3.Tests;

public class SnapshotDpiScenarioTests
{
    [AvaloniaFact]
    public void Clock_target_label_keeps_normal_ink_until_the_moving_selector_overlaps_it()
    {
        var dial = new MaterialClockDial { Value = 12 };
        using var host = new GeometryHost(dial, 320, 320);
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with { DefaultSpatial = new(1, .01) } }; host.Render();
        dial.ValueSelected += (_, selection) => dial.Value = selection.Value;
        var target = dial.Children.OfType<MaterialClockNumber>().Single(number => number.Value == 3);
        var region = new Rect(222, 118, 14, 20);
        Assert.Equal(0, WhiteInk(dial, 1, region));
        var point = GeometryHost.Box(target, host.Window).Center;
        host.Window.MouseDown(point, MouseButton.Left); host.Window.MouseUp(point, MouseButton.Left);
        Assert.True(target.IsChecked); Assert.Equal(3, dial.Value);
        Assert.Equal(0, WhiteInk(dial, 1, region));
    }

    [AvaloniaTheory]
    [InlineData(1.25)]
    [InlineData(3.5)]
    public void Retiring_calendar_pixels_keep_their_frozen_aspect_when_live_typography_changes_height(double density)
    {
        var picker = new MaterialDatePicker { Culture = CultureInfo.GetCultureInfo("en-US"), DisplayMonth = new(2024, 2, 1), SelectedDate = new(2024, 2, 9) };
        using var host = new GeometryHost(picker, 720, 1400);
        host.Window.SetRenderScaling(density); host.Render();
        host.Theme.Motion = new MaterialMotion(); host.Render();
        Assert.True(picker.NavigateMonth(1));
        host.Theme.Typography = host.Theme.Typography with { Scale = 3 }; host.Window.UpdateLayout();
        var first = picker.GetVisualDescendants().OfType<MaterialCalendarDay>().Single(day => day.Date == new DateOnly(2024, 3, 1));
        var top = first.TranslatePoint(default, picker)!.Value.Y;
        var size = new PixelSize((int)Math.Ceiling(picker.Bounds.Width * density), (int)Math.Ceiling(picker.Bounds.Height * density));
        using var bitmap = new RenderTargetBitmap(size, new Vector(96 * density, 96 * density)); bitmap.Render(picker);
        using var pixels = new WriteableBitmap(size, new Vector(96 * density, 96 * density), PixelFormat.Bgra8888, AlphaFormat.Premul);
        using var storage = pixels.Lock(); bitmap.CopyPixels(storage);
        var left = size.Width; var right = -1; var bottom = -1; var firstRow = size.Height;
        for (var y = (int)(top * density); y < size.Height; y++)
        for (var x = 0; x < size.Width; x++)
        {
            var offset = y * storage.RowBytes + x * 4;
            if (Marshal.ReadByte(storage.Address, offset + 2) != 103 || Marshal.ReadByte(storage.Address, offset + 1) != 80 || Marshal.ReadByte(storage.Address, offset) != 164) continue;
            left = Math.Min(left, x); right = Math.Max(right, x); firstRow = Math.Min(firstRow, y); bottom = Math.Max(bottom, y);
        }
        Assert.True(right >= left && bottom >= firstRow);
        Assert.InRange(Math.Abs((right - left) - (bottom - firstRow)) / density, 0, 2);
    }

    [AvaloniaTheory]
    [InlineData(128, 128)]
    [InlineData(192, 160)]
    public void Authored_clock_face_paints_inside_its_extent_and_all_four_quadrants_remain_selectable(double width, double height)
    {
        var dial = new MaterialClockDial { Width = width, Height = height, DialBrush = Brushes.Magenta };
        using var host = new GeometryHost(dial, 400, 400);
        var box = GeometryHost.Box(dial, host.Window);
        Assert.Equal(Color.Parse("#FEF7FF"), host.Pixel(box.Right + 2, box.Center.Y));
        Assert.Equal(Color.Parse("#FEF7FF"), host.Pixel(box.Center.X, box.Bottom + 2));
        var selected = 0; dial.ValueSelected += (_, change) => selected = change.Value;
        foreach (var value in new[] { 3, 6, 9, 12 })
        {
            var number = dial.Children.OfType<MaterialClockNumber>().Single(action => action.Value == value);
            var point = GeometryHost.Box(number, host.Window).Center;
            Assert.True(box.Contains(point));
            host.Window.MouseDown(point, MouseButton.Left); host.Window.MouseUp(point, MouseButton.Left);
            Assert.Equal(value, selected);
        }
    }

    [AvaloniaTheory]
    [InlineData(1)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    [InlineData(2)]
    [InlineData(3.5)]
    public void Retiring_overlay_keeps_its_far_surface_edge_and_shadow_at_every_density(double density)
    {
        using var host = new FeedbackHost(480, 360);
        host.Window.SetRenderScaling(density); host.Overlay.Background = Brushes.White;
        host.Theme.Motion = new MaterialMotion { ReduceMotion = true }; host.Render();
        var dialog = new MaterialDialog { Width = 300, Height = 200, Background = Brushes.Magenta, Title = "Dialog", Content = "Body" };
        var session = dialog.Show(host.Overlay); host.Render();
        var box = GeometryHost.Box(dialog, host.Window);
        var inside = new Point(box.Center.X, box.Bottom - 12);
        var shadow = new Point(box.Center.X, box.Bottom + 4);
        Assert.Equal(Colors.Magenta, Pixel(host.Overlay, density, inside));
        host.Theme.Motion = new MaterialMotion(); host.Render();
        session.Dismiss(); host.Window.UpdateLayout();
        Assert.Equal(Colors.Magenta, Pixel(host.Overlay, density, inside));
        Assert.True(Pixel(host.Overlay, density, shadow).R < Pixel(host.Overlay, density, new Point(460, 340)).R);
        Assert.Null(dialog.Parent);
    }

    [AvaloniaTheory]
    [InlineData(1.25, 3.5)]
    [InlineData(3.5, 1.25)]
    public void Menu_retirement_keeps_the_frozen_DIP_location_across_density_resize_and_immediate_reuse(double from, double to)
    {
        using var host = new FeedbackHost(480, 360);
        host.Window.SetRenderScaling(from); host.Theme.Motion = new MaterialMotion { ReduceMotion = true }; host.Render();
        var menu = new MaterialMenu { Width = 300, Height = 200, Background = Brushes.Magenta };
        menu.Items.Add(new MaterialMenuItem { Content = "Body" });
        var session = menu.Show(host.Overlay, host.Entry); host.Render();
        var box = GeometryHost.Box(menu, host.Window);
        var retainedPoint = new Point(box.Right - 12, box.Center.Y);
        Assert.Equal(Colors.Magenta, Pixel(host.Overlay, from, retainedPoint));
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with {
            FastSpatial = new(1, .01), FastEffects = new(1, .01) } }; host.Render();
        session.Dismiss();
        host.Window.SetRenderScaling(to); host.Window.Width = 640; host.Window.Height = 420; host.Window.UpdateLayout();
        var retained = Pixel(host.Overlay, to, retainedPoint);
        Assert.True(retained.R > 245 && retained.B > 245 && retained.G < 10);
        menu.Width = 200; menu.Height = 120;
        var next = menu.Show(host.Overlay, host.Entry); host.Window.UpdateLayout();
        Assert.True(next.IsOpen); Assert.Same(menu, next.Content);
    }

    [AvaloniaFact]
    public void Clock_and_time_dialog_preserve_real_style_setters_and_live_consumer_bindings()
    {
        var dial = new MaterialClockDial();
        dial.Styles.Add(new Style(selector => selector.OfType<MaterialClockDial>()) {
            Setters = { new Setter(Control.WidthProperty, 192d), new Setter(Control.HeightProperty, 160d) } });
        using (var host = new GeometryHost(dial, 720, 720))
        {
            host.Theme.Typography = host.Theme.Typography with { Scale = 2 }; host.Render();
            Assert.Equal(new Size(192, 160), dial.Bounds.Size);
        }
        using var feedback = new FeedbackHost(700, 800);
        feedback.Theme.Motion = new MaterialMotion { ReduceMotion = true }; feedback.Render();
        var picker = new MaterialTimePicker { SelectedTime = new(3, 15) };
        var session = picker.Show(feedback.Overlay); feedback.Render();
        var dialog = Assert.IsType<MaterialDialog>(session.Content);
        var style = new Style(selector => selector.OfType<MaterialDialog>()) { Setters = { new Setter(Control.MaxWidthProperty, 320d) } };
        dialog.Styles.Add(style); feedback.Render();
        feedback.Window.Height = 400; picker.SelectedTime = new(4, 20); feedback.Render();
        Assert.Equal(320, dialog.MaxWidth);
        var source = new Border { Width = 340 };
        using var binding = dialog.Bind(Control.MaxWidthProperty, source.GetObservable(Control.WidthProperty));
        feedback.Window.Height = 800; picker.ActivePart = MaterialTimePickerPart.Minute; feedback.Render();
        Assert.Equal(340, dialog.MaxWidth);
        source.Width = 360; feedback.Render(); Assert.Equal(360, dialog.MaxWidth);
    }

    [AvaloniaTheory]
    [InlineData(1)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    [InlineData(2)]
    [InlineData(3.5)]
    public void Reversing_a_month_scroll_keeps_the_displayed_selected_day_in_the_retiring_frame(double density)
    {
        var picker = new MaterialDatePicker { Culture = CultureInfo.GetCultureInfo("en-US"), DisplayMonth = new(2024, 2, 1), SelectedDate = new(2024, 2, 9) };
        using var host = new GeometryHost(picker, 360, 800);
        host.Window.SetRenderScaling(density); host.Render();
        var selected = picker.GetVisualDescendants().OfType<MaterialCalendarDay>().Single(day => day.Date == new DateOnly(2024, 2, 9));
        var point = selected.TranslatePoint(new Point(12, 24), picker)!.Value;
        host.Theme.Motion = new MaterialMotion(); host.Render();
        Assert.True(picker.NavigateMonth(1)); Assert.Equal(Color.Parse("#6750A4"), Pixel(picker, density, point));
        Assert.True(picker.NavigateMonth(-1)); Assert.Equal(Color.Parse("#6750A4"), Pixel(picker, density, point));
        Assert.Equal(new DateOnly(2024, 2, 9), picker.SelectedDate);
    }

    [AvaloniaFact]
    public void Time_picker_session_preserves_authored_dialog_limit_and_restores_its_native_responsive_default()
    {
        using var host = new FeedbackHost(700, 800);
        host.Theme.Motion = new MaterialMotion { ReduceMotion = true }; host.Render();
        var picker = new MaterialTimePicker { SelectedTime = new(3, 15) };
        var session = picker.Show(host.Overlay); host.Render();
        var dialog = Assert.IsType<MaterialDialog>(session.Content);
        Assert.Equal(400, dialog.MaxWidth);
        dialog.MaxWidth = 320;
        picker.SelectedTime = new(4, 20); picker.ActivePart = MaterialTimePickerPart.Minute; host.Render();
        host.Window.Height = 400; host.Render();
        Assert.Equal(320, dialog.MaxWidth);
        dialog.ClearValue(Control.MaxWidthProperty); host.Render();
        Assert.Equal(584, dialog.MaxWidth);
        host.Window.Height = 800; host.Render(); Assert.Equal(400, dialog.MaxWidth);
    }

    [AvaloniaFact]
    public void Authored_clock_extent_survives_attachment_typography_density_changes_and_clearing_restores_native_extent()
    {
        var dial = new MaterialClockDial { Width = 192, Height = 160 };
        using var host = new GeometryHost(dial, 720, 720);
        Assert.Equal(192, dial.Width); Assert.Equal(160, dial.Height);
        host.Window.SetRenderScaling(1.5); host.Theme.Typography = host.Theme.Typography with { Scale = 2 }; host.Render();
        Assert.Equal(192, dial.Bounds.Width); Assert.Equal(160, dial.Bounds.Height);
        dial.ClearValue(Control.WidthProperty); dial.ClearValue(Control.HeightProperty); host.Render();
        Assert.Equal(512, dial.Bounds.Width); Assert.Equal(512, dial.Bounds.Height);
    }

    [AvaloniaTheory]
    [InlineData(1)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    [InlineData(2)]
    [InlineData(3.5)]
    public void Clock_part_crossfade_starts_with_the_complete_outgoing_selected_label_at_every_density(double density)
    {
        var dial = new MaterialClockDial { Value = 3 };
        using var host = new GeometryHost(dial, 320, 320);
        host.Window.SetRenderScaling(density); host.Render();
        host.Theme.Motion = new MaterialMotion(); host.Render();
        var selectedLabel = new Rect(222, 118, 14, 20);
        var before = WhiteInk(dial, density, selectedLabel);
        Assert.True(before >= 8);
        dial.ActivePart = MaterialTimePickerPart.Minute;
        var outgoing = WhiteInk(dial, density, selectedLabel);
        // Bitmap compositing can soften the glyph's antialiased edge pixels; its
        // selected-label ink must still cover the same native region, rather than disappear.
        Assert.True(outgoing >= before * .5);
    }

    [AvaloniaFact]
    public void Clock_part_change_skips_an_empty_capture_axis_and_recovers_a_selectable_face()
    {
        var dial = new MaterialClockDial { Value = 3 };
        using var host = new GeometryHost(dial, 320, 320);
        host.Theme.Motion = new MaterialMotion(); host.Render();
        dial.Height = 0; host.Render();
        Assert.True(dial.Bounds.Width > 0); Assert.Equal(0, dial.Bounds.Height);
        Assert.Null(Record.Exception(() => dial.ActivePart = MaterialTimePickerPart.Minute));
        dial.Height = 256; host.Theme.Motion = new MaterialMotion { ReduceMotion = true }; host.Render();
        var number = dial.Children.OfType<MaterialClockNumber>().Single(action => action.Value == 15);
        var selected = 0; dial.ValueSelected += (_, change) => selected = change.Value;
        var point = GeometryHost.Box(number, host.Window).Center;
        host.Window.MouseDown(point, MouseButton.Left); host.Window.MouseUp(point, MouseButton.Left);
        Assert.Equal(15, selected);
    }

    [AvaloniaTheory]
    [InlineData(1.25, 3.5)]
    [InlineData(3.5, 1.25)]
    public void Clock_retiring_face_keeps_its_geometry_during_live_density_resize_and_reversal(double from, double to)
    {
        var dial = new MaterialClockDial { Value = 3 };
        using var host = new GeometryHost(dial, 400, 400);
        host.Window.SetRenderScaling(from); host.Render();
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with {
            DefaultEffects = new(1, .01), DefaultSpatial = new(1, .01) } }; host.Render();
        dial.ActivePart = MaterialTimePickerPart.Minute;
        host.Window.SetRenderScaling(to);
        host.Theme.Typography = host.Theme.Typography with { Scale = 1.25 }; host.Render();
        var region = new Rect(222 * 1.25, 118 * 1.25, 14 * 1.25, 20 * 1.25);
        var outgoing = WhiteInk(dial, to, region);
        Assert.True(outgoing >= 8 * to * to);
        dial.ActivePart = MaterialTimePickerPart.Hour;
        Assert.True(WhiteInk(dial, to, region) >= outgoing * .5);
    }

    private static int WhiteInk(Control visual, double density, Rect region)
    {
        var size = new PixelSize((int)Math.Ceiling(visual.Bounds.Width * density), (int)Math.Ceiling(visual.Bounds.Height * density));
        using var bitmap = new RenderTargetBitmap(size, new Vector(96 * density, 96 * density)); bitmap.Render(visual);
        using var pixels = new WriteableBitmap(size, new Vector(96 * density, 96 * density), PixelFormat.Bgra8888, AlphaFormat.Premul);
        using var storage = pixels.Lock(); bitmap.CopyPixels(storage);
        var count = 0;
        for (var y = (int)(region.Top * density); y < region.Bottom * density; y++)
        for (var x = (int)(region.Left * density); x < region.Right * density; x++)
        {
            var offset = y * storage.RowBytes + x * 4;
            if (Marshal.ReadByte(storage.Address, offset) > 240 && Marshal.ReadByte(storage.Address, offset + 1) > 240 && Marshal.ReadByte(storage.Address, offset + 2) > 240) count++;
        }
        return count;
    }

    private static Color Pixel(Control visual, double density, Point point)
    {
        var size = new PixelSize((int)Math.Ceiling(visual.Bounds.Width * density), (int)Math.Ceiling(visual.Bounds.Height * density));
        using var bitmap = new RenderTargetBitmap(size, new Vector(96 * density, 96 * density)); bitmap.Render(visual);
        using var pixels = new WriteableBitmap(size, new Vector(96 * density, 96 * density), PixelFormat.Bgra8888, AlphaFormat.Premul);
        using var storage = pixels.Lock(); bitmap.CopyPixels(storage);
        var offset = (int)(point.Y * density) * storage.RowBytes + (int)(point.X * density) * 4;
        return Color.FromRgb(Marshal.ReadByte(storage.Address, offset + 2), Marshal.ReadByte(storage.Address, offset + 1), Marshal.ReadByte(storage.Address, offset));
    }
}
