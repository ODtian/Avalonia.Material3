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
        var progress = Response(time, _activeSpring);
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

    // Closed-form unit-mass step response; the pinned DefaultEffects pair is critically damped (1,1600).
    private static double Response(double time, MaterialSpring spring)
    {
        var omega = Math.Sqrt(spring.Stiffness);
        var damping = spring.DampingRatio;
        if (Math.Abs(damping - 1) < 1e-7)
            return 1 - (1 + omega * time) * Math.Exp(-omega * time);
        if (damping < 1)
        {
            var ratio = Math.Sqrt(1 - damping * damping);
            var phase = omega * ratio * time;
            return 1 - Math.Exp(-damping * omega * time) * (Math.Cos(phase) + damping / ratio * Math.Sin(phase));
        }
        var root = Math.Sqrt(damping * damping - 1);
        var first = -omega * (damping - root);
        var second = -omega * (damping + root);
        return 1 + (second * Math.Exp(first * time) - first * Math.Exp(second * time)) / (first - second);
    }

    private CornerRadius Resolve(CornerRadius radius)
    {
        // Normalize before interpolation: animating 9999 -> 8 would otherwise look unchanged until the last frame.
        // Use ratios that cannot overflow when hosts author large, finite saturated radii.
        static double Ratio(double edge, double first, double second)
        {
            var largest = Math.Max(first, second);
            return largest > 0 ? Math.Min(1, edge / largest / (first / largest + second / largest)) : 1;
        }
        var scale = Math.Min(Math.Min(Ratio(Bounds.Width, radius.TopLeft, radius.TopRight), Ratio(Bounds.Width, radius.BottomLeft, radius.BottomRight)),
            Math.Min(Ratio(Bounds.Height, radius.TopLeft, radius.BottomLeft), Ratio(Bounds.Height, radius.TopRight, radius.BottomRight)));
        return new CornerRadius(radius.TopLeft * scale, radius.TopRight * scale, radius.BottomRight * scale, radius.BottomLeft * scale);
    }

    private void Stop()
    {
        _timer.Stop();
        _elapsed.Stop();
    }
}
