using Avalonia.Controls;
using Avalonia.Layout;

namespace Avalonia.Material3.Controls;

internal sealed class MaterialDatePickerPanel : Panel
{
    private readonly MaterialDatePicker _owner;
    private readonly Control _header, _divider, _body, _error;
    internal MaterialDatePickerPanel(MaterialDatePicker owner, Control header, Control divider, Control body, Control error)
    { _owner=owner; _header=header; _divider=divider; _body=body; _error=error; Children.Add(header); Children.Add(divider); Children.Add(body); Children.Add(error); }
    protected override Size MeasureOverride(Size availableSize)
    {
        // Native DateEntryContainer gives the headline a weighted slot inside the calendar width.
        // Headline text must reflow inside that slot rather than choose a new container width.
        _body.Measure(availableSize);
        var naturalWidth = Math.Max(360, _body.DesiredSize.Width);
        var width = double.IsFinite(availableSize.Width) &&
            (_owner.HorizontalAlignment == HorizontalAlignment.Stretch || double.IsFinite(_owner.Width))
            ? availableSize.Width : Math.Min(availableSize.Width, naturalWidth);
        _header.Measure(new Size(width, double.PositiveInfinity));
        _divider.Measure(new Size(width, availableSize.Height));
        _error.Measure(new Size(width, double.PositiveInfinity));
        _body.Measure(new Size(width, Math.Max(0, availableSize.Height - _header.DesiredSize.Height - _error.DesiredSize.Height)));
        return new(width,
            _header.DesiredSize.Height + _body.DesiredSize.Height + _error.DesiredSize.Height);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var headerHeight = _header.DesiredSize.Height;
        var bodyHeight = Math.Min(_body.DesiredSize.Height, Math.Max(0, finalSize.Height - headerHeight - _error.DesiredSize.Height));
        _header.Arrange(new Rect(0, 0, finalSize.Width, headerHeight));
        _divider.Arrange(new Rect(0, headerHeight - 1, finalSize.Width, 1));
        _body.Arrange(new Rect(0, headerHeight, finalSize.Width, bodyHeight));
        _error.Arrange(new Rect(0, headerHeight + bodyHeight, finalSize.Width, _error.DesiredSize.Height));
        return finalSize;
    }
}
