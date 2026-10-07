using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Automation.Peers;

namespace Avalonia.Material3.Controls;

/// <summary>A Material Expressive action button preserving Avalonia Button content, command, input and automation behavior.</summary>
public class MaterialButton : Button
{
    public static readonly StyledProperty<object?> LeadingIconProperty =
        AvaloniaProperty.Register<MaterialButton, object?>(nameof(LeadingIcon));
    public static readonly StyledProperty<object?> TrailingIconProperty =
        AvaloniaProperty.Register<MaterialButton, object?>(nameof(TrailingIcon));
    public static readonly StyledProperty<IDataTemplate?> LeadingIconTemplateProperty =
        AvaloniaProperty.Register<MaterialButton, IDataTemplate?>(nameof(LeadingIconTemplate));
    public static readonly StyledProperty<IDataTemplate?> TrailingIconTemplateProperty =
        AvaloniaProperty.Register<MaterialButton, IDataTemplate?>(nameof(TrailingIconTemplate));
    public object? LeadingIcon { get => GetValue(LeadingIconProperty); set => SetValue(LeadingIconProperty, value); }
    public object? TrailingIcon { get => GetValue(TrailingIconProperty); set => SetValue(TrailingIconProperty, value); }
    public IDataTemplate? LeadingIconTemplate { get => GetValue(LeadingIconTemplateProperty); set => SetValue(LeadingIconTemplateProperty, value); }
    public IDataTemplate? TrailingIconTemplate { get => GetValue(TrailingIconTemplateProperty); set => SetValue(TrailingIconTemplateProperty, value); }

    public static readonly StyledProperty<MaterialButtonShape> ShapeProperty =
        AvaloniaProperty.Register<MaterialButton, MaterialButtonShape>(nameof(Shape), validate: value => Enum.IsDefined(value));
    public MaterialButtonShape Shape { get => GetValue(ShapeProperty); set => SetValue(ShapeProperty, value); }

    public static readonly StyledProperty<MaterialButtonVariant> VariantProperty =
        AvaloniaProperty.Register<MaterialButton, MaterialButtonVariant>(nameof(Variant),
            validate: value => Enum.IsDefined(value));
    public MaterialButtonVariant Variant { get => GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    public static readonly StyledProperty<MaterialButtonSize> SizeProperty =
        AvaloniaProperty.Register<MaterialButton, MaterialButtonSize>(nameof(Size), MaterialButtonSize.Small,
            validate: value => Enum.IsDefined(value));
    public static readonly DirectProperty<MaterialButton, double> ContainerHeightProperty =
        AvaloniaProperty.RegisterDirect<MaterialButton, double>(nameof(ContainerHeight), button => button.ContainerHeight);
    public static readonly DirectProperty<MaterialButton, double> IconSizeProperty =
        AvaloniaProperty.RegisterDirect<MaterialButton, double>(nameof(IconSize), button => button.IconSize);
    public static readonly DirectProperty<MaterialButton, double> IconSpacingProperty =
        AvaloniaProperty.RegisterDirect<MaterialButton, double>(nameof(IconSpacing), button => button.IconSpacing);

    public MaterialButtonSize Size { get => GetValue(SizeProperty); set => SetValue(SizeProperty, value); }
    public double ContainerHeight => Size switch { MaterialButtonSize.ExtraSmall => 32, MaterialButtonSize.Medium => 56, MaterialButtonSize.Large => 96, MaterialButtonSize.ExtraLarge => 136, _ => 40 };
    public double IconSize => GetIconSize(Size);
    protected virtual double GetIconSize(MaterialButtonSize size) => size switch { MaterialButtonSize.ExtraSmall or MaterialButtonSize.Small => 20, MaterialButtonSize.Medium => 24, MaterialButtonSize.Large => 32, _ => 40 };
    public double IconSpacing => GetIconSpacing(Size);
    // ButtonDefaults corrects the generated XS spacing token to 4 DIP at the pinned commit.
    private static double GetIconSpacing(MaterialButtonSize size) => size switch
    {
        MaterialButtonSize.ExtraSmall => 4, MaterialButtonSize.Large => 12, MaterialButtonSize.ExtraLarge => 16, _ => 8
    };

    public MaterialButton()
    {
        DataTemplates.Add(MaterialSymbolTemplate.Instance);
        UpdateSizePseudoClasses();
        UpdateVariantPseudoClasses();
    }

    private void UpdateVariantPseudoClasses()
    {
        foreach (var variant in Enum.GetValues<MaterialButtonVariant>())
            PseudoClasses.Set(":" + variant.ToString().ToLowerInvariant(), Variant == variant);
    }

    private void UpdateSizePseudoClasses()
    {
        foreach (var size in Enum.GetValues<MaterialButtonSize>())
            PseudoClasses.Set(":" + size.ToString().ToLowerInvariant(), Size == size);
    }

    public static readonly StyledProperty<bool> IsToggleProperty =
        AvaloniaProperty.Register<MaterialButton, bool>(nameof(IsToggle));
    public static readonly StyledProperty<bool> IsCheckedProperty =
        AvaloniaProperty.Register<MaterialButton, bool>(nameof(IsChecked), defaultBindingMode: Data.BindingMode.TwoWay);

    /// <summary>Whether activation changes the two-state selection.</summary>
    public bool IsToggle { get => GetValue(IsToggleProperty); set => SetValue(IsToggleProperty, value); }
    /// <summary>The selection, committed before Click and Command execute. Ignored visually when IsToggle is false.</summary>
    public bool IsChecked { get => GetValue(IsCheckedProperty); set => SetValue(IsCheckedProperty, value); }

    protected override Type StyleKeyOverride => typeof(MaterialButton);
    protected override AutomationPeer OnCreateAutomationPeer() => new MaterialButtonAutomationPeer(this);

    protected override void OnClick()
    {
        if (!IsEffectivelyEnabled)
            return;
        if (IsToggle)
            CommitToggleSelection();
        base.OnClick();
    }

    /// <summary>Commits toggle selection before Click/Command; specialized selection containers may retain a required choice.</summary>
    protected virtual void CommitToggleSelection() => SetCurrentValue(IsCheckedProperty, !IsChecked);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ShapeProperty)
            PseudoClasses.Set(":square", Shape == MaterialButtonShape.Square);
        if (change.Property == VariantProperty)
            UpdateVariantPseudoClasses();
        if (change.Property == SizeProperty)
        {
            var oldSize = change.GetOldValue<MaterialButtonSize>();
            // Notify template bindings without making derived geometry independently mutable.
            var oldHeight = oldSize switch { MaterialButtonSize.ExtraSmall => 32, MaterialButtonSize.Medium => 56, MaterialButtonSize.Large => 96, MaterialButtonSize.ExtraLarge => 136, _ => 40 };
            var oldIcon = GetIconSize(oldSize);
            RaisePropertyChanged(ContainerHeightProperty, oldHeight, ContainerHeight);
            RaisePropertyChanged(IconSizeProperty, oldIcon, IconSize);
            RaisePropertyChanged(IconSpacingProperty, GetIconSpacing(oldSize), IconSpacing);
            UpdateSizePseudoClasses();
        }
        if (change.Property == IsToggleProperty || change.Property == IsCheckedProperty)
        {
            PseudoClasses.Set(":toggle", IsToggle);
            PseudoClasses.Set(":checked", IsToggle && IsChecked);
        }
    }
}
