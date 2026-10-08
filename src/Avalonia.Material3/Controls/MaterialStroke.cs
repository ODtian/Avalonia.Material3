using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.Material3.Controls;

// Compose foundation Border.kt:159 rounds physical thickness upwards, whereas
// Canvas radio strokes retain their floating-point width and checkbox floors it.
internal static class MaterialStroke
{
    internal static double Foundation(double width, double density, Size size) => width <= 0 ? 0 :
        Math.Min(Math.Ceiling(width * density), Math.Ceiling(Math.Min(size.Width, size.Height) * density / 2)) / density;
    internal static CornerRadius Inset(CornerRadius radius, double inset) => new(
        Math.Max(0, radius.TopLeft - inset), Math.Max(0, radius.TopRight - inset),
        Math.Max(0, radius.BottomRight - inset), Math.Max(0, radius.BottomLeft - inset));
}

internal sealed class MaterialRadioRing : Control
{
    public static readonly StyledProperty<IBrush?> BorderBrushProperty = Border.BorderBrushProperty.AddOwner<MaterialRadioRing>();
    public static readonly StyledProperty<Thickness> BorderThicknessProperty = Border.BorderThicknessProperty.AddOwner<MaterialRadioRing>();
    public IBrush? BorderBrush { get => GetValue(BorderBrushProperty); set => SetValue(BorderBrushProperty, value); }
    public Thickness BorderThickness { get => GetValue(BorderThicknessProperty); set => SetValue(BorderThicknessProperty, value); }
    static MaterialRadioRing() => AffectsRender<MaterialRadioRing>(BorderBrushProperty, BorderThicknessProperty);
    public override void Render(DrawingContext context)
    {
        var stroke = BorderThickness.Left;
        context.DrawEllipse(null, new Pen(BorderBrush, stroke), new Point(Bounds.Width / 2, Bounds.Height / 2),
            Math.Max(0, Bounds.Width / 2 - stroke / 2), Math.Max(0, Bounds.Height / 2 - stroke / 2));
    }
}
