using System.Collections.Specialized;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Animation.Easings;
using Avalonia.Material3.Tokens;

namespace Avalonia.Material3.Controls;

public enum MaterialCarouselLayout { MultiBrowse, Uncontained, Hero, FullScreen }
public enum MaterialCarouselItemState { Ready, Loading, Failed }
public sealed class MaterialCarouselItemEventArgs(MaterialCarouselItem item) : EventArgs { public MaterialCarouselItem Item { get; } = item; }

/// <summary>An image and its description, owned and loaded by the host.</summary>
public sealed class MaterialCarouselItem : AvaloniaObject
{
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<MaterialCarouselItem, string?>(nameof(Title));
    public static readonly StyledProperty<IImage?> ImageProperty = AvaloniaProperty.Register<MaterialCarouselItem, IImage?>(nameof(Image));
    public static readonly StyledProperty<MaterialCarouselItemState> StateProperty = AvaloniaProperty.Register<MaterialCarouselItem, MaterialCarouselItemState>(nameof(State), validate: Enum.IsDefined);
    public static readonly StyledProperty<string?> ErrorMessageProperty = AvaloniaProperty.Register<MaterialCarouselItem, string?>(nameof(ErrorMessage));
    public static readonly StyledProperty<object?> ContentProperty = AvaloniaProperty.Register<MaterialCarouselItem, object?>(nameof(Content));
    public string? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public IImage? Image { get => GetValue(ImageProperty); set => SetValue(ImageProperty, value); }
    public MaterialCarouselItemState State { get => GetValue(StateProperty); set => SetValue(StateProperty, value); }
    public string? ErrorMessage { get => GetValue(ErrorMessageProperty); set => SetValue(ErrorMessageProperty, value); }
    public object? Content { get => GetValue(ContentProperty); set => SetValue(ContentProperty, value); }
}

/// <summary>A host-controlled collection with a binding-preserving current position.</summary>
public sealed class MaterialCarousel : TemplatedControl
{
    private static readonly StyledProperty<MaterialSpring> ReducedMotionSpringProperty = AvaloniaProperty.Register<MaterialCarousel, MaterialSpring>("ReducedMotionSpring", MaterialSpringScheme.Expressive.DefaultSpatial);
    public static readonly StyledProperty<MaterialCarouselLayout> LayoutProperty = AvaloniaProperty.Register<MaterialCarousel, MaterialCarouselLayout>(nameof(Layout), validate: Enum.IsDefined);
    public static readonly StyledProperty<double> PreferredItemWidthProperty = AvaloniaProperty.Register<MaterialCarousel, double>(nameof(PreferredItemWidth), 186, validate: value => double.IsFinite(value) && value >= 48);
    public static readonly StyledProperty<double> ItemSpacingProperty = AvaloniaProperty.Register<MaterialCarousel, double>(nameof(ItemSpacing), 0, validate: value => double.IsFinite(value) && value >= 0);
    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty = AvaloniaProperty.Register<MaterialCarousel, IDataTemplate?>(nameof(ItemTemplate));
    public static readonly StyledProperty<string> EmptyTextProperty = AvaloniaProperty.Register<MaterialCarousel, string>(nameof(EmptyText), "No items");
    public static readonly StyledProperty<TimeSpan> MotionDurationProperty = AvaloniaProperty.Register<MaterialCarousel, TimeSpan>(nameof(MotionDuration), TimeSpan.FromMilliseconds(200), validate: t => t >= TimeSpan.Zero);
    public static readonly StyledProperty<IEasing> MotionEasingProperty = AvaloniaProperty.Register<MaterialCarousel, IEasing>(nameof(MotionEasing), new SplineEasing(.2, 0, 0, 1), validate: e => e is not null);
    public static readonly StyledProperty<TimeSpan?> AnimationTimeProperty = AvaloniaProperty.Register<MaterialCarousel, TimeSpan?>(nameof(AnimationTime), validate: t => t is null || t >= TimeSpan.Zero);
    public static readonly DirectProperty<MaterialCarousel, double> PresentationPositionProperty = AvaloniaProperty.RegisterDirect<MaterialCarousel, double>(nameof(PresentationPosition), c => c.PresentationPosition);
    public static readonly StyledProperty<IEnumerable<MaterialCarouselItem>?> ItemsSourceProperty = AvaloniaProperty.Register<MaterialCarousel, IEnumerable<MaterialCarouselItem>?>(nameof(ItemsSource));
    public static readonly StyledProperty<int> CurrentIndexProperty = AvaloniaProperty.Register<MaterialCarousel, int>(nameof(CurrentIndex), 0, defaultBindingMode: BindingMode.TwoWay,
        coerce: (owner, index) => Math.Clamp(index, 0, Math.Max(0, ((MaterialCarousel)owner)._items.Count - 1)));
    public static readonly DirectProperty<MaterialCarousel, MaterialCarouselItem?> CurrentItemProperty = AvaloniaProperty.RegisterDirect<MaterialCarousel, MaterialCarouselItem?>(nameof(CurrentItem), owner => owner.CurrentItem);
    public static readonly DirectProperty<MaterialCarousel, bool> CanMoveNextProperty = AvaloniaProperty.RegisterDirect<MaterialCarousel, bool>(nameof(CanMoveNext), owner => owner.CanMoveNext);
    public static readonly DirectProperty<MaterialCarousel, bool> CanMovePreviousProperty = AvaloniaProperty.RegisterDirect<MaterialCarousel, bool>(nameof(CanMovePrevious), owner => owner.CanMovePrevious);
    public static readonly DirectProperty<MaterialCarousel, string> PositionDescriptionProperty = AvaloniaProperty.RegisterDirect<MaterialCarousel, string>(nameof(PositionDescription), owner => owner.PositionDescription);
    private List<MaterialCarouselItem> _items = [];
    private INotifyCollectionChanged? _collection;
    private MaterialCarouselItem? _current;
    private bool _canNext, _canPrevious;
    private string _positionDescription = "No items";
    private IPointer? _pointer;
    private MaterialCarouselItem? _pressedItem;
    private Point _start;
    private double _gestureInitialPosition;
    private double _dragFraction;
    private double _pointerVelocity, _lastPrimary;
    private double _wheelRemainder;
    private ulong _lastPointerTimestamp;
    private MaterialSplineDecay _decay;
    private double _decayStride;
    private bool _decaying, _freeFling;
    private double _presentationPosition, _settleFrom, _settleTo, _settleStart, _settleVelocity, _fromVelocity, _layoutStart, _layoutProgress = 1;
    private bool _animating, _layoutAnimating, _attached;
    private readonly MaterialFrameLease _frames;
    public MaterialCarousel()
    {
        _frames = MaterialRenderFrames.Bind(this, Advance);
        MaterialPickerSupport.Resource(this, ReducedMotionSpringProperty, "Motion.DefaultSpatial");
    }
    internal event Action? PresentationChanged;
    internal event Action? ContentChanged;
    internal event Action? LayoutChanged;
    internal double LayoutProgress => _layoutProgress;
    internal IReadOnlyList<MaterialCarouselItem> ItemList => _items;
    internal double ItemStride { get; set; }
    private bool HasDurationOverride => IsSet(MotionDurationProperty);
    static MaterialCarousel() => FocusableProperty.OverrideDefaultValue<MaterialCarousel>(true);
    public IEnumerable<MaterialCarouselItem>? ItemsSource { get => GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }
    public int CurrentIndex { get => GetValue(CurrentIndexProperty); set => SetValue(CurrentIndexProperty, value); }
    public MaterialCarouselItem? CurrentItem => _current;
    public bool CanMoveNext => _canNext;
    public bool CanMovePrevious => _canPrevious;
    public string PositionDescription => _positionDescription;
    public MaterialCarouselLayout Layout { get => GetValue(LayoutProperty); set => SetValue(LayoutProperty, value); }
    public double PreferredItemWidth { get => GetValue(PreferredItemWidthProperty); set => SetValue(PreferredItemWidthProperty, value); }
    public double ItemSpacing { get => GetValue(ItemSpacingProperty); set => SetValue(ItemSpacingProperty, value); }
    public IDataTemplate? ItemTemplate { get => GetValue(ItemTemplateProperty); set => SetValue(ItemTemplateProperty, value); }
    public string EmptyText { get => GetValue(EmptyTextProperty); set => SetValue(EmptyTextProperty, value); }
    public TimeSpan MotionDuration { get => GetValue(MotionDurationProperty); set => SetValue(MotionDurationProperty, value); }
    public IEasing MotionEasing { get => GetValue(MotionEasingProperty); set => SetValue(MotionEasingProperty, value); }
    public TimeSpan? AnimationTime { get => GetValue(AnimationTimeProperty); set => SetValue(AnimationTimeProperty, value); }
    public double PresentationPosition => _presentationPosition;
    public event EventHandler? CurrentItemChanged;
    public event EventHandler<MaterialCarouselItemEventArgs>? ItemRetryRequested;
    internal void RequestRetry(MaterialCarouselItem item)
    {
        if (!IsEffectivelyEnabled || !_items.Contains(item) || item.State != MaterialCarouselItemState.Failed) return;
        Focus();
        ItemRetryRequested?.Invoke(this, new(item));
    }
    public bool MoveNext() => MoveTo(CurrentIndex + 1);
    public bool MovePrevious() => MoveTo(CurrentIndex - 1);
    private bool MoveTo(int index)
    {
        if (!IsEffectivelyEnabled || index < 0 || index >= _items.Count || index == CurrentIndex) return false;
        SetCurrentValue(CurrentIndexProperty, index);
        return true;
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (_frames is null) return;
        if (change.Property == ItemsSourceProperty)
        {
            if (_collection is not null) _collection.CollectionChanged -= ItemsChanged;
            _collection = ItemsSource as INotifyCollectionChanged;
            if (_collection is not null) _collection.CollectionChanged += ItemsChanged;
            ReloadItems();
        }
        if (change.Property == CurrentIndexProperty)
        {
            if (!_freeFling) AnimatePosition(CurrentIndex);
            UpdatePosition();
        }
        if (change.Property == AnimationTimeProperty) _frames.SetTime(AnimationTime);
        if (change.Property == MotionDurationProperty || change.Property == MotionEasingProperty || change.Property == ReducedMotionSpringProperty) _frames.Sample();
        if (change.Property == IsEffectivelyEnabledProperty && !MaterialModalPaintScope.IsEnabledForPaint(this)) CancelGesture();
        if (change.Property == LayoutProperty || change.Property == PreferredItemWidthProperty || change.Property == ItemSpacingProperty || change.Property == CornerRadiusProperty || change.Property == FlowDirectionProperty)
        {
            CancelGesture();
            _layoutAnimating = HasDurationOverride && _attached && MaterialModalPaintScope.IsEnabledForPaint(this) && !GetValue(ReducedMotionSpringProperty).IsInstant && MotionDuration > TimeSpan.Zero
                && change.Property != FlowDirectionProperty && change.Property != CornerRadiusProperty;
            _layoutStart = _frames.Elapsed.TotalMilliseconds;
            _layoutProgress = _layoutAnimating ? 0 : 1;
            LayoutChanged?.Invoke();
            _frames.SetRunning(_animating || _layoutAnimating);
        }
        if (change.Property == EmptyTextProperty) DescribePosition();
        if (change.Property == ItemTemplateProperty || change.Property == EmptyTextProperty) ContentChanged?.Invoke();
    }
    private void ItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) => ReloadItems();
    private void ReloadItems()
    {
        CancelGesture();
        foreach (var item in _items.Distinct()) item.PropertyChanged -= ItemChanged;
        var previous = _current;
        _items = ItemsSource?.ToList() ?? [];
        foreach (var item in _items.Distinct()) item.PropertyChanged += ItemChanged;
        var retainedIndex = previous is null ? -1 : _items.IndexOf(previous);
        if (retainedIndex >= 0) SetCurrentValue(CurrentIndexProperty, retainedIndex);
        CoerceValue(CurrentIndexProperty);
        FinishAnimation();
        _layoutAnimating = false; _layoutProgress = 1;
        _frames.SetRunning(false);
        UpdatePosition();
        ContentChanged?.Invoke();
        LayoutChanged?.Invoke();
    }
    private void ItemChanged(object? sender, AvaloniaPropertyChangedEventArgs e) { DescribePosition(); ContentChanged?.Invoke(); }
    private void UpdatePosition()
    {
        var previous = _current;
        SetAndRaise(CurrentItemProperty, ref _current, _items.Count == 0 ? null : _items[CurrentIndex]);
        SetAndRaise(CanMoveNextProperty, ref _canNext, _items.Count > 0 && CurrentIndex < _items.Count - 1);
        SetAndRaise(CanMovePreviousProperty, ref _canPrevious, _items.Count > 0 && CurrentIndex > 0);
        DescribePosition();
        if (!ReferenceEquals(previous, _current)) CurrentItemChanged?.Invoke(this, EventArgs.Empty);
    }
    private void DescribePosition() => SetAndRaise(PositionDescriptionProperty, ref _positionDescription, _current is null ? EmptyText : $"{CurrentIndex + 1} of {_items.Count}: {_current.Title}; {_current.State}");
    protected override AutomationPeer OnCreateAutomationPeer() => new CarouselAutomationPeer(this);
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!IsEffectivelyEnabled) return;
        switch (e.Key)
        {
            case Key.Escape: CancelGesture(); break;
            case Key.Right when Layout != MaterialCarouselLayout.FullScreen: if (FlowDirection == FlowDirection.RightToLeft) MovePrevious(); else MoveNext(); break;
            case Key.Left when Layout != MaterialCarouselLayout.FullScreen: if (FlowDirection == FlowDirection.RightToLeft) MoveNext(); else MovePrevious(); break;
            case Key.Down when Layout == MaterialCarouselLayout.FullScreen: MoveNext(); break;
            case Key.Up when Layout == MaterialCarouselLayout.FullScreen: MovePrevious(); break;
            case Key.PageDown: MoveNext(); break;
            case Key.PageUp: MovePrevious(); break;
            case Key.Home: MoveTo(0); break;
            case Key.End: MoveTo(_items.Count - 1); break;
            default: return;
        }
        e.Handled = true;
    }
    // Desktop wheel input reuses item navigation and its existing settle recipe.
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (e.Handled || !IsEffectivelyEnabled || _pointer is not null ||
            (e.KeyModifiers & ~KeyModifiers.Shift) != KeyModifiers.None) return;
        var vertical = Layout == MaterialCarouselLayout.FullScreen;
        var shifted = !vertical && e.KeyModifiers.HasFlag(KeyModifiers.Shift) && e.Delta.Y != 0;
        if (!shifted && (vertical ? Math.Abs(e.Delta.Y) < Math.Abs(e.Delta.X) : Math.Abs(e.Delta.X) < Math.Abs(e.Delta.Y)))
        { _wheelRemainder = 0; return; }
        var delta = vertical ? e.Delta.Y
            : shifted ? e.Delta.Y : e.Delta.X;
        if (delta == 0) return;
        if (!double.IsFinite(delta)) return;
        var movement = -delta;
        if (!vertical && FlowDirection == FlowDirection.RightToLeft) movement = -movement;
        if (movement > 0 && !CanMoveNext || movement < 0 && !CanMovePrevious)
        { _wheelRemainder = 0; return; }
        _wheelRemainder += movement;
        // Ten 0.1 deltas form one tick even when binary addition lands just below 1.
        var steps = Math.Truncate(_wheelRemainder + Math.Sign(_wheelRemainder) * 1e-12);
        _wheelRemainder -= steps;
        if (steps != 0) MoveTo((int)Math.Clamp(CurrentIndex + steps, 0, _items.Count - 1));
        e.Handled = true;
    }
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!IsEffectivelyEnabled || _pointer is not null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || MaterialGestureOwnership.IsInteractive(e.Source)) return;
        _start = e.GetPosition(this);
        if (_frames.IsRunning) _frames.Sample();
        _gestureInitialPosition = _presentationPosition;
        _animating = false; _settleVelocity = 0;
        _pointer = e.Pointer;
        _lastPointerTimestamp = e.Timestamp; _lastPrimary = _pointerVelocity = 0;
        _dragFraction = 0;
        _pressedItem = (e.Source as Visual)?.GetSelfAndVisualAncestors().OfType<Border>().Select(b => b.Tag).OfType<MaterialCarouselItem>().FirstOrDefault();
        e.Pointer.Capture(this);
        Focus();
    }
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (e.Pointer != _pointer) return;
        var delta = e.GetPosition(this) - _start;
        var vertical = Layout == MaterialCarouselLayout.FullScreen;
        var primary = vertical ? delta.Y : delta.X;
        var cross = vertical ? delta.X : delta.Y;
        if (e.Timestamp > _lastPointerTimestamp)
            _pointerVelocity = -(primary - _lastPrimary) * 1000 / (e.Timestamp - _lastPointerTimestamp);
        _lastPrimary = primary; _lastPointerTimestamp = e.Timestamp;
        if (_dragFraction == 0 && (Math.Abs(primary) < 12 || Math.Abs(primary) < Math.Abs(cross) * 1.5)) return;
        // Owner-local coordinates already cross Avalonia's RTL mirror: logical negative means forward.
        var movement = -primary / Math.Max(.001, vertical ? Bounds.Height : ItemStride > 0 ? ItemStride : Math.Min(PreferredItemWidth, Bounds.Width));
        _dragFraction = Layout == MaterialCarouselLayout.Uncontained ? movement : Math.Clamp(movement, -1, 1);
        if (Layout != MaterialCarouselLayout.Uncontained && (!CanMoveNext && _dragFraction > 0 || !CanMovePrevious && _dragFraction < 0)) _dragFraction = 0;
        _animating = false;
        _frames.SetRunning(_layoutAnimating);
        SetPresentation(Math.Clamp(_gestureInitialPosition + _dragFraction, 0, Math.Max(0, _items.Count - 1)));
        e.Handled = true;
    }
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (e.Pointer != _pointer) return;
        var fraction = _dragFraction;
        var position = _presentationPosition;
        var tappedItem = _pressedItem;
        var delta = e.GetPosition(this) - _start;
        var tap = Math.Abs(delta.X) <= 12 && Math.Abs(delta.Y) <= 12;
        var velocity = e.Timestamp - _lastPointerTimestamp > 100 ? 0 : _pointerVelocity;
        CancelGesture();
        SetPresentation(position);
        if (Layout == MaterialCarouselLayout.Uncontained && !tap && Math.Abs(fraction) > 0)
        {
            _freeFling = true;
            try { SetCurrentValue(CurrentIndexProperty, (int)Math.Round(position, MidpointRounding.AwayFromZero)); }
            finally { _freeFling = false; }
            _decayStride = Math.Max(.001, ItemStride > 0 ? ItemStride : PreferredItemWidth);
            _decay = new(velocity);
            _settleVelocity = velocity / _decayStride;
            AnimatePosition(Math.Clamp(position + _decay.Distance / _decayStride, 0, Math.Max(0, _items.Count - 1)), decay: true);
        }
        else if (tap && tappedItem is not null && _items.IndexOf(tappedItem) is var index && index >= 0) { if (!MoveTo(index)) FinishAnimation(); }
        else
        {
            var stride = Math.Max(.001, Layout == MaterialCarouselLayout.FullScreen ? Bounds.Height : ItemStride > 0 ? ItemStride : PreferredItemWidth);
            _settleVelocity = velocity / stride;
            // PagerDefaults:400 DIP/s selects the bound in the fling direction;
            // low velocity uses the0.5 positional threshold. atMost(1) has no decay approach.
            var target = Math.Abs(velocity) >= 400
                ? velocity > 0 ? (int)Math.Ceiling(position) : (int)Math.Floor(position)
                : fraction > .5 ? CurrentIndex + 1 : fraction < -.5 ? CurrentIndex - 1 : CurrentIndex;
            target = Math.Clamp(target, Math.Max(0, CurrentIndex - 1), Math.Min(Math.Max(0, _items.Count - 1), CurrentIndex + 1));
            if (!MoveTo(target)) AnimatePosition(target);
        }
        e.Handled = Math.Abs(fraction) > 0;
    }
    private void CancelGesture()
    {
        _wheelRemainder = 0;
        var pointer = _pointer;
        _pointer = null;
        _pressedItem = null;
        _dragFraction = 0;
        var freePosition = Layout == MaterialCarouselLayout.Uncontained ? pointer is not null ? _gestureInitialPosition : _presentationPosition : CurrentIndex;
        FinishAnimation();
        if (Layout == MaterialCarouselLayout.Uncontained) SetPresentation(freePosition);
        if (pointer?.Captured == this) pointer.Capture(null);
    }
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e) { CancelGesture(); base.OnPointerCaptureLost(e); }
    private bool SetPresentation(double value, bool notify = true)
    {
        if (_presentationPosition == value) return false;
        SetAndRaise(PresentationPositionProperty, ref _presentationPosition, value);
        if (notify) PresentationChanged?.Invoke();
        return true;
    }
    private bool Advance(MaterialFrame frame)
    {
        var changed = false;
        var instant = frame.Rewound || GetValue(ReducedMotionSpringProperty).IsInstant || HasDurationOverride && MotionDuration == TimeSpan.Zero || !MaterialModalPaintScope.IsEnabledForPaint(this) || !_attached;
        var time = frame.Elapsed.TotalMilliseconds;
        if (_animating)
        {
            double value;
            if (instant) { value = _settleTo; _settleVelocity = 0; _animating = _decaying = false; }
            else if (_decaying)
            {
                var elapsed = Math.Max(0, (time - _settleStart) / 1000);
                var sample = _decay.Sample(elapsed);
                var raw = _settleFrom + sample.Position / _decayStride;
                value = Math.Clamp(raw, 0, Math.Max(0, _items.Count - 1));
                _settleVelocity = sample.Velocity / _decayStride;
                if (elapsed >= _decay.Duration || value != raw)
                { _animating = _decaying = false; _settleVelocity = 0; }
            }
            else if (HasDurationOverride)
            {
                var progress = Math.Clamp((time - _settleStart) / MotionDuration.TotalMilliseconds, 0, 1);
                value = _settleFrom + (_settleTo - _settleFrom) * MotionEasing.Ease(progress);
                if (progress >= 1) { _animating = false; _settleVelocity = 0; }
            }
            else
            {
                var elapsed = Math.Max(0, (time - _settleStart) / 1000);
                (value, _settleVelocity) = MaterialSpringResponse.Sample(elapsed, _settleFrom, _settleTo, _fromVelocity, new(1, 200));
                if (Math.Abs(value - _settleTo) < .001 && Math.Abs(_settleVelocity) < .01 || elapsed >= 10)
                { value = _settleTo; _settleVelocity = 0; _animating = false; }
            }
            changed |= SetPresentation(value, false);
            if (!_animating && Layout == MaterialCarouselLayout.Uncontained)
            {
                _freeFling = true;
                try { SetCurrentValue(CurrentIndexProperty, (int)Math.Round(value, MidpointRounding.AwayFromZero)); }
                finally { _freeFling = false; }
            }
        }
        if (_layoutAnimating)
        {
            var progress = instant ? 1 : Math.Clamp((time - _layoutStart) / MotionDuration.TotalMilliseconds, 0, 1);
            var next = MotionEasing.Ease(progress);
            changed |= next != _layoutProgress;
            _layoutProgress = next;
            if (progress >= 1) _layoutAnimating = false;
        }
        if (changed) PresentationChanged?.Invoke();
        return _animating || _layoutAnimating;
    }
    private void FinishAnimation()
    {
        _animating = _decaying = false; _settleVelocity = 0;
        SetPresentation(CurrentIndex);
        _frames?.SetRunning(_layoutAnimating);
    }
    private void AnimatePosition(double target, bool decay = false)
    {
        if (_frames.IsRunning) _frames.Sample();
        _decaying = decay && !HasDurationOverride;
        _settleFrom = _presentationPosition; _fromVelocity = _settleVelocity;
        _settleTo = target; _settleStart = _frames.Elapsed.TotalMilliseconds;
        _animating = true;
        Advance(new(_frames.Elapsed, TimeSpan.Zero, false));
        _frames.SetRunning(_animating || _layoutAnimating);
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _attached = false;
        CancelGesture();
        _layoutAnimating = false; _layoutProgress = 1;
        _frames.SetRunning(false);
        if (_collection is not null) _collection.CollectionChanged -= ItemsChanged;
        foreach (var item in _items.Distinct()) item.PropertyChanged -= ItemChanged;
        base.OnDetachedFromVisualTree(e);
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;
        _frames.SetTime(AnimationTime);
        if (_collection is not null) { _collection.CollectionChanged -= ItemsChanged; _collection.CollectionChanged += ItemsChanged; }
        ReloadItems();
    }
}
