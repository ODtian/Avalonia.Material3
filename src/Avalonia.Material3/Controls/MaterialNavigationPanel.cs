using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Avalonia.Material3.Controls;

/// <summary>The default non-virtualizing header layout. Items retain identity when presentation changes.</summary>
public class MaterialNavigationPanel : Panel
{
    private MaterialNavigation? _owner;
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _owner = this.GetVisualAncestors().OfType<MaterialNavigation>().FirstOrDefault();
        if (_owner is not null) _owner.PropertyChanged += OwnerChanged;
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_owner is not null) _owner.PropertyChanged -= OwnerChanged;
        _owner = null;
        base.OnDetachedFromVisualTree(e);
    }
    private void OwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs e) => InvalidateMeasure();
    protected override Size MeasureOverride(Size availableSize)
    {
        var vertical = _owner is MaterialNavigationRail;
        var width = double.IsFinite(availableSize.Width) ? availableSize.Width : 0;
        var scrollable = _owner is MaterialTabs { Layout: MaterialTabLayout.Scrollable };
        // Long labels grow vertically instead of creating a single tab wider than its viewport.
        var cell = scrollable ? Math.Max(90, _owner!.Bounds.Width > 0 ? _owner.Bounds.Width : 320)
            : !vertical && Children.Count > 0 && width > 0 ? Math.Max(48, width / Children.Count) : double.PositiveInfinity;
        double desiredWidth = 0, desiredHeight = 0;
        foreach (var child in Children)
        {
            child.Measure(new Size(vertical ? availableSize.Width : cell, double.PositiveInfinity));
            if (vertical) { desiredWidth = Math.Max(desiredWidth, child.DesiredSize.Width); desiredHeight += child.DesiredSize.Height; }
            else { desiredWidth += Math.Max(child.DesiredSize.Width, double.IsFinite(cell) ? cell : 0); desiredHeight = Math.Max(desiredHeight, child.DesiredSize.Height); }
        }
        if (vertical && Children.Count > 0) desiredHeight += (Children.Count - 1) * 4;
        return new Size(desiredWidth, desiredHeight);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var vertical = _owner is MaterialNavigationRail;
        var scrollable = _owner is MaterialTabs { Layout: MaterialTabLayout.Scrollable };
        var cell = Children.Count > 0 ? Math.Max(48, finalSize.Width / Children.Count) : 0;
        double offset = 0;
        foreach (var child in Children)
        {
            var itemWidth = scrollable ? child.DesiredSize.Width : cell;
            child.Arrange(vertical ? new Rect(0, offset, finalSize.Width, child.DesiredSize.Height) : new Rect(offset, 0, itemWidth, finalSize.Height));
            offset += vertical ? child.DesiredSize.Height + 4 : itemWidth;
        }
        return finalSize;
    }
}
