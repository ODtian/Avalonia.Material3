using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.Material3.Controls;

/// <summary>The pinned M3 Checkbox.kt18-DIP canvas; state motion drives path length and cross gravitation.</summary>
internal sealed class MaterialCheckboxGlyph : Control
{
    private readonly MaterialMotionValue _draw;
    private readonly MaterialMotionValue _cross;
    private readonly MaterialMotionValue _markAlpha;
    private readonly MaterialMotionSettings _motion;
    private readonly MaterialMotionBrush _markColor;
    private bool? _previous;
    public MaterialCheckboxGlyph()
    {
        _draw = new(this, 0, value => SetValue(DrawProgressProperty, value));
        _cross = new(this, 0, value => SetValue(CrossProgressProperty, value));
        _markAlpha = new(this, 0, _ => InvalidateVisual());
        _markColor = new(this, null, _ => InvalidateVisual());
        _motion = new(this, () => Retarget(_previous));
    }
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
            var previous = _previous;
            _previous = Value;
            Retarget(previous);
        }
        if (change.Property == CheckBrushProperty) Retarget(_previous);
    }
    private void Retarget(bool? previous)
    {
        if (_motion is null) return;
        _markColor.Set(CheckBrush, Value == false ? _motion.FastEffects : _motion.DefaultEffects);
        var cross = Value is null ? 1 : 0;
        if (!_motion.IsAttached || _motion.DefaultSpatial.IsInstant)
        {
            _draw.Snap(Value == false ? 0 : 1); _cross.Snap(cross); _markAlpha.Snap(Value == false ? 0 : 1);
            return;
        }
        if (Value == false)
        {
            _draw.Tween(0, TimeSpan.Zero, delay: TimeSpan.FromMilliseconds(100));
            _cross.Tween(0, TimeSpan.Zero, delay: TimeSpan.FromMilliseconds(100));
        }
        else
        {
            _draw.Spring(1, _motion.DefaultSpatial);
            if (previous == false) _cross.Snap(cross);
            else _cross.Spring(cross, _motion.DefaultSpatial);
        }
        _markAlpha.Spring(Value == false ? 0 : 1, Value == false ? _motion.FastEffects : _motion.DefaultEffects);
    }
    public override void Render(DrawingContext context)
    {
        if (_markColor.Value is null || DrawProgress <= 0) return;
        // AndroidX11ece46a, the M3 stylingFix=true branch.
        // Keep the nominal canvas origin; tight path bounds must never determine alignment.
        static double Mix(double from, double to, double progress) => from + (to - from) * progress;
        var left = new Point(Bounds.Width * .25, Bounds.Height * .5);
        var cross = new Point(Bounds.Width * Mix(.4, .5, CrossProgress), Bounds.Height * Mix(.65, .5, CrossProgress));
        var right = new Point(Bounds.Width * .75, Bounds.Height * Mix(.3, .5, CrossProgress));
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
        using (context.PushOpacity(Math.Clamp(_markAlpha.Value, 0, 1)))
            context.DrawGeometry(null, new Pen(_markColor.Value, MaterialCheckboxCanvas.Stroke(2, TopLevel.GetTopLevel(this)?.RenderScaling ?? 1), lineCap: PenLineCap.Square), geometry);
    }
}
