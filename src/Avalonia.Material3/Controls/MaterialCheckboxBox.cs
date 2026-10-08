using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.Material3.Controls;

// Checkbox.kt drawBox uses one full fill for matching colours. Separate colours use
// an inset fill and an outline, preventing disabled-alpha fill/stroke double compositing.
internal sealed class MaterialCheckboxBox : Control
{
    public static readonly StyledProperty<IBrush?> BackgroundProperty = Border.BackgroundProperty.AddOwner<MaterialCheckboxBox>();
    public static readonly StyledProperty<IBrush?> BorderBrushProperty = Border.BorderBrushProperty.AddOwner<MaterialCheckboxBox>();
    public static readonly StyledProperty<Thickness> BorderThicknessProperty = Border.BorderThicknessProperty.AddOwner<MaterialCheckboxBox>();
    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty = Border.CornerRadiusProperty.AddOwner<MaterialCheckboxBox>();
    public IBrush? Background { get => GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }
    public IBrush? BorderBrush { get => GetValue(BorderBrushProperty); set => SetValue(BorderBrushProperty, value); }
    public Thickness BorderThickness { get => GetValue(BorderThicknessProperty); set => SetValue(BorderThicknessProperty, value); }
    public CornerRadius CornerRadius { get => GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    static MaterialCheckboxBox() => AffectsRender<MaterialCheckboxBox>(BackgroundProperty, BorderBrushProperty, BorderThicknessProperty, CornerRadiusProperty);
    public override void Render(DrawingContext context)
    {
        var rect = new Rect(Bounds.Size);
        var matching = ReferenceEquals(Background, BorderBrush) ||
            Background is ISolidColorBrush fill && BorderBrush is ISolidColorBrush border &&
            fill.Color == border.Color && fill.Opacity == border.Opacity;
        if (matching) { context.DrawRectangle(Background, null, new RoundedRect(rect, CornerRadius)); return; }
        var stroke = BorderThickness.Left;
        var radius = CornerRadius.TopLeft;
        context.DrawRectangle(Background, null, new RoundedRect(rect.Deflate(stroke), Math.Max(0, radius - stroke)));
        context.DrawRectangle(null, new Pen(BorderBrush, stroke),
            new RoundedRect(rect.Deflate(stroke / 2), Math.Max(0, radius - stroke / 2)));
    }
}
