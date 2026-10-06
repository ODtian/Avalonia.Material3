using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Templates;
using Avalonia.Automation.Peers;
using Avalonia.Material3.Tokens;
using System.Diagnostics;

namespace Avalonia.Material3.Controls;

public enum MaterialSheetState { Hidden, PartiallyExpanded, Expanded }
public enum MaterialSheetEdge { Start, End }
public sealed record MaterialSheetDetent(MaterialSheetState State, double Offset);
public sealed class MaterialSheetStateChangingEventArgs(MaterialSheetState state) : EventArgs
{
    public MaterialSheetState State { get; } = state;
    public bool Cancel { get; set; }
}

/// <summary>A bounded Material information surface. Use MaterialSheetHost for standard layout.</summary>
[TemplatePart("PART_BodyScroll", typeof(ScrollViewer))]
[PseudoClasses(":modal", ":side", ":left", ":detached", ":hidden", ":dragging", ":compact-height", ":empty-extent")]
public abstract class MaterialSheet : ContentControl
{
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<MaterialSheet, string?>(nameof(Title));
    public static readonly StyledProperty<object?> HeaderProperty = AvaloniaProperty.Register<MaterialSheet, object?>(nameof(Header));
    public static readonly StyledProperty<IDataTemplate?> HeaderTemplateProperty = AvaloniaProperty.Register<MaterialSheet, IDataTemplate?>(nameof(HeaderTemplate));
    public static readonly StyledProperty<object?> ActionsProperty = AvaloniaProperty.Register<MaterialSheet, object?>(nameof(Actions));
    public static readonly StyledProperty<IDataTemplate?> ActionsTemplateProperty = AvaloniaProperty.Register<MaterialSheet, IDataTemplate?>(nameof(ActionsTemplate));
    public static readonly StyledProperty<string> HandleTextProperty = AvaloniaProperty.Register<MaterialSheet, string>(nameof(HandleText), "Resize information panel", validate: value => !string.IsNullOrWhiteSpace(value));
    public static readonly StyledProperty<string> ExpandTextProperty = AvaloniaProperty.Register<MaterialSheet, string>(nameof(ExpandText), "Expand", validate: value => !string.IsNullOrWhiteSpace(value));
    public static readonly StyledProperty<string> CollapseTextProperty = AvaloniaProperty.Register<MaterialSheet, string>(nameof(CollapseText), "Collapse", validate: value => !string.IsNullOrWhiteSpace(value));
    public static readonly StyledProperty<string> DismissTextProperty = AvaloniaProperty.Register<MaterialSheet, string>(nameof(DismissText), "Dismiss", validate: value => !string.IsNullOrWhiteSpace(value));
    public static readonly StyledProperty<double> ExpandedExtentProperty =
        AvaloniaProperty.Register<MaterialSheet, double>(nameof(ExpandedExtent), double.NaN, validate: value => double.IsNaN(value) || double.IsFinite(value) && value >= 0);
    public static readonly StyledProperty<double> PeekExtentProperty =
        AvaloniaProperty.Register<MaterialSheet, double>(nameof(PeekExtent), 56, validate: value => double.IsFinite(value) && value >= 0);
    public static readonly StyledProperty<bool> IsPartialEnabledProperty =
        AvaloniaProperty.Register<MaterialSheet, bool>(nameof(IsPartialEnabled), true);
    public static readonly StyledProperty<bool> AllowDismissProperty =
        AvaloniaProperty.Register<MaterialSheet, bool>(nameof(AllowDismiss));
    public static readonly StyledProperty<double> MinimumExtentProperty =
        AvaloniaProperty.Register<MaterialSheet, double>(nameof(MinimumExtent), 56, validate: value => double.IsFinite(value) && value >= 0);
    public static readonly StyledProperty<double> MaximumExtentProperty =
        AvaloniaProperty.Register<MaterialSheet, double>(nameof(MaximumExtent), double.PositiveInfinity, validate: value => !double.IsNaN(value) && value >= 0);
    public static readonly StyledProperty<Size?> AvailableSizeProperty =
        AvaloniaProperty.Register<MaterialSheet, Size?>(nameof(AvailableSize), validate: IsValidSize);
    public static readonly StyledProperty<MaterialSheetEdge> EdgeProperty =
        AvaloniaProperty.Register<MaterialSheet, MaterialSheetEdge>(nameof(Edge), MaterialSheetEdge.End, validate: Enum.IsDefined);
    public static readonly StyledProperty<bool> IsDetachedProperty =
        AvaloniaProperty.Register<MaterialSheet, bool>(nameof(IsDetached));
    public static readonly StyledProperty<bool> IsDraggableProperty =
        AvaloniaProperty.Register<MaterialSheet, bool>(nameof(IsDraggable), true);
    public static readonly StyledProperty<ScrollViewer?> ScrollSourceProperty =
        AvaloniaProperty.Register<MaterialSheet, ScrollViewer?>(nameof(ScrollSource));
    public static readonly StyledProperty<double> DragThresholdProperty =
        AvaloniaProperty.Register<MaterialSheet, double>(nameof(DragThreshold), 56, validate: value => double.IsFinite(value) && value > 0);
    public static readonly StyledProperty<double> VelocityThresholdProperty =
        AvaloniaProperty.Register<MaterialSheet, double>(nameof(VelocityThreshold), 125, validate: value => double.IsFinite(value) && value > 0);
    public static readonly DirectProperty<MaterialSheet, bool> IsDraggingProperty =
        AvaloniaProperty.RegisterDirect<MaterialSheet, bool>(nameof(IsDragging), sheet => sheet.IsDragging);
    public static readonly StyledProperty<MaterialSpring> SpatialSpringProperty =
        AvaloniaProperty.Register<MaterialSheet, MaterialSpring>(nameof(SpatialSpring), MaterialSpringScheme.Expressive.DefaultSpatial, validate: spring => spring is { IsValid: true });
    public static readonly DirectProperty<MaterialSheet, bool> IsSettlingProperty =
        AvaloniaProperty.RegisterDirect<MaterialSheet, bool>(nameof(IsSettling), sheet => sheet.IsSettling);
    public static readonly DirectProperty<MaterialSheet, bool> IsModalProperty =
        AvaloniaProperty.RegisterDirect<MaterialSheet, bool>(nameof(IsModal), sheet => sheet.IsModal);
    public static readonly DirectProperty<MaterialSheet, IReadOnlyList<MaterialSheetDetent>> DetentsProperty =
        AvaloniaProperty.RegisterDirect<MaterialSheet, IReadOnlyList<MaterialSheetDetent>>(nameof(Detents), sheet => sheet.Detents);
    public static readonly DirectProperty<MaterialSheet, MaterialSheetState> StateProperty =
        AvaloniaProperty.RegisterDirect<MaterialSheet, MaterialSheetState>(nameof(State), sheet => sheet.State);
    public static readonly DirectProperty<MaterialSheet, double> VisibleExtentProperty =
        AvaloniaProperty.RegisterDirect<MaterialSheet, double>(nameof(VisibleExtent), sheet => sheet.VisibleExtent);
    public static readonly DirectProperty<MaterialSheet, double> OffsetProperty =
        AvaloniaProperty.RegisterDirect<MaterialSheet, double>(nameof(Offset), sheet => sheet.Offset);
    private double _visibleExtent;
    private double _offset;
    private MaterialSheetState _state = MaterialSheetState.PartiallyExpanded;
    private bool _changing;
    private bool _isDragging;
    private IPointer? _pointer;
    private Point _pressPoint;
    private Point _lastPoint;
    private double _dragExtent;
    private double _expanded;
    private double _partial;
    private double _available;
    private double _sheetTravel, _velocity;
    private ulong _lastTimestamp;
    private MaterialSheetDragHandle? _pressedHandle;
    private bool _releasing;
    private bool _bodyGesture;
    private ScrollViewer? _bodyScroll;
    private ScrollViewer? _gestureScroll;
    private bool _isSettling;
    private readonly DispatcherTimer _motionTimer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private readonly Stopwatch _motionTime = new();
    private double _motionFrom, _motionTo, _motionExtent;
    private Window? _window;
    private Grid? _layout;
    private Control? _header;
    private Control? _actions;
    private bool _compact;
    private bool _isModal;
    private MaterialSheetEdge _modalEdge;
    private bool _modalDetached;
    private IReadOnlyList<MaterialSheetDetent> _detents = [];
    public MaterialSheet()
    {
        AddHandler(PointerPressedEvent, HandlePointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerMovedEvent, HandlePointerMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, HandlePointerReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
        _motionTimer.Tick += (_, _) => AdvanceMotion();
    }
    protected virtual bool IsSide => false;
    public MaterialOverlaySession? Session { get; private set; }
    public bool IsModal => _isModal;
    public IReadOnlyList<MaterialSheetDetent> Detents => _detents;
    public event EventHandler<MaterialSheetStateChangingEventArgs>? StateChanging;
    public event EventHandler<MaterialSheetState>? StateChanged;
    protected override AutomationPeer OnCreateAutomationPeer() => new MaterialSheetAutomationPeer(this);
    public string? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public object? Header { get => GetValue(HeaderProperty); set => SetValue(HeaderProperty, value); }
    public IDataTemplate? HeaderTemplate { get => GetValue(HeaderTemplateProperty); set => SetValue(HeaderTemplateProperty, value); }
    public object? Actions { get => GetValue(ActionsProperty); set => SetValue(ActionsProperty, value); }
    public IDataTemplate? ActionsTemplate { get => GetValue(ActionsTemplateProperty); set => SetValue(ActionsTemplateProperty, value); }
    public string HandleText { get => GetValue(HandleTextProperty); set => SetValue(HandleTextProperty, value); }
    public string ExpandText { get => GetValue(ExpandTextProperty); set => SetValue(ExpandTextProperty, value); }
    public string CollapseText { get => GetValue(CollapseTextProperty); set => SetValue(CollapseTextProperty, value); }
    public string DismissText { get => GetValue(DismissTextProperty); set => SetValue(DismissTextProperty, value); }
    public double ExpandedExtent { get => GetValue(ExpandedExtentProperty); set => SetValue(ExpandedExtentProperty, value); }
    public double PeekExtent { get => GetValue(PeekExtentProperty); set => SetValue(PeekExtentProperty, value); }
    public bool IsPartialEnabled { get => GetValue(IsPartialEnabledProperty); set => SetValue(IsPartialEnabledProperty, value); }
    public bool AllowDismiss { get => GetValue(AllowDismissProperty); set => SetValue(AllowDismissProperty, value); }
    public double MinimumExtent { get => GetValue(MinimumExtentProperty); set => SetValue(MinimumExtentProperty, value); }
    public double MaximumExtent { get => GetValue(MaximumExtentProperty); set => SetValue(MaximumExtentProperty, value); }
    public Size? AvailableSize { get => GetValue(AvailableSizeProperty); set => SetValue(AvailableSizeProperty, value); }
    public MaterialSheetEdge Edge { get => GetValue(EdgeProperty); set => SetValue(EdgeProperty, value); }
    public bool IsDetached { get => GetValue(IsDetachedProperty); set => SetValue(IsDetachedProperty, value); }
    public bool IsDraggable { get => GetValue(IsDraggableProperty); set => SetValue(IsDraggableProperty, value); }
    public double DragThreshold { get => GetValue(DragThresholdProperty); set => SetValue(DragThresholdProperty, value); }
    public double VelocityThreshold { get => GetValue(VelocityThresholdProperty); set => SetValue(VelocityThresholdProperty, value); }
    /// <summary>Optional host-declared nested scroll source inside Content. Otherwise the nearest body viewer participates.</summary>
    public ScrollViewer? ScrollSource { get => GetValue(ScrollSourceProperty); set => SetValue(ScrollSourceProperty, value); }
    public bool IsDragging => _isDragging;
    public bool IsSettling => _isSettling;
    public MaterialSpring SpatialSpring { get => GetValue(SpatialSpringProperty); set => SetValue(SpatialSpringProperty, value); }
    internal bool IsSideSheet => IsSide;
    internal bool IsPhysicalLeft => ((IsModal ? _modalEdge : Edge) == MaterialSheetEdge.Start) != (FlowDirection == Avalonia.Media.FlowDirection.RightToLeft);
    internal static bool IsValidSize(Size? size) => size is null || double.IsFinite(size.Value.Width) && double.IsFinite(size.Value.Height) && size.Value.Width >= 0 && size.Value.Height >= 0;
    public double VisibleExtent => _visibleExtent;
    public double Offset => _offset;
    public MaterialSheetState State => _state;
    public bool Expand() => ChangeState(MaterialSheetState.Expanded);
    public bool Collapse() => !IsSide && IsPartialEnabled && ChangeState(MaterialSheetState.PartiallyExpanded);
    public bool Dismiss() => IsModal ? Session!.Dismiss() : AllowDismiss && ChangeState(MaterialSheetState.Hidden);
    public MaterialOverlaySession Show(MaterialOverlayHost host, MaterialOverlayOptions? options = null)
    {
        Dispatcher.UIThread.VerifyAccess();
        if (Session is { IsOpen: true }) throw new InvalidOperationException("The sheet is already open.");
        options ??= new MaterialOverlayOptions { Placement = IsSide ? Edge == MaterialSheetEdge.Start ? MaterialOverlayPlacement.Start : MaterialOverlayPlacement.End : MaterialOverlayPlacement.Bottom, Margin = new Thickness(IsSide && IsDetached ? 16 : 0), CloseOnLightDismiss = true };
        if (!options.IsModal) throw new ArgumentException("Standard sheets use MaterialSheetHost, not a modeless overlay.", nameof(options));
        var session = host.Show(this, options);
        Session = session;
        _modalEdge = options.Placement == MaterialOverlayPlacement.Start ? MaterialSheetEdge.Start : options.Placement == MaterialOverlayPlacement.End ? MaterialSheetEdge.End : Edge;
        _modalDetached = IsSide && IsDetached;
        session.Closing += OnSessionClosing;
        session.Closed += OnSessionClosed;
        SetAndRaise(IsModalProperty, ref _isModal, true);
        SetState(!IsSide && IsPartialEnabled ? MaterialSheetState.PartiallyExpanded : MaterialSheetState.Expanded);
        UpdatePseudoClasses();
        InvalidateMeasure();
        return session;
    }
    private void OnSessionClosing(object? sender, MaterialOverlayClosingEventArgs e)
    {
        if (e.Result.Reason is MaterialOverlayCloseReason.Back or MaterialOverlayCloseReason.Escape &&
            State == MaterialSheetState.Expanded && !IsSide && IsPartialEnabled)
        {
            e.Cancel = true;
            Collapse();
            return;
        }
        if (_changing) { e.Cancel = true; return; }
        var args = new MaterialSheetStateChangingEventArgs(MaterialSheetState.Hidden);
        _changing = true;
        try { StateChanging?.Invoke(this, args); e.Cancel |= args.Cancel; }
        finally { _changing = false; }
    }
    private void OnSessionClosed(object? sender, MaterialOverlayResult e)
    {
        if (sender is MaterialOverlaySession session)
        {
            session.Closing -= OnSessionClosing;
            session.Closed -= OnSessionClosed;
        }
        Session = null;
        SetAndRaise(IsModalProperty, ref _isModal, false);
        CancelDrag();
        SetState(MaterialSheetState.Hidden);
    }
    private bool ChangeState(MaterialSheetState state)
    {
        Dispatcher.UIThread.VerifyAccess();
        if (_changing || State == state || !IsEffectivelyEnabled) return false;
        _changing = true;
        try
        {
            var args = new MaterialSheetStateChangingEventArgs(state);
            StateChanging?.Invoke(this, args);
            if (args.Cancel) return false;
            CancelDrag();
            SetState(state);
            StartMotion();
            return true;
        }
        finally { _changing = false; }
    }
    protected void SetState(MaterialSheetState state)
    {
        var previous = State;
        SetAndRaise(StateProperty, ref _state, state);
        UpdatePseudoClasses();
        InvalidateMeasure();
        if (previous != state) StateChanged?.Invoke(this, state);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ExpandedExtentProperty || change.Property == PeekExtentProperty || change.Property == IsPartialEnabledProperty ||
            change.Property == MinimumExtentProperty || change.Property == MaximumExtentProperty || change.Property == AvailableSizeProperty)
        {
            if (!IsPartialEnabled && State == MaterialSheetState.PartiallyExpanded) SetState(MaterialSheetState.Expanded);
            InvalidateMeasure();
        }
        if (change.Property == EdgeProperty || change.Property == IsDetachedProperty || change.Property == FlowDirectionProperty)
        {
            CancelDrag(); StopMotion(); UpdatePseudoClasses(); InvalidateMeasure();
        }
        if (change.Property == IsDraggableProperty || change.Property == IsEnabledProperty) { if (!IsDraggable || !IsEffectivelyEnabled) CancelDrag(); }
        if (change.Property.Name == nameof(IsEffectivelyEnabled) && !IsEffectivelyEnabled) CancelDrag();
        if (change.Property == ContentProperty || change.Property == ScrollSourceProperty) CancelDrag();
        if (change.Property == SpatialSpringProperty && SpatialSpring.IsInstant) { StopMotion(); InvalidateMeasure(); }
    }
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        CancelDrag();
        StopMotion();
        base.OnApplyTemplate(e);
        _bodyScroll = e.NameScope.Find<ScrollViewer>("PART_BodyScroll");
        _layout = e.NameScope.Find<Grid>("SheetLayout");
        _header = e.NameScope.Find<Control>("SheetHeader");
        _actions = e.NameScope.Find<Control>("SheetActions");
        if (_layout is not null) _layout.RowDefinitions = new RowDefinitions(_compact ? "Auto,Auto,Auto,Auto" : "Auto,Auto,*,Auto");
    }
    private void HandlePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_pointer is not null) { CancelDrag(); return; }
        if (!IsEffectivelyEnabled || e.Source is not Visual source ||
            e.Pointer.Type == PointerType.Mouse && !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        var path = source.GetVisualAncestors().Prepend(source).TakeWhile(visual => visual != this).ToArray();
        _pressedHandle = path.OfType<MaterialSheetDragHandle>().FirstOrDefault();
        var handle = _pressedHandle is not null;
        _bodyGesture = !handle;
        _gestureScroll = null;
        if (!handle)
        {
            if (!IsDraggable || IsSide || e.Pointer.Type == PointerType.Mouse || path.OfType<Control>().Any(control => control.Focusable && control is not ScrollViewer)) return;
            _gestureScroll = ScrollSource is { } declared && this.IsVisualAncestorOf(declared) ? declared : path.OfType<ScrollViewer>().FirstOrDefault() ?? _bodyScroll;
            if (_gestureScroll is null || !path.Contains(_gestureScroll)) return;
        }
        _pointer = e.Pointer;
        _pressPoint = _lastPoint = e.GetPosition(TopLevel.GetTopLevel(this));
        _dragExtent = VisibleExtent;
        _sheetTravel = _velocity = 0; _lastTimestamp = e.Timestamp;
        _pressedHandle?.Focus(NavigationMethod.Pointer);
        if (handle) e.Handled = true;
    }
    private double Axis(Vector delta) => IsSide ? delta.X * (IsPhysicalLeft ? -1 : 1) : delta.Y;
    private void HandlePointerMoved(object? sender, PointerEventArgs e)
    {
        if (_pointer != e.Pointer || !IsDraggable) return;
        var point = e.GetPosition(TopLevel.GetTopLevel(this));
        var total = Axis(point - _pressPoint);
        if (!IsDragging)
        {
            if (Math.Abs(total) < 8) return;
            var vector = point - _pressPoint;
            var cross = IsSide ? vector.Y : vector.X;
            if (Math.Abs(cross) > Math.Abs(total)) { CancelDrag(); return; }
            SetAndRaise(IsDraggingProperty, ref _isDragging, true);
            StopMotion();
            PseudoClasses.Set(":dragging", true);
            e.Pointer.Capture(this);
        }
        var previousExtent = _dragExtent;
        var delta = Axis(point - _lastPoint);
        if (_bodyGesture && _gestureScroll is { } scroll)
        {
            if (delta > 0)
            {
                var consumed = Math.Min(delta, scroll.Offset.Y);
                scroll.Offset = new Vector(scroll.Offset.X, scroll.Offset.Y - consumed);
                delta -= consumed;
            }
            var next = Math.Clamp(_dragExtent - delta, IsModal || AllowDismiss ? 0 : _partial, _expanded);
            var leftover = delta - (_dragExtent - next);
            _dragExtent = next;
            if (leftover < 0)
                scroll.Offset = new Vector(scroll.Offset.X, Math.Min(Math.Max(0, scroll.Extent.Height - scroll.Viewport.Height), scroll.Offset.Y - leftover));
        }
        else _dragExtent = Math.Clamp(_dragExtent - delta, IsModal || AllowDismiss ? 0 : _partial, _expanded);
        var sheetDelta = previousExtent - _dragExtent;
        _sheetTravel += sheetDelta;
        _velocity = e.Timestamp > _lastTimestamp ? sheetDelta * 1000 / (e.Timestamp - _lastTimestamp) : 0;
        _lastTimestamp = e.Timestamp;
        _lastPoint = point;
        InvalidateMeasure();
        e.Handled = true;
    }
    private void HandlePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_pointer != e.Pointer) return;
        var movement = _sheetTravel;
        var velocity = _velocity;
        var bodyGesture = _bodyGesture;
        var handle = _pressedHandle;
        var dragging = IsDragging;
        CancelDrag();
        var threshold = IsSide ? _expanded / 2 : DragThreshold;
        if (dragging && (Math.Abs(movement) >= threshold || Math.Abs(velocity) >= VelocityThreshold))
        {
            if (Math.Abs(movement) < threshold) movement = velocity;
            if (movement < 0) Expand();
            else if (!IsSide && State == MaterialSheetState.Expanded && IsPartialEnabled) Collapse();
            else Dismiss();
        }
        else if (!dragging && !bodyGesture && handle is not null && new Rect(handle.Bounds.Size).Contains(e.GetPosition(handle)))
        {
            if (State == MaterialSheetState.Expanded) { if (IsSide) Dismiss(); else Collapse(); } else Expand();
        }
        if (dragging || !bodyGesture) e.Handled = true;
    }
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        if (!_releasing && _pointer == e.Pointer) CancelDragCore(false);
    }
    /// <summary>Cancel without settling. Restores the last stable state, releases capture, and never confirms a session.</summary>
    public void CancelDrag() => CancelDragCore(true);
    private void CancelDragCore(bool releaseCapture)
    {
        var pointer = _pointer;
        _pointer = null;
        _gestureScroll = null;
        _pressedHandle = null;
        SetAndRaise(IsDraggingProperty, ref _isDragging, false);
        PseudoClasses.Set(":dragging", false);
        _releasing = true;
        try { if (releaseCapture && pointer?.Captured == this) pointer.Capture(null); }
        finally { _releasing = false; }
        InvalidateMeasure();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        CancelDrag();
        StopMotion();
        if (_window is not null) _window.Deactivated -= WindowDeactivated;
        _window = null;
        base.OnDetachedFromVisualTree(e);
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _window = TopLevel.GetTopLevel(this) as Window;
        if (_window is not null) _window.Deactivated += WindowDeactivated;
    }
    private void WindowDeactivated(object? sender, EventArgs e) { CancelDrag(); StopMotion(); InvalidateMeasure(); }
    private double StateExtent => State == MaterialSheetState.Hidden ? 0 : State == MaterialSheetState.Expanded ? _expanded : _partial;
    private void StartMotion()
    {
        StopMotion();
        if (SpatialSpring.IsInstant || TopLevel.GetTopLevel(this) is null || _available <= 0 || State == MaterialSheetState.Hidden) return;
        _motionFrom = _motionExtent = VisibleExtent;
        _motionTo = StateExtent;
        if (Math.Abs(_motionFrom - _motionTo) < 0.1) return;
        SetAndRaise(IsSettlingProperty, ref _isSettling, true);
        _motionTime.Restart(); _motionTimer.Start();
        InvalidateMeasure();
    }
    private void StopMotion()
    {
        _motionTimer.Stop(); _motionTime.Stop();
        SetAndRaise(IsSettlingProperty, ref _isSettling, false);
    }
    private void AdvanceMotion()
    {
        var seconds = _motionTime.Elapsed.TotalSeconds;
        var spring = SpatialSpring;
        var omega = Math.Sqrt(spring.Stiffness);
        var damping = spring.DampingRatio;
        double fraction;
        if (spring.IsInstant) fraction = 1;
        else if (Math.Abs(damping - 1) < 1e-7) fraction = 1 - (1 + omega * seconds) * Math.Exp(-omega * seconds);
        else if (damping < 1)
        {
            var root = Math.Sqrt(1 - damping * damping);
            fraction = 1 - Math.Exp(-damping * omega * seconds) * (Math.Cos(omega * root * seconds) + damping / root * Math.Sin(omega * root * seconds));
        }
        else
        {
            var ratio = Math.Sqrt(damping * damping - 1);
            var first = -omega * (damping - ratio); var second = -omega * (damping + ratio);
            fraction = 1 + (second * Math.Exp(first * seconds) - first * Math.Exp(second * seconds)) / (first - second);
        }
        _motionExtent = Math.Clamp(_motionFrom + (_motionTo - _motionFrom) * fraction, 0, _expanded);
        if (!double.IsFinite(fraction) || seconds >= 10 || seconds > 0.1 && Math.Abs(_motionExtent - _motionTo) < 0.1) StopMotion();
        InvalidateMeasure();
    }
    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":modal", IsModal);
        PseudoClasses.Set(":side", IsSide);
        PseudoClasses.Set(":left", IsPhysicalLeft);
        PseudoClasses.Set(":detached", IsSide && (IsModal ? _modalDetached : IsDetached));
        PseudoClasses.Set(":hidden", State == MaterialSheetState.Hidden);
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        if (AvailableSize is { } declared) availableSize = new Size(Math.Min(availableSize.Width, declared.Width), Math.Min(availableSize.Height, declared.Height));
        if (!double.IsFinite(availableSize.Height) || !double.IsFinite(availableSize.Width)) throw new InvalidOperationException("Sheets require a bounded host or a finite AvailableSize.");
        var natural = base.MeasureOverride(new Size(availableSize.Width, double.PositiveInfinity));
        var available = IsSide ? availableSize.Width : availableSize.Height;
        var wanted = double.IsNaN(ExpandedExtent) ? IsSide ? 256 : natural.Height : ExpandedExtent;
        var minimum = Math.Max(MinimumExtent, !IsModal && !IsSide ? PeekExtent : 0);
        var expanded = Math.Min(available, Math.Max(minimum, Math.Min(MaximumExtent, wanted)));
        var partial = IsModal ? Math.Min(availableSize.Height / 2, expanded) : Math.Min(availableSize.Height, PeekExtent);
        if (State == MaterialSheetState.PartiallyExpanded && _available > 0 && _expanded == _partial && expanded > partial)
            SetState(MaterialSheetState.Expanded);
        var detents = new List<MaterialSheetDetent> { new(MaterialSheetState.Expanded, available - expanded) };
        if (!IsSide && IsPartialEnabled) detents.Add(new(MaterialSheetState.PartiallyExpanded, available - partial));
        if (IsModal || AllowDismiss) detents.Add(new(MaterialSheetState.Hidden, available));
        if (!_detents.SequenceEqual(detents)) SetAndRaise(DetentsProperty, ref _detents, detents.AsReadOnly());
        if (_expanded != expanded || _partial != partial || _available != available) StopMotion();
        _expanded = expanded; _partial = partial; _available = available;
        var extent = IsDragging ? Math.Clamp(_dragExtent, 0, expanded) : IsSettling ? _motionExtent : StateExtent;
        SetAndRaise(VisibleExtentProperty, ref _visibleExtent, extent);
        SetAndRaise(OffsetProperty, ref _offset, available - extent);
        var result = IsSide ? new Size(extent, availableSize.Height) : new Size(availableSize.Width, extent);
        base.MeasureOverride(result);
        var compact = _header is not null && _actions is not null && _header.DesiredSize.Height + _actions.DesiredSize.Height + 96 > result.Height;
        if (_compact != compact)
        {
            _compact = compact;
            if (_layout is not null) _layout.RowDefinitions = new RowDefinitions(compact ? "Auto,Auto,Auto,Auto" : "Auto,Auto,*,Auto");
            PseudoClasses.Set(":compact-height", compact);
            base.MeasureOverride(result);
        }
        PseudoClasses.Set(":empty-extent", extent <= 0);
        return result;
    }
}

public class MaterialBottomSheet : MaterialSheet
{
    protected override Type StyleKeyOverride => typeof(MaterialBottomSheet);
}

public class MaterialSideSheet : MaterialSheet
{
    public MaterialSideSheet() { SetCurrentValue(AllowDismissProperty, true); SetState(MaterialSheetState.Hidden); }
    protected override bool IsSide => true;
    protected override Type StyleKeyOverride => typeof(MaterialSideSheet);
}
