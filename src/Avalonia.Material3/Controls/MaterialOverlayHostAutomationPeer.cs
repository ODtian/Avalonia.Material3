using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;

namespace Avalonia.Material3.Controls;

internal sealed class MaterialOverlayHostAutomationPeer(MaterialOverlayHost host) : ControlAutomationPeer(host)
{
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;
}

// Keep the accessibility hierarchy congruent with the visual hierarchy. Flattening a dialog
// directly into host.Children makes ControlAutomationPeer reconnect it to its visual parent
// on native queries, leaving UIA's fragment graph inconsistent (E_FAIL on GetFocus/Navigate).
internal sealed class MaterialOverlayScopeAutomationPeer : ControlAutomationPeer
{
    public MaterialOverlayScopeAutomationPeer(Control owner) : base(owner)
    {
        owner.PropertyChanged += (_, change) =>
        {
            if (change.Property != Input.InputElement.IsEnabledProperty) return;
            InvalidateChildren();
            // Reconnect cached parents before the next native focus notification/tree walk.
            GetChildren();
        };
    }
    protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => Owner.IsEnabled ? base.GetChildrenCore() : null;
}
