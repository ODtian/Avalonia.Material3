using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;

namespace Avalonia.Material3.Controls;

/// <summary>A Material switch retaining Avalonia's toggle binding, keyboard and drag behavior.</summary>
[PseudoClasses(":error", ":off-icon")]
public class MaterialSwitch : ToggleSwitch
{
    public static readonly StyledProperty<object?> OnIconProperty = AvaloniaProperty.Register<MaterialSwitch, object?>(nameof(OnIcon));
    public static readonly StyledProperty<object?> OffIconProperty = AvaloniaProperty.Register<MaterialSwitch, object?>(nameof(OffIcon));

    /// <summary>Optional decorative content in the selected 24 DIP handle; keep content within 16 DIP.</summary>
    public object? OnIcon { get => GetValue(OnIconProperty); set => SetValue(OnIconProperty, value); }
    /// <summary>Optional decorative content in the unselected handle, which grows to 24 DIP when supplied.</summary>
    public object? OffIcon { get => GetValue(OffIconProperty); set => SetValue(OffIconProperty, value); }

    public static readonly StyledProperty<bool> IsErrorProperty = MaterialCheckBox.IsErrorProperty.AddOwner<MaterialSwitch>();
    public static readonly StyledProperty<string?> ErrorTextProperty = MaterialCheckBox.ErrorTextProperty.AddOwner<MaterialSwitch>();

    /// <summary>Shows host validation feedback; M3 switch tokens do not define a separate error variant.</summary>
    public bool IsError { get => GetValue(IsErrorProperty); set => SetValue(IsErrorProperty, value); }
    /// <summary>Visible and accessible error explanation when IsError is true.</summary>
    public string? ErrorText { get => GetValue(ErrorTextProperty); set => SetValue(ErrorTextProperty, value); }

    protected override Type StyleKeyOverride => typeof(MaterialSwitch);
    protected override AutomationPeer OnCreateAutomationPeer() => new SelectionToggleAutomationPeer(this);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsErrorProperty)
            PseudoClasses.Set(":error", IsError);
        else if (change.Property == OffIconProperty)
            PseudoClasses.Set(":off-icon", OffIcon is not null);
    }

    static MaterialSwitch()
    {
        IsCheckedProperty.OverrideMetadata<MaterialSwitch>(new StyledPropertyMetadata<bool?>(false, coerce: (_, value) => value ?? false));
        IsThreeStateProperty.OverrideMetadata<MaterialSwitch>(new StyledPropertyMetadata<bool>(false, coerce: (_, _) => false));
        AutomationProperties.ControlTypeOverrideProperty.OverrideDefaultValue<MaterialSwitch>(AutomationControlType.CheckBox);
        OnContentProperty.OverrideDefaultValue<MaterialSwitch>(null);
        OffContentProperty.OverrideDefaultValue<MaterialSwitch>(null);
    }
}
