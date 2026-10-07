using Avalonia.Controls;
using Avalonia.Material3.Tokens;

namespace Avalonia.Material3.Controls;

/// <summary>Private measured FAB geometry, independently interpolating container and icon targets with the effective spatial spring.</summary>
internal sealed class MaterialFabGeometryPresenter : Decorator
{
    public static readonly StyledProperty<double> TargetExtentProperty = AvaloniaProperty.Register<MaterialFabGeometryPresenter, double>(nameof(TargetExtent), 56);
    public static readonly StyledProperty<double> TargetIconSizeProperty = AvaloniaProperty.Register<MaterialFabGeometryPresenter, double>(nameof(TargetIconSize), 24);
    public static readonly StyledProperty<MaterialSpring> SpringProperty = AvaloniaProperty.Register<MaterialFabGeometryPresenter, MaterialSpring>(nameof(Spring), new(1, 1400), validate: value => value is { IsValid: true });
    public static readonly DirectProperty<MaterialFabGeometryPresenter, double> ExtentProperty = AvaloniaProperty.RegisterDirect<MaterialFabGeometryPresenter, double>(nameof(Extent), control => control.Extent);
    public static readonly DirectProperty<MaterialFabGeometryPresenter, double> IconExtentProperty = AvaloniaProperty.RegisterDirect<MaterialFabGeometryPresenter, double>(nameof(IconExtent), control => control.IconExtent);
    public double TargetExtent { get => GetValue(TargetExtentProperty); set => SetValue(TargetExtentProperty, value); }
    public double TargetIconSize { get => GetValue(TargetIconSizeProperty); set => SetValue(TargetIconSizeProperty, value); }
    public MaterialSpring Spring { get => GetValue(SpringProperty); set => SetValue(SpringProperty, value); }
    private double _extent = 56;
    private double _iconExtent = 24;
    public double Extent => _extent;
    public double IconExtent => _iconExtent;
    private readonly MaterialFrameLease _frames;
    private MaterialSpring _activeSpring = new(1, 1400);
    private double _toExtent = 56, _toIcon = 24;
    private double _fromExtent;
    private double _fromIcon;
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
        if (e.Property != TargetExtentProperty && e.Property != TargetIconSizeProperty && e.Property != SpringProperty) return;
        if (_frames is null) return;
        if (_frames.IsRunning) _frames.Sample();
        if (!_attached || Spring.IsInstant || (Extent == TargetExtent && IconExtent == TargetIconSize)) { Snap(); return; }
        _fromExtent = Extent;
        _fromIcon = IconExtent;
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
        SetAndRaise(ExtentProperty, ref _extent, _toExtent);
        SetAndRaise(IconExtentProperty, ref _iconExtent, _toIcon);
    }
    private void Stop() => _frames?.SetRunning(false);
    private bool Advance(MaterialFrame frame)
    {
        var seconds = frame.Elapsed.TotalSeconds;
        var progress = MaterialSpringResponse.Evaluate(seconds, _activeSpring);
        if (!double.IsFinite(progress)) { Complete(); return false; }
        // Clamp the UI projection, keeping positive targets even with underdamped host overrides.
        var extent = _fromExtent + (_toExtent - _fromExtent) * Math.Clamp(progress, 0, 1);
        var icon = _fromIcon + (_toIcon - _fromIcon) * Math.Clamp(progress, 0, 1);
        if (seconds >= 10 || (Math.Abs(extent - _toExtent) < 0.01 && Math.Abs(icon - _toIcon) < 0.01)) { Complete(); return false; }
        SetAndRaise(ExtentProperty, ref _extent, extent);
        SetAndRaise(IconExtentProperty, ref _iconExtent, icon);
        return true;
    }
}
