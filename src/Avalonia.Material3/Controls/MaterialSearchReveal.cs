using Avalonia.Controls;

namespace Avalonia.Material3.Controls;

// The current public bool API follows pinned SearchBar/DockedSearchBar expanded overloads.
internal sealed class MaterialSearchReveal : Decorator
{
    public static readonly StyledProperty<MaterialSearch?> OwnerProperty = AvaloniaProperty.Register<MaterialSearchReveal, MaterialSearch?>(nameof(Owner));
    public MaterialSearch? Owner { get => GetValue(OwnerProperty); set => SetValue(OwnerProperty, value); }
    private Size _natural;
    public MaterialSearchReveal()
    {
        ClipToBounds = true; UseLayoutRounding = false;
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (Owner is { } owner) { owner.PropertyChanged += OwnerChanged; owner.ExpansionChanged += Update; }
        Update();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (Owner is { } owner) { owner.PropertyChanged -= OwnerChanged; owner.ExpansionChanged -= Update; }
        base.OnDetachedFromVisualTree(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != OwnerProperty) return;
        if (change.OldValue is MaterialSearch old) { old.PropertyChanged -= OwnerChanged; old.ExpansionChanged -= Update; }
        if (VisualRoot is not null && Owner is { } owner) { owner.PropertyChanged += OwnerChanged; owner.ExpansionChanged += Update; }
        Update();
    }
    private void OwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == MaterialSearch.IsOpenProperty || change.Property == MaterialSearch.ModeProperty || change.Property == MaterialSearch.ViewPresentationProperty) Update();
    }
    private void Update()
    {
        var open = Owner?.IsOpen == true; var progress = Owner?.ExpansionProgress ?? 0;
        IsEnabled = IsHitTestVisible = open;
        IsVisible = open || progress > 0; Opacity = Math.Clamp(progress, 0, 1); InvalidateMeasure();
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        Child?.Measure(availableSize); _natural = Child?.DesiredSize ?? default;
        return new(_natural.Width, _natural.Height * (Owner?.IsFullscreenPresentation == true ? 1 : Math.Clamp(Owner?.ExpansionProgress ?? 0, 0, 1)));
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        Child?.Arrange(new Rect(0, 0, finalSize.Width, _natural.Height)); return finalSize;
    }
}
