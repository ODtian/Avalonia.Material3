using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.Material3.Controls;

// Size motion is painted inside a fixed canvas, so layout rounding cannot move the center.
internal sealed class MaterialCircleGlyph : Control
{
    public static readonly StyledProperty<double> DiameterProperty =
        AvaloniaProperty.Register<MaterialCircleGlyph, double>(nameof(Diameter));
    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<MaterialCircleGlyph, IBrush?>(nameof(Fill));
    public double Diameter { get => GetValue(DiameterProperty); set => SetValue(DiameterProperty, value); }
    public IBrush? Fill { get => GetValue(FillProperty); set => SetValue(FillProperty, value); }

    static MaterialCircleGlyph() => AffectsRender<MaterialCircleGlyph>(DiameterProperty, FillProperty);

    public override void Render(DrawingContext context)
    {
        if (Diameter <= 0 || Fill is null) return;
        var radius = Diameter / 2;
        context.DrawEllipse(Fill, null, new Point(Bounds.Width / 2, Bounds.Height / 2), radius, radius);
    }
}
