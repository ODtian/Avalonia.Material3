using System.Windows.Input;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Material3.Controls;

/// <summary>Reusable pull gesture and feedback. The host performs work and reports Status/ResultMessage.</summary>
public sealed class MaterialPullToRefresh : ContentControl
{
    public static readonly StyledProperty<MaterialProgressStatus> StatusProperty = AvaloniaProperty.Register<MaterialPullToRefresh, MaterialProgressStatus>(nameof(Status), MaterialProgressStatus.Idle, defaultBindingMode: BindingMode.TwoWay, validate: Enum.IsDefined);
    public static readonly StyledProperty<string?> ResultMessageProperty = AvaloniaProperty.Register<MaterialPullToRefresh, string?>(nameof(ResultMessage));
    public static readonly StyledProperty<double> ThresholdProperty = AvaloniaProperty.Register<MaterialPullToRefresh, double>(nameof(Threshold), 80, validate: v => double.IsFinite(v) && v > 0);
    public static readonly StyledProperty<bool> IsAtStartProperty = AvaloniaProperty.Register<MaterialPullToRefresh, bool>(nameof(IsAtStart), true);
    public static readonly StyledProperty<bool> IsExpressiveProperty = AvaloniaProperty.Register<MaterialPullToRefresh, bool>(nameof(IsExpressive));
    public static readonly StyledProperty<ICommand?> RefreshCommandProperty = AvaloniaProperty.Register<MaterialPullToRefresh, ICommand?>(nameof(RefreshCommand));
    public static readonly StyledProperty<object?> CommandParameterProperty = AvaloniaProperty.Register<MaterialPullToRefresh, object?>(nameof(CommandParameter));
    public static readonly DirectProperty<MaterialPullToRefresh, double> DistanceFractionProperty = AvaloniaProperty.RegisterDirect<MaterialPullToRefresh, double>(nameof(DistanceFraction), c => c.DistanceFraction);
    public static readonly DirectProperty<MaterialPullToRefresh, bool> IsArmedProperty = AvaloniaProperty.RegisterDirect<MaterialPullToRefresh, bool>(nameof(IsArmed), c => c.IsArmed);
    public static readonly DirectProperty<MaterialPullToRefresh, bool> IsPullingProperty = AvaloniaProperty.RegisterDirect<MaterialPullToRefresh, bool>(nameof(IsPulling), c => c.IsPulling);
    public static readonly DirectProperty<MaterialPullToRefresh, string> StatusDescriptionProperty = AvaloniaProperty.RegisterDirect<MaterialPullToRefresh, string>(nameof(StatusDescription), c => c.StatusDescription);
    private IPointer? _pointer;
    private Point _start;
    private double _distance;
    private bool _armed, _pulling;
    private string _description = "Pull to refresh";
    internal event Action? FeedbackChanged;
    public MaterialPullToRefresh()
    {
        AddHandler(PointerPressedEvent, Pressed, RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, Moved, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, Released, RoutingStrategies.Tunnel);
    }
    static MaterialPullToRefresh() => FocusableProperty.OverrideDefaultValue<MaterialPullToRefresh>(true);
    public MaterialProgressStatus Status { get => GetValue(StatusProperty); set => SetValue(StatusProperty, value); }
    public string? ResultMessage { get => GetValue(ResultMessageProperty); set => SetValue(ResultMessageProperty, value); }
    public double Threshold { get => GetValue(ThresholdProperty); set => SetValue(ThresholdProperty, value); }
    /// <summary>Host boundary for arbitrary content; a nested ScrollViewer is also checked at gesture start.</summary>
    public bool IsAtStart { get => GetValue(IsAtStartProperty); set => SetValue(IsAtStartProperty, value); }
    public bool IsExpressive { get => GetValue(IsExpressiveProperty); set => SetValue(IsExpressiveProperty, value); }
    public ICommand? RefreshCommand { get => GetValue(RefreshCommandProperty); set => SetValue(RefreshCommandProperty, value); }
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }
    public double DistanceFraction => _distance;
    public bool IsArmed => _armed;
    public bool IsPulling => _pulling;
    public string StatusDescription => _description;
    public event EventHandler? RefreshRequested;
    protected override AutomationPeer OnCreateAutomationPeer() => new RefreshAutomationPeer(this);
    private bool Busy => Status is MaterialProgressStatus.Running or MaterialProgressStatus.Paused;
    private bool CanRequest => IsEffectivelyEnabled && !Busy && (RefreshCommand?.CanExecute(CommandParameter) ?? true);
    /// <summary>Keyboard/automation/host equivalent of a completed pull, without running a task in the library.</summary>
    public bool RequestRefresh()
    {
        if (!CanRequest) return false;
        CancelPull();
        SetCurrentValue(StatusProperty, MaterialProgressStatus.Running);
        RefreshRequested?.Invoke(this, EventArgs.Empty);
        RefreshCommand?.Execute(CommandParameter);
        return true;
    }
    private void Pressed(object? sender, PointerPressedEventArgs e)
    {
        if (!CanRequest || !IsAtStart || _pointer is not null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || MaterialCarousel.IsNestedInteractive(e.Source)) return;
        var source = e.Source as Visual;
        if (source?.GetSelfAndVisualAncestors().OfType<ScrollViewer>().FirstOrDefault() is { Offset.Y: > 0 }) return;
        _pointer = e.Pointer;
        _start = e.GetPosition(this);
    }
    private void Moved(object? sender, PointerEventArgs e)
    {
        if (e.Pointer != _pointer) return;
        if (!CanRequest || !IsAtStart) { CancelPull(); return; }
        var delta = e.GetPosition(this) - _start;
        if (!_pulling)
        {
            if (Math.Abs(delta.X) > 12 && Math.Abs(delta.X) > Math.Abs(delta.Y)) { CancelPull(); return; }
            if (delta.Y < 12 || delta.Y < Math.Abs(delta.X) * 1.5) return;
            SetAndRaise(IsPullingProperty, ref _pulling, true);
            e.Pointer.Capture(this);
            Focus();
        }
        var adjusted = Math.Max(0, delta.Y) * .5;
        var fraction = adjusted / Threshold;
        var tension = Math.Clamp(fraction - 1, 0, 2);
        SetDistance(fraction <= 1 ? fraction : 1 + tension - tension * tension / 4);
        SetAndRaise(IsArmedProperty, ref _armed, adjusted > Threshold);
        Describe();
        e.Handled = true;
    }
    private void Released(object? sender, PointerReleasedEventArgs e)
    {
        if (e.Pointer != _pointer) return;
        var trigger = _pulling && _armed;
        var consumed = _pulling;
        CancelPull();
        if (trigger) RequestRefresh();
        e.Handled = consumed;
    }
    private void SetDistance(double value)
    {
        SetAndRaise(DistanceFractionProperty, ref _distance, value);
        FeedbackChanged?.Invoke();
    }
    private void CancelPull()
    {
        var pointer = _pointer;
        _pointer = null;
        SetAndRaise(IsPullingProperty, ref _pulling, false);
        SetAndRaise(IsArmedProperty, ref _armed, false);
        if (pointer?.Captured == this) pointer.Capture(null);
        SetDistance(Busy ? 1 : 0);
        Describe();
    }
    private void Describe()
    {
        SetAndRaise(StatusDescriptionProperty, ref _description, Busy ? Status == MaterialProgressStatus.Paused ? "Refresh paused" : "Refreshing"
            : Status is MaterialProgressStatus.Completed or MaterialProgressStatus.Failed ? $"{Status}: {ResultMessage ?? (Status == MaterialProgressStatus.Completed ? "Content updated" : "Refresh failed; retry available")}"
            : _armed ? "Release to refresh" : "Pull to refresh");
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == StatusProperty || change.Property == IsEffectivelyEnabledProperty || change.Property == IsAtStartProperty || change.Property == ContentProperty || change.Property == ThresholdProperty)
            CancelPull();
        if (change.Property == ResultMessageProperty || change.Property == StatusProperty)
        {
            Describe();
            PseudoClasses.Set(":result", Status is MaterialProgressStatus.Completed or MaterialProgressStatus.Failed);
            PseudoClasses.Set(":error", Status == MaterialProgressStatus.Failed);
        }
        FeedbackChanged?.Invoke();
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape && _pointer is not null) { CancelPull(); e.Handled = true; }
        else if (e.Key == Key.F5 && IsEffectivelyEnabled) { RequestRefresh(); e.Handled = true; }
    }
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e) { if (e.Source == this) CancelPull(); base.OnPointerCaptureLost(e); }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) { CancelPull(); base.OnDetachedFromVisualTree(e); }
}
