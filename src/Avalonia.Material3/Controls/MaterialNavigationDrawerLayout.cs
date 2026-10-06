using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Presenters;
using Avalonia.VisualTree;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System.Diagnostics;

namespace Avalonia.Material3.Controls;

/// <summary>Explicit host layout: an open standard drawer reserves its width; content owns page routing.</summary>
public class MaterialNavigationDrawerLayout : ContentControl
{
    public static readonly StyledProperty<MaterialNavigationDrawer?> DrawerProperty = AvaloniaProperty.Register<MaterialNavigationDrawerLayout, MaterialNavigationDrawer?>(nameof(Drawer));
    public MaterialNavigationDrawer? Drawer { get => GetValue(DrawerProperty); set => SetValue(DrawerProperty, value); }
    public static readonly StyledProperty<MaterialOverlayHost?> OverlayHostProperty = AvaloniaProperty.Register<MaterialNavigationDrawerLayout, MaterialOverlayHost?>(nameof(OverlayHost));
    public MaterialOverlayHost? OverlayHost { get => GetValue(OverlayHostProperty); set => SetValue(OverlayHostProperty, value); }
    public static readonly StyledProperty<bool> IsEdgeSwipeEnabledProperty = AvaloniaProperty.Register<MaterialNavigationDrawerLayout, bool>(nameof(IsEdgeSwipeEnabled));
    public bool IsEdgeSwipeEnabled { get => GetValue(IsEdgeSwipeEnabledProperty); set => SetValue(IsEdgeSwipeEnabledProperty, value); }
    private ContentPresenter? _drawerPresenter;
    private MaterialNavigationDrawer? _subscribed;
    private bool _reconciling;
    private IPointer? _edgePointer;
    private Point _edgeStart;
    private long _edgeTime;
    private bool _edgeDragging;
    private double _edgeDistance;
    public MaterialNavigationDrawerLayout()
    {
        AddHandler(PointerPressedEvent, EdgePressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerMovedEvent, EdgeMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, EdgeReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
    }
    protected override Type StyleKeyOverride => typeof(MaterialNavigationDrawerLayout);
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_drawerPresenter is not null) _drawerPresenter.Content = null;
        base.OnApplyTemplate(e);
        _drawerPresenter = e.NameScope.Find<ContentPresenter>("PART_DrawerPresenter");
        WatchDrawer();
    }
    private void WatchDrawer()
    {
        if (_subscribed is not null) _subscribed.PropertyChanged -= DrawerChanged;
        _subscribed = Drawer;
        if (_subscribed is not null) _subscribed.PropertyChanged += DrawerChanged;
        Reconcile();
    }
    private void DrawerChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == MaterialNavigationDrawer.IsOpenProperty || change.Property == MaterialNavigationDrawer.ModeProperty || change.Property == MaterialNavigationDrawer.DrawerWidthProperty) Reconcile();
    }
    private void Reconcile()
    {
        if (_reconciling || _drawerPresenter is null || VisualRoot is null) return;
        _reconciling = true;
        try
        {
            if (Drawer is { } drawer)
            {
                var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() as Control;
                if (focused != drawer && (focused is null || !drawer.IsVisualAncestorOf(focused))) focused = null;
                if (drawer.Mode == MaterialNavigationDrawerMode.Standard && drawer.Session is not null && !drawer.CloseForPresentationChange())
                    drawer.SetCurrentValue(MaterialNavigationDrawer.ModeProperty, MaterialNavigationDrawerMode.Modal);
                var persistent = drawer.IsOpen && drawer.Mode == MaterialNavigationDrawerMode.Standard;
                _drawerPresenter.Content = persistent ? drawer : null;
                _drawerPresenter.UpdateChild();
                if (drawer.IsOpen && drawer.Mode == MaterialNavigationDrawerMode.Modal && drawer.Session is null)
                {
                    var host = OverlayHost ?? this.GetVisualAncestors().OfType<MaterialOverlayHost>().FirstOrDefault()
                        ?? throw new InvalidOperationException("A modal DrawerLayout requires an ancestor or explicit MaterialOverlayHost.");
                    drawer.Show(host, new MaterialOverlayOptions
                    {
                        Placement = MaterialOverlayPlacement.Start, Margin = new Thickness(0), CloseOnLightDismiss = true, Anchor = this,
                        InitialFocus = focused ?? drawer.ItemsView.OfType<MaterialNavigationItem>().FirstOrDefault(item => item.IsEffectivelyEnabled && item.IsVisible)
                    });
                }
                else if (persistent && focused is not null)
                    Dispatcher.UIThread.Post(() => { if (drawer.IsOpen && drawer.Mode == MaterialNavigationDrawerMode.Standard && focused.IsEffectivelyEnabled && focused.IsEffectivelyVisible && TopLevel.GetTopLevel(focused) == TopLevel.GetTopLevel(this)) focused.Focus(NavigationMethod.Tab); }, DispatcherPriority.Loaded);
            }
            else { _drawerPresenter.Content = null; _drawerPresenter.UpdateChild(); }
            _drawerPresenter.InvalidateMeasure();
            (_drawerPresenter.GetVisualParent() as Control)?.InvalidateMeasure();
            InvalidateMeasure();
        }
        finally { _reconciling = false; }
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) { base.OnAttachedToVisualTree(e); WatchDrawer(); }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        CancelEdge();
        if (_subscribed is not null) _subscribed.PropertyChanged -= DrawerChanged;
        _subscribed = null;
        base.OnDetachedFromVisualTree(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DrawerProperty)
        {
            if (_subscribed?.Session is not null && !_subscribed.Close())
            {
                SetCurrentValue(DrawerProperty, _subscribed);
                return;
            }
            WatchDrawer();
        }
        if (change.Property == OverlayHostProperty) Reconcile();
        if (change.Property == BoundsProperty || change.Property == IsEdgeSwipeEnabledProperty || change.Property == FlowDirectionProperty || change.Property == IsEffectivelyEnabledProperty) CancelEdge();
    }
    private void EdgePressed(object? sender, PointerPressedEventArgs e)
    {
        if (_edgePointer is not null) { if (_edgePointer != e.Pointer) CancelEdge(); return; }
        if (!IsEdgeSwipeEnabled || !IsEffectivelyEnabled || Drawer is not { IsOpen: false, IsGestureEnabled: true } || e.Pointer.Type == PointerType.Mouse) return;
        var point = e.GetPosition(this);
        var start = FlowDirection == Avalonia.Media.FlowDirection.RightToLeft ? Bounds.Width - point.X : point.X;
        if (start is < 0 or > 24) return;
        _edgePointer = e.Pointer; _edgeStart = point; _edgeTime = Stopwatch.GetTimestamp(); _edgeDistance = 0;
    }
    private void EdgeMoved(object? sender, PointerEventArgs e)
    {
        if (_edgePointer != e.Pointer) return;
        if (Drawer is not { IsOpen: false, IsGestureEnabled: true }) { CancelEdge(); return; }
        var delta = e.GetPosition(this) - _edgeStart;
        if (!_edgeDragging && Math.Abs(delta.Y) > 8 && Math.Abs(delta.Y) >= Math.Abs(delta.X)) { CancelEdge(); return; }
        var distance = FlowDirection == Avalonia.Media.FlowDirection.RightToLeft ? -delta.X : delta.X;
        if (!_edgeDragging && (distance <= 8 || Math.Abs(delta.Y) >= Math.Abs(delta.X))) return;
        _edgeDragging = true; e.Pointer.Capture(this); _edgeDistance = Math.Max(0, distance); e.Handled = true;
    }
    private void EdgeReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_edgePointer != e.Pointer) return;
        var open = _edgeDragging && Drawer is { IsOpen: false, IsGestureEnabled: true } drawer
            && (_edgeDistance >= Math.Min(drawer.DrawerWidth, Bounds.Width) * .5 || _edgeDistance / Math.Max(.001, Stopwatch.GetElapsedTime(_edgeTime).TotalSeconds) >= 400);
        var handled = _edgeDragging; CancelEdge();
        if (open) Drawer!.SetCurrentValue(MaterialNavigationDrawer.IsOpenProperty, true);
        if (handled) e.Handled = true;
    }
    private void CancelEdge()
    {
        var pointer = _edgePointer; _edgePointer = null; _edgeDragging = false; _edgeDistance = 0;
        if (pointer?.Captured == this) pointer.Capture(null);
    }
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e) { CancelEdge(); base.OnPointerCaptureLost(e); }
}

/// <summary>Default logical-start persistent-drawer layout. No implicit breakpoint or application routing.</summary>
public class MaterialDrawerLayoutPanel : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count != 2) return default;
        Children[0].Measure(availableSize);
        Children[1].Measure(new Size(Math.Max(0, availableSize.Width - Children[0].DesiredSize.Width), availableSize.Height));
        return new Size(Children[0].DesiredSize.Width + Children[1].DesiredSize.Width, Math.Max(Children[0].DesiredSize.Height, Children[1].DesiredSize.Height));
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count != 2) return finalSize;
        var width = Math.Min(finalSize.Width, Children[0].DesiredSize.Width);
        // Avalonia mirrors this logical-start layout; do not physically reverse it a second time.
        Children[0].Arrange(new Rect(0, 0, width, finalSize.Height));
        Children[1].Arrange(new Rect(width, 0, finalSize.Width - width, finalSize.Height));
        return finalSize;
    }
}
