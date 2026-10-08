using System.Globalization;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Runtime.InteropServices;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Material3.Tests;

public class CalendarViewportScenarioTests
{
    [AvaloniaFact]
    public async Task Date_input_mode_retargets_height_when_typography_changes_during_its_transition()
    {
        var picker = new MaterialDatePicker { SelectedDate = new(2024, 2, 9), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top };
        using var host = new GeometryHost(picker, 360, 1100);
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with { DefaultSpatial = new(1, 1000) } }; host.Render();
        picker.Mode = MaterialDatePickerMode.Input; host.Window.UpdateLayout();
        host.Theme.Typography = host.Theme.Typography with { Scale = 2 }; host.Render();
        for (var frame = 0; frame < 8; frame++) { await Task.Delay(100); host.Render(); }
        Assert.True(GeometryHost.Box(picker.StartInput, host.Window).Bottom <= GeometryHost.Box(picker, host.Window).Bottom);
    }

    [AvaloniaFact]
    public void Month_scroll_keeps_the_retiring_selected_day_at_Android_render_density()
    {
        var picker = new MaterialDatePicker { Culture = CultureInfo.GetCultureInfo("en-US"), DisplayMonth = new(2024, 2, 1), SelectedDate = new(2024, 2, 9) };
        using var host = new GeometryHost(picker, 360, 800);
        host.Window.SetRenderScaling(3.5); host.Render();
        var day = picker.GetVisualDescendants().OfType<MaterialCalendarDay>().Single(action => action.Date == new DateOnly(2024, 2, 9));
        var box = GeometryHost.Box(day, host.Window);
        var point = new Point(box.Left + 12, box.Center.Y);
        Assert.Equal(Color.Parse("#6750A4"), host.Pixel(point.X, point.Y));
        host.Theme.Motion = new MaterialMotion(); host.Render();
        Assert.True(picker.NavigateMonth(1));
        // Offscreen public rendering samples the current displayed phase without advancing
        // the window's spring while its high-density screenshot is being captured.
        var local = host.Window.TranslatePoint(point, picker)!.Value;
        var size = new PixelSize((int)(picker.Bounds.Width * 3.5), (int)(picker.Bounds.Height * 3.5));
        using var bitmap = new RenderTargetBitmap(size, new Vector(336, 336)); bitmap.Render(picker);
        using var pixels = new WriteableBitmap(size, new Vector(336, 336), PixelFormat.Bgra8888, AlphaFormat.Premul);
        using var storage = pixels.Lock(); bitmap.CopyPixels(storage);
        var offset = (int)(local.Y * 3.5) * storage.RowBytes + (int)(local.X * 3.5) * 4;
        Assert.Equal(Color.Parse("#6750A4"), Color.FromRgb(Marshal.ReadByte(storage.Address, offset + 2), Marshal.ReadByte(storage.Address, offset + 1), Marshal.ReadByte(storage.Address, offset)));
    }

    [AvaloniaFact]
    public void Single_calendar_recovers_visible_days_and_pointer_selection_after_the_Android_initial_zero_viewport()
    {
        var picker = new MaterialDatePicker { Culture = CultureInfo.GetCultureInfo("en-US"), DisplayMonth = new(2024, 2, 1), SelectedDate = new(2024, 2, 9) };
        using var host = new GeometryHost(picker, 1, 1);
        host.Window.Width = 360; host.Window.Height = 800; host.Render();
        var day = picker.GetVisualDescendants().OfType<MaterialCalendarDay>().Single(action => action.Date == new DateOnly(2024, 2, 10));
        var box = GeometryHost.Box(day, host.Window);
        Assert.True(day.IsEffectivelyVisible && box.Height >= 48 && box.Bottom < 800);
        var hit = host.Window.InputHitTest(box.Center) as Visual;
        Assert.True(hit == day || hit is not null && day.IsVisualAncestorOf(hit));
        host.Window.MouseDown(box.Center, MouseButton.Left); host.Window.MouseUp(box.Center, MouseButton.Left); host.Render();
        Assert.Equal(new DateOnly(2024, 2, 10), picker.SelectedDate);
    }
}
