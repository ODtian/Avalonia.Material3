using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Avalonia.Material3.Controls;

// BadgedBox's anchor-only measurement and parent end/top rulers projected into native templates.
// Children: fixed icon frame, then decorative content. Badge size never participates in label layout.
internal sealed class MaterialNavigationIconPanel : Panel
{
    private MaterialNavigationItem? _owner;
    public MaterialNavigationIconPanel() => UseLayoutRounding = false;
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _owner = this.GetVisualAncestors().OfType<MaterialNavigationItem>().FirstOrDefault();
        if (_owner is not null) _owner.PropertyChanged += OwnerChanged;
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_owner is not null) _owner.PropertyChanged -= OwnerChanged;
        _owner = null; base.OnDetachedFromVisualTree(e);
    }
    private void OwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    { if (e.Property == BoundsProperty) InvalidateArrange(); }
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count == 0) return default;
        Children[0].Measure(availableSize);
        if (Children.Count > 1) Children[1].Measure(Size.Infinity);
        return Children[0].DesiredSize;
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count == 0) return finalSize;
        Children[0].Arrange(new Rect(finalSize));
        if (Children.Count < 2 || !Children[1].IsVisible) return finalSize;
        var allowed = _owner is { Bounds.Width: > 0, Bounds.Height: > 0 } owner && owner.TransformToVisual(this) is { } transform
            ? new Rect(owner.Bounds.Size).TransformToAABB(transform) : new Rect(finalSize);
        var badge = Children[1];
        var size = badge.DesiredSize;
        var count = size.Width > 6;
        var x = finalSize.Width - (count ? 12 : 6);
        var y = (count ? 14 : 6) - size.Height;
        if (!Children[0].IsVisible) { x = allowed.Right - size.Width - 4; y = allowed.Top + 4; }
        x = Math.Max(allowed.Left, Math.Min(x, allowed.Right - size.Width));
        y = Math.Max(y, allowed.Top);
        badge.Arrange(new Rect(x, y, size.Width, size.Height));
        return finalSize;
    }
}
