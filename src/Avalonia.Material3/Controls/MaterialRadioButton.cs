using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace Avalonia.Material3.Controls;

/// <summary>A Material radio option using Avalonia's GroupName, selection and automation contracts.</summary>
[PseudoClasses(":error")]
public class MaterialRadioButton : RadioButton
{
    public static readonly StyledProperty<bool> IsErrorProperty = MaterialCheckBox.IsErrorProperty.AddOwner<MaterialRadioButton>();
    public static readonly StyledProperty<string?> ErrorTextProperty = MaterialCheckBox.ErrorTextProperty.AddOwner<MaterialRadioButton>();

    /// <summary>Shows host validation feedback; M3 radio tokens do not define a separate error variant.</summary>
    public bool IsError { get => GetValue(IsErrorProperty); set => SetValue(IsErrorProperty, value); }
    /// <summary>Visible and accessible error explanation when IsError is true.</summary>
    public string? ErrorText { get => GetValue(ErrorTextProperty); set => SetValue(ErrorTextProperty, value); }

    static MaterialRadioButton()
    {
        IsCheckedProperty.OverrideMetadata<MaterialRadioButton>(new StyledPropertyMetadata<bool?>(false, coerce: (_, value) => value ?? false));
        IsThreeStateProperty.OverrideMetadata<MaterialRadioButton>(new StyledPropertyMetadata<bool>(false, coerce: (_, _) => false));
    }

    protected override Type StyleKeyOverride => typeof(MaterialRadioButton);
    protected override AutomationPeer OnCreateAutomationPeer() => new SelectionRadioAutomationPeer(this);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsErrorProperty)
            PseudoClasses.Set(":error", IsError);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled || e.KeyModifiers != KeyModifiers.None ||
            e.Key is not (Key.Left or Key.Right or Key.Up or Key.Down))
            return;

        var root = TopLevel.GetTopLevel(this);
        if (root is not { } visual)
            return;
        var group = visual.GetVisualDescendants().OfType<MaterialRadioButton>()
            .Where(option => option.IsEffectivelyEnabled && option.IsEffectivelyVisible && option.Focusable &&
                (string.IsNullOrEmpty(GroupName)
                    ? string.IsNullOrEmpty(option.GroupName) && option.Parent == Parent
                    : option.GroupName == GroupName))
            .ToArray();
        var index = Array.IndexOf(group, this);
        if (index < 0)
            return;
        var direction = e.Key is Key.Left or Key.Up ? -1 : 1;
        var next = group[(index + direction + group.Length) % group.Length];
        next.Focus(NavigationMethod.Directional);
        next.SetCurrentValue(IsCheckedProperty, true);
        e.Handled = true;
    }
}
