using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Material3.Tokens;

namespace Avalonia.Material3.Controls;

// Selection remains a synchronous native state. Only capsule/underline paint consumes frame time.
internal sealed class MaterialNavigationSelectionPresenter : Control
{
    public static readonly StyledProperty<bool> IsSelectedProperty = AvaloniaProperty.Register<MaterialNavigationSelectionPresenter, bool>(nameof(IsSelected));
    public static readonly StyledProperty<IBrush?> BrushProperty = AvaloniaProperty.Register<MaterialNavigationSelectionPresenter, IBrush?>(nameof(Brush));
    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty = Border.CornerRadiusProperty.AddOwner<MaterialNavigationSelectionPresenter>();
    private static readonly StyledProperty<MaterialSpring> SpatialSpringProperty = AvaloniaProperty.Register<MaterialNavigationSelectionPresenter, MaterialSpring>("SpatialSpring", MaterialSpringScheme.Expressive.FastSpatial);
    private static readonly StyledProperty<MaterialSpring> EffectsSpringProperty = AvaloniaProperty.Register<MaterialNavigationSelectionPresenter, MaterialSpring>("EffectsSpring", MaterialSpringScheme.Expressive.DefaultEffects);
    private readonly MaterialFrameLease _frames;
    private IBrush? _paintBrush;
    private double _size, _alpha, _fromSize, _fromAlpha, _target, _sizeVelocity, _alphaVelocity, _fromSizeVelocity, _fromAlphaVelocity;
    private MaterialSpring _spatial = MaterialSpringScheme.Expressive.FastSpatial, _effects = MaterialSpringScheme.Expressive.DefaultEffects;
    private bool _attached;
    public bool IsSelected { get => GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }
    public IBrush? Brush { get => GetValue(BrushProperty); set => SetValue(BrushProperty, value); }
    public CornerRadius CornerRadius { get => GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    public MaterialNavigationSelectionPresenter()
    {
        IsHitTestVisible = false; UseLayoutRounding = false;
        _frames = MaterialRenderFrames.Bind(this, Advance);
        MaterialPickerSupport.Resource(this, SpatialSpringProperty, "Motion.FastSpatial");
        MaterialPickerSupport.Resource(this, EffectsSpringProperty, "Motion.DefaultEffects");
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e); _attached = true;
        _paintBrush = Brush; Snap();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _attached = false; _frames.SetRunning(false); base.OnDetachedFromVisualTree(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BrushProperty)
        {
            // Deselect styles immediately become Transparent, but exiting ink keeps its last role.
            if (IsSelected || _alpha == 0 || Brush is not null and not ISolidColorBrush || Brush is ISolidColorBrush { Color.A: > 0 }) _paintBrush = Brush;
            InvalidateVisual();
        }
        else if (change.Property == IsSelectedProperty) Retarget();
        else if (change.Property == SpatialSpringProperty || change.Property == EffectsSpringProperty) Retarget();
        else if (change.Property == CornerRadiusProperty) InvalidateVisual();
    }
    private void Snap()
    {
        _target = _size = _alpha = IsSelected ? 1 : 0;
        _sizeVelocity = _alphaVelocity = 0;
        _frames?.SetRunning(false); InvalidateVisual();
    }
    private void Retarget()
    {
        if (_frames is null) return;
        if (!_attached || GetValue(SpatialSpringProperty).IsInstant && GetValue(EffectsSpringProperty).IsInstant) { Snap(); return; }
        if (_frames.IsRunning) _frames.Sample();
        _fromSize = _size; _fromAlpha = _alpha; _fromSizeVelocity = _sizeVelocity; _fromAlphaVelocity = _alphaVelocity;
        _spatial = GetValue(SpatialSpringProperty); _effects = GetValue(EffectsSpringProperty);
        _target = IsSelected ? 1 : 0;
        if (_fromSize == _target && _fromAlpha == _target && _sizeVelocity == 0 && _alphaVelocity == 0) return;
        _frames.Restart(); _frames.SetRunning(true); InvalidateVisual();
    }
    private bool Advance(MaterialFrame frame)
    {
        var time = frame.Elapsed.TotalSeconds;
        (_size, _sizeVelocity) = MaterialSpringResponse.Sample(time, _fromSize, _target, _fromSizeVelocity, _spatial);
        (_alpha, _alphaVelocity) = MaterialSpringResponse.Sample(time, _fromAlpha, _target, _fromAlphaVelocity, _effects);
        var settled = time >= 10 || Math.Abs(_size - _target) < .001 && Math.Abs(_alpha - _target) < .001
            && Math.Abs(_sizeVelocity) < .01 && Math.Abs(_alphaVelocity) < .01;
        if (settled) Snap();
        InvalidateVisual(); return !settled;
    }
    public override void Render(DrawingContext context)
    {
        if (_alpha <= 0 || _size <= 0 || _paintBrush is null) return;
        var width = Bounds.Width * _size;
        var rect = new Rect((Bounds.Width - width) / 2, 0, width, Bounds.Height);
        using (context.PushOpacity(Math.Clamp(_alpha, 0, 1))) context.DrawRectangle(_paintBrush, null, new RoundedRect(rect, CornerRadius));
    }
}
