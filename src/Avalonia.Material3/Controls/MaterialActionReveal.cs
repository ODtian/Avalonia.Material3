using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Material3.Tokens;
using Avalonia.Threading;

namespace Avalonia.Material3.Controls;

/// <summary>Private bounded spring projection of layout extent and opacity, with immediate input exclusion on collapse.</summary>
internal sealed class MaterialActionReveal : Decorator
{
    public static readonly StyledProperty<bool> IsExpandedProperty = AvaloniaProperty.Register<MaterialActionReveal, bool>(nameof(IsExpanded), true);
    public static readonly StyledProperty<Orientation> OrientationProperty = AvaloniaProperty.Register<MaterialActionReveal, Orientation>(nameof(Orientation));
    public static readonly StyledProperty<MaterialSpring> SpatialSpringProperty = AvaloniaProperty.Register<MaterialActionReveal, MaterialSpring>(nameof(SpatialSpring), new(1, 1400), validate: value => value is { IsValid: true });
    public static readonly StyledProperty<MaterialSpring> EffectsSpringProperty = AvaloniaProperty.Register<MaterialActionReveal, MaterialSpring>(nameof(EffectsSpring), new(1, 3800), validate: value => value is { IsValid: true });
    public bool IsExpanded { get => GetValue(IsExpandedProperty); set => SetValue(IsExpandedProperty, value); }
    public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }
    public MaterialSpring SpatialSpring { get => GetValue(SpatialSpringProperty); set => SetValue(SpatialSpringProperty, value); }
    public MaterialSpring EffectsSpring { get => GetValue(EffectsSpringProperty); set => SetValue(EffectsSpringProperty, value); }
    protected override Type StyleKeyOverride => typeof(Decorator);
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private readonly Stopwatch _elapsed = new();
    private bool _attached;
    private double _extent = 1;
    private double _alpha = 1;
    private double _fromExtent;
    private double _fromAlpha;
    private Size _fullSize;
    public MaterialActionReveal()
    {
        ClipToBounds = true;
        _timer.Tick += (_, _) => Advance();
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
        _timer.Stop();
        _elapsed.Stop();
        base.OnDetachedFromVisualTree(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsExpandedProperty || change.Property == SpatialSpringProperty || change.Property == EffectsSpringProperty)
        {
            // Closing actions cannot be invoked or focused while their visual exit is still playing.
            IsEnabled = IsExpanded;
            IsHitTestVisible = IsExpanded;
            if (!_attached || (SpatialSpring.IsInstant && EffectsSpring.IsInstant)) Snap();
            else
            {
                if (_timer.IsEnabled) Advance();
                _fromExtent = _extent;
                _fromAlpha = _alpha;
                IsVisible = true;
                _elapsed.Restart();
                _timer.Start();
            }
        }
        if (change.Property == OrientationProperty) InvalidateMeasure();
    }
    private void Snap()
    {
        _timer.Stop();
        _elapsed.Stop();
        _extent = _alpha = IsExpanded ? 1 : 0;
        Opacity = _alpha;
        IsVisible = IsExpanded;
        IsEnabled = IsHitTestVisible = IsExpanded;
        InvalidateMeasure();
    }
    private void Advance()
    {
        var target = IsExpanded ? 1.0 : 0.0;
        var seconds = _elapsed.Elapsed.TotalSeconds;
        static double Mix(double from, double to, double fraction) => Math.Clamp(from + (to - from) * fraction, 0, 1);
        _extent = Mix(_fromExtent, target, Response(seconds, SpatialSpring));
        _alpha = Mix(_fromAlpha, target, Response(seconds, EffectsSpring));
        // A bounded UI projection settles within one thousandth of its extent; no unbounded timer survives detach.
        if (seconds >= 10 || (Math.Abs(_extent - target) < 0.001 && Math.Abs(_alpha - target) < 0.001)) { Snap(); return; }
        Opacity = _alpha;
        InvalidateMeasure();
    }
    private static double Response(double time, MaterialSpring spring)
    {
        if (spring.IsInstant) return 1;
        var omega = Math.Sqrt(spring.Stiffness);
        var damping = spring.DampingRatio;
        if (Math.Abs(damping - 1) < 1e-7) return 1 - (1 + omega * time) * Math.Exp(-omega * time);
        if (damping < 1)
        {
            var root = Math.Sqrt(1 - damping * damping);
            return 1 - Math.Exp(-damping * omega * time) * (Math.Cos(omega * root * time) + damping / root * Math.Sin(omega * root * time));
        }
        var ratio = Math.Sqrt(damping * damping - 1);
        var first = -omega * (damping - ratio);
        var second = -omega * (damping + ratio);
        return 1 + (second * Math.Exp(first * time) - first * Math.Exp(second * time)) / (first - second);
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        Child?.Measure(availableSize);
        _fullSize = Child?.DesiredSize ?? default;
        return Orientation == Orientation.Horizontal ? new Size(_fullSize.Width * _extent, _fullSize.Height) : new Size(_fullSize.Width, _fullSize.Height * _extent);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        Child?.Arrange(new Rect(0, 0, Orientation == Orientation.Horizontal ? Math.Max(finalSize.Width, _fullSize.Width) : finalSize.Width,
            Orientation == Orientation.Vertical ? Math.Max(finalSize.Height, _fullSize.Height) : finalSize.Height));
        return finalSize;
    }
}
