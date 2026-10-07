using Avalonia.Controls;

namespace Avalonia.Material3.Controls;

internal sealed class MaterialDatePickerPanel : Panel
{
    private readonly Control _header, _divider, _body, _error;
    internal MaterialDatePickerPanel(Control header, Control divider, Control body, Control error)
    { _header=header; _divider=divider; _body=body; _error=error; Children.Add(header); Children.Add(divider); Children.Add(body); Children.Add(error); }
    protected override Size MeasureOverride(Size availableSize)
    {
        _header.Measure(new Size(availableSize.Width, double.PositiveInfinity));
        _divider.Measure(availableSize); _error.Measure(new Size(availableSize.Width, double.PositiveInfinity));
        _body.Measure(new Size(availableSize.Width, Math.Max(0, availableSize.Height - _header.DesiredSize.Height - _error.DesiredSize.Height)));
        return new(Math.Max(_header.DesiredSize.Width, Math.Max(_body.DesiredSize.Width, _error.DesiredSize.Width)),
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
