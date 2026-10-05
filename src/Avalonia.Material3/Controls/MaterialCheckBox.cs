using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;

namespace Avalonia.Material3.Controls;

/// <summary>A Material checkbox with Avalonia's two/three-state binding, input and toggle automation contract.</summary>
[PseudoClasses(":error")]
public class MaterialCheckBox : CheckBox
{
    public static readonly StyledProperty<bool> IsErrorProperty =
        AvaloniaProperty.Register<MaterialCheckBox, bool>(nameof(IsError));
    public static readonly StyledProperty<string?> ErrorTextProperty =
        AvaloniaProperty.Register<MaterialCheckBox, string?>(nameof(ErrorText));

    /// <summary>Shows validation feedback without changing the checked value or blocking input.</summary>
    public bool IsError { get => GetValue(IsErrorProperty); set => SetValue(IsErrorProperty, value); }
    /// <summary>Visible and accessible error explanation when IsError is true.</summary>
    public string? ErrorText { get => GetValue(ErrorTextProperty); set => SetValue(ErrorTextProperty, value); }

    protected override Type StyleKeyOverride => typeof(MaterialCheckBox);
    protected override AutomationPeer OnCreateAutomationPeer() => new SelectionToggleAutomationPeer(this);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsErrorProperty)
            PseudoClasses.Set(":error", IsError);
    }
}
