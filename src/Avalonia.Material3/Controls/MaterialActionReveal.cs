using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Material3.Tokens;

namespace Avalonia.Material3.Controls;

/// <summary>Private bounded reveal. Semantic input closes immediately; presentation has a stable measured target.</summary>
internal sealed class MaterialActionReveal : Decorator
{
    public static readonly StyledProperty<bool> IsExpandedProperty = AvaloniaProperty.Register<MaterialActionReveal, bool>(nameof(IsExpanded), true);
    public static readonly StyledProperty<bool> RevealFromEndProperty = AvaloniaProperty.Register<MaterialActionReveal, bool>(nameof(RevealFromEnd));
    public static readonly StyledProperty<Orientation> OrientationProperty = AvaloniaProperty.Register<MaterialActionReveal, Orientation>(nameof(Orientation));
    public static readonly StyledProperty<MaterialSpring> SpatialSpringProperty = AvaloniaProperty.Register<MaterialActionReveal, MaterialSpring>(nameof(SpatialSpring), new(1, 1400), validate: value => value is { IsValid: true });
    public static readonly StyledProperty<MaterialSpring> EffectsSpringProperty = AvaloniaProperty.Register<MaterialActionReveal, MaterialSpring>(nameof(EffectsSpring), new(1, 3800), validate: value => value is { IsValid: true });
    public bool RevealFromEnd { get => GetValue(RevealFromEndProperty); set => SetValue(RevealFromEndProperty, value); }
    public bool IsExpanded { get => GetValue(IsExpandedProperty); set => SetValue(IsExpandedProperty, value); }
    public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }
    public MaterialSpring SpatialSpring { get => GetValue(SpatialSpringProperty); set => SetValue(SpatialSpringProperty, value); }
    public MaterialSpring EffectsSpring { get => GetValue(EffectsSpringProperty); set => SetValue(EffectsSpringProperty, value); }
    protected override Type StyleKeyOverride => typeof(Decorator);
    private readonly MaterialFrameLease _frames;
    private bool _attached, _hasFullSize;
    private double _extent = 1, _alpha = 1, _target = 1, _fromExtent, _fromAlpha;
    private Size _fullSize, _constraint, _rootSize;
    private Control? _measuredChild;
    internal double RevealFraction => _extent;
    internal bool IsRevealing => _frames.IsRunning;
    internal event Action? Settled;

    public MaterialActionReveal()
    {
        ClipToBounds = true;
        _frames = MaterialRenderFrames.Bind(this, Advance, ignoreOwnerEnabled: true);
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;
        Snap();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _attached = false;
        _frames.SetRunning(false);
        base.OnDetachedFromVisualTree(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ChildProperty) _hasFullSize = false;
        if (change.Property == IsExpandedProperty || change.Property == SpatialSpringProperty || change.Property == EffectsSpringProperty)
        {
            if (_frames is null) return;
            if (_frames.IsRunning) _frames.Sample();
            IsEnabled = IsHitTestVisible = IsExpanded;
            _target = IsExpanded ? 1 : 0;
            if (!_attached || (SpatialSpring.IsInstant && EffectsSpring.IsInstant)
                || (_extent == _target && _alpha == _target)) Snap();
            else
            {
                _fromExtent = _extent;
                _fromAlpha = _alpha;
                IsVisible = true;
                _frames.Restart();
                _frames.SetRunning(true);
            }
        }
        if (change.Property == OrientationProperty || change.Property == RevealFromEndProperty) InvalidateMeasure();
    }
    private void Snap()
    {
        _target = IsExpanded ? 1 : 0;
        Complete();
    }
    private void Complete()
    {
        var wasMoving = _frames?.IsRunning == true;
        _frames?.SetRunning(false);
        _extent = _alpha = _target;
        Opacity = _alpha;
        IsVisible = _target > 0;
        IsEnabled = IsHitTestVisible = IsExpanded;
        InvalidateMeasure();
        if (wasMoving) Settled?.Invoke();
    }
    private bool Advance(MaterialFrame frame)
    {
        var seconds = frame.Elapsed.TotalSeconds;
        var spatial = MaterialSpringResponse.Evaluate(seconds, SpatialSpring);
        var effects = MaterialSpringResponse.Evaluate(seconds, EffectsSpring);
        if (!double.IsFinite(spatial) || !double.IsFinite(effects)) { Complete(); return false; }
        static double Project(double current, double from, double target, double response)
        {
            var bounded = Math.Clamp(from + (target - from) * response, 0, 1);
            // Extent is a reveal, not an unbounded spatial translation. Once a clip reaches its edge
            // it cannot recoil while the independent effects track finishes. Keep the same spring curve.
            return target > from ? Math.Max(current, bounded) : Math.Min(current, bounded);
        }
        _extent = Project(_extent, _fromExtent, _target, spatial);
        _alpha = Project(_alpha, _fromAlpha, _target, effects);
        if (seconds >= 10 || (Math.Abs(_extent - _target) < .001 && Math.Abs(_alpha - _target) < .001))
        { Complete(); return false; }
        Opacity = _alpha;
        InvalidateMeasure();
        return true;
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        var rootSize = TopLevel.GetTopLevel(this)?.ClientSize ?? availableSize;
        if (!_hasFullSize || Child != _measuredChild || Child?.IsMeasureValid == false
            || rootSize != _rootSize || availableSize.Width != _constraint.Width
            || (!IsRevealing && availableSize != _constraint))
        {
            Child?.Measure(availableSize);
            _fullSize = Child?.DesiredSize ?? default;
            _measuredChild = Child;
            _constraint = availableSize;
            _rootSize = rootSize;
            _hasFullSize = true;
        }
        return Orientation == Orientation.Horizontal ? new Size(_fullSize.Width * _extent, _fullSize.Height)
            : new Size(_fullSize.Width, _fullSize.Height * _extent);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var width = Orientation == Orientation.Horizontal ? Math.Max(finalSize.Width, _fullSize.Width) : finalSize.Width;
        var height = Orientation == Orientation.Vertical ? Math.Max(finalSize.Height, _fullSize.Height) : finalSize.Height;
        Child?.Arrange(new Rect(RevealFromEnd && Orientation == Orientation.Horizontal ? finalSize.Width - width : 0,
            RevealFromEnd && Orientation == Orientation.Vertical ? finalSize.Height - height : 0, width, height));
        return finalSize;
    }
}
