using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Material3.Tokens;

namespace Avalonia.Material3.Controls;

// Single next/previous uses the pinned LazyList animateScrollToItem spring.
// Range and externally assigned months keep scrollToItem's direct update.
internal sealed class MaterialCalendarMonthPanel : Decorator
{
    private readonly MaterialMotionValue _offset;
    private readonly MaterialMotionSettings _motion;
    private readonly TranslateTransform _transform = new();
    private MaterialSnapshot? _previous;
    private Size _capturedSize;
    private double _width;
    private int _direction;
    internal MaterialCalendarMonthPanel(Control content)
    {
        Child = content; ClipToBounds = true;
        content.RenderTransform = _transform;
        _offset = new(this, 0, value => { _transform.X = value; if (value == 0) { _previous?.Dispose(); _previous = null; } InvalidateVisual(); });
        _motion = new(this, () => { if (_motion is not null && _motion.FastEffects.IsInstant) _offset.Snap(0); });
        DetachedFromVisualTree += (_, _) => { _previous?.Dispose(); _previous = null; };
    }
    internal void Capture(int direction)
    {
        var size = Bounds.Size;
        var captured = size.Width > 0 && size.Height > 0 && !_motion.FastEffects.IsInstant && Child is not null
            ? MaterialSnapshot.Capture(this, new Rect(size)) : null;
        // Capture the displayed composition before releasing its previous retirement: a
        // reversed scroll can contain both the incoming live month and its outgoing raster.
        _previous?.Dispose(); _previous = captured; _capturedSize = size; _width = size.Width; _direction = direction;
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
            _previous.Draw(context, new Rect(new Point(_offset.Value - _direction * _width, 0), _capturedSize));
    }
}
