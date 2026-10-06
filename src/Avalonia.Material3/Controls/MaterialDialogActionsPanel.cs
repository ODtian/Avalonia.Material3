using Avalonia.Controls;

namespace Avalonia.Material3.Controls;

// Confirm follows cancel horizontally, precedes it when stacked, following pinned AlertDialogFlowRow.
internal sealed class MaterialDialogActionsPanel : Panel
{
    private bool _stack;
    protected override Size MeasureOverride(Size availableSize)
    {
        var visible = Children.Where(child => child.IsVisible).ToArray();
        foreach (var child in visible) child.Measure(availableSize);
        var width = visible.Sum(child => child.DesiredSize.Width) + Math.Max(0, visible.Length - 1) * 8;
        _stack = width > availableSize.Width;
        return _stack ? new Size(visible.Max(child => child.DesiredSize.Width), visible.Sum(child => child.DesiredSize.Height) + Math.Max(0, visible.Length - 1) * 8)
            : new Size(width, visible.Select(child => child.DesiredSize.Height).DefaultIfEmpty().Max());
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var visible = Children.Where(child => child.IsVisible).ToArray();
        if (_stack)
        {
            var y = 0d;
            foreach (var child in visible.Reverse())
            {
                var w = Math.Min(finalSize.Width, child.DesiredSize.Width);
                child.Arrange(new Rect(finalSize.Width - w, y, w, child.DesiredSize.Height));
                y += child.DesiredSize.Height + 8;
            }
        }
        else
        {
            var width = visible.Sum(child => child.DesiredSize.Width) + Math.Max(0, visible.Length - 1) * 8;
            // Arrange logical cancel→confirm at the logical end; Avalonia owns physical RTL mirroring.
            var x = finalSize.Width - width;
            foreach (var child in visible)
            {
                child.Arrange(new Rect(x, 0, child.DesiredSize.Width, finalSize.Height));
                x += child.DesiredSize.Width + 8;
            }
        }
        return finalSize;
    }
}
