using System.Collections.Specialized;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using System.Diagnostics;
using Avalonia.Animation.Easings;
using Avalonia.Threading;

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
    private double _presentationPosition, _settleFrom, _settleStart;
    private bool _animating, _attached;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    public MaterialCarousel() { _timer.Tick += (_, _) => Advance(); }
    internal event Action? PresentationChanged;
    internal IReadOnlyList<MaterialCarouselItem> ItemList => _items;
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
        if (change.Property == ItemsSourceProperty)
        {
            if (_collection is not null) _collection.CollectionChanged -= ItemsChanged;
            _collection = ItemsSource as INotifyCollectionChanged;
            if (_collection is not null) _collection.CollectionChanged += ItemsChanged;
            ReloadItems();
        }
        if (change.Property == CurrentIndexProperty)
        {
            _settleFrom = _presentationPosition;
            _settleStart = Now;
            _animating = true;
            Advance();
            UpdatePosition();
        }
        if (change.Property == AnimationTimeProperty || change.Property == MotionDurationProperty || change.Property == MotionEasingProperty) Advance();
        if (change.Property == IsEffectivelyEnabledProperty && !IsEffectivelyEnabled) CancelGesture();
        if (change.Property == LayoutProperty || change.Property == PreferredItemWidthProperty || change.Property == ItemSpacingProperty || change.Property == CornerRadiusProperty || change.Property == FlowDirectionProperty)
        {
            CancelGesture();
            PresentationChanged?.Invoke();
        }
        if (change.Property == EmptyTextProperty) DescribePosition();
        if (change.Property == ItemTemplateProperty || change.Property == EmptyTextProperty) PresentationChanged?.Invoke();
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
        UpdatePosition();
    }
    private void ItemChanged(object? sender, AvaloniaPropertyChangedEventArgs e) { DescribePosition(); PresentationChanged?.Invoke(); }
    private void UpdatePosition()
    {
        var previous = _current;
        SetAndRaise(CurrentItemProperty, ref _current, _items.Count == 0 ? null : _items[CurrentIndex]);
        SetAndRaise(CanMoveNextProperty, ref _canNext, _items.Count > 0 && CurrentIndex < _items.Count - 1);
        SetAndRaise(CanMovePreviousProperty, ref _canPrevious, _items.Count > 0 && CurrentIndex > 0);
        DescribePosition();
        PresentationChanged?.Invoke();
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
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!IsEffectivelyEnabled || _pointer is not null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || IsNestedInteractive(e.Source)) return;
        _start = e.GetPosition(this);
        _gestureInitialPosition = Layout == MaterialCarouselLayout.Uncontained ? _presentationPosition : CurrentIndex;
        _pointer = e.Pointer;
        _dragFraction = 0;
        _pressedItem = (e.Source as Visual)?.GetSelfAndVisualAncestors().OfType<Border>().Select(b => b.Tag).OfType<MaterialCarouselItem>().FirstOrDefault();
        e.Pointer.Capture(this);
        Focus();
    }
    internal static bool IsNestedInteractive(object? source) => source is Visual visual && visual.GetSelfAndVisualAncestors().OfType<Control>().Any(c => c is Button or TextBox or Slider);
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (e.Pointer != _pointer) return;
        var delta = e.GetPosition(this) - _start;
        var vertical = Layout == MaterialCarouselLayout.FullScreen;
        var primary = vertical ? delta.Y : delta.X;
        var cross = vertical ? delta.X : delta.Y;
        if (Math.Abs(primary) < 12 || Math.Abs(primary) < Math.Abs(cross) * 1.5) return;
        var sign = !vertical && FlowDirection == FlowDirection.RightToLeft ? 1 : -1;
        var movement = sign * primary / Math.Max(48, vertical ? Bounds.Height : Math.Min(PreferredItemWidth, Bounds.Width));
        _dragFraction = Layout == MaterialCarouselLayout.Uncontained ? movement : Math.Clamp(movement, -1, 1);
        if (Layout != MaterialCarouselLayout.Uncontained && (!CanMoveNext && _dragFraction > 0 || !CanMovePrevious && _dragFraction < 0)) _dragFraction = 0;
        _animating = false;
        _timer.Stop();
        SetPresentation(Math.Clamp(_gestureInitialPosition + _dragFraction, 0, Math.Max(0, _items.Count - 1)));
        PresentationChanged?.Invoke();
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
        CancelGesture();
        SetPresentation(position);
        if (Layout == MaterialCarouselLayout.Uncontained && !tap && Math.Abs(fraction) > 0)
        {
            SetCurrentValue(CurrentIndexProperty, (int)Math.Round(position, MidpointRounding.AwayFromZero));
            _animating = false;
            _timer.Stop();
            SetPresentation(position);
        }
        else if (fraction >= .25) MoveNext();
        else if (fraction <= -.25) MovePrevious();
        else if (tap && tappedItem is not null && _items.IndexOf(tappedItem) is var index && index >= 0) { if (!MoveTo(index)) FinishAnimation(); }
        else FinishAnimation();
        e.Handled = Math.Abs(fraction) > 0;
    }
    private void CancelGesture()
    {
        var pointer = _pointer;
        _pointer = null;
        _pressedItem = null;
        _dragFraction = 0;
        var freePosition = Layout == MaterialCarouselLayout.Uncontained ? pointer is not null ? _gestureInitialPosition : _presentationPosition : CurrentIndex;
        FinishAnimation();
        if (Layout == MaterialCarouselLayout.Uncontained) SetPresentation(freePosition);
        if (pointer?.Captured == this) pointer.Capture(null);
        PresentationChanged?.Invoke();
    }
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e) { CancelGesture(); base.OnPointerCaptureLost(e); }
    private double Now => AnimationTime?.TotalMilliseconds ?? _clock.Elapsed.TotalMilliseconds;
    private void SetPresentation(double value)
    {
        SetAndRaise(PresentationPositionProperty, ref _presentationPosition, value);
        PresentationChanged?.Invoke();
    }
    private void Advance()
    {
        if (!_animating) return;
        if (MotionDuration == TimeSpan.Zero || !IsEffectivelyEnabled || !_attached) { FinishAnimation(); return; }
        var progress = Math.Clamp((Now - _settleStart) / MotionDuration.TotalMilliseconds, 0, 1);
        if (progress >= 1 || Now < _settleStart) { FinishAnimation(); return; }
        SetPresentation(_settleFrom + (CurrentIndex - _settleFrom) * MotionEasing.Ease(progress));
        if (AnimationTime is null) _timer.Start(); else _timer.Stop();
    }
    private void FinishAnimation() { _animating = false; _timer.Stop(); SetPresentation(CurrentIndex); }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _attached = false;
        CancelGesture();
        if (_collection is not null) _collection.CollectionChanged -= ItemsChanged;
        foreach (var item in _items.Distinct()) item.PropertyChanged -= ItemChanged;
        base.OnDetachedFromVisualTree(e);
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;
        if (_collection is not null) { _collection.CollectionChanged -= ItemsChanged; _collection.CollectionChanged += ItemsChanged; }
        ReloadItems();
    }
}
