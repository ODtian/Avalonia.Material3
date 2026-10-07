using Avalonia.Controls;

namespace Avalonia.Material3.Controls;

// Year selection overlays the month, with DefaultEffects size/enter and FastEffects exit.
internal sealed class MaterialCalendarYearPanel : Panel
{
    private readonly Control _month, _years;
    private readonly MaterialMotionValue _extent, _alpha;
    private readonly MaterialMotionSettings _motion;
    private bool _showYears, _initialized;
    private Size _yearSize;
    internal MaterialCalendarYearPanel(Control month, Control years)
    {
        _month = month; _years = years; Children.Add(month); Children.Add(years);
        ClipToBounds = true;
        _extent = new(this, 0, _ => InvalidateArrange());
        _alpha = new(this, .6, value => years.Opacity = Math.Clamp(value, 0, 1));
        _motion = new(this, () => Update(_showYears));
    }
    internal void Update(bool show)
    {
        var appeared = show && !_showYears; _showYears = show;
        if (_motion is null || !_initialized) return;
        _years.IsEnabled = show; _month.IsEnabled = !show;
        if (_motion.FastEffects.IsInstant)
        { _extent.Snap(show ? 1 : 0); _alpha.Snap(show ? 1 : 0); return; }
        if (appeared && _extent.Value <= 0) _alpha.Snap(.6);
        _extent.Spring(show ? 1 : 0, _motion.DefaultEffects, 1 / Math.Max(1, _yearSize.Height));
        _alpha.Spring(show ? 1 : 0, show ? _motion.DefaultEffects : _motion.FastEffects);
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        _month.Measure(availableSize); _years.IsVisible = true; _years.Measure(availableSize); _yearSize = _years.DesiredSize;
        if (!_initialized) { _initialized = true; Update(_showYears); }
        return new(Math.Max(_month.DesiredSize.Width, _yearSize.Width), Math.Max(_month.DesiredSize.Height, _yearSize.Height));
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        _month.Arrange(new Rect(finalSize)); _years.Arrange(new Rect(0, 0, finalSize.Width, _yearSize.Height));
        _years.Clip = new Avalonia.Media.RectangleGeometry(new Rect(0, _yearSize.Height * (1 - Math.Clamp(_extent.Value, 0, 1)), finalSize.Width,
            _yearSize.Height * Math.Clamp(_extent.Value, 0, 1)));
        _years.IsVisible = _showYears || _extent.Value > 0;
        return finalSize;
    }
}
