using Avalonia.Animation.Easings;
using Avalonia.Controls;

namespace Avalonia.Material3.Controls;

// The current public bool API follows pinned SearchBar/DockedSearchBar expanded overloads.
internal sealed class MaterialSearchReveal : Decorator
{
    public static readonly StyledProperty<MaterialSearch?> OwnerProperty = AvaloniaProperty.Register<MaterialSearchReveal, MaterialSearch?>(nameof(Owner));
    public MaterialSearch? Owner { get => GetValue(OwnerProperty); set => SetValue(OwnerProperty, value); }
    private readonly MaterialMotionValue _extent, _alpha;
    private readonly MaterialMotionSettings _motion;
    private bool _attached;
    private Size _natural;
    public MaterialSearchReveal()
    {
        ClipToBounds = true; UseLayoutRounding = false;
        _extent = new(this, 0, value => { InvalidateMeasure(); IsVisible = Owner?.IsOpen == true || value > 0; });
        _alpha = new(this, 0, value => Opacity = Math.Clamp(value, 0, 1));
        _motion = new(this, () => Update(true));
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e); _attached = true; Update(false);
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _attached = false; base.OnDetachedFromVisualTree(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != OwnerProperty) return;
        if (change.OldValue is MaterialSearch old) old.PropertyChanged -= OwnerChanged;
        if (Owner is { } owner) owner.PropertyChanged += OwnerChanged;
        Update(false);
    }
    private void OwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == MaterialSearch.IsOpenProperty) Update(true);
        if (change.Property == MaterialSearch.ModeProperty || change.Property == MaterialSearch.ViewPresentationProperty) Update(false);
    }
    private void Update(bool animate)
    {
        if (_extent is null || _motion is null) return;
        var open = Owner?.IsOpen == true; var target = open ? 1 : 0;
        IsEnabled = IsHitTestVisible = open;
        if (!animate || !_attached || _motion.FastEffects.IsInstant || Owner?.Mode is not (MaterialSearchMode.Bar or MaterialSearchMode.View))
        { _extent.Snap(target); _alpha.Snap(target); IsVisible = open; return; }
        IsVisible = true;
        // An interrupted expansion uses the source's predictive-back exit spec without delay.
        var interrupted = _extent.IsRunning && _extent.Value is > 0 and < 1;
        var duration = TimeSpan.FromMilliseconds(interrupted || !open ? 350 : 600);
        var delay = interrupted ? TimeSpan.Zero : TimeSpan.FromMilliseconds(100);
        var easing = interrupted || !open ? new SplineEasing(0, 1, 0, 1) : new SplineEasing(.05, .7, .1, 1);
        _extent.Tween(target, duration, easing, delay);
        _alpha.Tween(target, duration, easing, delay);
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        Child?.Measure(availableSize); _natural = Child?.DesiredSize ?? default;
        return new(_natural.Width, _natural.Height * Math.Clamp(_extent.Value, 0, 1));
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        Child?.Arrange(new Rect(0, 0, finalSize.Width, _natural.Height)); return finalSize;
    }
}
