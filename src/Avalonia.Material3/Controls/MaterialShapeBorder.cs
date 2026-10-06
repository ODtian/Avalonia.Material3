using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Material3.Tokens;
using Avalonia.Threading;

namespace Avalonia.Material3.Controls;

/// <summary>Template-only spring projection. Input/automation remain on the owning Button.</summary>
internal sealed class MaterialShapeBorder : Border
{
    public static readonly StyledProperty<CornerRadius> ShapeCornerRadiusProperty =
        AvaloniaProperty.Register<MaterialShapeBorder, CornerRadius>(nameof(ShapeCornerRadius));
    public static readonly StyledProperty<MaterialSpring> ShapeSpringProperty =
        AvaloniaProperty.Register<MaterialShapeBorder, MaterialSpring>(nameof(ShapeSpring), new(1, 1600),
            validate: spring => spring is { IsValid: true });

    public CornerRadius ShapeCornerRadius { get => GetValue(ShapeCornerRadiusProperty); set => SetValue(ShapeCornerRadiusProperty, value); }
    public MaterialSpring ShapeSpring { get => GetValue(ShapeSpringProperty); set => SetValue(ShapeSpringProperty, value); }
    protected override Type StyleKeyOverride => typeof(Border);

    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private readonly Stopwatch _elapsed = new();
    private CornerRadius _from;
    private CornerRadius _target;
    private MaterialSpring _activeSpring = new(1, 1600);
    private bool _attached;
    private double _previousProgress;
    private double _previousTime;

    public MaterialShapeBorder() => _timer.Tick += (_, _) => Advance();

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs args)
    {
        base.OnAttachedToVisualTree(args);
        _attached = true;
        UpdateShape(false);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs args)
    {
        _attached = false;
        Stop();
        base.OnDetachedFromVisualTree(args);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BoundsProperty)
            UpdateShape(false); // Layout changes establish a new finite shape; never tween saturated token radii.
        else if (change.Property == ShapeCornerRadiusProperty || change.Property == ShapeSpringProperty)
            UpdateShape(true);
    }

    private void UpdateShape(bool animate)
    {
        if (_timer.IsEnabled)
            Advance();
        var target = Resolve(ShapeCornerRadius);
        Stop();
        if (!animate || !_attached || ShapeSpring.IsInstant || CornerRadius == target)
        {
            SetCurrentValue(CornerRadiusProperty, target);
            return;
        }
        _from = CornerRadius;
        _target = target;
        _activeSpring = ShapeSpring;
        _previousProgress = _previousTime = 0;
        _elapsed.Restart();
        _timer.Start();
    }

    private void Advance()
    {
        var time = _elapsed.Elapsed.TotalSeconds;
        var progress = MaterialSpringResponse.Evaluate(time, _activeSpring);
        var amplitude = Math.Max(Math.Max(Math.Abs(_from.TopLeft - _target.TopLeft), Math.Abs(_from.TopRight - _target.TopRight)),
            Math.Max(Math.Abs(_from.BottomRight - _target.BottomRight), Math.Abs(_from.BottomLeft - _target.BottomLeft)));
        var speed = time > _previousTime ? Math.Abs(progress - _previousProgress) / (time - _previousTime) * amplitude : double.PositiveInfinity;
        if (!double.IsFinite(progress) || time >= 10 || (Math.Abs(1 - progress) * amplitude <= 0.01 && speed <= 0.1))
        {
            SetCurrentValue(CornerRadiusProperty, _target);
            Stop();
            return;
        }
        static double Mix(double start, double end, double progress) => Math.Max(0, start + (end - start) * progress);
        SetCurrentValue(CornerRadiusProperty, Resolve(new CornerRadius(
            Mix(_from.TopLeft, _target.TopLeft, progress), Mix(_from.TopRight, _target.TopRight, progress),
            Mix(_from.BottomRight, _target.BottomRight, progress), Mix(_from.BottomLeft, _target.BottomLeft, progress))));
        _previousProgress = progress;
        _previousTime = time;
    }

    private CornerRadius Resolve(CornerRadius radius)
    {
        // Full is a percentage-like token, not a 9999-DIP fixed corner. Resolve it before
        // interpolation. AndroidX CornerBasedShape normalizes start/end sides independently;
        // a global scalar incorrectly turns an adjacent fixed4/8 inner corner into ~0.
        var shortest = Math.Max(0, Math.Min(Bounds.Width, Bounds.Height));
        double Finite(double value) => value >= 9999 ? shortest : value;
        var tl = Finite(radius.TopLeft); var tr = Finite(radius.TopRight);
        var br = Finite(radius.BottomRight); var bl = Finite(radius.BottomLeft);
        static void Fit(double edge, ref double first, ref double second)
        {
            var largest = Math.Max(first, second);
            var scale = largest > 0 ? Math.Min(1, edge / largest / (first / largest + second / largest)) : 1;
            first *= scale; second *= scale;
        }
        Fit(shortest, ref tl, ref bl);
        Fit(shortest, ref tr, ref br);
        // Keep exotic asymmetric caller corners finite on horizontal edges too, so the
        // renderer does not perform a second, hidden normalization after interpolation.
        Fit(Bounds.Width, ref tl, ref tr);
        Fit(Bounds.Width, ref bl, ref br);
        return new CornerRadius(tl, tr, br, bl);
    }

    private void Stop()
    {
        _timer.Stop();
        _elapsed.Stop();
    }
}
