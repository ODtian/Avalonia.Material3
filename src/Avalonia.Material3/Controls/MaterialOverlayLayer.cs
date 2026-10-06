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
        var bounds = options.Anchor?.TransformToVisual(this) is { } transform ? new Rect(options.Anchor.Bounds.Size).TransformToAABB(transform) : (Rect?)null;
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
                    if (y + h + options.Offset.Y > finalSize.Height - m.Bottom) y = anchor.Top - h;
                }
                break;
        }
        x = Math.Clamp(x + options.Offset.X, m.Left, m.Left + width - w);
        y = Math.Clamp(y + options.Offset.Y, m.Top, m.Top + height - h);
        Container.Arrange(new Rect(x, y, w, h));
        return finalSize;
    }
}
