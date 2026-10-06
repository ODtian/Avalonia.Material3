using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.Material3.Controls;

// A circular visual, not a minimum40×40 Border that becomes an ellipse/capsule when text grows.
// Native ToggleButton/content ownership remains on MaterialCalendarDay.
internal sealed class MaterialCalendarCircle : Decorator
{
    public static readonly StyledProperty<IBrush?> BackgroundProperty = Border.BackgroundProperty.AddOwner<MaterialCalendarCircle>();
    public static readonly StyledProperty<IBrush?> BorderBrushProperty = Border.BorderBrushProperty.AddOwner<MaterialCalendarCircle>();
    public static readonly StyledProperty<Thickness> BorderThicknessProperty = Border.BorderThicknessProperty.AddOwner<MaterialCalendarCircle>();
    public IBrush? Background { get => GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }
    public IBrush? BorderBrush { get => GetValue(BorderBrushProperty); set => SetValue(BorderBrushProperty, value); }
    public Thickness BorderThickness { get => GetValue(BorderThicknessProperty); set => SetValue(BorderThicknessProperty, value); }
    static MaterialCalendarCircle() => AffectsRender<MaterialCalendarCircle>(BackgroundProperty, BorderBrushProperty, BorderThicknessProperty);
    protected override Size MeasureOverride(Size availableSize)
    {
        Child?.Measure(Size.Infinity);
        var content = Child?.DesiredSize ?? default;
        var diameter = Math.Max(40, Math.Max(content.Width, content.Height));
        return new(diameter, diameter);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        Child?.Arrange(new Rect(finalSize)); return finalSize;
    }
    public override void Render(DrawingContext context)
    {
        var radius = Math.Min(Bounds.Width, Bounds.Height) / 2;
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        context.DrawEllipse(Background, null, center, radius, radius);
        var stroke = Math.Max(Math.Max(BorderThickness.Left, BorderThickness.Right), Math.Max(BorderThickness.Top, BorderThickness.Bottom));
        if (stroke > 0) context.DrawEllipse(null, new Pen(BorderBrush, stroke), center, Math.Max(0, radius - stroke / 2), Math.Max(0, radius - stroke / 2));
    }
}
