using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.Material3.Controls;

// A layout implementation, not an extension contract. The public seam is host/options/session.
internal sealed class MaterialOverlayLayer(MaterialOverlayHost host, MaterialOverlayOptions options, Border scrim, Border container) : Panel
{
    protected override AutomationPeer OnCreateAutomationPeer() => new MaterialOverlayScopeAutomationPeer(this);
    private Rect? _anchorBounds;
    public Border Container { get; } = container;
    public void UpdateAnchor()
    {
        if (Container.FlowDirection != host.FlowDirection) { Container.FlowDirection = host.FlowDirection; InvalidateArrange(); }
        var bounds = options.Anchor?.TransformToVisual(this) is { } transform ?
            (options.AnchorPoint is { } point ? new Rect(point, new Size()).TransformToAABB(transform) :
                new Rect(options.Anchor.Bounds.Size).TransformToAABB(transform)) : (Rect?)null;
        if (bounds != _anchorBounds) { _anchorBounds = bounds; InvalidateArrange(); }
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        scrim.Measure(availableSize);
        var margin = options.Margin;
        Container.Measure(new Size(Math.Max(0, availableSize.Width - margin.Left - margin.Right), Math.Max(0, availableSize.Height - margin.Top - margin.Bottom)));
        return availableSize;
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        scrim.Arrange(new Rect(finalSize));
        var m = options.Margin;
        var width = Math.Max(0, finalSize.Width - m.Left - m.Right);
        var height = Math.Max(0, finalSize.Height - m.Top - m.Bottom);
        var w = Math.Min(Container.DesiredSize.Width, width);
        var h = Math.Min(Container.DesiredSize.Height, height);
        var x = m.Left + (width - w) / 2;
        var y = m.Top + (height - h) / 2;
        var offsetX = options.Offset.X;
        var offsetY = options.Offset.Y;
        var rtl = host.FlowDirection == FlowDirection.RightToLeft;
        switch (options.Placement)
        {
            case MaterialOverlayPlacement.FullScreen: x = m.Left; y = m.Top; w = width; h = height; break;
            case MaterialOverlayPlacement.Bottom: y = m.Top + height - h; break;
            case MaterialOverlayPlacement.Start: x = rtl ? m.Left + width - w : m.Left; h = height; y = m.Top; break;
            case MaterialOverlayPlacement.End: x = rtl ? m.Left : m.Left + width - w; h = height; y = m.Top; break;
            case MaterialOverlayPlacement.Anchor:
                if (_anchorBounds is { } anchor)
                {
                    x = rtl ? anchor.Right - w : anchor.Left;
                    y = anchor.Bottom;
                    var position = options.AnchorPosition;
                    if (position == MaterialOverlayAnchorPosition.Start) position = rtl ? MaterialOverlayAnchorPosition.Right : MaterialOverlayAnchorPosition.Left;
                    if (position == MaterialOverlayAnchorPosition.End) position = rtl ? MaterialOverlayAnchorPosition.Left : MaterialOverlayAnchorPosition.Right;
                    if (position == MaterialOverlayAnchorPosition.Above)
                    {
                        y = anchor.Top - h;
                        if (y + offsetY < m.Top) { y = anchor.Bottom; offsetY = Math.Abs(offsetY); }
                    }
                    else if (position is MaterialOverlayAnchorPosition.Left or MaterialOverlayAnchorPosition.Right)
                    {
                        y = anchor.Top;
                        x = position == MaterialOverlayAnchorPosition.Left ? anchor.Left - w : anchor.Right;
                        if (x + offsetX < m.Left) { x = anchor.Right; offsetX = Math.Abs(offsetX); }
                        if (x + w + offsetX > finalSize.Width - m.Right) { x = anchor.Left - w; offsetX = -Math.Abs(offsetX); }
                    }
                    else if (y + h + offsetY > finalSize.Height - m.Bottom) { y = anchor.Top - h; offsetY = -Math.Abs(offsetY); }
                    if (position is MaterialOverlayAnchorPosition.Above or MaterialOverlayAnchorPosition.Below &&
                        x + w + options.Offset.X > finalSize.Width - m.Right) x = anchor.Right - w;
                }
                break;
        }
        x = Math.Clamp(x + offsetX, m.Left, m.Left + width - w);
        y = Math.Clamp(y + offsetY, m.Top, m.Top + height - h);
        Container.Arrange(new Rect(x, y, w, h));
        return finalSize;
    }
}
