using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;

namespace Avalonia.Material3.Controls;

// Fixed-shape SelectableChip: retained content, SlowEffects in/FastEffects out,
// FastSpatial expansion/DefaultEffects shrink. Ordinary ChipContent is immediate.
internal sealed class MaterialChipIconSlot : Decorator
{
    public static readonly StyledProperty<MaterialChip?> OwnerProperty = AvaloniaProperty.Register<MaterialChipIconSlot, MaterialChip?>(nameof(Owner));
    public static readonly StyledProperty<bool> LeadingProperty = AvaloniaProperty.Register<MaterialChipIconSlot, bool>(nameof(Leading));
    public MaterialChip? Owner { get => GetValue(OwnerProperty); set => SetValue(OwnerProperty, value); }
    public bool Leading { get => GetValue(LeadingProperty); set => SetValue(LeadingProperty, value); }
    private readonly ContentPresenter _presenter = new() { HorizontalContentAlignment = HorizontalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center };
    private readonly Border _artwork;
    private readonly MaterialMotionValue _extent, _alpha;
    private readonly MaterialMotionSettings _motion;
    private double _size = 18;
    private bool _attached, _present;
    public MaterialChipIconSlot()
    {
        ClipToBounds = true; UseLayoutRounding = false; IsHitTestVisible = false;
        _artwork = new Border { Child = new Viewbox { Child = _presenter }, Width = 18, Height = 18 };
        Child = _artwork;
        _extent = new(this, 0, value =>
        {
            InvalidateMeasure();
            if (!_present && value <= 0) _presenter.Content = null;
        });
        _alpha = new(this, 0, value => Opacity = Math.Clamp(value, 0, 1));
        _motion = new(this, () => Update(true));
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == OwnerProperty)
        {
            if (change.OldValue is MaterialChip old) old.PropertyChanged -= OwnerChanged;
            if (Owner is { } owner) owner.PropertyChanged += OwnerChanged;
            Update(false);
        }
        if (change.Property == LeadingProperty) Update(false);
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e); _attached = true; Update(false);
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _attached = false; base.OnDetachedFromVisualTree(e);
    }
    private void OwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == MaterialButton.IsCheckedProperty || change.Property == IsEffectivelyEnabledProperty || change.Property == MaterialModalPaintScope.EnabledForPaintProperty ||
            change.Property == IsPointerOverProperty || change.Property == IsKeyboardFocusWithinProperty || change.Property == MaterialChip.IsDraggedProperty) UpdateColor();
        if (change.Property == MaterialButton.LeadingIconProperty || change.Property == MaterialButton.TrailingIconProperty ||
            change.Property == MaterialButton.LeadingIconTemplateProperty || change.Property == MaterialButton.TrailingIconTemplateProperty ||
            change.Property == MaterialChip.AvatarProperty || change.Property == MaterialChip.AvatarTemplateProperty || change.Property == MaterialChip.ChipVariantProperty) Update(true);
    }
    private void Update(bool animate)
    {
        if (_extent is null || _motion is null || Owner is not { } owner) return;
        var avatar = Leading && owner.Avatar is not null;
        var content = Leading ? owner.Avatar ?? owner.LeadingIcon : owner.TrailingIcon;
        var template = Leading ? avatar ? owner.AvatarTemplate : owner.LeadingIconTemplate : owner.TrailingIconTemplate;
        var present = content is not null;
        if (present)
        {
            _presenter.ContentTemplate = template; _presenter.Content = content;
            _size = avatar ? 24 : 18;
            _artwork.Width = _artwork.Height = _size;
            _artwork.ClipToBounds = avatar; _artwork.CornerRadius = avatar ? new CornerRadius(9999) : default;
        }
        var changed = _present != present; _present = present;
        UpdateColor();
        if (!animate || !_attached || _motion.FastEffects.IsInstant || owner.ChipVariant is MaterialChipVariant.Assist or MaterialChipVariant.Suggestion)
        { _extent.Snap(present ? 1 : 0); _alpha.Snap(present ? 1 : 0); return; }
        if (!changed && !_extent.IsRunning) return;
        _extent.Spring(present ? 1 : 0, present ? _motion.FastSpatial : _motion.DefaultEffects, 1 / _size);
        _alpha.Spring(present ? 1 : 0, present ? _motion.SlowEffects : _motion.FastEffects);
    }
    private void UpdateColor()
    {
        if (Owner is not { } owner) return;
        var role = !MaterialModalPaintScope.IsEnabledForPaint(owner) ? "OnSurface" : !Leading ? owner.IsChecked ? "OnSecondaryContainer" : "OnSurfaceVariant"
            : owner.ChipVariant == MaterialChipVariant.Input ? owner.IsChecked && owner.IsDragged ? "OnSecondaryContainer"
                : owner.IsChecked || owner.IsPointerOver || owner.IsKeyboardFocusWithin ? "Primary" : "OnSurfaceVariant"
            : owner.ChipVariant == MaterialChipVariant.Filter && owner.IsChecked ? "OnSecondaryContainer" : "Primary";
        MaterialPickerSupport.Resource(_presenter, ContentPresenter.ForegroundProperty, role + "Brush");
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        Child?.Measure(new Size(_size, _size));
        var extent = Math.Max(0, _extent.Value);
        return new((_size + 8) * extent, _size);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var x = Leading ? 0 : finalSize.Width - _size;
        Child?.Arrange(new Rect(x, (finalSize.Height - _size) / 2, _size, _size)); return finalSize;
    }
}
