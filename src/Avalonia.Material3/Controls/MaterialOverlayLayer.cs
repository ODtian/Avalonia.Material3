using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.Material3.Controls;

// A layout implementation, not an extension contract. The public seam is host/options/session.
internal sealed class MaterialOverlayLayer : Panel
{
    private readonly MaterialOverlayHost host;
    private readonly MaterialOverlayOptions options;
    private readonly Border scrim;
    internal MaterialOverlayMotion? Presentation { get; }
    internal Border Scrim => scrim;
    internal MaterialOverlayOptions Options => options;
    internal MaterialOverlayLayer(MaterialOverlayHost host, MaterialOverlayOptions options, Border scrim, Border container)
    {
        this.host = host; this.options = options; this.scrim = scrim; Container = container;
        if (container.Child is MaterialMenu or MaterialTooltip or MaterialSnackbar or MaterialDialog or MaterialNavigationDrawer) Presentation = new(this);
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new MaterialOverlayScopeAutomationPeer(this);
    private Rect? _anchorBounds;
    public Border Container { get; }
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
        if (Presentation?.ExitBounds is { } frozen)
        {
            Container.Arrange(frozen); Presentation.UpdateGeometry(); return finalSize;
        }
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
        Presentation?.UpdateGeometry();
        if (Container.Child is MaterialMenu && _anchorBounds is { } pivotAnchor && Presentation is not null)
        {
            static double Pivot(double near, double far, double anchorNear, double anchorFar) =>
                near >= anchorFar ? 0 : far <= anchorNear ? 1 : far == near ? 0 :
                ((Math.Max(near, anchorNear) + Math.Min(far, anchorFar)) / 2 - near) / (far - near);
            Container.RenderTransformOrigin = new RelativePoint(Pivot(x, x + w, pivotAnchor.Left, pivotAnchor.Right),
                Pivot(y, y + h, pivotAnchor.Top, pivotAnchor.Bottom), RelativeUnit.Relative);
        }
        return finalSize;
    }
}
