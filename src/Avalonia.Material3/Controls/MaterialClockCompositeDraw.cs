using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace Avalonia.Material3.Controls;

// Native blend phases share one target; the opaque output retains its RGB.
internal sealed class MaterialClockCompositeDraw(Rect bounds, Point center, double faceRadius, Point selector, double radius, double stroke,
    double dotRadius, Color background, Color primary, Color selected, double density,
    MaterialNativeText.GlyphPaint[] glyphs) : ICustomDrawOperation
{
    public Rect Bounds => bounds;
    public bool HitTest(Point point) => false;
    public bool Equals(ICustomDrawOperation? other) => ReferenceEquals(this, other);
    public void Dispose() { foreach (var glyph in glyphs) glyph.Dispose(); }
    public void Render(ImmediateDrawingContext context)
    {
        var size = new PixelSize(Math.Max(1, (int)Math.Ceiling(bounds.Width * density)), Math.Max(1, (int)Math.Ceiling(bounds.Height * density)));
        var info = new SKImageInfo(size.Width, size.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        if (context.TryGetFeature<ISkiaSharpApiLeaseFeature>() is { } feature)
        {
            using var lease = feature.Lease();
            if (lease.GrContext is not null && lease.CurrentOpacity == 1)
            {
                Paint(lease.SkCanvas, false);
                return;
            }
            if (lease.GrContext is { } gpu)
            {
                using var surface = SKSurface.Create(gpu, true, info, 0, GRSurfaceOrigin.TopLeft);
                if (surface is not null)
                {
                    Paint(surface.Canvas, true);
                    using var image = surface.Snapshot();
                    using var paint = new SKPaint { Color = SKColors.White.WithAlpha((byte)Math.Clamp(Math.Round(255 * lease.CurrentOpacity), 0, 255)) };
                    lease.SkCanvas.DrawImage(image, new SKRect(0, 0, size.Width, size.Height),
                        new SKRect((float)bounds.Left, (float)bounds.Top, (float)bounds.Right, (float)bounds.Bottom), paint);
                    return;
                }
            }
        }
        using var bitmap = new WriteableBitmap(size, new Vector(96 * density, 96 * density), PixelFormat.Bgra8888, AlphaFormat.Premul);
        using (var pixels = bitmap.Lock())
        using (var surface = SKSurface.Create(info, pixels.Address, pixels.RowBytes)) Paint(surface.Canvas, true);
        context.DrawBitmap(bitmap, new Rect(0, 0, size.Width, size.Height), bounds);
    }
    private void Paint(SKCanvas canvas, bool owned)
    {
        if (owned) { canvas.Clear(Colour(background)); canvas.Scale((float)density); }
        else
        {
            using var backgroundPaint = new SKPaint { IsAntialias = true, Color = Colour(background) };
            canvas.DrawCircle((float)center.X, (float)center.Y, (float)faceRadius, backgroundPaint);
        }
        using var paint = new SKPaint { IsAntialias = true, BlendMode = SKBlendMode.Clear };
        canvas.DrawCircle((float)selector.X, (float)selector.Y, (float)radius, paint);
        foreach (var glyph in glyphs) glyph.Paint(canvas, 1);
        paint.Color = Colour(primary); paint.BlendMode = SKBlendMode.Xor;
        canvas.DrawCircle((float)selector.X, (float)selector.Y, (float)radius, paint);
        var vector = new Vector(selector.X - center.X, selector.Y - center.Y); var length = vector.Length;
        var end = length > 0 ? selector - vector * (radius / length) : center;
        paint.BlendMode = SKBlendMode.SrcOver; paint.Style = SKPaintStyle.Stroke; paint.StrokeWidth = (float)stroke;
        canvas.DrawLine((float)center.X, (float)center.Y, (float)end.X, (float)end.Y, paint);
        paint.Style = SKPaintStyle.Fill; canvas.DrawCircle((float)center.X, (float)center.Y, (float)dotRadius, paint);
        paint.BlendMode = SKBlendMode.DstOver; paint.Color = Colour(selected);
        canvas.DrawCircle((float)selector.X, (float)selector.Y, (float)radius, paint);
        if (owned)
        {
            canvas.DrawColor(SKColors.Black, SKBlendMode.DstOver);
            using var outside = new SKPath { FillType = SKPathFillType.InverseEvenOdd };
            outside.AddCircle((float)center.X, (float)center.Y, (float)faceRadius);
            paint.BlendMode = SKBlendMode.Clear;
            canvas.DrawPath(outside, paint);
        }
    }
    private static SKColor Colour(Color color) => new(color.R, color.G, color.B, color.A);
}
