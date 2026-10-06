using System.Runtime.InteropServices;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Material3.Tests;

// All positions are PHYSICAL WINDOW offsets from transformed AABBs, never owner-local expected coordinates.
public class ReviewPhysicalScenarioTests
{
    public static Rect Physical(Visual control, Window window) => new Rect(control.Bounds.Size)
        .TransformToAABB(control.TransformToVisual(window)!.Value);

    public static Color Pixel(Window window, Point physical)
    {
        using var bitmap = window.CaptureRenderedFrame()!;
        using var frame = bitmap.Lock();
        var offset = (int)(physical.Y * window.RenderScaling) * frame.RowBytes + (int)(physical.X * window.RenderScaling) * 4;
        var first = Marshal.ReadByte(frame.Address, offset);
        var green = Marshal.ReadByte(frame.Address, offset + 1);
        var third = Marshal.ReadByte(frame.Address, offset + 2);
        return frame.Format == PixelFormat.Bgra8888 ? Color.FromRgb(third, green, first) : Color.FromRgb(first, green, third);
    }

    [AvaloniaTheory]
    [InlineData(12, 14, 1)]
    [InlineData(12, 14, 2)]
    [InlineData(10, 14, 1)]
    [InlineData(12, 12, 1)]
    public void Range_calendar_paints_40_dip_band_inside_48_dip_targets_including_endpoints_rows_and_fonts(int start, int end, double scale)
    {
        using var host = new FeedbackHost(800, 800);
        host.Theme.Typography = host.Theme.Typography with { Scale = scale };
        var picker = new MaterialDatePicker { SelectionMode = MaterialDateSelectionMode.Range,
            DisplayMonth = new(2024, 2, 1), SelectedDate = new(2024, 2, start), RangeEnd = new(2024, 2, end) };
        picker.Show(host.Overlay); host.Render();
        foreach (var day in picker.GetVisualDescendants().OfType<MaterialCalendarDay>().Where(d => d.IsInRange))
        {
            var box = Physical(day, host.Window);
            Assert.True(box.Height >= 48 && box.Width >= 48);
            Assert.Equal(Color.Parse("#ECE6F0"), Pixel(host.Window, new(box.Left + 12, box.Top + 2)));
            Assert.Equal(Color.Parse("#ECE6F0"), Pixel(host.Window, new(box.Left + 12, box.Bottom - 2)));
            if (day.Date.Day != start && day.Date.Day != end)
            {
                Assert.Equal(Color.Parse("#E8DEF8"), Pixel(host.Window, new(box.Left + 2, box.Center.Y)));
                Assert.Equal(Color.Parse("#ECE6F0"), Pixel(host.Window, new(box.Left + 2, box.Center.Y - 21)));
            }
        }
    }
}
