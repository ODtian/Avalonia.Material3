using Avalonia.Controls;
using Avalonia.Material3.Tokens;

namespace Avalonia.Material3.Controls;

/// <summary>Private measured FAB geometry, independently interpolating container and icon targets with the effective spatial spring.</summary>
internal sealed class MaterialFabGeometryPresenter : Decorator
{
    public static readonly StyledProperty<double> TargetExtentProperty = AvaloniaProperty.Register<MaterialFabGeometryPresenter, double>(nameof(TargetExtent), 56);
    public static readonly StyledProperty<double> TargetIconSizeProperty = AvaloniaProperty.Register<MaterialFabGeometryPresenter, double>(nameof(TargetIconSize), 24);
    public static readonly StyledProperty<MaterialSpring> SpringProperty = AvaloniaProperty.Register<MaterialFabGeometryPresenter, MaterialSpring>(nameof(Spring), new(1, 1400), validate: value => value is { IsValid: true });
    public static readonly StyledProperty<bool> AnimateGeometryProperty = AvaloniaProperty.Register<MaterialFabGeometryPresenter, bool>(nameof(AnimateGeometry));
    public static readonly DirectProperty<MaterialFabGeometryPresenter, MaterialSpring> ShapeMotionSpringProperty = AvaloniaProperty.RegisterDirect<MaterialFabGeometryPresenter, MaterialSpring>(nameof(ShapeMotionSpring), control => control.ShapeMotionSpring);
    public static readonly DirectProperty<MaterialFabGeometryPresenter, double> ExtentProperty = AvaloniaProperty.RegisterDirect<MaterialFabGeometryPresenter, double>(nameof(Extent), control => control.Extent);
    public static readonly DirectProperty<MaterialFabGeometryPresenter, double> IconExtentProperty = AvaloniaProperty.RegisterDirect<MaterialFabGeometryPresenter, double>(nameof(IconExtent), control => control.IconExtent);
    public double TargetExtent { get => GetValue(TargetExtentProperty); set => SetValue(TargetExtentProperty, value); }
    public double TargetIconSize { get => GetValue(TargetIconSizeProperty); set => SetValue(TargetIconSizeProperty, value); }
    public MaterialSpring Spring { get => GetValue(SpringProperty); set => SetValue(SpringProperty, value); }
    public bool AnimateGeometry { get => GetValue(AnimateGeometryProperty); set => SetValue(AnimateGeometryProperty, value); }
    private MaterialSpring _shapeSpring = new(1, 1400) { IsInstant = true };
    public MaterialSpring ShapeMotionSpring => _shapeSpring;
    private double _extent = 56;
    private double _iconExtent = 24;
    public double Extent => _extent;
    public double IconExtent => _iconExtent;
    private readonly MaterialFrameLease _frames;
    private MaterialSpring _activeSpring = new(1, 1400);
    private double _toExtent = 56, _toIcon = 24;
    private double _fromExtent;
    private double _fromIcon;
    private double _extentVelocity, _iconVelocity, _fromExtentVelocity, _fromIconVelocity;
    private bool _attached;
    protected override Type StyleKeyOverride => typeof(Decorator);
    public MaterialFabGeometryPresenter()
    {
        UseLayoutRounding = false;
        _frames = MaterialRenderFrames.Bind(this, Advance);
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
        Stop();
        base.OnDetachedFromVisualTree(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property != TargetExtentProperty && e.Property != TargetIconSizeProperty && e.Property != SpringProperty && e.Property != AnimateGeometryProperty) return;
        if (_frames is null) return;
        SetAndRaise(ShapeMotionSpringProperty, ref _shapeSpring, AnimateGeometry ? Spring : Spring with { IsInstant = true });
        if (_frames.IsRunning) _frames.Sample();
        if (!AnimateGeometry || !_attached || Spring.IsInstant || (Extent == TargetExtent && IconExtent == TargetIconSize && _extentVelocity == 0 && _iconVelocity == 0)) { Snap(); return; }
        _fromExtent = Extent;
        _fromIcon = IconExtent;
        _fromExtentVelocity = _extentVelocity; _fromIconVelocity = _iconVelocity;
        _toExtent = TargetExtent;
        _toIcon = TargetIconSize;
        _activeSpring = Spring;
        _frames.Restart();
        _frames.SetRunning(true);
    }
    private void Snap()
    {
        _toExtent = TargetExtent;
        _toIcon = TargetIconSize;
        Complete();
    }
    private void Complete()
    {
        Stop();
        _extentVelocity = _iconVelocity = 0;
        SetAndRaise(ExtentProperty, ref _extent, _toExtent);
        SetAndRaise(IconExtentProperty, ref _iconExtent, _toIcon);
    }
    private void Stop() => _frames?.SetRunning(false);
    private bool Advance(MaterialFrame frame)
    {
        var seconds = frame.Elapsed.TotalSeconds;
        var extentSample = MaterialSpringResponse.Sample(seconds, _fromExtent, _toExtent, _fromExtentVelocity, _activeSpring);
        var iconSample = MaterialSpringResponse.Sample(seconds, _fromIcon, _toIcon, _fromIconVelocity, _activeSpring);
        _extentVelocity = extentSample.Velocity; _iconVelocity = iconSample.Velocity;
        var extent = Math.Max(0, extentSample.Value); var icon = Math.Max(0, iconSample.Value);
        if (!double.IsFinite(extent) || !double.IsFinite(icon) || seconds >= 10
            || Math.Abs(extent - _toExtent) < .1 && Math.Abs(icon - _toIcon) < .1 && Math.Abs(_extentVelocity) < 6.25 && Math.Abs(_iconVelocity) < 6.25)
        { Complete(); return false; }
        SetAndRaise(ExtentProperty, ref _extent, extent);
        SetAndRaise(IconExtentProperty, ref _iconExtent, icon);
        return true;
    }
}
