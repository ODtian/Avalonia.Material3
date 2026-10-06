using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.Material3.Controls;

public partial class MaterialSplitButtonPart
{
    public static readonly DirectProperty<MaterialSplitButtonPart, Thickness> EffectivePaddingProperty =
        AvaloniaProperty.RegisterDirect<MaterialSplitButtonPart, Thickness>(nameof(EffectivePadding), button => button.EffectivePadding);
    private Thickness _effectivePadding;
    /// <summary>Pinned padding, reduced horizontally only when finite width would otherwise leave no readable content area.</summary>
    public Thickness EffectivePadding => _effectivePadding;
    internal double MinimumReadableWidth => Math.Max(48, RequiredContentWidth + 16);
    private double RequiredContentWidth => IsSecondary && UseIconContent ? SecondaryContentSize : Math.Max(16, FontSize)
        + (LeadingIcon is null ? 0 : IconSize + IconSpacing) + (TrailingIcon is null ? 0 : IconSize + IconSpacing);
    protected override Size MeasureOverride(Size availableSize)
    {
        var horizontal = Padding.Left + Padding.Right;
        var scale = horizontal > 0 && double.IsFinite(availableSize.Width)
            ? Math.Clamp((availableSize.Width - BorderThickness.Left - BorderThickness.Right - RequiredContentWidth) / horizontal, 0, 1) : 1;
        SetAndRaise(EffectivePaddingProperty, ref _effectivePadding, new Thickness(Padding.Left * scale, Padding.Top, Padding.Right * scale, Padding.Bottom));
        return base.MeasureOverride(availableSize);
    }
    public static readonly DirectProperty<MaterialSplitButtonPart, double> SharedContainerHeightProperty =
        AvaloniaProperty.RegisterDirect<MaterialSplitButtonPart, double>(nameof(SharedContainerHeight), button => button.SharedContainerHeight);
    private double _sharedContainerHeight;
    public double SharedContainerHeight => _sharedContainerHeight;
    internal void SetSharedHeight(double height) => SetAndRaise(SharedContainerHeightProperty, ref _sharedContainerHeight, Math.Max(ContainerHeight, height - (Size == MaterialButtonSize.ExtraSmall ? 16 : 10)));
    public static readonly DirectProperty<MaterialSplitButtonPart, CornerRadius> SplitCornerRadiusProperty =
        AvaloniaProperty.RegisterDirect<MaterialSplitButtonPart, CornerRadius>(nameof(SplitCornerRadius), button => button.SplitCornerRadius);
    public static readonly DirectProperty<MaterialSplitButtonPart, double> SecondaryContentSizeProperty =
        AvaloniaProperty.RegisterDirect<MaterialSplitButtonPart, double>(nameof(SecondaryContentSize), button => button.SecondaryContentSize);
    public static readonly StyledProperty<bool> UseIconContentProperty = AvaloniaProperty.Register<MaterialSplitButtonPart, bool>(nameof(UseIconContent), true);
    public bool UseIconContent { get => GetValue(UseIconContentProperty); set => SetValue(UseIconContentProperty, value); }
    public static readonly StyledProperty<bool> RotateSecondaryContentProperty = AvaloniaProperty.Register<MaterialSplitButtonPart, bool>(nameof(RotateSecondaryContent), true);
    public static readonly DirectProperty<MaterialSplitButtonPart, double> SecondaryRotationProperty =
        AvaloniaProperty.RegisterDirect<MaterialSplitButtonPart, double>(nameof(SecondaryRotation), button => button.SecondaryRotation);
    private CornerRadius _splitCornerRadius;
    public CornerRadius SplitCornerRadius => _splitCornerRadius;
    public bool RotateSecondaryContent { get => GetValue(RotateSecondaryContentProperty); set => SetValue(RotateSecondaryContentProperty, value); }
    public double SecondaryRotation => IsSecondary && IsChecked && RotateSecondaryContent ? 180 : 0;
    public double SecondaryContentSize => Size switch { MaterialButtonSize.Medium => 26, MaterialButtonSize.Large => 38, MaterialButtonSize.ExtraLarge => 50, _ => 22 };
    private readonly Dictionary<string, double> _corners = new() { ["Full"] = 9999, ["ExtraSmall"] = 4, ["Small"] = 8, ["Medium"] = 12, ["LargeIncreased"] = 20 };
    private readonly List<IDisposable> _resources = [];
    protected override Type StyleKeyOverride => typeof(MaterialSplitButtonPart);
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        PseudoClasses.Set(":secondary", IsSecondary);
        PseudoClasses.Set(":secondary-icon", IsSecondary && UseIconContent);
        foreach (var key in _corners.Keys.ToArray())
            _resources.Add(this.GetResourceObservable("M3.Shape.Corner" + key).Subscribe(new ShapeObserver(value =>
            {
                if (value is CornerRadius radius) { _corners[key] = radius.TopLeft; UpdateSplitShape(); }
            })));
        UpdateSplitShape();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        foreach (var resource in _resources) resource.Dispose(); _resources.Clear();
        base.OnDetachedFromVisualTree(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == UseIconContentProperty) PseudoClasses.Set(":secondary-icon", IsSecondary && UseIconContent);
        if (e.Property == SizeProperty)
        {
            var old = e.GetOldValue<MaterialButtonSize>();
            var oldIcon = old switch { MaterialButtonSize.Medium => 26, MaterialButtonSize.Large => 38, MaterialButtonSize.ExtraLarge => 50, _ => 22 };
            RaisePropertyChanged(SecondaryContentSizeProperty, oldIcon, SecondaryContentSize);
        }
        if (e.Property == IsCheckedProperty || e.Property == RotateSecondaryContentProperty)
            RaisePropertyChanged(SecondaryRotationProperty, SecondaryRotation == 0 ? 180 : 0, SecondaryRotation);
        if (e.Property == SizeProperty || e.Property == IsPressedProperty || e.Property == IsCheckedProperty || e.Property == FlowDirectionProperty || e.Property == SharedContainerHeightProperty)
            UpdateSplitShape();
    }
    private void UpdateSplitShape()
    {
        var innerKey = IsPressed ? Size switch { MaterialButtonSize.ExtraSmall => "Small", MaterialButtonSize.Large or MaterialButtonSize.ExtraLarge => "LargeIncreased", _ => "Medium" }
            : Size switch { MaterialButtonSize.Large => "Small", MaterialButtonSize.ExtraLarge => "Medium", _ => "ExtraSmall" };
        var inner = _corners[innerKey];
        // Wrapping can make a container taller than it is wide. Preserve the nominal cap
        // instead of letting a vertical capsule curve under readable multi-line content.
        var full = SharedContainerHeight > ContainerHeight + .01 ? Math.Min(_corners["Full"], ContainerHeight / 2) : _corners["Full"];
        var leading = !IsSecondary;
        if (FlowDirection == FlowDirection.RightToLeft) leading = !leading;
        var target = IsSecondary && IsChecked && !IsPressed ? new CornerRadius(full) : leading ? new CornerRadius(full, inner, inner, full) : new CornerRadius(inner, full, full, inner);
        SetAndRaise(SplitCornerRadiusProperty, ref _splitCornerRadius, target);
    }
    private sealed class ShapeObserver(Action<object?> changed) : IObserver<object?>
    {
        public void OnNext(object? value) => changed(value);
        public void OnCompleted() { }
        public void OnError(Exception error) { }
    }
}
