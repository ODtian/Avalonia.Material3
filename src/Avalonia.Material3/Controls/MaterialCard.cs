using Avalonia.Controls;

namespace Avalonia.Material3.Controls;

public enum MaterialCardVariant { Filled, Elevated, Outlined }

/// <summary>A filled, elevated or outlined card. Primary interaction is opt-in; nested actions remain usable.</summary>
public class MaterialCard : MaterialContentItem
{
    public static readonly StyledProperty<MaterialCardVariant> VariantProperty = AvaloniaProperty.Register<MaterialCard, MaterialCardVariant>(nameof(Variant), validate: value => Enum.IsDefined(value));
    static MaterialCard()
    {
        IsInteractiveProperty.OverrideDefaultValue<MaterialCard>(false);
        FocusableProperty.OverrideDefaultValue<MaterialCard>(false);
    }
    public static readonly StyledProperty<bool> IsDraggedProperty = AvaloniaProperty.Register<MaterialCard, bool>(nameof(IsDragged));
    public bool IsDragged { get => GetValue(IsDraggedProperty); set => SetValue(IsDraggedProperty, value); }
    public MaterialCardVariant Variant { get => GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsDraggedProperty) PseudoClasses.Set(":dragged", IsDragged);
        if (change.Property == VariantProperty)
        {
            PseudoClasses.Set(":elevated", Variant == MaterialCardVariant.Elevated);
            PseudoClasses.Set(":outlined", Variant == MaterialCardVariant.Outlined);
        }
    }
}
