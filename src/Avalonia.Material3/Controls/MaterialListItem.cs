using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Avalonia.Material3.Controls;

/// <summary>The minimum line layout; longer content is never truncated by the default template.</summary>
public enum MaterialListLines { One, Two, Three }

/// <summary>A variable-height Material list row with independent content and action slots.</summary>
public class MaterialListItem : MaterialContentItem
{
    public static readonly StyledProperty<MaterialListLines> LinesProperty = AvaloniaProperty.Register<MaterialListItem, MaterialListLines>(nameof(Lines), validate: value => Enum.IsDefined(value));
    public static readonly StyledProperty<bool> IsExpressiveProperty = AvaloniaProperty.Register<MaterialListItem, bool>(nameof(IsExpressive));
    public bool IsExpressive { get => GetValue(IsExpressiveProperty); set => SetValue(IsExpressiveProperty, value); }
    public static readonly StyledProperty<bool> IsExpandableProperty = AvaloniaProperty.Register<MaterialListItem, bool>(nameof(IsExpandable));
    public static readonly StyledProperty<bool> IsExpandedProperty = AvaloniaProperty.Register<MaterialListItem, bool>(nameof(IsExpanded));
    public static readonly StyledProperty<object?> ExpandedContentProperty = AvaloniaProperty.Register<MaterialListItem, object?>(nameof(ExpandedContent));
    public static readonly StyledProperty<IDataTemplate?> ExpandedContentTemplateProperty = AvaloniaProperty.Register<MaterialListItem, IDataTemplate?>(nameof(ExpandedContentTemplate));
    public static readonly StyledProperty<bool> IsRevealEnabledProperty = AvaloniaProperty.Register<MaterialListItem, bool>(nameof(IsRevealEnabled));
    public static readonly StyledProperty<bool> IsRevealedProperty = AvaloniaProperty.Register<MaterialListItem, bool>(nameof(IsRevealed));
    public static readonly StyledProperty<double> RevealWidthProperty = AvaloniaProperty.Register<MaterialListItem, double>(nameof(RevealWidth), 128, validate: value => double.IsFinite(value) && value >= 48);
    public static readonly DirectProperty<MaterialListItem, Thickness> RevealTranslationProperty = AvaloniaProperty.RegisterDirect<MaterialListItem, Thickness>(nameof(RevealTranslation), item => item.RevealTranslation);
    public static readonly DirectProperty<MaterialListItem, bool> AreRevealActionsVisibleProperty = AvaloniaProperty.RegisterDirect<MaterialListItem, bool>(nameof(AreRevealActionsVisible), item => item.AreRevealActionsVisible);
    private bool _areRevealActionsVisible;
    private Thickness _revealTranslation;
    public bool AreRevealActionsVisible => _areRevealActionsVisible;
    public double RevealWidth { get => GetValue(RevealWidthProperty); set => SetValue(RevealWidthProperty, value); }
    /// <summary>Layout offset used by the default reveal template. Zero when closed.</summary>
    public Thickness RevealTranslation => _revealTranslation;
    public static readonly StyledProperty<object?> RevealedActionsProperty = AvaloniaProperty.Register<MaterialListItem, object?>(nameof(RevealedActions));
    public static readonly StyledProperty<IDataTemplate?> RevealedActionsTemplateProperty = AvaloniaProperty.Register<MaterialListItem, IDataTemplate?>(nameof(RevealedActionsTemplate));
    public static readonly StyledProperty<bool> IsReorderEnabledProperty = AvaloniaProperty.Register<MaterialListItem, bool>(nameof(IsReorderEnabled));
    public bool IsReorderEnabled { get => GetValue(IsReorderEnabledProperty); set => SetValue(IsReorderEnabledProperty, value); }
    public static readonly StyledProperty<bool> IsReorderActionsVisibleProperty = AvaloniaProperty.Register<MaterialListItem, bool>(nameof(IsReorderActionsVisible));
    public bool IsReorderActionsVisible { get => GetValue(IsReorderActionsVisibleProperty); set => SetValue(IsReorderActionsVisibleProperty, value); }
    private Button? _moveUpButton;
    private Button? _moveDownButton;
    public static readonly DirectProperty<MaterialListItem, bool> IsReorderingProperty = AvaloniaProperty.RegisterDirect<MaterialListItem, bool>(nameof(IsReordering), item => item.IsReordering);
    private bool _isReordering;
    private Button? _reorderHandle;
    private MaterialList? _reorderList;
    private IPointer? _reorderPointer;
    private double _reorderStartY;
    private bool _reorderMoved;
    private ITransform? _savedTransform;
    public bool IsReordering => _isReordering;
    private Button? _expandButton;
    private Button? _revealButton;
    private Point? _gestureStart;
    private IPointer? _gesturePointer;
    private double _gestureStartOffset;
    private bool _cancelPrimary;
    public bool IsRevealEnabled { get => GetValue(IsRevealEnabledProperty); set => SetValue(IsRevealEnabledProperty, value); }
    public bool IsRevealed { get => GetValue(IsRevealedProperty); set => SetValue(IsRevealedProperty, value); }
    public object? RevealedActions { get => GetValue(RevealedActionsProperty); set => SetValue(RevealedActionsProperty, value); }
    public IDataTemplate? RevealedActionsTemplate { get => GetValue(RevealedActionsTemplateProperty); set => SetValue(RevealedActionsTemplateProperty, value); }
    public MaterialListLines Lines { get => GetValue(LinesProperty); set => SetValue(LinesProperty, value); }
    public bool IsExpandable { get => GetValue(IsExpandableProperty); set => SetValue(IsExpandableProperty, value); }
    public bool IsExpanded { get => GetValue(IsExpandedProperty); set => SetValue(IsExpandedProperty, value); }
    public object? ExpandedContent { get => GetValue(ExpandedContentProperty); set => SetValue(ExpandedContentProperty, value); }
    public IDataTemplate? ExpandedContentTemplate { get => GetValue(ExpandedContentTemplateProperty); set => SetValue(ExpandedContentTemplateProperty, value); }
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        EndReorder();
        if (_reorderHandle is not null)
        {
            _reorderHandle.RemoveHandler(PointerPressedEvent, ReorderPressed);
            _reorderHandle.RemoveHandler(PointerMovedEvent, ReorderMoved);
            _reorderHandle.RemoveHandler(PointerReleasedEvent, ReorderReleased);
            _reorderHandle.PointerCaptureLost -= ReorderCaptureLost;
            _reorderHandle.Click -= ReorderClicked;
        }
        if (_moveUpButton is not null) _moveUpButton.Click -= MoveUpClicked;
        if (_moveDownButton is not null) _moveDownButton.Click -= MoveDownClicked;
        if (_expandButton is not null) _expandButton.Click -= ExpandClicked;
        if (_revealButton is not null) _revealButton.Click -= RevealClicked;
        base.OnApplyTemplate(e);
        _expandButton = e.NameScope.Find<Button>("PART_ExpandButton");
        _revealButton = e.NameScope.Find<Button>("PART_RevealButton");
        if (_expandButton is not null) _expandButton.Click += ExpandClicked;
        if (_revealButton is not null) _revealButton.Click += RevealClicked;
        _reorderHandle = e.NameScope.Find<Button>("PART_ReorderHandle");
        if (_reorderHandle is not null)
        {
            _reorderHandle.AddHandler(PointerPressedEvent, ReorderPressed, RoutingStrategies.Tunnel);
            _reorderHandle.AddHandler(PointerMovedEvent, ReorderMoved, RoutingStrategies.Tunnel);
            _reorderHandle.AddHandler(PointerReleasedEvent, ReorderReleased, RoutingStrategies.Tunnel);
            _reorderHandle.PointerCaptureLost += ReorderCaptureLost;
            _reorderHandle.Click += ReorderClicked;
        }
        _moveUpButton = e.NameScope.Find<Button>("PART_MoveUpButton");
        _moveDownButton = e.NameScope.Find<Button>("PART_MoveDownButton");
        if (_moveUpButton is not null) _moveUpButton.Click += MoveUpClicked;
        if (_moveDownButton is not null) _moveDownButton.Click += MoveDownClicked;
    }
    private void ReorderClicked(object? sender, RoutedEventArgs e)
    {
        if (IsReorderEnabled && IsEffectivelyEnabled) SetCurrentValue(IsReorderActionsVisibleProperty, !IsReorderActionsVisible);
    }
    private void MoveUpClicked(object? sender, RoutedEventArgs e) => MoveBy(-1);
    private void MoveDownClicked(object? sender, RoutedEventArgs e) => MoveBy(1);
    private void MoveBy(int delta)
    {
        var list = this.GetVisualAncestors().OfType<MaterialList>().FirstOrDefault();
        if (list is not null) list.MoveItem(this, list.Children.IndexOf(this) + delta);
    }
    private void ReorderPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!IsReorderEnabled || !IsEffectivelyEnabled || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        _reorderList = this.GetVisualAncestors().OfType<MaterialList>().FirstOrDefault();
        if (_reorderList is null || !_reorderList.Children.Contains(this)) return;
        _reorderStartY = e.GetPosition(_reorderList).Y;
        _reorderMoved = false;
        _savedTransform = RenderTransform;
        _reorderPointer = e.Pointer;
        SetAndRaise(IsReorderingProperty, ref _isReordering, true);
        PseudoClasses.Set(":reordering", true);
        e.Pointer.Capture(_reorderHandle);
        Focus();
        e.Handled = true;
    }
    private void ReorderMoved(object? sender, PointerEventArgs e)
    {
        if (!IsReordering || e.Pointer != _reorderPointer || _reorderList is null) return;
        var delta = e.GetPosition(_reorderList).Y - _reorderStartY;
        _reorderMoved |= Math.Abs(delta) > 16;
        if (_reorderMoved) SetCurrentValue(RenderTransformProperty, new TranslateTransform(0, delta));
        e.Handled = true;
    }
    private void ReorderReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!IsReordering || e.Pointer != _reorderPointer || _reorderList is null) return;
        var list = _reorderList;
        var y = e.GetPosition(list).Y;
        if (list.Children.Count == 0) { EndReorder(); e.Handled = true; return; }
        var dragged = _reorderMoved || Math.Abs(y - _reorderStartY) > 16;
        var index = list.Children.Select((child, i) => (Distance: Math.Abs(child.Bounds.Center.Y - y), Index: i)).MinBy(candidate => candidate.Distance).Index;
        EndReorder();
        if (dragged) list.MoveItem(this, index);
        else ReorderClicked(sender, new RoutedEventArgs());
        e.Handled = true;
    }
    private void ReorderCaptureLost(object? sender, PointerCaptureLostEventArgs e) => EndReorder();
    private void EndReorder()
    {
        if (!IsReordering) return;
        var pointer = _reorderPointer;
        _reorderPointer = null;
        _reorderList = null;
        SetCurrentValue(RenderTransformProperty, _savedTransform);
        SetAndRaise(IsReorderingProperty, ref _isReordering, false);
        PseudoClasses.Set(":reordering", false);
        pointer?.Capture(null);
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        EndReorder();
        ResetRevealGesture();
        base.OnDetachedFromVisualTree(e);
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new ListItemPeer(this);
    private sealed class ListItemPeer : MaterialContentItemAutomationPeer, IExpandCollapseProvider
    {
        private readonly MaterialListItem _owner;
        public ListItemPeer(MaterialListItem owner) : base(owner)
        {
            _owner = owner;
            owner.PropertyChanged += (_, e) =>
            {
                if (e.Property == IsExpandedProperty)
                    RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty, (bool)e.OldValue! ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed, (bool)e.NewValue! ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
            };
        }
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ListItem;
        protected override object? GetProviderCore(Type providerType) => providerType == typeof(IExpandCollapseProvider) && !_owner.IsExpandable ? null : base.GetProviderCore(providerType);
        public ExpandCollapseState ExpandCollapseState => _owner.IsExpanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
        public bool ShowsMenu => false;
        public void Expand() { EnsureEnabled(); if (_owner.IsExpandable) _owner.SetCurrentValue(IsExpandedProperty, true); }
        public void Collapse() { EnsureEnabled(); if (_owner.IsExpandable) _owner.SetCurrentValue(IsExpandedProperty, false); }
    }
    private void ExpandClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ToggleExpanded();
    private void ToggleExpanded()
    {
        if (IsExpandable && IsEffectivelyEnabled) SetCurrentValue(IsExpandedProperty, !IsExpanded);
    }
    private void RevealClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (IsRevealEnabled && IsEffectivelyEnabled) SetCurrentValue(IsRevealedProperty, !IsRevealed);
    }
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (IsEffectivelyEnabled && IsInteractive && IsRevealEnabled && !IsNestedInput(e) && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _gestureStart = e.GetPosition(this);
            _gestureStartOffset = IsRevealed ? -RevealWidth : 0;
            _gesturePointer = e.Pointer;
            e.Pointer.Capture(this);
        }
        base.OnPointerPressed(e);
    }
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_gestureStart is not { } start || e.Pointer != _gesturePointer) return;
        var delta = e.GetPosition(this) - start;
        if (Math.Abs(delta.X) > 16 && Math.Abs(delta.X) > Math.Abs(delta.Y) * 1.5)
            SetRevealOffset(Math.Clamp(_gestureStartOffset + delta.X, -RevealWidth, 0));
    }
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (_gestureStart is { } start && e.Pointer == _gesturePointer && IsEffectivelyEnabled)
        {
            var delta = e.GetPosition(this) - start;
            _cancelPrimary = Math.Abs(delta.X) > 16 || Math.Abs(delta.Y) > 16;
            if (Math.Abs(delta.X) > 16 && Math.Abs(delta.X) > Math.Abs(delta.Y) * 1.5)
            {
                var exposure = Math.Clamp(-_gestureStartOffset - delta.X, 0, RevealWidth);
                SetCurrentValue(IsRevealedProperty, exposure >= Math.Min(48, RevealWidth / 2));
            }
        }
        try { base.OnPointerReleased(e); }
        finally { ResetRevealGesture(); _cancelPrimary = false; }
    }
    private void SetRevealOffset(double offset)
    {
        SetAndRaise(RevealTranslationProperty, ref _revealTranslation, new(offset, 0, -offset, 0));
        SetAndRaise(AreRevealActionsVisibleProperty, ref _areRevealActionsVisible, IsRevealed || offset < 0);
    }
    private void ResetRevealGesture()
    {
        var pointer = _gesturePointer;
        _gestureStart = null;
        _gesturePointer = null;
        SetRevealOffset(IsRevealed ? -RevealWidth : 0);
        if (pointer?.Captured == this) pointer.Capture(null);
    }
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        ResetRevealGesture();
        base.OnPointerCaptureLost(e);
    }
    protected override void OnClick()
    {
        if (!_cancelPrimary) base.OnClick();
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape && IsReordering)
        {
            EndReorder();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && IsReorderActionsVisible)
        {
            SetCurrentValue(IsReorderActionsVisibleProperty, false);
            Focus();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && IsRevealed)
        {
            SetCurrentValue(IsRevealedProperty, false);
            Focus();
            e.Handled = true;
        }
        else if (e.Key == Key.Right && e.KeyModifiers == KeyModifiers.Alt && IsRevealEnabled)
        {
            SetCurrentValue(IsRevealedProperty, true);
            e.Handled = true;
        }
        else if (e.KeyModifiers == KeyModifiers.Alt && e.Key is Key.Up or Key.Down && IsReorderEnabled && IsEffectivelyEnabled)
        {
            MoveBy(e.Key == Key.Up ? -1 : 1);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && e.KeyModifiers == KeyModifiers.Alt && IsExpandable)
        {
            ToggleExpanded();
            e.Handled = true;
        }
        else base.OnKeyDown(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if ((change.Property == IsReorderEnabledProperty && !IsReorderEnabled) || (change.Property == IsEffectivelyEnabledProperty && !IsEffectivelyEnabled))
            EndReorder();
        if ((change.Property == IsRevealEnabledProperty && !IsRevealEnabled) || (change.Property == IsEffectivelyEnabledProperty && !IsEffectivelyEnabled) || (change.Property == IsInteractiveProperty && !IsInteractive))
            ResetRevealGesture();
        if (change.Property == IsExpressiveProperty) PseudoClasses.Set(":expressive", IsExpressive);
        if (change.Property == IsExpandedProperty) PseudoClasses.Set(":expanded", IsExpanded);
        if (change.Property == IsRevealedProperty || change.Property == RevealWidthProperty)
        {
            PseudoClasses.Set(":revealed", IsRevealed);
            SetRevealOffset(IsRevealed ? -RevealWidth : 0);
        }
        if (change.Property == LinesProperty)
        {
            PseudoClasses.Set(":two-line", Lines == MaterialListLines.Two);
            PseudoClasses.Set(":three-line", Lines == MaterialListLines.Three);
        }
    }
}
