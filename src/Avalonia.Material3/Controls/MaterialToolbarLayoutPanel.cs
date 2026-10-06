using Avalonia.Controls;
using Avalonia.Layout;

namespace Avalonia.Material3.Controls;

internal sealed class MaterialToolbarLayoutPanel : Panel
{
    public static readonly StyledProperty<Orientation> OrientationProperty = AvaloniaProperty.Register<MaterialToolbarLayoutPanel, Orientation>(nameof(Orientation));
    public static readonly StyledProperty<MaterialToolbarFabPosition> FloatingActionPositionProperty = AvaloniaProperty.Register<MaterialToolbarLayoutPanel, MaterialToolbarFabPosition>(nameof(FloatingActionPosition));
    public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }
    public MaterialToolbarFabPosition FloatingActionPosition { get => GetValue(FloatingActionPositionProperty); set => SetValue(FloatingActionPositionProperty, value); }
    private bool Horizontal => Orientation == Orientation.Horizontal;
    private double RevealGap => !Children[1].IsVisible ? 0 : Children[0] is MaterialActionReveal reveal ? 4 * reveal.RevealFraction : Children[0].IsVisible ? 4 : 0;
    static MaterialToolbarLayoutPanel() => AffectsMeasure<MaterialToolbarLayoutPanel>(OrientationProperty, FloatingActionPositionProperty);
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count != 2) return default;
        Children[1].Measure(availableSize);
        var fab = Children[1].DesiredSize;
        var gap = RevealGap;
        Children[0].Measure(Horizontal ? new Size(Math.Max(0, availableSize.Width - fab.Width - gap), availableSize.Height) : new Size(availableSize.Width, Math.Max(0, availableSize.Height - fab.Height - gap)));
        var body = Children[0].DesiredSize;
        return Horizontal ? new Size(body.Width + fab.Width + gap, Math.Max(body.Height, fab.Height)) : new Size(Math.Max(body.Width, fab.Width), body.Height + fab.Height + gap);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count != 2) return finalSize;
        var fab = Children[1].DesiredSize;
        var gap = RevealGap;
        var major = Horizontal ? finalSize.Width : finalSize.Height;
        var cross = Horizontal ? finalSize.Height : finalSize.Width;
        var fabMajor = Math.Min(major, Horizontal ? fab.Width : fab.Height);
        var fabCross = Math.Min(cross, Horizontal ? fab.Height : fab.Width);
        var bodyMajor = Math.Max(0, major - fabMajor - gap);
        var bodyCross = Math.Min(cross, Horizontal ? Children[0].DesiredSize.Height : Children[0].DesiredSize.Width);
        var fabAtStart = FloatingActionPosition == MaterialToolbarFabPosition.Start;
        var bodyOffset = fabAtStart ? fabMajor + gap : 0;
        var fabOffset = fabAtStart ? 0 : bodyMajor + gap;
        Children[0].Arrange(Horizontal ? new Rect(bodyOffset, (cross - bodyCross) / 2, bodyMajor, bodyCross) : new Rect((cross - bodyCross) / 2, bodyOffset, bodyCross, bodyMajor));
        Children[1].Arrange(Horizontal ? new Rect(fabOffset, (cross - fabCross) / 2, fabMajor, fabCross) : new Rect((cross - fabCross) / 2, fabOffset, fabCross, fabMajor));
        return finalSize;
    }
}
