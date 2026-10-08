using Avalonia.Controls;

namespace Avalonia.Material3.Controls;

/// <summary>Stock icon/label layout: padding, gap and wrapped cross extent follow the same reveal.</summary>
internal sealed class MaterialExtendedFabPanel : Panel
{
    public MaterialExtendedFabPanel() => UseLayoutRounding = false;
    private Thickness _expandedPadding;
    private double _gap;
    private double Fraction => Children.Count == 2 && Children[1] is MaterialActionReveal reveal ? reveal.RevealFraction : 1;
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count != 2 || TemplatedParent is not MaterialExtendedFab owner) return default;
        var density = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        _expandedPadding = MaterialPhysicalLayout.Insets(owner.ExpandedContentPadding, density);
        _gap = Children[0].IsVisible ? MaterialPhysicalLayout.Round(owner.IconSpacing, density) : 0;
        Children[0].Measure(availableSize.Deflate(_expandedPadding));
        var icon = Children[0].DesiredSize;
        // Reserve the full recipe while measuring the label, not an ever-changing fraction of its
        // padding. Wrapping must not chase the collapsing container's extent.
        Children[1].Measure(new Size(Math.Max(0, availableSize.Width - _expandedPadding.Left - _expandedPadding.Right - icon.Width - _gap),
            Math.Max(0, availableSize.Height - _expandedPadding.Top - _expandedPadding.Bottom)));
        var label = Children[1].DesiredSize;
        var fraction = Fraction;
        return new Size(icon.Width + label.Width + (_gap + _expandedPadding.Left + _expandedPadding.Right) * fraction,
            Math.Max(icon.Height, label.Height * fraction) + (_expandedPadding.Top + _expandedPadding.Bottom) * fraction);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count != 2) return finalSize;
        var fraction = Fraction;
        var padding = new Thickness(_expandedPadding.Left * fraction, _expandedPadding.Top * fraction,
            _expandedPadding.Right * fraction, _expandedPadding.Bottom * fraction);
        var icon = Children[0].DesiredSize;
        var height = Math.Max(0, finalSize.Height - padding.Top - padding.Bottom);
        Children[0].Arrange(new Rect(padding.Left, padding.Top + (height - icon.Height) / 2, icon.Width, icon.Height));
        var x = padding.Left + icon.Width + _gap * fraction;
        Children[1].Arrange(new Rect(x, padding.Top, Math.Max(0, finalSize.Width - x - padding.Right), height));
        return finalSize;
    }
}
