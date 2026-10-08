using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Avalonia.Material3.Controls;

// Capture regions and destinations are source-local DIP. The owned immutable raster is
// outward pixel-aligned at capture density; Draw crops the exact requested physical subregion
// so alignment padding never shifts or compresses the destination's original DIP geometry.
// Consumers keep their own motion/overflow policy and dispose at settlement or detachment.
internal sealed class MaterialSnapshot : IDisposable
{
    private RenderTargetBitmap? _bitmap;
    private readonly Rect _source;
    internal Rect Bounds { get; }
    private MaterialSnapshot(RenderTargetBitmap bitmap, Rect bounds, Rect source) { _bitmap = bitmap; Bounds = bounds; _source = source; }

    internal static MaterialSnapshot? Capture(Control visual, Rect bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return null;
        var density = TopLevel.GetTopLevel(visual)?.RenderScaling ?? 1;
        var requested = bounds;
        var left = Math.Floor(bounds.Left * density); var top = Math.Floor(bounds.Top * density);
        var right = Math.Ceiling(bounds.Right * density); var bottom = Math.Ceiling(bounds.Bottom * density);
        bounds = new Rect(left / density, top / density, (right - left) / density, (bottom - top) / density);
        var bitmap = new RenderTargetBitmap(new PixelSize((int)(right - left), (int)(bottom - top)), new Vector(96 * density, 96 * density));
        var crop = new Border { Width = bounds.Width, Height = bounds.Height, UseLayoutRounding = false,
            Background = new VisualBrush(visual) { SourceRect = new RelativeRect(bounds, RelativeUnit.Absolute),
                DestinationRect = RelativeRect.Fill, Stretch = Stretch.Fill } };
        crop.Measure(bounds.Size); crop.Arrange(new Rect(bounds.Size));
        try { bitmap.Render(crop); }
        catch { bitmap.Dispose(); throw; }
        var source = new Rect((requested.Left - bounds.Left) * density, (requested.Top - bounds.Top) * density,
            requested.Width * density, requested.Height * density);
        return new(bitmap, requested, source);
    }

    // Reusing a captured frame at a new monitor density keeps its logical destination
    // and frozen content; the renderer resamples the complete original physical raster.
    internal void Draw(DrawingContext context, Rect destination)
    {
        if (_bitmap is { } bitmap && destination.Width > 0 && destination.Height > 0)
            context.DrawImage(bitmap, _source, destination);
    }
    internal IDisposable OpacityMask(DrawingContext context, Rect destination)
    {
        if (_bitmap is not { } bitmap) throw new ObjectDisposedException(nameof(MaterialSnapshot));
        var source = new Rect(_source.X / bitmap.PixelSize.Width, _source.Y / bitmap.PixelSize.Height,
            _source.Width / bitmap.PixelSize.Width, _source.Height / bitmap.PixelSize.Height);
        var brush = new ImageBrush(bitmap) { SourceRect = new RelativeRect(source, RelativeUnit.Relative), Stretch = Stretch.Fill };
        return context.PushOpacityMask(brush, destination);
    }
    public void Dispose() { _bitmap?.Dispose(); _bitmap = null; }
}
