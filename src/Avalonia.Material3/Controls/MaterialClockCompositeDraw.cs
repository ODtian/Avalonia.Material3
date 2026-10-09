using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace Avalonia.Material3.Controls;

// Native blend phases share one target; the opaque output retains its RGB.
internal sealed class MaterialClockCompositeDraw(Rect bounds, Point center, double faceRadius, Point selector, double radius, float angle, double stroke,
    double dotRadius, Color background, Color primary, Color selected, double density,
    MaterialNativeText.GlyphPaint[] glyphs) : ICustomDrawOperation
{
    private static readonly HashSet<string> BranchDiagnostics = [];
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
            if (angle == 0 || angle == -.8377580642700195f)
            {
                var key = $"{lease.GrContext is not null}/{lease.CurrentOpacity:R}/{angle:R}";
                lock (BranchDiagnostics)
                    if (BranchDiagnostics.Count < 8 && BranchDiagnostics.Add(key))
                    {
                        var matrix = lease.SkCanvas.TotalMatrix; var target = lease.SkCanvas.DeviceClipBounds;
                        using var targetImage = lease.SkSurface?.Snapshot();
                        Console.WriteLine($"M3ClockTarget gpu={lease.GrContext is not null} opacity={lease.CurrentOpacity:R} direct={lease.GrContext is not null && lease.CurrentOpacity == 1} " +
                            $"matrix={matrix.ScaleX:R},{matrix.ScaleY:R},{matrix.SkewX:R},{matrix.SkewY:R},{matrix.TransX:R},{matrix.TransY:R} clip={target} target={targetImage?.ColorType}/{targetImage?.AlphaType}/srgb{targetImage?.ColorSpace?.IsSrgb} " +
                            $"selectorDip={selector.X:R},{selector.Y:R} selectorPx={(float)selector.X * (float)density:R},{(float)selector.Y * (float)density:R} radiusPx={(float)radius * (float)density:R} angle={angle:R}");
                    }
            }
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
        var scale = (float)density;
        var centerPx = new SKPoint((float)center.X * scale, (float)center.Y * scale);
        var selectorPx = new SKPoint((float)selector.X * scale, (float)selector.Y * scale);
        var radiusPx = (float)radius * scale;
        var saved = canvas.Save();
        canvas.Scale(1 / scale);
        if (!owned)
        {
            using var backgroundPaint = new SKPaint { IsAntialias = true, Color = Colour(background) };
            canvas.DrawCircle(centerPx.X, centerPx.Y, (float)faceRadius * scale, backgroundPaint);
        }
        using var paint = new SKPaint { IsAntialias = true, BlendMode = SKBlendMode.Clear };
        canvas.DrawCircle(selectorPx.X, selectorPx.Y, radiusPx, paint);
        canvas.RestoreToCount(saved);
        foreach (var glyph in glyphs) glyph.Paint(canvas, 1);
        saved = canvas.Save();
        canvas.Scale(1 / scale);
        paint.Color = Colour(primary); paint.BlendMode = SKBlendMode.Xor;
        canvas.DrawCircle(selectorPx.X, selectorPx.Y, radiusPx, paint);
        var end = new SKPoint(selectorPx.X - radiusPx * (float)Math.Cos(angle), selectorPx.Y - radiusPx * (float)Math.Sin(angle));
        paint.BlendMode = SKBlendMode.SrcOver; paint.Style = SKPaintStyle.Stroke; paint.StrokeWidth = (float)stroke * scale;
        canvas.DrawLine(centerPx.X, centerPx.Y, end.X, end.Y, paint);
        paint.Style = SKPaintStyle.Fill; canvas.DrawCircle(centerPx.X, centerPx.Y, (float)dotRadius * scale, paint);
        paint.BlendMode = SKBlendMode.DstOver; paint.Color = Colour(selected);
        canvas.DrawCircle(selectorPx.X, selectorPx.Y, radiusPx, paint);
        if (owned)
        {
            canvas.DrawColor(SKColors.Black, SKBlendMode.DstOver);
            using var outside = new SKPath { FillType = SKPathFillType.InverseEvenOdd };
            outside.AddCircle(centerPx.X, centerPx.Y, (float)faceRadius * scale);
            paint.BlendMode = SKBlendMode.Clear;
            canvas.DrawPath(outside, paint);
        }
        canvas.RestoreToCount(saved);
    }
    private static SKColor Colour(Color color) => new(color.R, color.G, color.B, color.A);
}
