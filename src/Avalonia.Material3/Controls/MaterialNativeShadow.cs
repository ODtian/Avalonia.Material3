using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.VisualTree;
using SkiaSharp;

namespace Avalonia.Material3.Controls;

// AOSP15 window lighting and Skia15 drawFastShadow share physical-device coordinates.
// Callers supply DIP geometry/elevation; retained commands own values, not UI objects.
internal static class MaterialNativeShadow
{
    internal readonly record struct Lighting(double Density, Matrix ToDevice, Matrix FromDevice, Point Light, double Height, double Radius, double CasterAlpha);
    private static Lighting? Light(Control owner)
    {
        var root = TopLevel.GetTopLevel(owner);
        if (root is not null && !root.IsAttachedToVisualTree()) return null;
        var density = root?.RenderScaling ?? 1;
        var matrix = (root is not null ? owner.TransformToVisual(root) : null) ?? Matrix.Identity;
        matrix *= Matrix.CreateScale(density, density);
        if (!matrix.TryInvert(out var inverse)) return null;
        var screen = root?.Screens?.ScreenFromTopLevel(root)?.Bounds;
        var origin = root?.PointToScreen(default) ?? default;
        var width = screen?.Width ?? (root?.ClientSize.Width ?? owner.Bounds.Width) * density;
        var height = screen?.Height ?? (root?.ClientSize.Height ?? owner.Bounds.Height) * density;
        var screenX = screen?.X ?? 0; var screenY = screen?.Y ?? 0;
        var light = new Point(screenX + width / 2 - origin.X, screenY - origin.Y);
        return new(density, matrix, inverse, light, 500 * density * (Math.Min(width, height) / (450 * density) + 2) / 3, 800 * density, owner.Opacity);
    }
    internal static Rect Bounds(Control owner, Rect rect, double elevation)
    {
        if (Light(owner) is not { } light) return rect;
        var z = elevation * light.Density;
        var device = rect.TransformToAABB(light.ToDevice);
        var ambient = device.Inflate(Math.Min(z / 2, 150));
        var ratio = Math.Clamp(z / (light.Height - z), 0, .95);
        var scale = Math.Clamp(light.Height / (light.Height - z), 1, 1.95);
        var spot = new Rect(device.TopLeft * scale - new Vector(light.Light.X * ratio, light.Light.Y * ratio), device.Size * scale)
            .Inflate(light.Radius * ratio);
        return ambient.Union(spot).TransformToAABB(light.FromDevice);
    }
    internal static void Draw(DrawingContext context, Control owner, RoundedRect shape, double elevation)
    {
        if (elevation <= 0 || shape.Rect.Width <= 0 || shape.Rect.Height <= 0) return;
        if (Light(owner) is not { } light) return;
        context.Custom(new ShadowDraw(shape, elevation, light, Bounds(owner, shape.Rect, elevation)));
    }
    private sealed class ShadowDraw(RoundedRect shape, double elevation, Lighting light, Rect bounds) : ICustomDrawOperation
    {
        public Rect Bounds => bounds;
        public bool HitTest(Point point) => false;
        public bool Equals(ICustomDrawOperation? other) => ReferenceEquals(this, other);
        public void Dispose() { }
        public void Render(ImmediateDrawingContext context)
        {
            if (context.TryGetFeature<ISkiaSharpApiLeaseFeature>() is { } feature)
            {
                using var lease = feature.Lease();
                Draw(lease.SkCanvas, lease.CurrentOpacity); return;
            }
            var density = light.Density;
            var size = new PixelSize(Math.Max(1, (int)Math.Ceiling(bounds.Width * density)), Math.Max(1, (int)Math.Ceiling(bounds.Height * density)));
            using var bitmap = new WriteableBitmap(size, new Vector(96 * density, 96 * density), PixelFormat.Bgra8888, AlphaFormat.Premul);
            using (var storage = bitmap.Lock())
            {
                using var surface = SKSurface.Create(new SKImageInfo(size.Width, size.Height, SKColorType.Bgra8888, SKAlphaType.Premul), storage.Address, storage.RowBytes);
                surface.Canvas.Clear(SKColors.Transparent); surface.Canvas.Scale((float)density);
                surface.Canvas.Translate((float)-bounds.X, (float)-bounds.Y); Draw(surface.Canvas, 1);
            }
            context.DrawBitmap(bitmap, new Rect(0, 0, size.Width, size.Height), bounds);
        }
        private void Draw(SKCanvas canvas, double opacity)
        {
            var matrix = light.ToDevice;
            var axisX = matrix.M11 * matrix.M11 + matrix.M12 * matrix.M12;
            var axisY = matrix.M21 * matrix.M21 + matrix.M22 * matrix.M22;
            var aligned = Math.Abs(matrix.M12) < .00001 && Math.Abs(matrix.M21) < .00001;
            var uniform = shape.RadiiTopLeft == shape.RadiiTopRight && shape.RadiiTopLeft == shape.RadiiBottomLeft && shape.RadiiTopLeft == shape.RadiiBottomRight;
            var analytic = aligned && Math.Abs(axisX - axisY) < .00001 && uniform && shape.RadiiTopLeft.X == shape.RadiiTopLeft.Y;
            var rect = shape.Rect.TransformToAABB(light.ToDevice);
            var scale = Math.Sqrt(light.ToDevice.M11 * light.ToDevice.M11 + light.ToDevice.M12 * light.ToDevice.M12);
            var radius = shape.RadiiTopLeft.X * scale;
            var z = elevation * light.Density;
            var ambient = Math.Min(z / 2, 150);
            var ratio = Math.Clamp(z / (light.Height - z), 0, .95);
            var spotScale = Math.Clamp(light.Height / (light.Height - z), 1, 1.95);
            var blur = light.Radius * ratio;
            var projected = new Rect(rect.TopLeft * spotScale - new Vector(light.Light.X * ratio, light.Light.Y * ratio), rect.Size * spotScale);
            var dr = radius * (spotScale - 1);
            var upper = projected.TopLeft - rect.TopLeft + new Vector(dr, dr);
            var lower = projected.BottomRight - rect.BottomRight - new Vector(dr, dr);
            var offset = radius <= 0 ? Math.Max(Math.Max(Math.Abs(upper.X), Math.Abs(upper.Y)), Math.Max(Math.Abs(lower.X), Math.Abs(lower.Y)))
                : Math.Sqrt(Math.Max(upper.X * upper.X + upper.Y * upper.Y, lower.X * lower.X + lower.Y * lower.Y)) + dr;
            var saved = canvas.Save();
            try
            {
                var inverse = light.FromDevice;
                canvas.Concat(new SKMatrix((float)inverse.M11, (float)inverse.M21, (float)inverse.M31,
                    (float)inverse.M12, (float)inverse.M22, (float)inverse.M32, 0, 0, 1));
                if (!analytic) { MaterialConvexShadow.Draw(canvas, shape, light, elevation, opacity); return; }
                var inheritedOpacity = light.CasterAlpha > 0 ? opacity / light.CasterAlpha : 0;
                MaterialShadowMesh.Draw(canvas, rect.Inflate(ambient), radius + ambient, ambient * (1 + z / 128), ambient, 9, light.CasterAlpha, inheritedOpacity);
                MaterialShadowMesh.Draw(canvas, projected.Inflate(blur), radius * spotScale + blur, 2 * blur, blur + Math.Max(blur, offset), 48, light.CasterAlpha, inheritedOpacity);
            }
            finally { canvas.RestoreToCount(saved); }
        }
    }
}
