using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Avalonia.Material3.Controls;

internal interface IMaterialActionDisclosure
{
    bool IsRevealing { get; }
    event Action? Settled;
}

internal sealed class MaterialFabMenuDisclosure : Decorator, IMaterialActionDisclosure
{
    public static readonly StyledProperty<bool> IsExpandedProperty = AvaloniaProperty.Register<MaterialFabMenuDisclosure, bool>(nameof(IsExpanded));
    public bool IsExpanded { get => GetValue(IsExpandedProperty); set => SetValue(IsExpandedProperty, value); }
    public bool IsRevealing { get; private set; }
    public event Action? Settled;
    public MaterialFabMenuDisclosure() { ClipToBounds = true; UseLayoutRounding = false; IsEnabled = IsHitTestVisible = IsVisible = false; }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != IsExpandedProperty) return;
        IsRevealing = true;
        IsEnabled = IsHitTestVisible = IsExpanded;
        if (IsExpanded) IsVisible = true;
    }
    internal void Complete()
    {
        if (!IsRevealing) return;
        IsRevealing = false; IsVisible = IsExpanded;
        Settled?.Invoke();
    }
}

internal sealed class MaterialFabMenuItemsControl : ItemsControl
{
    protected override Type StyleKeyOverride => typeof(ItemsControl);
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    { recycleKey = null; return true; }
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new MaterialFabMenuItemMask();
}

// The child keeps its native measured width; the surface and hit clip follow the animated mask.
internal sealed class MaterialFabMenuItemMask : ContentPresenter
{
    private readonly MaterialMotionValue _width, _alpha;
    private readonly MaterialMotionSettings _motion;
    private Size _fullSize;
    private MaterialFabMenuItem? _item;
    private readonly Backdrop _backdrop;
    private bool _target;
    internal event Action? Changed;
    internal bool HasPaint => _alpha.Value > 0;
    internal bool AtTarget => _width.Value == (_target ? 1 : 0) && _alpha.Value == (_target ? 1 : 0);
    public MaterialFabMenuItemMask()
    {
        UseLayoutRounding = false;
        _backdrop = new(this) { IsHitTestVisible = false, ZIndex = -1 };
        VisualChildren.Add(_backdrop);
        _width = new(this, 0, _ => { InvalidateMeasure(); Changed?.Invoke(); });
        _alpha = new(this, 0, value => { Opacity = Math.Clamp(value, 0, 1); Changed?.Invoke(); });
        _motion = new(this, () => SetVisible(_target));
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != ChildProperty) return;
        if (_item is not null) _item.SetMenuMask(false);
        _item = Child as MaterialFabMenuItem;
        _item?.SetMenuMask(true);
        if (_backdrop is not null && !VisualChildren.Contains(_backdrop)) VisualChildren.Add(_backdrop);
    }
    internal void SetVisible(bool visible, bool initial = false)
    {
        _target = visible;
        if (initial || !_motion.IsAttached)
        { _width.Snap(visible ? 1 : 0); _alpha.Snap(visible ? 1 : 0); }
        else
        { _width.Spring(visible ? 1 : 0, _motion.FastSpatial); _alpha.Spring(visible ? 1 : 0, _motion.FastEffects); }
        InvalidateMeasure();
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        base.MeasureOverride(availableSize);
        _fullSize = Child?.DesiredSize ?? default;
        return new Size(_fullSize.Width * Math.Max(0, _width.Value), _fullSize.Height);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var radius = Math.Min(finalSize.Width, finalSize.Height) / 2;
        Clip = new RectangleGeometry(new Rect(finalSize), radius, radius);
        if (!VisualChildren.Contains(_backdrop)) VisualChildren.Add(_backdrop);
        _backdrop.Measure(finalSize); _backdrop.Arrange(new Rect(finalSize));
        Child?.Arrange(new Rect(finalSize.Width - _fullSize.Width, 0, _fullSize.Width, finalSize.Height));
        return finalSize;
    }
    private sealed class Backdrop(MaterialFabMenuItemMask owner) : Control
    {
        public override void Render(DrawingContext context)
        {
            if (owner._item is null || !owner.HasPaint || Bounds.Width <= 10 || Bounds.Height <= 10) return;
            var rect = new Rect(5, 5, Bounds.Width - 10, Bounds.Height - 10);
            context.DrawRectangle(owner._item.Background, null, new RoundedRect(rect, Math.Min(rect.Width, rect.Height) / 2));
        }
    }
}

// Pinned FloatingActionButtonMenuItemColumn: SlowEffects integer count, nearest item first.
internal sealed class MaterialFabMenuItemsPanel : Panel
{
    private MaterialFabMenu? _owner;
    private MaterialFabMenuDisclosure? _disclosure;
    private readonly MaterialMotionValue _count;
    private readonly MaterialMotionSettings _motion;
    private readonly HashSet<MaterialFabMenuItemMask> _tracked = [];
    private bool _initial = true;
    public MaterialFabMenuItemsPanel()
    {
        UseLayoutRounding = false;
        _count = new(this, 0, _ => UpdateTargets());
        _motion = new(this, UpdateMotion);
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _owner = this.GetVisualAncestors().OfType<MaterialFabMenu>().FirstOrDefault();
        _disclosure = this.GetVisualAncestors().OfType<MaterialFabMenuDisclosure>().FirstOrDefault();
        if (_owner is not null) _owner.PropertyChanged += OwnerChanged;
        _initial = true; UpdateMotion();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_owner is not null) _owner.PropertyChanged -= OwnerChanged;
        foreach (var mask in _tracked) mask.Changed -= Changed;
        _tracked.Clear(); _owner = null; _disclosure = null;
        base.OnDetachedFromVisualTree(e);
    }
    private void OwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    { if (change.Property == MaterialFabMenu.IsExpandedProperty) UpdateMotion(); }
    private void UpdateMotion()
    {
        if (_owner is null) return;
        var target = _owner.IsExpanded ? Children.Count : 0;
        if (_initial) _count.Snap(target);
        else _count.Spring(target, _motion.SlowEffects, 1);
        UpdateTargets();
    }
    private void UpdateTargets()
    {
        if (_owner is null) return;
        var visibleCount = Math.Clamp((int)Math.Floor(_count.Value + .5), 0, Children.Count);
        for (var i = 0; i < Children.Count; i++)
            if (Children[i] is MaterialFabMenuItemMask mask)
            {
                if (_tracked.Add(mask)) mask.Changed += Changed;
                mask.SetVisible(i >= Children.Count - visibleCount, _initial);
            }
        _initial = false; Changed();
    }
    private void Changed()
    {
        InvalidateMeasure();
        var target = _owner?.IsExpanded == true ? Children.Count : 0;
        if (_count.Value == target && _tracked.All(mask => mask.AtTarget)) _disclosure?.Complete();
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        if (_tracked.Count != Children.Count) UpdateMotion();
        var width = 0d; var height = 0d;
        foreach (var child in Children)
        { child.Measure(new Size(availableSize.Width, double.PositiveInfinity)); width = Math.Max(width, child.DesiredSize.Width); height += child.DesiredSize.Height; }
        height += Math.Max(0, Children.Count - 1) * 4;
        return new Size(width, _tracked.Any(mask => mask.HasPaint) ? height : 0);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var atStart = _owner?.Anchor is MaterialActionAnchor.TopStart or MaterialActionAnchor.BottomStart;
        var y = 0d;
        foreach (var child in Children)
        {
            child.Arrange(new Rect(atStart ? 0 : finalSize.Width - child.DesiredSize.Width, y, child.DesiredSize.Width, child.DesiredSize.Height));
            y += child.DesiredSize.Height + 4;
        }
        return finalSize;
    }
}
