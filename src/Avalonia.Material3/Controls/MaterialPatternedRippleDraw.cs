using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Material3.Controls;

// The immutable AOSP shader is compiled once. Retained draw commands contain values,
// and acquire a typed renderer lease only on the render thread.
internal sealed class MaterialPatternedRippleDraw(Rect rectangle, Point touch, double radius, double progress,
    double noisePhase, Color color, double density) : ICustomDrawOperation
{
    private static readonly Lazy<SKRuntimeEffect> Effect = new(() =>
    {
        using var source = typeof(MaterialPatternedRippleDraw).Assembly.GetManifestResourceStream("M3.PatternedRipple.sksl")!;
        using var reader = new StreamReader(source);
        return SKRuntimeEffect.CreateShader(reader.ReadToEnd(), out var error)
            ?? throw new InvalidOperationException("The bundled AOSP ripple shader could not compile: " + error);
    });
    internal static void Prepare() => _ = Effect.Value;
    public Rect Bounds => rectangle;
    public bool HitTest(Point point) => false;
    public bool Equals(ICustomDrawOperation? other) => ReferenceEquals(this, other);
    public void Dispose() { }
    public void Render(ImmediateDrawingContext context)
    {
        if (context.TryGetFeature<ISkiaSharpApiLeaseFeature>() is { } feature)
        {
            using var lease = feature.Lease();
            var matrix = lease.SkCanvas.TotalMatrix;
            var scale = Math.Max(.001, Math.Sqrt(matrix.ScaleX * matrix.ScaleX + matrix.SkewY * matrix.SkewY));
            Draw(lease.SkCanvas, scale, lease.CurrentOpacity);
        }
        else
        {
            // Other backends receive the same SkSL evaluated on a CPU surface.
            var scale = Math.Max(.001, density);
            var size = new PixelSize(Math.Max(1, (int)Math.Ceiling(rectangle.Width * scale)), Math.Max(1, (int)Math.Ceiling(rectangle.Height * scale)));
            using var bitmap = new WriteableBitmap(size, new Vector(96 * scale, 96 * scale), PixelFormat.Bgra8888, AlphaFormat.Premul);
            using (var storage = bitmap.Lock())
            {
                using var surface = SKSurface.Create(new SKImageInfo(size.Width, size.Height, SKColorType.Bgra8888, SKAlphaType.Premul),
                    storage.Address, storage.RowBytes);
                surface.Canvas.Clear(SKColors.Transparent);
                surface.Canvas.Scale((float)scale); surface.Canvas.Translate((float)-rectangle.X, (float)-rectangle.Y);
                Draw(surface.Canvas, scale, 1);
            }
            context.DrawBitmap(bitmap, new Rect(0, 0, size.Width, size.Height), rectangle);
        }
    }
    private void Draw(SKCanvas canvas, double density, double opacity)
    {
        var width = (float)(rectangle.Width * density); var height = (float)(rectangle.Height * density);
        var phase = (float)noisePhase;
        var center = rectangle.Center;
        var uniforms = new SKRuntimeEffectUniforms(Effect.Value)
        {
            ["in_origin"] = new[] { (float)(center.X * density), (float)(center.Y * density) },
            ["in_touch"] = new[] { (float)(touch.X * density), (float)(touch.Y * density) },
            ["in_progress"] = (float)progress,
            ["in_maxRadius"] = (float)(Math.Round(radius * density, MidpointRounding.AwayFromZero) * 2.3),
            ["in_resolutionScale"] = new[] { 1 / width, 1 / height },
            ["in_noiseScale"] = new[] { 2.1f / width, 2.1f / height },
            ["in_hasMask"] = 0f,
            ["in_noisePhase"] = phase * .001f,
            ["in_turbulencePhase"] = phase,
            ["in_color"] = new[] { color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f },
            ["in_sparkleColor"] = new[] { 1f, 1f, 1f, 141 / 255f }
        };
        for (var index = 1; index <= 3; index++)
        {
            var baseOffset = index == 1 ? .75 : index == 2 ? .3 : 1.5;
            var drift = index == 1 ? .01 : -.0066;
            var frequency = index == 1 ? .825 : index == 2 ? .675 : .525;
            uniforms["in_tCircle" + index] = new[] { (float)(baseOffset + phase * drift * Math.Cos(frequency)),
                (float)(baseOffset + phase * drift * Math.Sin(frequency)) };
            var rotation = phase * Math.PI * (index == 2 ? -.0078125 : .0078125)
                + Math.PI * (index == 1 ? 1.7 : index == 2 ? 2 : 2.75);
            uniforms["in_tRotation" + index] = new[] { (float)Math.Cos(rotation), (float)Math.Sin(rotation) };
        }
        using var mask = SKShader.CreateColor(SKColors.White);
        var children = new SKRuntimeEffectChildren(Effect.Value) { ["in_shader"] = mask };
        using var shader = Effect.Value.ToShader(uniforms, children);
        using var paint = new SKPaint { Shader = shader, IsAntialias = true, Color = SKColors.White.WithAlpha((byte)Math.Clamp(Math.Round(opacity * 255), 0, 255)) };
        var saved = canvas.Save();
        try
        {
            canvas.Scale((float)(1 / density));
            // AOSP AnimatedRippleDrawable draws the shader through its target-radius circle.
            canvas.DrawCircle((float)(center.X * density), (float)(center.Y * density), (float)Math.Round(radius * density, MidpointRounding.AwayFromZero), paint);
        }
        finally { canvas.RestoreToCount(saved); }
    }
}
