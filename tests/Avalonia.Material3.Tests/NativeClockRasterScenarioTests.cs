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
