using Avalonia.Controls;

namespace Avalonia.Material3.Controls;

internal sealed class MaterialDatePickerHeader : Panel
{
    private readonly Control _title, _headline, _mode;
    private readonly MaterialDateRangeHeadline _rangeHeadline = new();
    private Control Headline => Range ? _rangeHeadline : _headline;
    internal bool Range { get; set; }
    private double Start => Range ? 64 : 24;
    private double TitleTop => Range ? 0 : 16;
    private double RowHeight => Math.Max(Headline.DesiredSize.Height + 12, _mode.DesiredSize.Height + 12);
    internal MaterialDatePickerHeader(Control title, Control headline, Control mode)
    { _title = title; _headline = headline; _mode = mode; Children.Add(title); Children.Add(headline); Children.Add(_rangeHeadline); Children.Add(mode); }
    internal void SetRangeText(string start, string end) => _rangeHeadline.SetText(start, end);
    protected override Size MeasureOverride(Size availableSize)
    {
        _mode.Measure(Size.Infinity);
        _headline.IsVisible = !Range; _rangeHeadline.IsVisible = Range;
        _title.Measure(new Size(Math.Max(0, availableSize.Width - Start - 12), double.PositiveInfinity));
        Headline.Measure(new Size(Math.Max(0, availableSize.Width - Start - 24 - _mode.DesiredSize.Width), double.PositiveInfinity));
        return new(Math.Max(360, Start + 24 + _mode.DesiredSize.Width + Headline.DesiredSize.Width),
            Math.Max(Range ? 68 : 120, TitleTop + _title.DesiredSize.Height + RowHeight + 1));
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        _title.Arrange(new Rect(Start, TitleTop, Math.Max(0, finalSize.Width - Start - 12), _title.DesiredSize.Height));
        var row = finalSize.Height - RowHeight - 1;
        Headline.Arrange(new Rect(Start, row + (RowHeight - Headline.DesiredSize.Height - 12) / 2,
            Math.Max(0, finalSize.Width - Start - 24 - _mode.DesiredSize.Width), Headline.DesiredSize.Height));
        _mode.Arrange(new Rect(finalSize.Width - 12 - _mode.DesiredSize.Width, row + (RowHeight - _mode.DesiredSize.Height - 12) / 2,
            _mode.DesiredSize.Width, _mode.DesiredSize.Height));
        return finalSize;
    }
}

internal sealed class MaterialDateRangeHeadline : Panel
{
    private readonly TextBlock _start = MaterialPickerSupport.Text("TitleLarge", "OnSurfaceVariant");
    private readonly TextBlock _end = MaterialPickerSupport.Text("TitleLarge", "OnSurfaceVariant");
    internal MaterialDateRangeHeadline()
    {
        var delimiter = MaterialPickerSupport.Text("TitleLarge", "OnSurfaceVariant"); delimiter.Text = "-";
        Children.Add(_start); Children.Add(delimiter); Children.Add(_end);
    }
    internal void SetText(string start, string end) { _start.Text = start; _end.Text = end; }
    protected override Size MeasureOverride(Size availableSize)
    {
        var width = 0d; var height = 0d;
        foreach (var child in Children)
        {
            child.Measure(new Size(Math.Max(0, availableSize.Width - width), availableSize.Height));
            width += child.DesiredSize.Width + 4; height = Math.Max(height, child.DesiredSize.Height);
        }
        return new(width - 4, height);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var x = 0d;
        foreach (var child in Children)
        {
            child.Arrange(new Rect(x, (finalSize.Height - child.DesiredSize.Height) / 2, child.DesiredSize.Width, child.DesiredSize.Height));
            x += child.DesiredSize.Width + 4;
        }
        return finalSize;
    }
}
