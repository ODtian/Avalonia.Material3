using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Automation.Peers;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Controls.Presenters;
using Avalonia.Styling;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.VisualTree;

namespace Avalonia.Material3.Controls;

public enum MaterialTopAppBarVariant { Small, CenterAligned, Medium, Large, MediumFlexible, LargeFlexible }
public enum MaterialAppBarScrollBehavior { Pinned, EnterAlways, ExitUntilCollapsed }

/// <summary>A page-owned title and action surface. Navigation content is host supplied, not a router.</summary>
public class MaterialTopAppBar : TemplatedControl
{
    public static readonly StyledProperty<MaterialAppBarScrollBehavior> ScrollBehaviorProperty = AvaloniaProperty.Register<MaterialTopAppBar, MaterialAppBarScrollBehavior>(nameof(ScrollBehavior), validate: Enum.IsDefined);
    public static readonly StyledProperty<ScrollViewer?> ScrollSourceProperty = AvaloniaProperty.Register<MaterialTopAppBar, ScrollViewer?>(nameof(ScrollSource));
    public static readonly DirectProperty<MaterialTopAppBar, double> ExpandedHeightProperty = AvaloniaProperty.RegisterDirect<MaterialTopAppBar, double>(nameof(ExpandedHeight), c => c.ExpandedHeight);
    public static readonly DirectProperty<MaterialTopAppBar, double> CollapsedHeightProperty = AvaloniaProperty.RegisterDirect<MaterialTopAppBar, double>(nameof(CollapsedHeight), c => c.CollapsedHeight);
    public static readonly DirectProperty<MaterialTopAppBar, double> CollapsedFractionProperty = AvaloniaProperty.RegisterDirect<MaterialTopAppBar, double>(nameof(CollapsedFraction), c => c.CollapsedFraction);
    public static readonly DirectProperty<MaterialTopAppBar, bool> IsScrolledProperty = AvaloniaProperty.RegisterDirect<MaterialTopAppBar, bool>(nameof(IsScrolled), c => c.IsScrolled);
    private double _expandedHeight = 64, _collapsedHeight = 64, _collapsedFraction, _collapse, _lastOffset, _contentOffset;
    private bool _isScrolled;
    private ScrollViewer? _subscribedScroll;
    private ScrollViewer? _watchedScroll;
    public static readonly StyledProperty<IBrush?> ScrolledBackgroundProperty = AvaloniaProperty.Register<MaterialTopAppBar, IBrush?>(nameof(ScrolledBackground));
    public static readonly DirectProperty<MaterialTopAppBar, IBrush?> CurrentBackgroundProperty = AvaloniaProperty.RegisterDirect<MaterialTopAppBar, IBrush?>(nameof(CurrentBackground), c => c.CurrentBackground);
    private IBrush? _currentBackground;
    public IBrush? ScrolledBackground { get => GetValue(ScrolledBackgroundProperty); set => SetValue(ScrolledBackgroundProperty, value); }
    public IBrush? CurrentBackground => _currentBackground;
    public double ExpandedHeight => _expandedHeight;
    public double CollapsedHeight => _collapsedHeight;
    public double CollapsedFraction => _collapsedFraction;
    public bool IsScrolled => _isScrolled;
    public MaterialAppBarScrollBehavior ScrollBehavior { get => GetValue(ScrollBehaviorProperty); set => SetValue(ScrollBehaviorProperty, value); }
    public ScrollViewer? ScrollSource { get => GetValue(ScrollSourceProperty); set => SetValue(ScrollSourceProperty, value); }
    public static readonly StyledProperty<MaterialTopAppBarVariant> VariantProperty = AvaloniaProperty.Register<MaterialTopAppBar, MaterialTopAppBarVariant>(nameof(Variant), validate: Enum.IsDefined);
    public static readonly StyledProperty<string?> SubtitleProperty = AvaloniaProperty.Register<MaterialTopAppBar, string?>(nameof(Subtitle));
    public static readonly StyledProperty<bool> CenterTitleProperty = AvaloniaProperty.Register<MaterialTopAppBar, bool>(nameof(CenterTitle));
    public MaterialTopAppBarVariant Variant { get => GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
    public string? Subtitle { get => GetValue(SubtitleProperty); set => SetValue(SubtitleProperty, value); }
    public bool CenterTitle { get => GetValue(CenterTitleProperty); set => SetValue(CenterTitleProperty, value); }
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<MaterialTopAppBar, string?>(nameof(Title));
    public static readonly StyledProperty<IDataTemplate?> TitleTemplateProperty = AvaloniaProperty.Register<MaterialTopAppBar, IDataTemplate?>(nameof(TitleTemplate));
    public IDataTemplate? TitleTemplate { get => GetValue(TitleTemplateProperty); set => SetValue(TitleTemplateProperty, value); }
    public static readonly StyledProperty<object?> NavigationContentProperty = AvaloniaProperty.Register<MaterialTopAppBar, object?>(nameof(NavigationContent));
    public static readonly StyledProperty<IDataTemplate?> NavigationContentTemplateProperty = AvaloniaProperty.Register<MaterialTopAppBar, IDataTemplate?>(nameof(NavigationContentTemplate));
    public static readonly StyledProperty<object?> ActionsProperty = AvaloniaProperty.Register<MaterialTopAppBar, object?>(nameof(Actions));
    public static readonly StyledProperty<IDataTemplate?> ActionsTemplateProperty = AvaloniaProperty.Register<MaterialTopAppBar, IDataTemplate?>(nameof(ActionsTemplate));
    public string? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public object? NavigationContent { get => GetValue(NavigationContentProperty); set => SetValue(NavigationContentProperty, value); }
    public IDataTemplate? NavigationContentTemplate { get => GetValue(NavigationContentTemplateProperty); set => SetValue(NavigationContentTemplateProperty, value); }
    public object? Actions { get => GetValue(ActionsProperty); set => SetValue(ActionsProperty, value); }
    public IDataTemplate? ActionsTemplate { get => GetValue(ActionsTemplateProperty); set => SetValue(ActionsTemplateProperty, value); }
    internal bool IsTwoRow => Variant is not (MaterialTopAppBarVariant.Small or MaterialTopAppBarVariant.CenterAligned);
    internal double TitleBottomPadding => Variant is MaterialTopAppBarVariant.Large or MaterialTopAppBarVariant.LargeFlexible ? 28 : 24;
    internal double NominalHeight => Variant switch
    {
        MaterialTopAppBarVariant.Medium => 112,
        MaterialTopAppBarVariant.Large => 152,
        MaterialTopAppBarVariant.MediumFlexible => string.IsNullOrEmpty(Subtitle) ? 112 : 136,
        MaterialTopAppBarVariant.LargeFlexible => string.IsNullOrEmpty(Subtitle) ? 120 : 152,
        _ => 64
    };
    private ContentPresenter? _navigationPresenter;
    private readonly List<(MaterialIconButton Icon, Style Role)> _navigationIcons = [];
    private readonly MaterialMotionBrush _containerColor;
    private readonly MaterialMotionSettings _motion;
    private readonly MaterialFrameLease _settlement;
    private enum SettlementPhase { None, Waiting, Decay, Snap }
    private SettlementPhase _settlementPhase;
    private MaterialSplineDecay _scrollDecay;
    private double _settlementFrom, _settlementTarget, _scrollVelocity;
    private TimeSpan _lastScrollTime;
    private IPointer? _dragPointer;
    private IPointer? _sourcePointer;
    private Point _dragPoint;
    private ulong _dragTimestamp;
    private static readonly Avalonia.Animation.Easings.SplineEasing ContainerColorEasing = new(.4, 0, 1, 1);
    public MaterialTopAppBar()
    {
        _containerColor = new(this, null, PaintBackground);
        _motion = new(this, UpdateBackground);
        _settlement = MaterialRenderFrames.Bind(this, AdvanceSettlement);
        AddHandler(PointerPressedEvent, BarPressed, RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, BarMoved, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, BarReleased, RoutingStrategies.Tunnel);
        UpdatePresentation();
    }
    private void ClearNavigationRole()
    {
        foreach (var (icon, role) in _navigationIcons) icon.Styles.Remove(role);
        _navigationIcons.Clear();
    }
    private void UpdateNavigationRole()
    {
        ClearNavigationRole();
        if (VisualRoot is null) return;
        var content = _navigationPresenter?.Child ?? NavigationContent as Control;
        if (content is null) return;
        foreach (var icon in content.GetVisualDescendants().OfType<MaterialIconButton>().Concat(content is MaterialIconButton button ? [button] : Array.Empty<MaterialIconButton>()))
        {
            var role = new Style(selector => selector.OfType<MaterialIconButton>().Class(":icon-standard").Not(disabled => disabled.Class(":disabled")))
            {
                Setters = { new Setter(ForegroundProperty, new DynamicResourceExtension("M3.OnSurfaceBrush")) }
            };
            icon.Styles.Add(role);
            _navigationIcons.Add((icon, role));
        }
    }
    private void NavigationPresenterChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == ContentPresenter.ChildProperty) UpdateNavigationRole();
    }
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_navigationPresenter is not null) _navigationPresenter.PropertyChanged -= NavigationPresenterChanged;
        ClearNavigationRole();
        base.OnApplyTemplate(e);
        _navigationPresenter = e.NameScope.Find<ContentPresenter>("Navigation");
        if (_navigationPresenter is not null) _navigationPresenter.PropertyChanged += NavigationPresenterChanged;
        UpdateNavigationRole();
    }
    internal bool UsesCollapsedTitle => IsTwoRow && CollapsedFraction >= .5;
    internal double RenderedHeight => ExpandedHeight - _collapse;
    internal void SetMeasuredHeights(double collapsed, double expanded)
    {
        var fullyCollapsed = CollapsedFraction == 1;
        SetAndRaise(CollapsedHeightProperty, ref _collapsedHeight, collapsed);
        SetAndRaise(ExpandedHeightProperty, ref _expandedHeight, Math.Max(collapsed, expanded));
        _collapse = fullyCollapsed ? ExpandedHeight - CollapsedHeight : Math.Clamp(_collapse, 0, ExpandedHeight - CollapsedHeight);
        UpdateScrollPresentation();
    }
    /// <summary>Optional nested-scroll adapter. Positive delta collapses. Return value is consumed DIP.
    /// Do not also bind ScrollSource when a host forwards the same delta.</summary>
    public double ApplyScrollDelta(double delta, double contentOffset)
    {
        if (!double.IsFinite(delta) || !double.IsFinite(contentOffset) || contentOffset < 0) throw new ArgumentOutOfRangeException(nameof(delta));
        var old = _collapse;
        StopSettlement();
        _contentOffset = contentOffset;
        if (ScrollBehavior != MaterialAppBarScrollBehavior.Pinned && (delta >= 0 || ScrollBehavior == MaterialAppBarScrollBehavior.EnterAlways || contentOffset <= 0))
            _collapse = Math.Clamp(_collapse + delta, 0, ExpandedHeight - CollapsedHeight);
        SetAndRaise(IsScrolledProperty, ref _isScrolled, contentOffset > .01);
        UpdateScrollPresentation();
        InvalidateMeasure();
        if (delta != 0 && _dragPointer is null && _sourcePointer is null) WaitForScrollEnd();
        return _collapse - old;
    }
    public void ResetScroll()
    {
        StopSettlement();
        _collapse = 0;
        _lastOffset = 0;
        _contentOffset = 0;
        SetAndRaise(IsScrolledProperty, ref _isScrolled, false);
        UpdateScrollPresentation();
        InvalidateMeasure();
    }
    private void UpdateScrollPresentation()
    {
        SetAndRaise(CollapsedFractionProperty, ref _collapsedFraction, ExpandedHeight > CollapsedHeight ? _collapse / (ExpandedHeight - CollapsedHeight) : 0);
        PseudoClasses.Set(":collapsed", UsesCollapsedTitle);
        PseudoClasses.Set(":scrolled", IsTwoRow ? CollapsedFraction > .01 : IsScrolled);
        UpdateBackground();
    }
    private void UpdateBackground()
    {
        var fraction = IsTwoRow ? CollapsedFraction : IsScrolled ? 1 : 0;
        if (_containerColor is null || _motion is null) return;
        if (IsTwoRow) _containerColor.Snap(MaterialMotionBrush.Interpolate(Background, ScrolledBackground, ContainerColorEasing.Ease(fraction)));
        else _containerColor.Set(_contentOffset / Math.Max(1, CollapsedHeight) > .01 ? ScrolledBackground : Background, _motion.DefaultEffects);
    }
    private void PaintBackground(IBrush? brush)
    {
        if (_currentBackground is ISolidColorBrush previous && brush is ISolidColorBrush next && previous.Color == next.Color && previous.Opacity == next.Opacity) return;
        SetAndRaise(CurrentBackgroundProperty, ref _currentBackground, brush);
    }
    private void StopScroll()
    {
        if (_subscribedScroll is not { } old) return;
        old.ScrollChanged -= ScrollChanged;
        old.RemoveHandler(PointerWheelChangedEvent, ScrollWheel);
        old.RemoveHandler(PointerPressedEvent, SourcePressed);
        old.RemoveHandler(PointerReleasedEvent, SourceReleased);
        _sourcePointer = null;
        _subscribedScroll = null;
    }
    private void StartScroll()
    {
        StopWatchingScroll();
        ResetScroll();
        if (VisualRoot is null || ScrollSource is not { } scroll) return;
        _watchedScroll = scroll;
        scroll.AttachedToVisualTree += SourceAttached;
        scroll.DetachedFromVisualTree += SourceDetached;
        SubscribeScroll(scroll);
    }
    private void SubscribeScroll(ScrollViewer scroll)
    {
        if (TopLevel.GetTopLevel(scroll) is null || TopLevel.GetTopLevel(scroll) != TopLevel.GetTopLevel(this)) return;
        _subscribedScroll = scroll;
        scroll.ScrollChanged += ScrollChanged;
        scroll.AddHandler(PointerWheelChangedEvent, ScrollWheel, RoutingStrategies.Tunnel);
        scroll.AddHandler(PointerPressedEvent, SourcePressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        scroll.AddHandler(PointerReleasedEvent, SourceReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
        ObserveOffset(scroll.Offset.Y);
    }
    private void SourceAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is ScrollViewer scroll) { StopScroll(); ResetScroll(); SubscribeScroll(scroll); }
    }
    private void SourceDetached(object? sender, VisualTreeAttachmentEventArgs e) { StopScroll(); ResetScroll(); }
    private void StopWatchingScroll()
    {
        StopScroll();
        if (_watchedScroll is not { } scroll) return;
        scroll.AttachedToVisualTree -= SourceAttached;
        scroll.DetachedFromVisualTree -= SourceDetached;
        _watchedScroll = null;
    }
    private void ScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (e.OffsetDelta.Y != 0 && _subscribedScroll is { } scroll) ObserveOffset(scroll.Offset.Y);
    }
    private void ObserveOffset(double offset)
    {
        StopSettlement();
        var now = MaterialRenderFrames.Now;
        var deltaTime = (now - _lastScrollTime).TotalSeconds;
        _scrollVelocity = deltaTime is > 0 and < .1 ? (offset - _lastOffset) / deltaTime : 0;
        _lastScrollTime = now;
        _contentOffset = offset;
        var delta = offset - _lastOffset;
        _lastOffset = offset;
        if (ScrollBehavior == MaterialAppBarScrollBehavior.ExitUntilCollapsed)
        {
            _collapse = Math.Clamp(offset, 0, ExpandedHeight - CollapsedHeight);
            SetAndRaise(IsScrolledProperty, ref _isScrolled, offset > .01);
            UpdateScrollPresentation();
            InvalidateMeasure();
        }
        else ApplyScrollDelta(delta, offset);
        WaitForScrollEnd();
    }
    private void ScrollWheel(object? sender, PointerWheelEventArgs e)
    {
        WaitForScrollEnd();
        if (ScrollBehavior == MaterialAppBarScrollBehavior.ExitUntilCollapsed && e.Delta.Y > 0 && _subscribedScroll is { Offset.Y: <= 0 } && _collapse > 0)
            e.Handled = ApplyScrollDelta(-e.Delta.Y * 48, 0) != 0;
    }
    private void SourcePressed(object? sender, PointerPressedEventArgs e)
    {
        _sourcePointer = e.Pointer; StopSettlement(); _scrollVelocity = 0; _lastScrollTime = MaterialRenderFrames.Now;
    }
    private void SourceReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_sourcePointer != e.Pointer) return;
        _sourcePointer = null;
        StartSettlement((MaterialRenderFrames.Now - _lastScrollTime).TotalMilliseconds <= 100 ? _scrollVelocity : 0);
    }
    private void BarPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ScrollBehavior == MaterialAppBarScrollBehavior.Pinned || !IsEffectivelyEnabled || MaterialGestureOwnership.IsInteractive(e.Source)
            || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        StopSettlement(); _dragPointer = e.Pointer; _dragPoint = e.GetPosition(TopLevel.GetTopLevel(this)); _dragTimestamp = e.Timestamp;
        _scrollVelocity = 0; e.Pointer.Capture(this); e.Handled = true;
    }
    private void BarMoved(object? sender, PointerEventArgs e)
    {
        if (e.Pointer != _dragPointer) return;
        var point = e.GetPosition(TopLevel.GetTopLevel(this)); var delta = _dragPoint.Y - point.Y;
        _scrollVelocity = e.Timestamp > _dragTimestamp ? delta * 1000 / (e.Timestamp - _dragTimestamp) : 0;
        _dragPoint = point; _dragTimestamp = e.Timestamp;
        ApplyScrollDelta(delta, _contentOffset); e.Handled = true;
    }
    private void BarReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.Pointer != _dragPointer) return;
        _dragPointer = null; e.Pointer.Capture(null);
        StartSettlement(e.Timestamp - _dragTimestamp <= 100 ? _scrollVelocity : 0); e.Handled = true;
    }
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    { _dragPointer = null; base.OnPointerCaptureLost(e); }
    private void StopSettlement()
    {
        _settlementPhase = SettlementPhase.None; _settlement?.SetRunning(false);
    }
    private void WaitForScrollEnd()
    {
        // Avalonia wheel/adapter deltas expose no end phase. A quiet frame interval
        // maps that input stream to the same reference settlement as pointer release.
        if (_settlement is null || ScrollBehavior == MaterialAppBarScrollBehavior.Pinned || _dragPointer is not null || _sourcePointer is not null) return;
        _settlementPhase = SettlementPhase.Waiting; _settlement.Restart(); _settlement.SetRunning(true);
    }
    private void PaintCollapse(double value)
    {
        _collapse = Math.Clamp(value, 0, ExpandedHeight - CollapsedHeight);
        UpdateScrollPresentation(); InvalidateMeasure();
    }
    private void StartSettlement(double velocity)
    {
        StopSettlement();
        if (ScrollBehavior == MaterialAppBarScrollBehavior.Pinned || CollapsedFraction < .01 || CollapsedFraction == 1 || VisualRoot is null) return;
        _settlementFrom = _collapse;
        _settlementTarget = CollapsedFraction < .5 ? 0 : ExpandedHeight - CollapsedHeight;
        if (_motion.DefaultEffects.IsInstant) { PaintCollapse(_settlementTarget); return; }
        _scrollDecay = new(velocity);
        _settlementPhase = Math.Abs(velocity) > 1 ? SettlementPhase.Decay : SettlementPhase.Snap;
        _settlement.Restart(); _settlement.SetRunning(true);
    }
    private bool AdvanceSettlement(MaterialFrame frame)
    {
        if (_settlementPhase == SettlementPhase.None) return false;
        if (_settlementPhase == SettlementPhase.Waiting)
        {
            if (frame.Elapsed.TotalMilliseconds < 150) return true;
            StartSettlement(0); return _settlementPhase != SettlementPhase.None;
        }
        if (_motion.DefaultEffects.IsInstant) { PaintCollapse(_settlementTarget); _settlementPhase = SettlementPhase.None; return false; }
        var time = frame.Elapsed.TotalSeconds;
        if (_settlementPhase == SettlementPhase.Decay)
        {
            var sample = _scrollDecay.Sample(time); var raw = _settlementFrom + sample.Position;
            PaintCollapse(raw);
            if (time >= _scrollDecay.Duration || Math.Abs(raw - _collapse) > .5)
            {
                _settlementFrom = _collapse; _settlementTarget = CollapsedFraction < .5 ? 0 : ExpandedHeight - CollapsedHeight;
                _settlementPhase = SettlementPhase.Snap; _settlement.Restart();
            }
            return true;
        }
        var snap = MaterialSpringResponse.Sample(time, _settlementFrom, _settlementTarget, 0, _motion.DefaultEffects);
        var finished = Math.Abs(snap.Value - _settlementTarget) < .01 && Math.Abs(snap.Velocity) < .625;
        PaintCollapse(finished ? _settlementTarget : snap.Value);
        if (finished) _settlementPhase = SettlementPhase.None;
        return !finished;
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) { base.OnAttachedToVisualTree(e); StartScroll(); UpdateNavigationRole(); }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) { StopSettlement(); _dragPointer?.Capture(null); _dragPointer = null; StopWatchingScroll(); ClearNavigationRole(); base.OnDetachedFromVisualTree(e); }
    private void UpdatePresentation()
    {
        var fraction = CollapsedFraction;
        foreach (var variant in Enum.GetValues<MaterialTopAppBarVariant>()) PseudoClasses.Set(":" + variant.ToString().ToLowerInvariant(), Variant == variant);
        PseudoClasses.Set(":subtitle", !string.IsNullOrEmpty(Subtitle));
        PseudoClasses.Set(":title-template", TitleTemplate is not null);
        SetAndRaise(ExpandedHeightProperty, ref _expandedHeight, Math.Max(CollapsedHeight, NominalHeight));
        _collapse = fraction * (ExpandedHeight - CollapsedHeight);
        UpdateScrollPresentation();
        InvalidateMeasure();
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == VariantProperty || change.Property == SubtitleProperty || change.Property == CenterTitleProperty || change.Property == TitleProperty || change.Property == TitleTemplateProperty) UpdatePresentation();
        if (change.Property == ScrollSourceProperty || change.Property == ScrollBehaviorProperty) StartScroll();
        if (change.Property == BackgroundProperty || change.Property == ScrolledBackgroundProperty) UpdateBackground();
        if (change.Property == NavigationContentProperty || change.Property == NavigationContentTemplateProperty) UpdateNavigationRole();
    }
    protected override Type StyleKeyOverride => typeof(MaterialTopAppBar);
    protected override AutomationPeer OnCreateAutomationPeer() => new AppBarAutomationPeer(this);
}

internal sealed class AppBarAutomationPeer : ControlAutomationPeer
{
    public AppBarAutomationPeer(Control owner) : base(owner)
    {
        owner.PropertyChanged += (_, change) =>
        {
            if (change.Property == MaterialTopAppBar.TitleProperty)
                RaisePropertyChangedEvent(Avalonia.Automation.AutomationElementIdentifiers.NameProperty, change.OldValue, GetName());
            if (change.Property == MaterialTopAppBar.CollapsedFractionProperty || change.Property == MaterialTopAppBar.IsScrolledProperty)
                RaisePropertyChangedEvent(Avalonia.Automation.AutomationElementIdentifiers.ItemStatusProperty, null, GetItemStatus());
        };
    }
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ToolBar;
    protected override string? GetNameCore() => base.GetNameCore() ?? (Owner is MaterialTopAppBar bar ? bar.Title : "Page actions");
    protected override string GetItemStatusCore() => Owner is MaterialTopAppBar bar ? bar.CollapsedFraction >= .5 ? "collapsed" : bar.IsScrolled ? "scrolled" : "expanded" : "page-actions";
}
