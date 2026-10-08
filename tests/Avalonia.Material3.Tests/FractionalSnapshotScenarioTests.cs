using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Runtime.InteropServices;
using Xunit;

namespace Avalonia.Material3.Tests;

public class FractionalSnapshotScenarioTests
{
    [AvaloniaTheory]
    [InlineData(1.25)]
    [InlineData(3.5)]
    public void Clock_retiring_custom_content_keeps_its_fractional_origin_and_extent(double density)
    {
        var dial = new MaterialClockDial { Width = 301.3, Height = 300.2, Value = 3 };
        using var host = new GeometryHost(dial, 400, 400);
        host.Window.SetRenderScaling(density); host.Render();
        var number = dial.Children.OfType<MaterialClockNumber>().Single(action => action.Value == 3);
        number.ContentTemplate = new FuncDataTemplate<object>((_, _) => new Border { Width = 24, Height = 12, Background = Brushes.Lime }); host.Render();
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with {
            DefaultEffects = new(1, .01), DefaultSpatial = new(1, .01) } }; host.Render();
        var before = GreenCentroid(dial, density);
        dial.ActivePart = MaterialTimePickerPart.Minute;
        var after = GreenCentroid(dial, density);
        Assert.InRange(Math.Abs(before.X - after.X), 0, .15);
        Assert.InRange(Math.Abs(before.Y - after.Y), 0, .15);
    }

    private static Point GreenCentroid(Control control, double density)
    {
        var size = new PixelSize((int)Math.Ceiling(control.Bounds.Width * density), (int)Math.Ceiling(control.Bounds.Height * density));
        using var bitmap = new RenderTargetBitmap(size, new Vector(96 * density, 96 * density)); bitmap.Render(control);
        using var pixels = new WriteableBitmap(size, new Vector(96 * density, 96 * density), PixelFormat.Bgra8888, AlphaFormat.Premul);
        using var storage = pixels.Lock(); bitmap.CopyPixels(storage);
        double weight = 0, xTotal = 0, yTotal = 0;
        for (var y = 0; y < size.Height; y++)
        for (var x = 0; x < size.Width; x++)
        {
            var offset = y * storage.RowBytes + x * 4;
            var amount = Math.Max(0, Marshal.ReadByte(storage.Address, offset + 1) - Math.Max(Marshal.ReadByte(storage.Address, offset), Marshal.ReadByte(storage.Address, offset + 2)));
            weight += amount; xTotal += amount * (x + .5); yTotal += amount * (y + .5);
        }
        Assert.True(weight > 1000);
        return new(xTotal / weight / density, yTotal / weight / density);
    }
}
