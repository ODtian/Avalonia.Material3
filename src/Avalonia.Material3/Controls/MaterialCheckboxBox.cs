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
    public static readonly StyledProperty<double> ColorAlphaProperty = AvaloniaProperty.Register<MaterialCheckboxBox, double>(nameof(ColorAlpha), 1);
    public IBrush? Background { get => GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }
    public IBrush? BorderBrush { get => GetValue(BorderBrushProperty); set => SetValue(BorderBrushProperty, value); }
    public Thickness BorderThickness { get => GetValue(BorderThicknessProperty); set => SetValue(BorderThicknessProperty, value); }
    public CornerRadius CornerRadius { get => GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    public double ColorAlpha { get => GetValue(ColorAlphaProperty); set => SetValue(ColorAlphaProperty, value); }
    static MaterialCheckboxBox() => AffectsRender<MaterialCheckboxBox>(BackgroundProperty, BorderBrushProperty, BorderThicknessProperty, CornerRadiusProperty, ColorAlphaProperty);
    public MaterialCheckboxBox() => UseLayoutRounding = false;
    public override void Render(DrawingContext context)
    {
        var rect = new Rect(Bounds.Size);
        var matching = ReferenceEquals(Background, BorderBrush) ||
            Background is ISolidColorBrush fill && BorderBrush is ISolidColorBrush border &&
            fill.Color == border.Color && fill.Opacity == border.Opacity;
        void Draw(IBrush? brush, double stroke, RoundedRect shape)
        {
            var alpha = Math.Clamp(ColorAlpha, 0, 1);
            if (brush is ISolidColorBrush solid)
            {
                var color = solid.Color;
                brush = new SolidColorBrush(Color.FromArgb((byte)Math.Round(color.A * alpha, MidpointRounding.AwayFromZero), color.R, color.G, color.B), solid.Opacity);
                alpha = 1;
            }
            using var opacity = context.PushOpacity(alpha);
            context.DrawRectangle(stroke == 0 ? brush : null, stroke == 0 ? null : new Pen(brush, stroke), shape);
        }
        if (matching) { Draw(Background, 0, new RoundedRect(rect, CornerRadius)); return; }
        var stroke = MaterialCheckboxCanvas.Stroke(BorderThickness.Left, TopLevel.GetTopLevel(this)?.RenderScaling ?? 1);
        var radius = CornerRadius.TopLeft;
        Draw(Background, 0, new RoundedRect(rect.Deflate(stroke), Math.Max(0, radius - stroke)));
        if (stroke > 0) Draw(BorderBrush, stroke, new RoundedRect(rect.Deflate(stroke / 2), Math.Max(0, radius - stroke / 2)));
    }
}
