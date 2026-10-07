using Avalonia.Controls;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.Controls.Presenters;
using Avalonia.VisualTree;

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
    private double _checkedProgress, _checkedTarget, _fromCheckedProgress, _checkedVelocity, _fromCheckedVelocity;
    private bool _attached;
    private MaterialExpansionButton? _toggle;
    private MaterialShapeBorder? _container;
    private ContentPresenter? _icon;
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
        _toggle = this.GetVisualAncestors().OfType<MaterialExpansionButton>().FirstOrDefault();
        if (_toggle is not null) _toggle.PropertyChanged += ToggleChanged;
        _container = this.GetVisualDescendants().OfType<MaterialShapeBorder>().FirstOrDefault(c => c.Name == "Container");
        _icon = this.GetVisualDescendants().OfType<ContentPresenter>().FirstOrDefault(c => c.Name == "PART_ContentPresenter");
        Snap();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _attached = false;
        if (_toggle is not null) _toggle.PropertyChanged -= ToggleChanged;
        _toggle = null; _container = null; _icon = null;
        Stop();
        base.OnDetachedFromVisualTree(e);
    }
    private void ToggleChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == MaterialExpansionButton.IsExpandedProperty) Retarget();
        if (change.Property == MaterialFab.BackgroundProperty || change.Property == MaterialFab.ForegroundProperty
            || change.Property == MaterialExpansionButton.IsExpandedProperty || change.Property == MaterialFab.SizeProperty) PaintToggle();
    }
    private void PaintToggle()
    {
        if (_toggle is not { Expansion: MaterialFabMenu } toggle || _container is null) return;
        var progress = _checkedProgress;
        IBrush? Role(string name) => this.TryFindResource("M3." + name + "Brush", ActualThemeVariant, out var resource) ? resource as IBrush : null;
        _container.SetCurrentValue(Border.BackgroundProperty, MaterialMotionBrush.Interpolate(Role("PrimaryContainer"), Role("Primary"), progress));
        _icon?.SetCurrentValue(ContentPresenter.ForegroundProperty, MaterialMotionBrush.Interpolate(Role("OnPrimaryContainer"), Role("OnPrimary"), progress));
        // Size/icon/shape and colors share the same normalized FastSpatial response.
        var closedCorner = toggle.PresentedSize switch { MaterialFabSize.Small => 12d, MaterialFabSize.Medium => 20d,
            MaterialFabSize.Large => 28d, _ => 16d };
        _container.ShapeSpring = Spring with { IsInstant = true };
        _container.ShapeCornerRadius = new CornerRadius(Math.Max(0, closedCorner + (28 - closedCorner) * progress));
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property != TargetExtentProperty && e.Property != TargetIconSizeProperty && e.Property != SpringProperty && e.Property != AnimateGeometryProperty) return;
        Retarget();
    }
    private void Retarget()
    {
        if (_frames is null) return;
        SetAndRaise(ShapeMotionSpringProperty, ref _shapeSpring, AnimateGeometry ? Spring : Spring with { IsInstant = true });
        if (_frames.IsRunning) _frames.Sample();
        if (_toggle is { Expansion: MaterialFabMenu } menuToggle)
        {
            var target = menuToggle.IsExpanded ? 1 : 0;
            if (!AnimateGeometry || !_attached || Spring.IsInstant) { Snap(); return; }
            if (_checkedTarget == target && _activeSpring == Spring && _frames.IsRunning) return;
            _fromCheckedProgress = _checkedProgress; _fromCheckedVelocity = _checkedVelocity;
            _checkedTarget = target; _activeSpring = Spring;
            _frames.Restart(); _frames.SetRunning(true); return;
        }
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
        if (_toggle is { Expansion: MaterialFabMenu } toggle)
        {
            _checkedTarget = toggle.IsExpanded ? 1 : 0;
            _toExtent = toggle.ClosedContainerSize + (56 - toggle.ClosedContainerSize) * _checkedTarget;
            _toIcon = toggle.ClosedIconSize + (20 - toggle.ClosedIconSize) * _checkedTarget;
        }
        else
        {
        _toExtent = TargetExtent;
        _toIcon = TargetIconSize;
        }
        Complete();
    }
    private void Complete()
    {
        Stop();
        _extentVelocity = _iconVelocity = 0;
        _checkedProgress = _checkedTarget; _checkedVelocity = 0;
        SetAndRaise(ExtentProperty, ref _extent, _toExtent);
        SetAndRaise(IconExtentProperty, ref _iconExtent, _toIcon);
        PaintToggle();
    }
    private void Stop() => _frames?.SetRunning(false);
    private bool Advance(MaterialFrame frame)
    {
        var seconds = frame.Elapsed.TotalSeconds;
        if (_toggle is { Expansion: MaterialFabMenu } toggle)
        {
            var sample = MaterialSpringResponse.Sample(seconds, _fromCheckedProgress, _checkedTarget, _fromCheckedVelocity, _activeSpring);
            _checkedProgress = sample.Value; _checkedVelocity = sample.Velocity;
            if (seconds >= 10 || Math.Abs(sample.Value - _checkedTarget) < .01 && Math.Abs(sample.Velocity) < .625)
            { Snap(); return false; }
            SetAndRaise(ExtentProperty, ref _extent, Math.Max(0, toggle.ClosedContainerSize + (56 - toggle.ClosedContainerSize) * _checkedProgress));
            SetAndRaise(IconExtentProperty, ref _iconExtent, Math.Max(0, toggle.ClosedIconSize + (20 - toggle.ClosedIconSize) * _checkedProgress));
            PaintToggle(); return true;
        }
        var extentSample = MaterialSpringResponse.Sample(seconds, _fromExtent, _toExtent, _fromExtentVelocity, _activeSpring);
        var iconSample = MaterialSpringResponse.Sample(seconds, _fromIcon, _toIcon, _fromIconVelocity, _activeSpring);
        _extentVelocity = extentSample.Velocity; _iconVelocity = iconSample.Velocity;
        var extent = Math.Max(0, extentSample.Value); var icon = Math.Max(0, iconSample.Value);
        if (!double.IsFinite(extent) || !double.IsFinite(icon) || seconds >= 10
            || Math.Abs(extent - _toExtent) < .1 && Math.Abs(icon - _toIcon) < .1 && Math.Abs(_extentVelocity) < 6.25 && Math.Abs(_iconVelocity) < 6.25)
        { Complete(); return false; }
        SetAndRaise(ExtentProperty, ref _extent, extent);
        SetAndRaise(IconExtentProperty, ref _iconExtent, icon);
        PaintToggle();
        return true;
    }
}
