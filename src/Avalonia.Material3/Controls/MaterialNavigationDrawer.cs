using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Automation.Peers;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Diagnostics;
using Avalonia.VisualTree;

namespace Avalonia.Material3.Controls;

public enum MaterialNavigationDrawerMode { Standard, Modal }

/// <summary>Destination selection, not a router. Standard layout reservation is supplied by DrawerLayout.</summary>
public class MaterialNavigationDrawer : MaterialNavigation
{
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<MaterialNavigationDrawer, string?>(nameof(Title), "Navigation menu");
    public static readonly StyledProperty<object?> FooterProperty = AvaloniaProperty.Register<MaterialNavigationDrawer, object?>(nameof(Footer));
    public static readonly StyledProperty<string> CloseTextProperty = AvaloniaProperty.Register<MaterialNavigationDrawer, string>(nameof(CloseText), "Close navigation drawer", validate: value => !string.IsNullOrWhiteSpace(value));
    public static readonly StyledProperty<bool> IsOpenProperty = AvaloniaProperty.Register<MaterialNavigationDrawer, bool>(nameof(IsOpen), true, defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<double> DrawerWidthProperty = AvaloniaProperty.Register<MaterialNavigationDrawer, double>(nameof(DrawerWidth), 360, validate: value => double.IsFinite(value) && value >= 240 && value <= 360);
    public static readonly StyledProperty<MaterialNavigationDrawerMode> ModeProperty = AvaloniaProperty.Register<MaterialNavigationDrawer, MaterialNavigationDrawerMode>(nameof(Mode), validate: Enum.IsDefined);
    public string? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public object? Footer { get => GetValue(FooterProperty); set => SetValue(FooterProperty, value); }
    public string CloseText { get => GetValue(CloseTextProperty); set => SetValue(CloseTextProperty, value); }
    public bool IsOpen { get => GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }
    public double DrawerWidth { get => GetValue(DrawerWidthProperty); set => SetValue(DrawerWidthProperty, value); }
    public MaterialNavigationDrawerMode Mode { get => GetValue(ModeProperty); set => SetValue(ModeProperty, value); }
    public static readonly StyledProperty<Avalonia.Media.BoxShadows> BoxShadowProperty = AvaloniaProperty.Register<MaterialNavigationDrawer, Avalonia.Media.BoxShadows>(nameof(BoxShadow));
    /// <summary>Surface shadow. Default zero follows pinned DrawerDefaults; hosts can opt into elevation.</summary>
    public Avalonia.Media.BoxShadows BoxShadow { get => GetValue(BoxShadowProperty); set => SetValue(BoxShadowProperty, value); }
    public static readonly StyledProperty<bool> IsStandardDismissibleProperty = AvaloniaProperty.Register<MaterialNavigationDrawer, bool>(nameof(IsStandardDismissible), true);
    public bool IsStandardDismissible { get => GetValue(IsStandardDismissibleProperty); set => SetValue(IsStandardDismissibleProperty, value); }
    public static readonly StyledProperty<bool> IsGestureEnabledProperty = AvaloniaProperty.Register<MaterialNavigationDrawer, bool>(nameof(IsGestureEnabled), true);
    public static readonly DirectProperty<MaterialNavigationDrawer, double> DragOffsetProperty = AvaloniaProperty.RegisterDirect<MaterialNavigationDrawer, double>(nameof(DragOffset), c => c.DragOffset);
    public bool IsGestureEnabled { get => GetValue(IsGestureEnabledProperty); set => SetValue(IsGestureEnabledProperty, value); }
    private double _dragOffset;
    public double DragOffset => _dragOffset;
    private IPointer? _pointer;
    private Point _start;
    private double _distance;
    private double _gestureBase;
    private MaterialNavigationDrawerLayout? DrawerLayout => this.GetVisualAncestors().OfType<MaterialNavigationDrawerLayout>().FirstOrDefault();
    private long _startTime;
    private bool _dragging;
    private Window? _window;
    public static readonly DirectProperty<MaterialNavigationDrawer, MaterialOverlaySession?> SessionProperty = AvaloniaProperty.RegisterDirect<MaterialNavigationDrawer, MaterialOverlaySession?>(nameof(Session), c => c.Session);
    private MaterialOverlaySession? _session;
    private bool _updatingOpen, _changingPresentation;
    public MaterialOverlaySession? Session => _session;
    private Button? _closeButton;
    private Border? _surface;
    private readonly Avalonia.Media.ScaleTransform _bounceSurface = new(1, 1);
    private readonly Avalonia.Media.ScaleTransform _bounceContent = new(1, 1);
    private readonly Avalonia.Media.TranslateTransform _dragTransform = new();
    internal void SetPresentationOffset(double offset)
    {
        var factor = Bounds.Width > 0 ? 1 + Math.Max(0, offset) / Bounds.Width : 1;
        _bounceSurface.ScaleX = factor; _bounceContent.ScaleX = 1 / factor;
    }
    public MaterialNavigationDrawer()
    {
        UpdateDrawerPresentation();
        AddHandler(PointerPressedEvent, DragPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerMovedEvent, DragMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, DragReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
    }
    protected override Type StyleKeyOverride => typeof(MaterialNavigationDrawer);
    protected override AutomationPeer OnCreateAutomationPeer() => new MaterialNavigationDrawerAutomationPeer(this);
    protected override Size MeasureOverride(Size availableSize)
    {
        var size = base.MeasureOverride(new Size(Math.Min(availableSize.Width, DrawerWidth), availableSize.Height));
        return new Size(Math.Min(availableSize.Width, DrawerWidth), double.IsFinite(availableSize.Height) ? availableSize.Height : size.Height);
    }
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        CancelDrag();
        if (_closeButton is not null) _closeButton.Click -= CloseClicked;
        base.OnApplyTemplate(e);
        _closeButton = e.NameScope.Find<Button>("PART_CloseButton");
        _surface = e.NameScope.Find<Border>("PART_Surface");
        if (_surface is not null)
        {
            var transforms = new Avalonia.Media.TransformGroup(); transforms.Children.Add(_dragTransform); transforms.Children.Add(_bounceSurface);
            _surface.RenderTransform = transforms; _surface.RenderTransformOrigin = new RelativePoint(1, .5, RelativeUnit.Relative);
            if (_surface.Child is Control content)
            {
                content.RenderTransform = _bounceContent; content.RenderTransformOrigin = new RelativePoint(1, 0, RelativeUnit.Relative);
            }
        }
        if (_closeButton is not null) _closeButton.Click += CloseClicked;
    }
    private void CloseClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();
    private void DragPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_pointer is not null) { if (_pointer != e.Pointer) CancelDrag(); return; }
        if (!IsOpen || !IsGestureEnabled || !IsEffectivelyEnabled || e.Pointer.Type == PointerType.Mouse || (Mode == MaterialNavigationDrawerMode.Standard && !IsStandardDismissible)) return;
        _pointer = e.Pointer;
        _start = e.GetPosition(TopLevel.GetTopLevel(this));
        _distance = 0;
        _gestureBase = Session?.Layer.Presentation?.DrawerOffset ?? (DrawerLayout is { } layout ? (layout.PresentationFraction - 1) * Bounds.Width : 0);
        _startTime = Stopwatch.GetTimestamp();
    }
    private void DragMoved(object? sender, PointerEventArgs e)
    {
        if (_pointer != e.Pointer) return;
        var delta = e.GetPosition(TopLevel.GetTopLevel(this)) - _start;
        if (!_dragging)
        {
            if (Math.Abs(delta.Y) > 8 && Math.Abs(delta.Y) >= Math.Abs(delta.X)) { CancelDrag(); return; }
            var closing = FlowDirection == Avalonia.Media.FlowDirection.RightToLeft ? delta.X : -delta.X;
            if (closing <= 8 || Math.Abs(delta.Y) >= Math.Abs(delta.X)) return;
            _dragging = true;
            e.Pointer.Capture(this);
        }
        _distance = Math.Clamp(FlowDirection == Avalonia.Media.FlowDirection.RightToLeft ? delta.X : -delta.X, 0, Bounds.Width);
        SetAndRaise(DragOffsetProperty, ref _dragOffset, FlowDirection == Avalonia.Media.FlowDirection.RightToLeft ? _distance : -_distance);
        // The surface inherits Avalonia's RTL mirror. Translation is logical-start in both modes;
        // DragOffset remains the publicly observed physical signed distance.
        if (Session?.Layer.Presentation is { } presentation) presentation.SetDrawerGesture(_gestureBase - _distance);
        else if (DrawerLayout is { } layout) layout.SetDrawerGesture(_gestureBase - _distance);
        else _dragTransform.X = -_distance;
        e.Handled = true;
    }
    private void DragReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_pointer != e.Pointer) return;
        var commit = _dragging && (_distance - _gestureBase >= Bounds.Width * .5 || _distance / Math.Max(.001, Stopwatch.GetElapsedTime(_startTime).TotalSeconds) >= 400);
        var dragged = _dragging;
        CancelDrag(settle: !commit);
        if (commit && !Close()) RestoreGesture();
        if (dragged) e.Handled = true;
    }
    private void RestoreGesture()
    {
        Session?.Layer.Presentation?.RestoreDrawerGesture();
        if (Session is null) DrawerLayout?.RestoreDrawerGesture();
    }
    private void CancelDrag(bool settle = true)
    {
        var wasDragging = _dragging;
        var pointer = _pointer;
        _pointer = null;
        _dragging = false;
        _distance = 0;
        SetAndRaise(DragOffsetProperty, ref _dragOffset, 0);
        _dragTransform.X = 0;
        if (settle && wasDragging) RestoreGesture();
        if (pointer?.Captured == this) pointer.Capture(null);
    }
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e) { CancelDrag(); base.OnPointerCaptureLost(e); }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _window = TopLevel.GetTopLevel(this) as Window;
        if (_window is not null) _window.Deactivated += Deactivated;
    }
    private void Deactivated(object? sender, EventArgs e) => CancelDrag();
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        CancelDrag();
        if (_window is not null) _window.Deactivated -= Deactivated;
        _window = null;
        base.OnDetachedFromVisualTree(e);
    }
    /// <summary>Present an unparented modal drawer. Default policy is logical Start, edge-to-edge, light dismissal.</summary>
    public MaterialOverlaySession Show(MaterialOverlayHost host, MaterialOverlayOptions? options = null)
    {
        if (Mode != MaterialNavigationDrawerMode.Modal) throw new InvalidOperationException("Use DrawerLayout for a standard drawer; choose Modal before Show.");
        if (Session is { IsOpen: true }) throw new InvalidOperationException("The drawer is already presented.");
        options ??= new MaterialOverlayOptions
        {
            Placement = MaterialOverlayPlacement.Start, Margin = new Thickness(0), CloseOnLightDismiss = true,
            InitialFocus = ItemsView.OfType<MaterialNavigationItem>().FirstOrDefault(item => item.IsEffectivelyEnabled && item.IsVisible)
        };
        if (!options.IsModal || options.Placement != MaterialOverlayPlacement.Start) throw new ArgumentException("Modal drawers require modal logical-Start placement.", nameof(options));
        var session = host.Show(this, options);
        SetAndRaise(SessionProperty, ref _session, session);
        session.Closed += SessionClosed;
        SetOpen(true);
        return session;
    }
    private void SetOpen(bool open)
    {
        _updatingOpen = true;
        try { SetCurrentValue(IsOpenProperty, open); }
        finally { _updatingOpen = false; }
    }
    private void SessionClosed(object? sender, MaterialOverlayResult result)
    {
        if (sender is MaterialOverlaySession session) session.Closed -= SessionClosed;
        SetAndRaise(SessionProperty, ref _session, null);
        if (!_changingPresentation) SetOpen(false);
    }
    internal bool CloseForPresentationChange()
    {
        if (Session is null) return true;
        _changingPresentation = true;
        try { return Session.Dismiss(); }
        finally { _changingPresentation = false; }
    }
    public bool Close()
    {
        if (Session is { } session) return session.Dismiss();
        if (!IsOpen || !IsStandardDismissible) return false;
        SetOpen(false);
        return true;
    }
    internal override bool TryActivate(MaterialNavigationItem item)
    {
        if (Session is { } session && !session.Dismiss()) return false;
        return base.TryActivate(item);
    }
    internal override void Activate(MaterialNavigationItem item)
    {
        if (Session is { } session && !session.Dismiss()) throw new InvalidOperationException("Destination selection cannot commit while closing the modal drawer is vetoed or covered.");
        base.Activate(item);
    }
    private void UpdateDrawerPresentation()
    {
        PseudoClasses.Set(":modal", Mode == MaterialNavigationDrawerMode.Modal);
        PseudoClasses.Set(":permanent", Mode == MaterialNavigationDrawerMode.Standard && !IsStandardDismissible);
        PseudoClasses.Set(":rtl", FlowDirection == Avalonia.Media.FlowDirection.RightToLeft);
        InvalidateMeasure();
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ModeProperty || change.Property == DrawerWidthProperty || change.Property == FlowDirectionProperty || change.Property == IsStandardDismissibleProperty) UpdateDrawerPresentation();
        if (change.Property == IsOpenProperty && !_updatingOpen && !IsOpen && Session is { } session && !session.Dismiss()) SetOpen(true);
        if (change.Property == BoundsProperty || change.Property == IsGestureEnabledProperty || change.Property == IsEffectivelyEnabledProperty || change.Property == ModeProperty || change.Property == FlowDirectionProperty || change.Property == IsOpenProperty) CancelDrag();
    }
}
