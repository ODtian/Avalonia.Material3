using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Material3.Tokens;
using Avalonia.Threading;

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
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private readonly Stopwatch _elapsed = new();
    private double _fromExtent;
    private double _fromIcon;
    private bool _attached;
    protected override Type StyleKeyOverride => typeof(Decorator);
    public MaterialFabGeometryPresenter() => _timer.Tick += (_, _) => Advance();
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
        if (!_attached || Spring.IsInstant) { Snap(); return; }
        if (_timer.IsEnabled) Advance();
        _fromExtent = Extent;
        _fromIcon = IconExtent;
        _elapsed.Restart();
        _timer.Start();
    }
    private void Snap()
    {
        Stop();
        SetAndRaise(ExtentProperty, ref _extent, TargetExtent);
        SetAndRaise(IconExtentProperty, ref _iconExtent, TargetIconSize);
    }
    private void Stop() { _timer.Stop(); _elapsed.Stop(); }
    private void Advance()
    {
        var seconds = _elapsed.Elapsed.TotalSeconds;
        var progress = Response(seconds, Spring);
        // Clamp the UI projection, keeping positive targets even with underdamped host overrides.
        var extent = _fromExtent + (TargetExtent - _fromExtent) * Math.Clamp(progress, 0, 1);
        var icon = _fromIcon + (TargetIconSize - _fromIcon) * Math.Clamp(progress, 0, 1);
        if (seconds >= 10 || (Math.Abs(extent - TargetExtent) < 0.01 && Math.Abs(icon - TargetIconSize) < 0.01)) { Snap(); return; }
        SetAndRaise(ExtentProperty, ref _extent, extent);
        SetAndRaise(IconExtentProperty, ref _iconExtent, icon);
    }
    private static double Response(double seconds, MaterialSpring spring)
    {
        var omega = Math.Sqrt(spring.Stiffness);
        var damping = spring.DampingRatio;
        if (Math.Abs(damping - 1) < 1e-7) return 1 - (1 + omega * seconds) * Math.Exp(-omega * seconds);
        if (damping < 1)
        {
            var root = Math.Sqrt(1 - damping * damping);
            return 1 - Math.Exp(-damping * omega * seconds) * (Math.Cos(omega * root * seconds) + damping / root * Math.Sin(omega * root * seconds));
        }
        var ratio = Math.Sqrt(damping * damping - 1);
        var first = -omega * (damping - ratio);
        var second = -omega * (damping + ratio);
        return 1 + (second * Math.Exp(first * seconds) - first * Math.Exp(second * seconds)) / (first - second);
    }
}
