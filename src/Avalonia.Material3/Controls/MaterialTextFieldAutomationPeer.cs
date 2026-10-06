using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace Avalonia.Material3.Controls;

/// <summary>Text-field semantics with supporting feedback and a non-disclosing password value provider.</summary>
public sealed class MaterialTextFieldAutomationPeer : ControlAutomationPeer, IValueProvider
{
    private string _name;
    private string _help;
    private string? _status;

    public MaterialTextFieldAutomationPeer(MaterialTextField owner) : base(owner)
    {
        _name = GetName();
        _help = GetHelpText();
        _status = GetItemStatus();
        owner.PropertyChanged += OwnerPropertyChanged;
    }

    public new MaterialTextField Owner => (MaterialTextField)base.Owner;
    public bool IsReadOnly => Owner.IsReadOnly;
    // Remains protected even while the host reveals the password visually.
    public string? Value => Owner.PasswordChar == default ? Owner.Text : string.Empty;

    public void SetValue(string? value)
    {
        if (Owner.IsReadOnly || !Owner.IsEffectivelyEnabled)
            throw new InvalidOperationException("The text field is not editable.");
        Owner.SetCurrentValue(TextBox.TextProperty, value);
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Edit;
    protected override string? GetPlaceholderTextCore() => Owner.PlaceholderText;
    protected override string? GetNameCore() => NonEmpty(base.GetNameCore()) ?? NonEmpty(Owner.Label) ?? Owner.PlaceholderText;
    protected override string? GetHelpTextCore() => string.Join(". ", new[]
    {
        NonEmpty(base.GetHelpTextCore()), NonEmpty(Owner.EffectiveSupportingText), Owner.ShowCounter ? Owner.CounterText : null
    }.Where(value => value is not null));
    protected override string? GetItemStatusCore() => Owner.HasError ? "Invalid" : base.GetItemStatusCore();

    private static string? NonEmpty(string? text) => string.IsNullOrWhiteSpace(text) ? null : text;

    private void OwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TextBox.TextProperty)
            RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty,
                Owner.PasswordChar == default ? e.OldValue : string.Empty, Value);
        if (e.Property == TextBox.IsReadOnlyProperty)
            RaisePropertyChangedEvent(ValuePatternIdentifiers.IsReadOnlyProperty, e.OldValue, e.NewValue);
        var name = GetName();
        var help = GetHelpText();
        var status = GetItemStatus();
        if (_name != name)
            RaisePropertyChangedEvent(AutomationElementIdentifiers.NameProperty, _name, name);
        if (_help != help)
            RaisePropertyChangedEvent(AutomationElementIdentifiers.HelpTextProperty, _help, help);
        if (_status != status)
            RaisePropertyChangedEvent(AutomationElementIdentifiers.ItemStatusProperty, _status, status);
        _name = name;
        _help = help;
        _status = status;
    }
}
