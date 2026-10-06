using Avalonia.Controls;

namespace Avalonia.Material3.Controls;

public enum MaterialTabVariant { Primary, Secondary }
public enum MaterialTabLayout { Fixed, Scrollable }

/// <summary>Primary or secondary tabs with an actual selected page. Scrollable headers use natural widths.</summary>
public class MaterialTabs : MaterialNavigation
{
    public static readonly StyledProperty<MaterialTabVariant> VariantProperty = AvaloniaProperty.Register<MaterialTabs, MaterialTabVariant>(nameof(Variant), validate: value => Enum.IsDefined(value));
    public static readonly StyledProperty<MaterialTabLayout> LayoutProperty = AvaloniaProperty.Register<MaterialTabs, MaterialTabLayout>(nameof(Layout), validate: value => Enum.IsDefined(value));
    public MaterialTabVariant Variant { get => GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
    public MaterialTabLayout Layout { get => GetValue(LayoutProperty); set => SetValue(LayoutProperty, value); }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == VariantProperty || change.Property == LayoutProperty) RefreshPresentation();
    }
}
