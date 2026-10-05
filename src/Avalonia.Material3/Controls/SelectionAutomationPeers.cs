using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Automation.Peers;
using Avalonia.Controls.Primitives;

namespace Avalonia.Material3.Controls;

// Preserve Avalonia's provider actions/state notifications. Only add the host's validation explanation.
internal sealed class SelectionToggleAutomationPeer(ToggleButton owner) : ToggleButtonAutomationPeer(owner)
{
    protected override string? GetHelpTextCore() =>
        SelectionAutomationHelp.Get(Owner) ?? base.GetHelpTextCore();
}

internal sealed class SelectionRadioAutomationPeer(MaterialRadioButton owner) : RadioButtonAutomationPeer(owner)
{
    protected override string? GetHelpTextCore() =>
        SelectionAutomationHelp.Get(Owner) ?? base.GetHelpTextCore();
}

internal static class SelectionAutomationHelp
{
    public static string? Get(ToggleButton owner) =>
        string.IsNullOrEmpty(AutomationProperties.GetHelpText(owner)) && owner.GetValue(MaterialCheckBox.IsErrorProperty)
            ? owner.GetValue(MaterialCheckBox.ErrorTextProperty)
            : null;
}
