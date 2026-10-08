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
    [InlineData(false)]
    [InlineData(true)]
    public void Disabled_switch_roles_are_opaque_native_surface_composites(bool selected)
    {
        var control = new MaterialSwitch { IsChecked = selected, IsEnabled = false, Margin = new Thickness(16),
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        using var host = new GeometryHost(control, 140, 100);
        Assert.Equal(selected ? Color.FromRgb(227, 220, 228) : Color.FromRgb(251, 244, 252), host.Pixel(selected ? 26 : 42, 40));
        Assert.Equal(selected ? Color.Parse("#FEF7FF") : Color.FromRgb(168, 163, 170), host.Pixel(selected ? 54 : 26, 40));
        Assert.Equal(Color.FromRgb(227, 220, 228), host.Pixel(17, 40));
    }

    [AvaloniaTheory]
    [InlineData(1.25)]
    [InlineData(3.5)]
    public void Disabled_checkbox_uses_the_native_quantized_color_alpha(double density)
    {
        var checkbox = new MaterialCheckBox { IsChecked = true, IsEnabled = false, Margin = new Thickness(16),
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        using var host = new GeometryHost(checkbox, 100, 100);
        host.Window.SetRenderScaling(density); host.Render();
        var actual = host.Pixel(35, 35);
        // Skia's software 8888 blend can round one channel differently from the
        // native GPU. The isolated source alpha below is the component contract.
        Assert.InRange(actual.R, 168, 169); Assert.Equal(163, actual.G); Assert.Equal(170, actual.B);
        checkbox.Margin = default; host.Render();
        var size = new PixelSize((int)Math.Ceiling(checkbox.Bounds.Width * density), (int)Math.Ceiling(checkbox.Bounds.Height * density));
        using var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(size, new Vector(96 * density, 96 * density)); bitmap.Render(checkbox);
        using var pixels = new Avalonia.Media.Imaging.WriteableBitmap(size, new Vector(96 * density, 96 * density),
            Avalonia.Platform.PixelFormat.Bgra8888, Avalonia.Platform.AlphaFormat.Premul);
        using var storage = pixels.Lock(); bitmap.CopyPixels(storage);
        var offset = (int)(20 * density) * storage.RowBytes + (int)(20 * density) * 4;
        Assert.Equal(97, System.Runtime.InteropServices.Marshal.ReadByte(storage.Address, offset + 3));
    }

    [AvaloniaTheory]
    [InlineData("radio", 1.25, 2.5)]
    [InlineData("radio", 3.5, 7)]
    [InlineData("switch", 1.25, 3)]
    [InlineData("switch", 3.5, 7)]
    [InlineData("button", 1.25, 2)]
    [InlineData("button", 3.5, 4)]
    [InlineData("field", 1.25, 2)]
    [InlineData("field", 3.5, 4)]
    [InlineData("card", 1.25, 2)]
    [InlineData("card", 3.5, 4)]
    public void Each_component_keeps_its_native_stroke_recipe(string family, double density, double expectedStroke)
    {
        Control control = family switch
        {
            "radio" => new MaterialRadioButton { BorderBrush = Brushes.Black },
            "switch" => new MaterialSwitch { BorderBrush = Brushes.Black, Background = Brushes.Transparent },
            "button" => new MaterialButton { Variant = MaterialButtonVariant.Outlined, Width = 100, BorderBrush = Brushes.Black },
            "card" => new MaterialCard { Variant = MaterialCardVariant.Outlined, Width = 100, Height = 48, BorderBrush = Brushes.Black },
            _ => new MaterialTextField { Variant = MaterialTextFieldVariant.Outlined, Width = 100, BorderBrush = Brushes.Black }
        };
        control.Margin = new Thickness(16); control.HorizontalAlignment = HorizontalAlignment.Left;
        control.VerticalAlignment = VerticalAlignment.Top;
        using var host = new GeometryHost(control, 140, 100);
        host.Window.SetRenderScaling(density); host.Render();
        using var bitmap = host.Window.CaptureRenderedFrame()!; using var storage = bitmap.Lock();
        var y = (int)Math.Round((control.Bounds.Top + control.Bounds.Height / 2) * density);
        var x = (int)Math.Round(control.Bounds.Left * density);
        if (family == "radio") x += (int)Math.Round(14 * density);
        double coverage = 0;
        for (var i = x; i < x + (family == "radio" ? 8 : 7) * density; i++)
        {
            var green = System.Runtime.InteropServices.Marshal.ReadByte(storage.Address, y * storage.RowBytes + i * 4 + 1);
            coverage += (247 - green) / 247d;
        }
        Assert.InRange(Math.Abs(expectedStroke - coverage), 0, .04);
    }

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
