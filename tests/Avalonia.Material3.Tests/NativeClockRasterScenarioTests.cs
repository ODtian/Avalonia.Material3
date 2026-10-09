using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Material3.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.VisualTree;
using SkiaSharp;
using Xunit;

namespace Avalonia.Material3.Tests;

public class NativeClockRasterScenarioTests
{
    [AvaloniaFact]
    public void Clock_composite_refreshes_live_solid_roles_and_generated_numeral_ink()
    {
        var dialBrush = new SolidColorBrush(Colors.Yellow);
        var selectorBrush = new SolidColorBrush(Colors.Blue);
        var dial = new MaterialClockDial { Value = 12, DialBrush = dialBrush, SelectorBrush = selectorBrush };
        using var host = new GeometryHost(dial, 256, 256);
        host.Theme.Typography = host.Theme.Typography with { FontFamily = ReferenceFamily() };
        host.Window.SetRenderScaling(3.5); host.Render();
        Assert.Equal(Colors.Yellow, host.Pixel(128, 180));
        Assert.Equal(Colors.Blue, host.Pixel(128, 128));
        dialBrush.Color = Colors.Lime; selectorBrush.Color = Colors.Red; host.Render();
        Assert.Equal(Colors.Lime, host.Pixel(128, 180));
        Assert.Equal(Colors.Red, host.Pixel(128, 128));
        var number = dial.Children.OfType<MaterialClockNumber>().Single(mark => mark.Value == 3);
        var foreground = new SolidColorBrush(Colors.Blue); number.Foreground = foreground; host.Render();
        var box = GeometryHost.Box(number, host.Window);
        Assert.True(CountColour(host, box, Colors.Blue) > 10);
        foreground.Color = Colors.Red; host.Render();
        Assert.True(CountColour(host, box, Colors.Red) > 10);
        Assert.Equal(0, CountColour(host, box, Colors.Blue));
        number.IsVisible = false; host.Render();
        Assert.Equal(0, CountColour(host, box, Colors.Red));
        number.IsVisible = true; host.Render();
        Assert.True(CountColour(host, box, Colors.Red) > 10);
    }

    [AvaloniaTheory]
    [InlineData("scale")]
    [InlineData("clip")]
    [InlineData("opacity")]
    [InlineData("mask")]
    [InlineData("effect")]
    public void Clock_generated_numeral_preserves_caller_paint_constraints(string constraint)
    {
        var dial = new MaterialClockDial { Value = 12 };
        using var host = new GeometryHost(dial, 256, 256);
        host.Theme.Typography = host.Theme.Typography with { FontFamily = ReferenceFamily() };
        host.Window.SetRenderScaling(3.5); host.Render();
        var number = dial.Children.OfType<MaterialClockNumber>().Single(mark => mark.Value == 3);
        var box = GeometryHost.Box(number, host.Window);
        var originalInk = CountColour(host, box, Color.Parse("#1D1B20"));
        Assert.True(originalInk > 10);
        if (constraint == "scale") number.RenderTransform = new ScaleTransform(0, 0);
        else if (constraint == "clip") number.Clip = new RectangleGeometry(new Rect(0, 0, 1, 1));
        else if (constraint == "opacity") number.GetVisualDescendants().OfType<ContentPresenter>().Single().Opacity = 0;
        else if (constraint == "mask") number.OpacityMask = Brushes.Transparent;
        else number.Effect = new BlurEffect { Radius = 10 };
        Assert.Equal(0, CountColour(host, box, Color.Parse("#1D1B20")));
        if (constraint == "scale") number.RenderTransform = null;
        else if (constraint == "clip") number.Clip = null;
        else if (constraint == "opacity") number.GetVisualDescendants().OfType<ContentPresenter>().Single().Opacity = 1;
        else if (constraint == "mask") number.OpacityMask = null;
        else number.Effect = null;
        Assert.Equal(originalInk, CountColour(host, box, Color.Parse("#1D1B20")));
    }

    private static FontFamily ReferenceFamily() => new($"avares://{typeof(NativeClockRasterScenarioTests).Assembly.GetName().Name}/ReferenceFonts#Roboto");
    [AvaloniaFact]
    public void Clock_composite_route_changes_refresh_unchanged_numerals_together()
    {
        var dial = new MaterialClockDial { Value = 12 };
        using var host = new GeometryHost(dial, 256, 256);
        host.Theme.Typography = host.Theme.Typography with { FontFamily = ReferenceFamily() };
        host.Window.SetRenderScaling(3.5); host.Render();
        var numbers = dial.Children.OfType<MaterialClockNumber>().ToArray();
        var changed = numbers.Single(mark => mark.Value == 3);
        var unchanged = numbers.Single(mark => mark.Value == 9);
        var box = GeometryHost.Box(unchanged, host.Window);
        var ink = CountColour(host, box, Color.Parse("#1D1B20")); Assert.True(ink > 10);
        changed.Opacity = .5;
        Assert.Equal(ink, CountColour(host, box, Color.Parse("#1D1B20")));
        changed.Opacity = 1;
        Assert.Equal(ink, CountColour(host, box, Color.Parse("#1D1B20")));
    }

    [AvaloniaFact]
    public void Clock_center_dot_retains_native_four_DIP_radius_at_double_font_scale()
    {
        var dial = new MaterialClockDial { Value = 12 };
        using var host = new GeometryHost(dial, 512, 512);
        host.Theme.Typography = host.Theme.Typography with { FontFamily = ReferenceFamily(), Scale = 2 };
        host.Render();
        Assert.Equal(Color.Parse("#6750A4"), host.Pixel(258, 256));
        Assert.Equal(Color.Parse("#E6E0E9"), host.Pixel(261, 256));
    }

    private static int CountColour(GeometryHost host, Rect box, Color colour)
    {
        using var bitmap = host.Window.CaptureRenderedFrame()!; using var pixels = bitmap.Lock();
        var density = host.Window.RenderScaling; var count = 0;
        for (var y = (int)(box.Top * density); y < (int)(box.Bottom * density); y++)
        for (var x = (int)(box.Left * density); x < (int)(box.Right * density); x++)
        {
            var offset = y * pixels.RowBytes + x * 4; var red = pixels.Format == PixelFormat.Rgba8888 ? 0 : 2;
            if (System.Runtime.InteropServices.Marshal.ReadByte(pixels.Address, offset + red) == colour.R
                && System.Runtime.InteropServices.Marshal.ReadByte(pixels.Address, offset + 1) == colour.G
                && System.Runtime.InteropServices.Marshal.ReadByte(pixels.Address, offset + 2 - red) == colour.B) count++;
        }
        return count;
    }

    [AvaloniaFact]
    public void Clock_cardinal_targets_match_the_actual_native_float_polar_boundary()
    {
        var dial = new MaterialClockDial();
        using var host = new GeometryHost(dial, 256, 256); host.Window.SetRenderScaling(3.5); host.Render();
        Assert.Equal(717, dial.Children.OfType<MaterialClockNumber>().Single(mark => mark.Value == 3).Bounds.Left * 3.5, precision: 6);
        Assert.Equal(717, dial.Children.OfType<MaterialClockNumber>().Single(mark => mark.Value == 6).Bounds.Top * 3.5, precision: 6);
    }

    [AvaloniaTheory]
    [InlineData(TextHintingMode.Unspecified)]
    [InlineData(TextHintingMode.Light)]
    [InlineData(TextHintingMode.None)]
    public void Caller_clock_raster_fallback_keeps_its_matching_measure_route_and_can_return_to_native(TextHintingMode hint)
    {
        var family = new FontFamily($"avares://{typeof(NativeClockRasterScenarioTests).Assembly.GetName().Name}/ReferenceFonts#Roboto");
        var dial = new MaterialClockDial { Value = 3 };
        using var host = new GeometryHost(dial, 256, 256);
        host.Theme.Typography = host.Theme.Typography with { FontFamily = family }; host.Window.SetRenderScaling(3.5); host.Render();
        var number = dial.Children.OfType<MaterialClockNumber>().Single(mark => mark.Value == 3);
        Control Label() => number.GetVisualDescendants().OfType<ContentPresenter>().Single().Child!;
        Assert.Equal(32, Label().Bounds.Width * 3.5, precision: 6);
        if (hint != TextHintingMode.Unspecified) TextOptions.SetTextHintingMode(host.Window, hint);
        else number.Foreground = new LinearGradientBrush { GradientStops = [new GradientStop(Colors.Black, 0), new GradientStop(Colors.Blue, 1)] };
        host.Render(); Assert.Equal(34, Label().Bounds.Width * 3.5, precision: 6);
        if (hint != TextHintingMode.Unspecified) TextOptions.SetTextHintingMode(host.Window, TextHintingMode.Strong);
        else number.Foreground = Brushes.Black;
        host.Render(); Assert.Equal(32, Label().Bounds.Width * 3.5, precision: 6);
    }

    [AvaloniaFact]
    public void Clock_preserves_explicit_caller_rendering_mode_and_can_return_to_native()
    {
        var family = new FontFamily($"avares://{typeof(NativeClockRasterScenarioTests).Assembly.GetName().Name}/ReferenceFonts#Roboto");
        var dial = new MaterialClockDial { Value = 3 };
        using var host = new GeometryHost(dial, 256, 256);
        host.Theme.Typography = host.Theme.Typography with { FontFamily = family }; host.Window.SetRenderScaling(3.5); host.Render();
        var number = dial.Children.OfType<MaterialClockNumber>().Single(mark => mark.Value == 3);
        Control Label() => number.GetVisualDescendants().OfType<ContentPresenter>().Single().Child!;
        Assert.Equal(32, Label().Bounds.Width * 3.5, precision: 6);
        TextOptions.SetTextRenderingMode(host.Window, TextRenderingMode.Alias); host.Render();
        Assert.Equal(34, Label().Bounds.Width * 3.5, precision: 6);
        TextOptions.SetTextRenderingMode(host.Window, TextRenderingMode.Antialias); host.Render();
        Assert.Equal(32, Label().Bounds.Width * 3.5, precision: 6);
    }

    [AvaloniaTheory]
    [InlineData(1.25, 12)]
    [InlineData(3.5, 32)]
    public void Default_clock_paragraph_uses_the_actual_native_integer_intrinsic_width(double density, double pixels)
    {
        var family = new FontFamily($"avares://{typeof(NativeClockRasterScenarioTests).Assembly.GetName().Name}/ReferenceFonts#Roboto");
        var dial = new MaterialClockDial { Value = 3 };
        using var host = new GeometryHost(dial, 256, 256);
        host.Theme.Typography = host.Theme.Typography with { FontFamily = family }; host.Window.SetRenderScaling(density); host.Render();
        var number = dial.Children.OfType<MaterialClockNumber>().Single(mark => mark.Value == 3);
        var label = number.GetVisualDescendants().OfType<ContentPresenter>().Single().Child!;
        Assert.Equal(pixels, label.Bounds.Width * density, precision: 6);
    }

    [AvaloniaFact]
    public void Gallery_hinted_font_retains_its_existing_caller_measure_recipe()
    {
        var family = new FontFamily($"avares://{typeof(NativeClockRasterScenarioTests).Assembly.GetName().Name}/GalleryClockFonts#Gallery Roboto");
        var dial = new MaterialClockDial { Value = 3 };
        using var host = new GeometryHost(dial, 256, 256);
        host.Theme.Typography = host.Theme.Typography with { FontFamily = family }; host.Window.SetRenderScaling(3.5); host.Render();
        var number = dial.Children.OfType<MaterialClockNumber>().Single(mark => mark.Value == 3);
        Assert.Equal(34, number.GetVisualDescendants().OfType<ContentPresenter>().Single().Child!.Bounds.Width * 3.5, precision: 6);
    }

    [AvaloniaFact]
    public void Internal_clock_reproduction_has_the_pinned_true_400_face_and_unchanged_glyph_mapping()
    {
        using var original = AssetLoader.Open(new Uri($"avares://{typeof(NativeClockRasterScenarioTests).Assembly.GetName().Name}/ReferenceFonts/Roboto-Regular.ttf"));
        using var reproduction = AssetLoader.Open(new Uri("avares://Avalonia.Material3/Assets/Fonts/Roboto-Clock400.ttf"));
        using var sourceData = SKData.Create(original); using var derivedData = SKData.Create(reproduction);
        using var sourceFace = SKTypeface.FromData(sourceData); using var derivedFace = SKTypeface.FromData(derivedData);
        Assert.Equal(400, derivedFace.FontStyle.Weight); Assert.Equal(5, derivedFace.FontStyle.Width);
        Assert.Equal(SKFontStyleSlant.Upright, derivedFace.FontStyle.Slant);
        Assert.Equal(3362, derivedFace.GlyphCount); Assert.Equal(sourceFace.GlyphCount, derivedFace.GlyphCount);
        Assert.Equal(sourceFace.GetTableData(0x636d6170), derivedFace.GetTableData(0x636d6170));
    }

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
        protected override Size MeasureOverride(Size availableSize) => new((density == 1.25 ? 12 : 32) / density, Math.Ceiling(layout.Height * density) / density);
        public override void Render(DrawingContext context) => context.Custom(new NativeInk(face, size, (density == 1.25 ? 22 : 61) / density, density, Bounds.Size));
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
            using var font = new SKFont(face, (float)(size * density)) { Subpixel = false, LinearMetrics = false, EmbeddedBitmaps = true,
                BaselineSnap = true, Hinting = SKFontHinting.None, Edging = SKFontEdging.Antialias };
            using var blob = SKTextBlob.Create("3", font);
            using var paint = new SKPaint { Color = new SKColor(29, 27, 32), IsAntialias = true };
            var canvas = lease.SkCanvas; var saved = canvas.Save();
            canvas.Scale((float)(1 / density)); canvas.DrawText(blob, 0, (float)(baseline * density), paint); canvas.RestoreToCount(saved);
        }
    }
}
