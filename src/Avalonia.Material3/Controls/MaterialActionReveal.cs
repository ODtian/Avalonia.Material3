using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Avalonia.Material3.Controls;

/// <summary>Private bounded reveal. Semantic input closes immediately; presentation has a stable measured target.</summary>
internal sealed class MaterialActionReveal : Decorator, IMaterialActionDisclosure, IMaterialPaintOverflow
{
    public static readonly StyledProperty<bool> IsExpandedProperty = AvaloniaProperty.Register<MaterialActionReveal, bool>(nameof(IsExpanded), true);
    public static readonly StyledProperty<bool> RevealFromEndProperty = AvaloniaProperty.Register<MaterialActionReveal, bool>(nameof(RevealFromEnd));
    public static readonly StyledProperty<Orientation> OrientationProperty = AvaloniaProperty.Register<MaterialActionReveal, Orientation>(nameof(Orientation));
    public static readonly StyledProperty<MaterialSpring> SpatialSpringProperty = AvaloniaProperty.Register<MaterialActionReveal, MaterialSpring>(nameof(SpatialSpring), new(1, 1400), validate: value => value is { IsValid: true });
    public static readonly StyledProperty<MaterialSpring> EffectsSpringProperty = AvaloniaProperty.Register<MaterialActionReveal, MaterialSpring>(nameof(EffectsSpring), new(1, 3800), validate: value => value is { IsValid: true });
    public static readonly StyledProperty<bool> FadeContentProperty = AvaloniaProperty.Register<MaterialActionReveal, bool>(nameof(FadeContent), true);
    public static readonly StyledProperty<BoxShadows> ElevationShadowProperty = AvaloniaProperty.Register<MaterialActionReveal, BoxShadows>(nameof(ElevationShadow));
    public static readonly StyledProperty<CornerRadius> ShadowCornerRadiusProperty = AvaloniaProperty.Register<MaterialActionReveal, CornerRadius>(nameof(ShadowCornerRadius));
    public BoxShadows ElevationShadow { get => GetValue(ElevationShadowProperty); set => SetValue(ElevationShadowProperty, value); }
    public CornerRadius ShadowCornerRadius { get => GetValue(ShadowCornerRadiusProperty); set => SetValue(ShadowCornerRadiusProperty, value); }
    public bool RevealFromEnd { get => GetValue(RevealFromEndProperty); set => SetValue(RevealFromEndProperty, value); }
    public bool IsExpanded { get => GetValue(IsExpandedProperty); set => SetValue(IsExpandedProperty, value); }
    public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }
    public MaterialSpring SpatialSpring { get => GetValue(SpatialSpringProperty); set => SetValue(SpatialSpringProperty, value); }
    public MaterialSpring EffectsSpring { get => GetValue(EffectsSpringProperty); set => SetValue(EffectsSpringProperty, value); }
    public bool FadeContent { get => GetValue(FadeContentProperty); set => SetValue(FadeContentProperty, value); }
    protected override Type StyleKeyOverride => typeof(Decorator);
    private readonly MaterialFrameLease _frames;
    private bool _attached, _hasFullSize;
    private double _extent = 1, _alpha = 1, _target = 1, _fromExtent, _fromAlpha;
    private double _rawExtent = 1, _extentVelocity, _alphaVelocity, _fromExtentVelocity, _fromAlphaVelocity;
    private Size _fullSize, _constraint, _rootSize;
    private Control? _measuredChild;
    private Control? _shadowClipChild;
    private Geometry? _savedChildClip;
    internal double RevealFraction => _extent;
    internal double FullMajor => Orientation == Orientation.Horizontal ? _fullSize.Width : _fullSize.Height;
    internal bool IsRevealing => _frames.IsRunning;
    internal event Action? Settled;
    bool IMaterialActionDisclosure.IsRevealing => IsRevealing;
    event Action? IMaterialActionDisclosure.Settled { add => Settled += value; remove => Settled -= value; }

    static MaterialActionReveal() => AffectsRender<MaterialActionReveal>(ElevationShadowProperty, ShadowCornerRadiusProperty);

    public MaterialActionReveal()
    {
        UseLayoutRounding = false;
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
        if (change.Property == ChildProperty) { ReleaseShadowClip(); _hasFullSize = false; }
        if (change.Property == ElevationShadowProperty)
        {
            // Only the decorative elevation overflows; the original reveal rectangle
            // is applied to the foreground child in its local coordinates below.
            SetCurrentValue(ClipToBoundsProperty, ElevationShadow == default);
            if (ElevationShadow == default) ReleaseShadowClip();
            InvalidateArrange();
        }
        if (change.Property == IsExpandedProperty || change.Property == SpatialSpringProperty || change.Property == EffectsSpringProperty || change.Property == FadeContentProperty)
        {
            if (_frames is null) return;
            if (_frames.IsRunning) _frames.Sample();
            IsEnabled = IsHitTestVisible = IsExpanded;
            _target = IsExpanded ? 1 : 0;
            if (!_attached || (SpatialSpring.IsInstant && EffectsSpring.IsInstant)
                || (_rawExtent == _target && _alpha == (FadeContent ? _target : 1) && _extentVelocity == 0 && _alphaVelocity == 0)) Snap();
            else
            {
                _fromExtent = _rawExtent;
                _fromAlpha = _alpha;
                _fromExtentVelocity = _extentVelocity; _fromAlphaVelocity = _alphaVelocity;
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
        _rawExtent = _extent = _target; _alpha = FadeContent ? _target : 1;
        _extentVelocity = _alphaVelocity = 0;
        Opacity = _alpha;
        IsVisible = _target > 0;
        IsEnabled = IsHitTestVisible = IsExpanded;
        InvalidateMeasure();
        if (wasMoving) Settled?.Invoke();
    }
    private bool Advance(MaterialFrame frame)
    {
        var seconds = frame.Elapsed.TotalSeconds;
        (_rawExtent, _extentVelocity) = MaterialSpringResponse.Sample(seconds, _fromExtent, _target, _fromExtentVelocity, SpatialSpring);
        var alphaTarget = FadeContent ? _target : 1;
        (_alpha, _alphaVelocity) = MaterialSpringResponse.Sample(seconds, _fromAlpha, alphaTarget, _fromAlphaVelocity, EffectsSpring);
        _extent = Math.Clamp(_rawExtent, 0, 1); _alpha = Math.Clamp(_alpha, 0, 1);
        var scale = Math.Max(1, FullMajor);
        if (!double.IsFinite(_rawExtent) || !double.IsFinite(_alpha) || seconds >= 10
            || Math.Abs(_rawExtent - _target) * scale < 1 && Math.Abs(_extentVelocity) * scale < 62.5
                && Math.Abs(_alpha - alphaTarget) < .01 && Math.Abs(_alphaVelocity) < .625)
        { Complete(); return false; }
        Opacity = _alpha;
        InvalidateMeasure();
        return true;
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        MeasureFullTarget(availableSize);
        return Orientation == Orientation.Horizontal ? new Size(_fullSize.Width * _extent, _fullSize.Height)
            : new Size(_fullSize.Width, _fullSize.Height * _extent);
    }
    internal Size MeasureFullTarget(Size availableSize)
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
        return _fullSize;
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var width = Orientation == Orientation.Horizontal ? Math.Max(finalSize.Width, _fullSize.Width) : finalSize.Width;
        var height = Orientation == Orientation.Vertical ? Math.Max(finalSize.Height, _fullSize.Height) : finalSize.Height;
        Child?.Arrange(new Rect(RevealFromEnd && Orientation == Orientation.Horizontal ? finalSize.Width - width : 0,
            RevealFromEnd && Orientation == Orientation.Vertical ? finalSize.Height - height : 0, width, height));
        if (ElevationShadow != default && Child is { } child)
        {
            if (_shadowClipChild != child)
            {
                ReleaseShadowClip(); _shadowClipChild = child; _savedChildClip = child.Clip;
            }
            var clip = new RectangleGeometry(new Rect(finalSize).TransformToAABB(this.TransformToVisual(child)!.Value));
            child.SetCurrentValue(ClipProperty, _savedChildClip is null ? clip : new CombinedGeometry(GeometryCombineMode.Intersect, _savedChildClip, clip));
        }
        return finalSize;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (ElevationShadow == default) return;
        var maximum = Math.Min(Bounds.Width, Bounds.Height) / 2;
        double Radius(double value) => Math.Min(value, maximum);
        var rounded = new RoundedRect(new Rect(Bounds.Size), Radius(ShadowCornerRadius.TopLeft), Radius(ShadowCornerRadius.TopRight),
            Radius(ShadowCornerRadius.BottomRight), Radius(ShadowCornerRadius.BottomLeft));
        context.DrawRectangle(null, null, rounded, ElevationShadow);
    }
    Rect IMaterialPaintOverflow.GetPaintBounds(Rect bounds) => ElevationShadow.TransformBounds(bounds);

    private void ReleaseShadowClip()
    {
        if (_shadowClipChild is { } child) child.SetCurrentValue(ClipProperty, _savedChildClip);
        _shadowClipChild = null; _savedChildClip = null;
    }
}
