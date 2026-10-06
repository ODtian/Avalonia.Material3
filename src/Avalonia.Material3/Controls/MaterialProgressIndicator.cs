using System.Diagnostics;
using System.Globalization;
using Avalonia.Automation.Peers;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.Threading;

namespace Avalonia.Material3.Controls;

/// <summary>The host sets activity and terminal outcomes; reaching Value=1 never completes a task implicitly.</summary>
public enum MaterialProgressStatus { Idle, Running, Paused, Completed, Failed }

/// <summary>Host-owned feedback. The only timer renders frames; no tasks are executed or scheduled.</summary>
public abstract class MaterialProgressIndicator : TemplatedControl
{
    public static readonly StyledProperty<double> ValueProperty = AvaloniaProperty.Register<MaterialProgressIndicator, double>(
        nameof(Value), coerce: (_, value) => double.IsNaN(value) ? 0 : Math.Clamp(value, 0, 1));
    public static readonly StyledProperty<bool> IsExpressiveProperty = AvaloniaProperty.Register<MaterialProgressIndicator, bool>(nameof(IsExpressive));
    public static readonly StyledProperty<bool> IsIndeterminateProperty = AvaloniaProperty.Register<MaterialProgressIndicator, bool>(nameof(IsIndeterminate));
    public static readonly StyledProperty<TimeSpan?> AnimationTimeProperty = AvaloniaProperty.Register<MaterialProgressIndicator, TimeSpan?>(
        nameof(AnimationTime), validate: value => value is null || value >= TimeSpan.Zero);
    public static readonly StyledProperty<MaterialProgressStatus> StatusProperty = AvaloniaProperty.Register<MaterialProgressIndicator, MaterialProgressStatus>(
        nameof(Status), MaterialProgressStatus.Running, validate: Enum.IsDefined);
    public static readonly StyledProperty<string?> ResultMessageProperty = AvaloniaProperty.Register<MaterialProgressIndicator, string?>(nameof(ResultMessage));
    public static readonly StyledProperty<IBrush?> TrackBrushProperty = AvaloniaProperty.Register<MaterialProgressIndicator, IBrush?>(nameof(TrackBrush));
    public static readonly StyledProperty<IBrush?> ErrorBrushProperty = AvaloniaProperty.Register<MaterialProgressIndicator, IBrush?>(nameof(ErrorBrush));
    public static readonly StyledProperty<MaterialSpring> MotionSpringProperty = AvaloniaProperty.Register<MaterialProgressIndicator, MaterialSpring>(
        nameof(MotionSpring), new(0.8, 380), validate: spring => spring is { IsValid: true });
    public static readonly StyledProperty<TimeSpan> MotionDurationProperty = AvaloniaProperty.Register<MaterialProgressIndicator, TimeSpan>(
        nameof(MotionDuration), TimeSpan.FromMilliseconds(500), validate: duration => duration >= TimeSpan.Zero);
    public static readonly StyledProperty<IEasing> AccelerateEasingProperty = AvaloniaProperty.Register<MaterialProgressIndicator, IEasing>(
        nameof(AccelerateEasing), new SplineEasing(.3, 0, .8, .15), validate: easing => easing is not null);
    public static readonly StyledProperty<IEasing> DecelerateEasingProperty = AvaloniaProperty.Register<MaterialProgressIndicator, IEasing>(
        nameof(DecelerateEasing), new SplineEasing(.05, .7, .1, 1), validate: easing => easing is not null);
    public static readonly StyledProperty<IEasing> ProgressEasingProperty = AvaloniaProperty.Register<MaterialProgressIndicator, IEasing>(
        nameof(ProgressEasing), new SplineEasing(.2, 0, 0, 1), validate: easing => easing is not null);
    public static readonly DirectProperty<MaterialProgressIndicator, string> StatusDescriptionProperty = AvaloniaProperty.RegisterDirect<MaterialProgressIndicator, string>(nameof(StatusDescription), control => control.StatusDescription);
    public static readonly DirectProperty<MaterialProgressIndicator, double> GraphicWidthProperty = AvaloniaProperty.RegisterDirect<MaterialProgressIndicator, double>(nameof(GraphicWidth), control => control.GraphicWidth);
    public static readonly DirectProperty<MaterialProgressIndicator, double> GraphicHeightProperty = AvaloniaProperty.RegisterDirect<MaterialProgressIndicator, double>(nameof(GraphicHeight), control => control.GraphicHeight);

    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private readonly Stopwatch _clock = new();
    private double _amplitude;
    private double _amplitudeFrom;
    private double _amplitudeTarget;
    private double _amplitudeStart;
    private double _lastTime;
    private double _elapsed;
    private bool _attached;
    private string _description = "Running, 0%";
    protected MaterialProgressIndicator()
    {
        _timer.Tick += (_, _) => Advance(_clock.Elapsed.TotalSeconds);
        RefreshDescription();
    }
    public double Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public bool IsExpressive { get => GetValue(IsExpressiveProperty); set => SetValue(IsExpressiveProperty, value); }
    public bool IsIndeterminate { get => GetValue(IsIndeterminateProperty); set => SetValue(IsIndeterminateProperty, value); }
    /// <summary>Null uses attached UI frame time. Non-null supplies absolute time; rewind resets the cycle. Paused time is excluded.</summary>
    public TimeSpan? AnimationTime { get => GetValue(AnimationTimeProperty); set => SetValue(AnimationTimeProperty, value); }
    public MaterialProgressStatus Status { get => GetValue(StatusProperty); set => SetValue(StatusProperty, value); }
    public string? ResultMessage { get => GetValue(ResultMessageProperty); set => SetValue(ResultMessageProperty, value); }
    public IBrush? TrackBrush { get => GetValue(TrackBrushProperty); set => SetValue(TrackBrushProperty, value); }
    public IBrush? ErrorBrush { get => GetValue(ErrorBrushProperty); set => SetValue(ErrorBrushProperty, value); }
    public MaterialSpring MotionSpring { get => GetValue(MotionSpringProperty); set => SetValue(MotionSpringProperty, value); }
    public TimeSpan MotionDuration { get => GetValue(MotionDurationProperty); set => SetValue(MotionDurationProperty, value); }
    public IEasing AccelerateEasing { get => GetValue(AccelerateEasingProperty); set => SetValue(AccelerateEasingProperty, value); }
    public IEasing DecelerateEasing { get => GetValue(DecelerateEasingProperty); set => SetValue(DecelerateEasingProperty, value); }
    public IEasing ProgressEasing { get => GetValue(ProgressEasingProperty); set => SetValue(ProgressEasingProperty, value); }
    public string StatusDescription => _description;
    public virtual double GraphicWidth => double.NaN;
    public virtual double GraphicHeight => IsTerminal ? 24 : IsExpressive ? 10 : 4;
    internal bool IsTerminal => Status is MaterialProgressStatus.Completed or MaterialProgressStatus.Failed;
    internal double EffectiveValue => Status == MaterialProgressStatus.Completed ? 1 : Status == MaterialProgressStatus.Idle ? 0 : Value;
    internal bool EffectiveIndeterminate => IsIndeterminate && !IsTerminal && Status != MaterialProgressStatus.Idle;
    internal bool ReducedMotion => MotionSpring.IsInstant || MotionDuration == TimeSpan.Zero;
    internal double Elapsed => ReducedMotion ? 0 : _elapsed;
    internal double WaveAmplitude => _amplitude;
    protected virtual bool HasAnimatedFeedback => EffectiveIndeterminate || IsExpressive && (_amplitude > 0 || _amplitudeTarget > 0);
    private bool ShouldTick => _attached && IsVisible && IsEffectivelyEnabled && Status == MaterialProgressStatus.Running && !ReducedMotion && HasAnimatedFeedback;
    private bool CanAnimate => ShouldTick && IsEffectivelyVisible;
    internal event Action? FrameChanged;
    protected override AutomationPeer OnCreateAutomationPeer() => new MaterialProgressAutomationPeer(this);

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs args)
    {
        base.OnAttachedToVisualTree(args);
        _attached = true;
        _amplitude = _amplitudeFrom = _amplitudeTarget;
        _clock.Restart();
        _lastTime = AnimationTime?.TotalSeconds ?? 0;
        RefreshDescription();
        UpdateClock();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs args)
    {
        _attached = false;
        _timer.Stop();
        _clock.Stop();
        base.OnDetachedFromVisualTree(args);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == AnimationTimeProperty)
        {
            if (change.OldValue is TimeSpan && change.NewValue is TimeSpan time) Advance(time.TotalSeconds);
            else _lastTime = AnimationTime?.TotalSeconds ?? _clock.Elapsed.TotalSeconds;
        }
        if (change.Property == StatusProperty && Status == MaterialProgressStatus.Running && change.OldValue is MaterialProgressStatus old && old != MaterialProgressStatus.Paused)
            _elapsed = 0;
        if (change.Property == StatusProperty || change.Property == IsVisibleProperty || change.Property == IsEffectivelyEnabledProperty
            || change.Property == MotionSpringProperty || change.Property == MotionDurationProperty
            || change.Property == IsIndeterminateProperty || change.Property == IsExpressiveProperty)
            _lastTime = AnimationTime?.TotalSeconds ?? _clock.Elapsed.TotalSeconds;
        if (ReducedMotion) _elapsed = 0;
        var amplitudeTarget = IsExpressive && (EffectiveIndeterminate || EffectiveValue is > .1 and < .95) ? 1d : 0d;
        if (amplitudeTarget != _amplitudeTarget)
        {
            _amplitudeFrom = _amplitude;
            _amplitudeTarget = amplitudeTarget;
            _amplitudeStart = _elapsed;
        }
        UpdateAmplitude();
        if (change.Property == ValueProperty || change.Property == StatusProperty || change.Property == IsIndeterminateProperty || change.Property == ResultMessageProperty)
            RefreshDescription();
        if (change.Property == StatusProperty || change.Property == IsExpressiveProperty)
        {
            RaisePropertyChanged(GraphicHeightProperty, 0, GraphicHeight);
            RaisePropertyChanged(GraphicWidthProperty, 0, GraphicWidth);
        }
        PseudoClasses.Set(":expressive", IsExpressive);
        PseudoClasses.Set(":result", IsTerminal);
        PseudoClasses.Set(":paused", Status == MaterialProgressStatus.Paused);
        PseudoClasses.Set(":idle", Status == MaterialProgressStatus.Idle);
        UpdateClock();
        FrameChanged?.Invoke();
    }
    private void UpdateClock()
    {
        if (AnimationTime is null && ShouldTick) _timer.Start(); else _timer.Stop();
    }
    private void Advance(double time)
    {
        if (time < _lastTime)
        {
            _elapsed = _amplitudeStart = 0;
            _amplitude = _amplitudeFrom = _amplitudeTarget;
        }
        else if (CanAnimate) _elapsed += time - _lastTime;
        _lastTime = time;
        UpdateAmplitude();
        UpdateClock();
        FrameChanged?.Invoke();
    }
    private void RefreshDescription()
    {
        var description = IsTerminal
            ? Status + (string.IsNullOrWhiteSpace(ResultMessage) ? string.Empty : ": " + ResultMessage)
            : Status + (EffectiveIndeterminate ? ", indeterminate" : ", " + (EffectiveValue * 100).ToString("0", CultureInfo.CurrentCulture) + "%");
        SetAndRaise(StatusDescriptionProperty, ref _description, description);
    }
    private void UpdateAmplitude()
    {
        if (!_attached || ReducedMotion) { _amplitude = _amplitudeTarget; return; }
        var fraction = Math.Clamp((_elapsed - _amplitudeStart) / MotionDuration.TotalSeconds, 0, 1);
        var easing = _amplitudeTarget > _amplitudeFrom ? ProgressEasing : AccelerateEasing;
        _amplitude = _amplitudeFrom + (_amplitudeTarget - _amplitudeFrom) * easing.Ease(fraction);
    }
}

/// <summary>Material linear progress with rounded ends, a separated track and a contrast stop.</summary>
public sealed class MaterialLinearProgressIndicator : MaterialProgressIndicator { }

/// <summary>Material circular progress; determinate starts at twelve o'clock.</summary>
public sealed class MaterialCircularProgressIndicator : MaterialProgressIndicator
{
    public override double GraphicWidth => IsExpressive ? 48 : 40;
    public override double GraphicHeight => IsTerminal ? 24 : IsExpressive ? 48 : 40;
}

/// <summary>Expressive seven-shape cycle or progress-driven circle-to-soft-burst morph.</summary>
public sealed class MaterialLoadingIndicator : MaterialProgressIndicator
{
    public static readonly StyledProperty<bool> IsContainedProperty = AvaloniaProperty.Register<MaterialLoadingIndicator, bool>(nameof(IsContained));
    public static readonly StyledProperty<IBrush?> ContainerBrushProperty = AvaloniaProperty.Register<MaterialLoadingIndicator, IBrush?>(nameof(ContainerBrush));
    public static readonly StyledProperty<IBrush?> ContainedForegroundProperty = AvaloniaProperty.Register<MaterialLoadingIndicator, IBrush?>(nameof(ContainedForeground));
    public static readonly StyledProperty<CornerRadius> ContainerCornerRadiusProperty = AvaloniaProperty.Register<MaterialLoadingIndicator, CornerRadius>(nameof(ContainerCornerRadius), new CornerRadius(9999),
        validate: radius => new[] { radius.TopLeft, radius.TopRight, radius.BottomLeft, radius.BottomRight }.All(value => double.IsFinite(value) && value >= 0));
    static MaterialLoadingIndicator() => IsIndeterminateProperty.OverrideDefaultValue<MaterialLoadingIndicator>(true);
    public bool IsContained { get => GetValue(IsContainedProperty); set => SetValue(IsContainedProperty, value); }
    public IBrush? ContainerBrush { get => GetValue(ContainerBrushProperty); set => SetValue(ContainerBrushProperty, value); }
    public IBrush? ContainedForeground { get => GetValue(ContainedForegroundProperty); set => SetValue(ContainedForegroundProperty, value); }
    public CornerRadius ContainerCornerRadius { get => GetValue(ContainerCornerRadiusProperty); set => SetValue(ContainerCornerRadiusProperty, value); }
    public override double GraphicWidth => 48;
    public override double GraphicHeight => IsTerminal ? 24 : 48;
}
