using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.Material3.Controls;

/// <summary>The pinned M3 Checkbox.kt18-DIP canvas; state motion drives path length and cross gravitation.</summary>
internal sealed class MaterialCheckboxGlyph : Control
{
    public static readonly StyledProperty<bool?> ValueProperty =
        AvaloniaProperty.Register<MaterialCheckboxGlyph, bool?>(nameof(Value), false);
    public static readonly StyledProperty<IBrush?> CheckBrushProperty =
        AvaloniaProperty.Register<MaterialCheckboxGlyph, IBrush?>(nameof(CheckBrush));
    public static readonly StyledProperty<double> DrawProgressProperty =
        AvaloniaProperty.Register<MaterialCheckboxGlyph, double>(nameof(DrawProgress));
    public static readonly StyledProperty<double> CrossProgressProperty =
        AvaloniaProperty.Register<MaterialCheckboxGlyph, double>(nameof(CrossProgress));
    public bool? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public IBrush? CheckBrush { get => GetValue(CheckBrushProperty); set => SetValue(CheckBrushProperty, value); }
    public double DrawProgress { get => GetValue(DrawProgressProperty); set => SetValue(DrawProgressProperty, value); }
    public double CrossProgress { get => GetValue(CrossProgressProperty); set => SetValue(CrossProgressProperty, value); }

    protected override bool BypassFlowDirectionPolicies => true;
    static MaterialCheckboxGlyph()
    {
        AffectsRender<MaterialCheckboxGlyph>(CheckBrushProperty, DrawProgressProperty, CrossProgressProperty);
        UseLayoutRoundingProperty.OverrideDefaultValue<MaterialCheckboxGlyph>(false);
        IsHitTestVisibleProperty.OverrideDefaultValue<MaterialCheckboxGlyph>(false);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty)
        {
            SetCurrentValue(DrawProgressProperty, Value == false ? 0 : 1);
            SetCurrentValue(CrossProgressProperty, Value is null ? 1 : 0);
        }
    }
    public override void Render(DrawingContext context)
    {
        if (CheckBrush is null || DrawProgress <= 0) return;
        // AndroidX11ece46a, the M3 stylingFix=true branch.
        // Keep the nominal canvas origin; tight path bounds must never determine alignment.
        var origin = new Point((Bounds.Width - 18) / 2, (Bounds.Height - 18) / 2);
        static double Mix(double from, double to, double progress) => from + (to - from) * progress;
        var left = origin + new Vector(4.5, 9);
        var cross = origin + new Vector(Mix(7.2, 9, CrossProgress), Mix(11.7, 9, CrossProgress));
        var right = origin + new Vector(13.5, Mix(5.4, 9, CrossProgress));
        var firstVector = new Vector(cross.X - left.X, cross.Y - left.Y);
        var secondVector = new Vector(right.X - cross.X, right.Y - cross.Y);
        var first = firstVector.Length; var second = secondVector.Length;
        var distance = (first + second) * Math.Clamp(DrawProgress, 0, 1);
        var geometry = new StreamGeometry();
        using (var path = geometry.Open())
        {
            path.BeginFigure(left, false);
            path.LineTo(left + firstVector * Math.Min(1, distance / first));
            if (distance > first) path.LineTo(cross + secondVector * Math.Min(1, (distance - first) / second));
        }
        context.DrawGeometry(null, new Pen(CheckBrush, 2, lineCap: PenLineCap.Square), geometry);
    }
}
