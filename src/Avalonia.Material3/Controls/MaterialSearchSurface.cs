using Avalonia.Controls;
using Avalonia.Diagnostics;
using Avalonia.Data;
using Avalonia.Media;

namespace Avalonia.Material3.Controls;

// SearchBarLayout: a single expansion progress sizes, rounds and positions the shell.
internal sealed class MaterialSearchSurface : Decorator
{
    public static readonly StyledProperty<MaterialSearch?> OwnerProperty = AvaloniaProperty.Register<MaterialSearchSurface, MaterialSearch?>(nameof(Owner));
    public MaterialSearch? Owner { get => GetValue(OwnerProperty); set => SetValue(OwnerProperty, value); }
    private Size _surface;
    private double _topInset;
    public MaterialSearchSurface() { UseLayoutRounding = false; }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (Owner is { } owner) { owner.ExpansionChanged += ExpansionChanged; owner.PropertyChanged += OwnerChanged; }
        ExpansionChanged();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (Owner is { } owner) { owner.ExpansionChanged -= ExpansionChanged; owner.PropertyChanged -= OwnerChanged; }
        base.OnDetachedFromVisualTree(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != OwnerProperty) return;
        if (change.OldValue is MaterialSearch old) { old.ExpansionChanged -= ExpansionChanged; old.PropertyChanged -= OwnerChanged; }
        if (VisualRoot is not null && Owner is { } owner) { owner.ExpansionChanged += ExpansionChanged; owner.PropertyChanged += OwnerChanged; }
        ExpansionChanged();
    }
    private void OwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == MaterialSearch.ModeProperty || change.Property == MaterialSearch.ViewPresentationProperty
            || change.Property == MaterialSearch.CornerRadiusProperty || change.Property == MaterialSearch.BackgroundProperty) ExpansionChanged();
    }
    private void ExpansionChanged()
    {
        if (Child is Border border && Owner is { } owner)
        {
            var progress = Math.Clamp(owner.ExpansionProgress, 0, 1);
            var customShape = owner.GetDiagnostic(MaterialSearch.CornerRadiusProperty).Priority <= BindingPriority.LocalValue;
            border.SetCurrentValue(Border.CornerRadiusProperty, owner.IsFullscreenPresentation
                ? customShape ? progress == 1 ? new CornerRadius(0) : owner.CornerRadius : new CornerRadius(28 * (1 - progress))
                : owner.CornerRadius);
        }
        InvalidateMeasure();
        InvalidateVisual();
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Owner?.IsFullscreenPresentation != true || !double.IsFinite(availableSize.Height))
        { _topInset = 0; _surface = base.MeasureOverride(availableSize); return _surface; }
        var progress = Math.Clamp(Owner.ExpansionProgress, 0, 1);
        Child?.Measure(new Size(Math.Min(720, availableSize.Width), 56));
        var startWidth = Math.Min(availableSize.Width, Math.Clamp(Child?.DesiredSize.Width ?? 360, 360, 720));
        var width = startWidth + (availableSize.Width - startWidth) * progress;
        var height = 64 + (availableSize.Height - 64) * progress;
        _topInset = 8 * (1 - progress);
        _surface = new(width, Math.Max(0, height - _topInset));
        Child?.Measure(_surface);
        return new(width, height);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        Child?.Arrange(new Rect(0, _topInset, _surface.Width, _surface.Height));
        return finalSize;
    }
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Owner is not { IsFullscreenPresentation: true } owner || Child is not Border border) return;
        // The fullscreen shell fills the host viewport. Its content stays in the first row,
        // leaving the supporting text, tokens and explicit add action their own layout rows.
        var progress = Math.Clamp(owner.ExpansionProgress, 0, 1);
        var height = 64 + (owner.Bounds.Height - 64) * progress;
        context.DrawRectangle(owner.Background, null,
            new RoundedRect(new Rect(0, _topInset, _surface.Width, Math.Max(0, height - _topInset)), border.CornerRadius));
    }
}
