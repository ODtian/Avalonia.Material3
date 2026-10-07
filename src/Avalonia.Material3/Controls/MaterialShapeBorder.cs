using Avalonia.Controls;
using Avalonia.Material3.Tokens;

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

    private readonly MaterialFrameLease _frames;
    private Size _lastSize;
    private CornerRadius _from;
    private CornerRadius _target;
    private MaterialSpring _activeSpring = new(1, 1600);
    private bool _attached;
    private (double TL, double TR, double BR, double BL) _velocity, _fromVelocity;

    public MaterialShapeBorder() => _frames = MaterialRenderFrames.Bind(this, Advance);

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
        {
            if (_lastSize == Bounds.Size) return;
            _lastSize = Bounds.Size;
            if (_frames?.IsRunning == true)
            {
                // A concurrent width/size transition changes normalization, not the shape track's epoch.
                // Keep its current finite paint and retarget the finite endpoint without restarting/snap.
                _target = Resolve(ShapeCornerRadius);
                SetCurrentValue(CornerRadiusProperty, Resolve(CornerRadius));
            }
            else UpdateShape(false);
        }
        else if (change.Property == ShapeCornerRadiusProperty || change.Property == ShapeSpringProperty)
            UpdateShape(true);
    }

    private void UpdateShape(bool animate)
    {
        if (_frames is null) return;
        if (_frames.IsRunning)
            _frames.Sample();
        var target = Resolve(ShapeCornerRadius);
        Stop();
        if (!animate || !_attached || ShapeSpring.IsInstant || CornerRadius == target)
        {
            _velocity = default;
            SetCurrentValue(CornerRadiusProperty, target);
            return;
        }
        _from = CornerRadius;
        _target = target;
        _activeSpring = ShapeSpring;
        _fromVelocity = _velocity;
        _frames.Restart();
        _frames.SetRunning(true);
    }

    private bool Advance(MaterialFrame frame)
    {
        var time = frame.Elapsed.TotalSeconds;
        var tl = MaterialSpringResponse.Sample(time, _from.TopLeft, _target.TopLeft, _fromVelocity.TL, _activeSpring);
        var tr = MaterialSpringResponse.Sample(time, _from.TopRight, _target.TopRight, _fromVelocity.TR, _activeSpring);
        var br = MaterialSpringResponse.Sample(time, _from.BottomRight, _target.BottomRight, _fromVelocity.BR, _activeSpring);
        var bl = MaterialSpringResponse.Sample(time, _from.BottomLeft, _target.BottomLeft, _fromVelocity.BL, _activeSpring);
        _velocity = (tl.Velocity, tr.Velocity, br.Velocity, bl.Velocity);
        var distance = Math.Max(Math.Max(Math.Abs(tl.Value - _target.TopLeft), Math.Abs(tr.Value - _target.TopRight)), Math.Max(Math.Abs(br.Value - _target.BottomRight), Math.Abs(bl.Value - _target.BottomLeft)));
        var speed = Math.Max(Math.Max(Math.Abs(tl.Velocity), Math.Abs(tr.Velocity)), Math.Max(Math.Abs(br.Velocity), Math.Abs(bl.Velocity)));
        if (!double.IsFinite(distance) || time >= 10 || distance <= .01 && speed <= .625)
        {
            _velocity = default;
            SetCurrentValue(CornerRadiusProperty, _target);
            Stop();
            return false;
        }
        SetCurrentValue(CornerRadiusProperty, Resolve(new CornerRadius(
            Math.Max(0, tl.Value), Math.Max(0, tr.Value), Math.Max(0, br.Value), Math.Max(0, bl.Value))));
        return true;
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
        _frames?.SetRunning(false);
    }
}
