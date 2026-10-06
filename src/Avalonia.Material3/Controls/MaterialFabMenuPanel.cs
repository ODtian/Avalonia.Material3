using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.Material3.Controls;

internal sealed class MaterialFabMenuPanel : Panel
{
    public static readonly StyledProperty<MaterialActionAnchor> AnchorProperty = AvaloniaProperty.Register<MaterialFabMenuPanel, MaterialActionAnchor>(nameof(Anchor));
    public MaterialActionAnchor Anchor { get => GetValue(AnchorProperty); set => SetValue(AnchorProperty, value); }
    private bool AtTop => Anchor is MaterialActionAnchor.TopStart or MaterialActionAnchor.TopEnd;
    // Avalonia mirrors child coordinates at the RTL boundary; do not mirror a second time here.
    private bool AtLeft => Anchor is MaterialActionAnchor.TopStart or MaterialActionAnchor.BottomStart;
    static MaterialFabMenuPanel() => AffectsMeasure<MaterialFabMenuPanel>(AnchorProperty, FlowDirectionProperty);
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count != 2) return default;
        var toggle = Children[1];
        toggle.Measure(availableSize);
        var gap = Children[0].IsVisible ? 8 : 0;
        Children[0].Measure(new Size(availableSize.Width, Math.Max(0, availableSize.Height - toggle.DesiredSize.Height - gap)));
        return new Size(Math.Max(toggle.DesiredSize.Width, Children[0].DesiredSize.Width), toggle.DesiredSize.Height + Children[0].DesiredSize.Height + gap);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count != 2) return finalSize;
        var toggle = Children[1];
        var toggleHeight = Math.Min(finalSize.Height, toggle.DesiredSize.Height);
        var toggleWidth = Math.Min(finalSize.Width, toggle.DesiredSize.Width);
        var gap = Children[0].IsVisible ? 8 : 0;
        toggle.Arrange(new Rect(AtLeft ? 0 : finalSize.Width - toggleWidth, AtTop ? 0 : finalSize.Height - toggleHeight, toggleWidth, toggleHeight));
        Children[0].Arrange(new Rect(0, AtTop ? toggleHeight + gap : 0, finalSize.Width, Math.Max(0, finalSize.Height - toggleHeight - gap)));
        return finalSize;
    }
}
