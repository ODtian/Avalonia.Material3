using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Material3.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;
using Xunit;

namespace Avalonia.Material3.Tests;

public class NativeClockRasterScenarioTests
{
    [AvaloniaTheory]
    [InlineData(1.25)]
    [InlineData(3.5)]
    public void Default_roboto_numeral_matches_the_native_public_font_raster(double density)
    {
        var family = new FontFamily($"avares://{typeof(NativeClockRasterScenarioTests).Assembly.GetName().Name}/ReferenceFonts#Roboto");
        var dial = new MaterialClockDial { Value = 12 };
        using var host = new GeometryHost(dial, 256, 256);
        host.Theme.Typography = host.Theme.Typography with { FontFamily = family }; host.Window.SetRenderScaling(density); host.Render();
        var number = dial.Children.OfType<MaterialClockNumber>().Single(mark => mark.Value == 3);
        using var before = host.Window.CaptureRenderedFrame()!;
        var box = GeometryHost.Box(number, host.Window);
        using var source = AssetLoader.Open(new Uri($"avares://{typeof(NativeClockRasterScenarioTests).Assembly.GetName().Name}/ReferenceFonts/Roboto-Regular.ttf"));
        using var data = SKData.Create(source);
        using var face = SKTypeface.FromData(data);
        using var layout = new TextLayout("3", new Typeface(family), number.FontSize, number.Foreground,
            lineHeight: number.GetValue(TextBlock.LineHeightProperty), letterSpacing: number.LetterSpacing);
        number.ContentTemplate = new FuncDataTemplate<string>((_, _) => new NativeLabel(face, layout, number.FontSize, density)); host.Render();
        using var after = host.Window.CaptureRenderedFrame()!;
        using var original = before.Lock(); using var expected = after.Lock();
        var different = 0; var ink = 0;
        for (var y = (int)(box.Top * density); y < (int)(box.Bottom * density); y++)
        for (var x = (int)(box.Left * density); x < (int)(box.Right * density); x++)
        {
            if (System.Runtime.InteropServices.Marshal.ReadByte(original.Address, y * original.RowBytes + x * 4 + 1) < 180) ink++;
            for (var channel = 0; channel < 4; channel++)
                if (System.Runtime.InteropServices.Marshal.ReadByte(original.Address, y * original.RowBytes + x * 4 + channel) !=
                    System.Runtime.InteropServices.Marshal.ReadByte(expected.Address, y * expected.RowBytes + x * 4 + channel)) different++;
        }
        Assert.True(ink > 10);
        Assert.Equal(0, different);
    }

    // Independent single-character AndroidTextPaint oracle. The public paragraph
    // supplies only its measured box/baseline; font flags are pinned native values.
    private sealed class NativeLabel(SKTypeface face, TextLayout layout, double size, double density) : Control
    {
        protected override Size MeasureOverride(Size availableSize) => new(Math.Ceiling(layout.Width * density) / density, Math.Ceiling(layout.Height * density) / density);
        public override void Render(DrawingContext context) => context.Custom(new NativeInk(face, size, layout.TextLines[0].Baseline, density, Bounds.Size));
    }
    private sealed class NativeInk(SKTypeface face, double size, double baseline, double density, Size bounds) : ICustomDrawOperation
    {
        public Rect Bounds => new(bounds);
        public void Dispose() { }
        public bool Equals(ICustomDrawOperation? other) => false;
        public bool HitTest(Point point) => false;
        public void Render(ImmediateDrawingContext context)
        {
            using var lease = context.TryGetFeature<ISkiaSharpApiLeaseFeature>()!.Lease();
            using var font = new SKFont(face, (float)(size * density)) { Subpixel = false, LinearMetrics = false,
                BaselineSnap = true, Hinting = SKFontHinting.Normal, Edging = SKFontEdging.Antialias };
            using var blob = SKTextBlob.Create("3", font);
            using var paint = new SKPaint { Color = new SKColor(29, 27, 32), IsAntialias = true };
            var canvas = lease.SkCanvas; var saved = canvas.Save();
            canvas.Scale((float)(1 / density)); canvas.DrawText(blob, 0, (float)(baseline * density), paint); canvas.RestoreToCount(saved);
        }
    }
}
