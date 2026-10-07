using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;

namespace Avalonia.Material3.Controls;

/// <summary>An icon-and-label FAB. Collapsing hides only the label, not the action or its accessible name.</summary>
public class MaterialExtendedFab : MaterialFab
{
    public static readonly StyledProperty<object?> IconProperty = AvaloniaProperty.Register<MaterialExtendedFab, object?>(nameof(Icon));
    public static readonly StyledProperty<IDataTemplate?> IconTemplateProperty = AvaloniaProperty.Register<MaterialExtendedFab, IDataTemplate?>(nameof(IconTemplate));
    public static readonly StyledProperty<bool> IsExpandedProperty = AvaloniaProperty.Register<MaterialExtendedFab, bool>(nameof(IsExpanded), true, defaultBindingMode: BindingMode.TwoWay,
        coerce: (control, value) => value || ((MaterialExtendedFab)control).Icon is null);
    public static readonly DirectProperty<MaterialExtendedFab, double> IconSpacingProperty = AvaloniaProperty.RegisterDirect<MaterialExtendedFab, double>(nameof(IconSpacing), control => control.IconSpacing);
    public static readonly DirectProperty<MaterialExtendedFab, Thickness> ContentPaddingProperty = AvaloniaProperty.RegisterDirect<MaterialExtendedFab, Thickness>(nameof(ContentPadding), control => control.ContentPadding);
    public object? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public IDataTemplate? IconTemplate { get => GetValue(IconTemplateProperty); set => SetValue(IconTemplateProperty, value); }
    public bool IsExpanded { get => GetValue(IsExpandedProperty); set => SetValue(IsExpandedProperty, value); }
    protected override double GetContainerSize(MaterialFabSize size) => size == MaterialFabSize.Small ? 56 : base.GetContainerSize(size);
    protected override double GetIconSize(MaterialFabSize size) => size == MaterialFabSize.Large ? 32 : base.GetIconSize(size);
    public double IconSpacing => Size switch { MaterialFabSize.Medium => 12, MaterialFabSize.Large => 16, MaterialFabSize.Small => 8, _ => 12 };
    public Thickness ContentPadding => !IsExpanded ? default : ExpandedContentPadding;
    internal Thickness ExpandedContentPadding => Size switch
    {
        MaterialFabSize.Medium => new(26, 12), MaterialFabSize.Large => new(28, 16),
        MaterialFabSize.Small => new(16, 8), _ => new(16, 8, 20, 8)
    };
    protected override Type StyleKeyOverride => typeof(MaterialExtendedFab);
    public MaterialExtendedFab() => PseudoClasses.Set(":expanded", true);
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IconProperty) CoerceValue(IsExpandedProperty);
        if (change.Property == IsExpandedProperty || change.Property == SizeProperty)
        {
            PseudoClasses.Set(":expanded", IsExpanded);
            RaisePropertyChanged(ContentPaddingProperty, default, ContentPadding);
            RaisePropertyChanged(IconSpacingProperty, 0, IconSpacing);
        }
    }
}
