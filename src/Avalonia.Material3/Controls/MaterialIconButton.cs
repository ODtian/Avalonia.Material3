using Avalonia.Controls;

namespace Avalonia.Material3.Controls;

/// <summary>An icon-only action with the same Button input, command and two-state automation as MaterialButton.</summary>
public class MaterialIconButton : MaterialButton
{
    public static readonly StyledProperty<MaterialIconButtonVariant> IconVariantProperty =
        AvaloniaProperty.Register<MaterialIconButton, MaterialIconButtonVariant>(nameof(IconVariant),
            validate: value => Enum.IsDefined(value));
    /// <summary>Icon-only emphasis; the inherited action-button Variant is not used by the icon theme.</summary>
    public MaterialIconButtonVariant IconVariant { get => GetValue(IconVariantProperty); set => SetValue(IconVariantProperty, value); }

    public MaterialIconButton() => UpdateIconVariantPseudoClasses();

    private void UpdateIconVariantPseudoClasses()
    {
        foreach (var variant in Enum.GetValues<MaterialIconButtonVariant>())
            PseudoClasses.Set(":icon-" + variant.ToString().ToLowerInvariant(), IconVariant == variant);
    }

    public static readonly StyledProperty<MaterialIconButtonWidth> WidthModeProperty =
        AvaloniaProperty.Register<MaterialIconButton, MaterialIconButtonWidth>(nameof(WidthMode), MaterialIconButtonWidth.Default,
            validate: value => Enum.IsDefined(value));
    public static readonly DirectProperty<MaterialIconButton, double> ContainerWidthProperty =
        AvaloniaProperty.RegisterDirect<MaterialIconButton, double>(nameof(ContainerWidth), button => button.ContainerWidth);
    public MaterialIconButtonWidth WidthMode { get => GetValue(WidthModeProperty); set => SetValue(WidthModeProperty, value); }
    public double ContainerWidth => GetContainerWidth(Size, WidthMode);

    private double GetContainerWidth(MaterialButtonSize size, MaterialIconButtonWidth width)
    {
        var space = (size, width) switch
        {
            (MaterialButtonSize.ExtraSmall, MaterialIconButtonWidth.Narrow) => 4,
            (MaterialButtonSize.ExtraSmall, MaterialIconButtonWidth.Wide) => 10,
            (MaterialButtonSize.ExtraSmall, _) => 6,
            (MaterialButtonSize.Small, MaterialIconButtonWidth.Narrow) => 4,
            (MaterialButtonSize.Small, MaterialIconButtonWidth.Wide) => 14,
            (MaterialButtonSize.Small, _) => 8,
            (MaterialButtonSize.Medium, MaterialIconButtonWidth.Narrow) => 12,
            (MaterialButtonSize.Medium, MaterialIconButtonWidth.Wide) => 24,
            (MaterialButtonSize.Medium, _) => 16,
            (MaterialButtonSize.Large, MaterialIconButtonWidth.Narrow) => 16,
            (MaterialButtonSize.Large, MaterialIconButtonWidth.Wide) => 48,
            (MaterialButtonSize.Large, _) => 32,
            (_, MaterialIconButtonWidth.Narrow) => 32,
            (_, MaterialIconButtonWidth.Wide) => 72,
            _ => 48
        };
        return GetIconSize(size) + space * 2;
    }

    protected override Type StyleKeyOverride => typeof(MaterialIconButton);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IconVariantProperty)
            UpdateIconVariantPseudoClasses();
        if (change.Property == SizeProperty)
            RaisePropertyChanged(ContainerWidthProperty, GetContainerWidth(change.GetOldValue<MaterialButtonSize>(), WidthMode), ContainerWidth);
        else if (change.Property == WidthModeProperty)
            RaisePropertyChanged(ContainerWidthProperty, GetContainerWidth(Size, change.GetOldValue<MaterialIconButtonWidth>()), ContainerWidth);
    }
    protected override double GetIconSize(MaterialButtonSize size) => size == MaterialButtonSize.Small ? 24 : base.GetIconSize(size);
}
