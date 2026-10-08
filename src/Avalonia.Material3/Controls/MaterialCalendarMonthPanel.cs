using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Material3.Tokens;

namespace Avalonia.Material3.Controls;

// Single next/previous uses the pinned LazyList animateScrollToItem spring.
// Range and externally assigned months keep scrollToItem's direct update.
internal sealed class MaterialCalendarMonthPanel : Decorator
{
    private readonly MaterialMotionValue _offset;
    private readonly MaterialMotionSettings _motion;
    private readonly TranslateTransform _transform = new();
    private RenderTargetBitmap? _previous;
    private double _width;
    private int _direction;
    internal MaterialCalendarMonthPanel(Control content)
    {
        Child = content; ClipToBounds = true;
        content.RenderTransform = _transform;
        _offset = new(this, 0, value => { _transform.X = value; InvalidateVisual(); });
        _motion = new(this, () => { if (_motion is not null && _motion.FastEffects.IsInstant) _offset.Snap(0); });
        DetachedFromVisualTree += (_, _) => { _previous?.Dispose(); _previous = null; };
    }
    internal void Capture(int direction)
    {
        _previous?.Dispose(); _previous = null; _width = Bounds.Width; _direction = direction;
        if (_width <= 0 || Bounds.Height <= 0 || _motion.FastEffects.IsInstant || Child is null) return;
        var density = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        _previous = new(new PixelSize((int)Math.Ceiling(_width * density), (int)Math.Ceiling(Bounds.Height * density)), new Vector(96 * density, 96 * density));
        _previous.Render(Child);
    }
    internal void Scroll()
    {
        if (_previous is null) { _offset.Snap(0); return; }
        _offset.Snap(_direction * _width);
        _offset.Spring(0, new MaterialSpring(1, 200) { IsInstant = _motion.FastEffects.IsInstant }, 1);
    }
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (_previous is not null && Math.Abs(_offset.Value) > 0)
            context.DrawImage(_previous, new Rect(_previous.PixelSize.ToSize(1)), new Rect(_offset.Value - _direction * _width, 0, _width, Bounds.Height));
    }
}
