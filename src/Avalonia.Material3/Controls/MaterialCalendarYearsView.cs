using Avalonia.Controls;

namespace Avalonia.Material3.Controls;

internal sealed class MaterialCalendarYearsView : Panel
{
    internal double RowStride { get; private set; } = 64;
    protected override Size MeasureOverride(Size availableSize)
    {
        var width = 72d; var height = 48d;
        foreach (var child in Children)
        { child.Measure(Size.Infinity); width = Math.Max(width, child.DesiredSize.Width); height = Math.Max(height, child.DesiredSize.Height); }
        RowStride = height + 16;
        var rows = (Children.Count + 2) / 3;
        return new(width * 3, Math.Max(0, rows * RowStride - 16));
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var width = Children.Count == 0 ? 72 : Math.Max(72, Children.Max(c=>c.DesiredSize.Width));
        var gap = Math.Max(0,(finalSize.Width-width*3)/4);
        for(var i=0;i<Children.Count;i++) Children[i].Arrange(new Rect(gap+i%3*(width+gap),i/3*RowStride,width,RowStride-16));
        return finalSize;
    }
}

internal sealed class MaterialYearMenuContent : Panel
{
    private readonly Control _caption, _arrow;
    internal MaterialYearMenuContent(Control caption, Control arrow)
    { _caption=caption; _arrow=arrow; Children.Add(caption); Children.Add(arrow); }
    protected override Size MeasureOverride(Size availableSize)
    {
        _arrow.Measure(Size.Infinity);
        _caption.Measure(new Size(Math.Max(0,availableSize.Width-_arrow.DesiredSize.Width-8),availableSize.Height));
        return new(_caption.DesiredSize.Width+8+_arrow.DesiredSize.Width,Math.Max(_caption.DesiredSize.Height,_arrow.DesiredSize.Height));
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var width=Math.Max(0,finalSize.Width-_arrow.DesiredSize.Width-8);
        _caption.Arrange(new Rect(0,0,width,finalSize.Height));
        _arrow.Arrange(new Rect(width+8,(finalSize.Height-_arrow.DesiredSize.Height)/2,_arrow.DesiredSize.Width,_arrow.DesiredSize.Height));
        return finalSize;
    }
}
