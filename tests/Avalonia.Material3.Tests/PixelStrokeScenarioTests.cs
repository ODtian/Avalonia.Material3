using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Material3.Controls;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Material3.Tests;

public class PixelStrokeScenarioTests
{
    [AvaloniaTheory]
    [InlineData(1.25, 39, 23, 2)]
    [InlineData(3.5, 109, 63, 7)]
    public void Checkbox_uses_the_native_integer_canvas_placement_and_floored_stroke(double density, int expectedLeft, int expectedSize, double expectedStroke)
    {
        var checkbox = new MaterialCheckBox { Margin = new Thickness(16), HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top, BorderBrush = Brushes.Black };
        using var host = new GeometryHost(checkbox, 100, 100);
        host.Window.SetRenderScaling(density); host.Render();
        using var bitmap = host.Window.CaptureRenderedFrame()!; using var storage = bitmap.Lock();
        var left = bitmap.PixelSize.Width; var right = -1; var top = bitmap.PixelSize.Height; var bottom = -1;
        for (var y = 0; y < bitmap.PixelSize.Height; y++)
        for (var x = 0; x < bitmap.PixelSize.Width; x++)
        {
            var offset = y * storage.RowBytes + x * 4;
            if (System.Runtime.InteropServices.Marshal.ReadByte(storage.Address, offset + 1) >= 230) continue;
            left = Math.Min(left, x); right = Math.Max(right, x); top = Math.Min(top, y); bottom = Math.Max(bottom, y);
        }
        Assert.Equal(expectedLeft, left); Assert.Equal(expectedLeft, top);
        Assert.Equal(expectedSize, right - left + 1); Assert.Equal(expectedSize, bottom - top + 1);
        var row = (top + bottom) / 2;
        double coverage = 0;
        for (var x = left; x < left + expectedSize / 2; x++)
        {
            var offset = row * storage.RowBytes + x * 4;
            var green = System.Runtime.InteropServices.Marshal.ReadByte(storage.Address, offset + 1);
            coverage += (247 - green) / 247d;
        }
        Assert.InRange(Math.Abs(expectedStroke - coverage), 0, .02);
    }
}
