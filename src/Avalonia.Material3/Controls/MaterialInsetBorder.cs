using Avalonia.Controls;
using Avalonia.Layout;

namespace Avalonia.Material3.Controls;

/// <summary>Template decoration: actual BorderThickness is paint, ReservedBorderThickness is layout.
/// The native Border renderer still owns arbitrary brushes/nonuniform strokes/antialiasing.</summary>
internal sealed class MaterialInsetBorder : MaterialElevationBorder
{
    protected override Type StyleKeyOverride => typeof(MaterialInsetBorder);
    public static readonly StyledProperty<Thickness> ReservedBorderThicknessProperty =
        AvaloniaProperty.Register<MaterialInsetBorder, Thickness>(nameof(ReservedBorderThickness));
    public Thickness ReservedBorderThickness { get => GetValue(ReservedBorderThicknessProperty); set => SetValue(ReservedBorderThicknessProperty, value); }
    static MaterialInsetBorder() => AffectsMeasure<MaterialInsetBorder>(ReservedBorderThicknessProperty);
    protected override Size MeasureOverride(Size availableSize) { UpdateStroke(); return LayoutHelper.MeasureChild(Child, availableSize, Padding, ReservedBorderThickness); }
    protected override Size ArrangeOverride(Size finalSize) { UpdateStroke(finalSize); return LayoutHelper.ArrangeChild(Child, finalSize, Padding, ReservedBorderThickness); }
}
