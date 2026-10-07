using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace Avalonia.Material3.Controls;

/// <summary>Bounded action spacing; extra space never inflates individual hit targets.</summary>
internal sealed class MaterialToolbarItemsPanel : Panel
{
    public MaterialToolbarItemsPanel() => UseLayoutRounding = false;
    public static readonly StyledProperty<Orientation> OrientationProperty = AvaloniaProperty.Register<MaterialToolbarItemsPanel, Orientation>(nameof(Orientation));
    public static readonly StyledProperty<double> SpacingProperty = AvaloniaProperty.Register<MaterialToolbarItemsPanel, double>(nameof(Spacing), 4);
    public static readonly StyledProperty<int> StretchChildIndexProperty = AvaloniaProperty.Register<MaterialToolbarItemsPanel, int>(nameof(StretchChildIndex), -1);
    private static readonly StyledProperty<MaterialToolbarVariant> VariantProperty = AvaloniaProperty.Register<MaterialToolbarItemsPanel, MaterialToolbarVariant>("Variant");
    public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }
    public double Spacing { get => GetValue(SpacingProperty); set => SetValue(SpacingProperty, value); }
    public int StretchChildIndex { get => GetValue(StretchChildIndexProperty); set => SetValue(StretchChildIndexProperty, value); }
    private IDisposable? _orientationBinding;
    private IDisposable? _variantBinding;
    private bool Horizontal => Orientation == Orientation.Horizontal;
    private double Major(Size size) => Horizontal ? size.Width : size.Height;
    private double Cross(Size size) => Horizontal ? size.Height : size.Width;
    static MaterialToolbarItemsPanel() => AffectsMeasure<MaterialToolbarItemsPanel>(OrientationProperty, SpacingProperty, VariantProperty, StretchChildIndexProperty);
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (this.FindAncestorOfType<MaterialToolbar>() is { } toolbar)
        {
            _orientationBinding = Bind(OrientationProperty, toolbar.GetObservable(MaterialToolbar.OrientationProperty));
            _variantBinding = Bind(VariantProperty, toolbar.GetObservable(MaterialToolbar.VariantProperty));
        }
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _orientationBinding?.Dispose();
        _variantBinding?.Dispose();
        _orientationBinding = _variantBinding = null;
        base.OnDetachedFromVisualTree(e);
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        var sum = 0.0;
        var cross = 0.0;
        var count = 0;
        foreach (var child in Children)
        {
            child.Measure(Horizontal ? new Size(double.PositiveInfinity, availableSize.Height) : new Size(availableSize.Width, double.PositiveInfinity));
            if (!child.IsVisible || Major(child.DesiredSize) == 0) continue;
            sum += Major(child.DesiredSize);
            cross = Math.Max(cross, Cross(child.DesiredSize));
            count++;
        }
        sum += Math.Max(0, count - 1) * Spacing;
        return Horizontal ? new Size(sum, cross) : new Size(cross, sum);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var visible = Children.Where(child => child.IsVisible && Major(child.DesiredSize) > 0).ToArray();
        var major = Major(finalSize);
        var cross = Cross(finalSize);
        var total = visible.Sum(child => Major(child.DesiredSize));
        var gapCount = Math.Max(0, visible.Length - 1);
        var stretch = StretchChildIndex >= 0 && StretchChildIndex < Children.Count ? Children[StretchChildIndex] : null;
        var extra = Math.Max(0, major - total - gapCount * Spacing);
        var gap = stretch is null && GetValue(VariantProperty) == MaterialToolbarVariant.Docked && gapCount > 0
            ? Math.Clamp((major - total) / gapCount, Spacing, 32) : Spacing;
        var offset = stretch is null ? Math.Max(0, (major - total - gapCount * gap) / 2) : 0;
        foreach (var child in visible)
        {
            var childMajor = Major(child.DesiredSize) + (child == stretch ? extra : 0);
            var childCross = Math.Min(cross, Cross(child.DesiredSize));
            child.Arrange(Horizontal ? new Rect(offset, (cross - childCross) / 2, childMajor, childCross) : new Rect((cross - childCross) / 2, offset, childCross, childMajor));
            offset += childMajor + gap;
        }
        return finalSize;
    }
}
