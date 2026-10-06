using Avalonia.Automation.Peers;
using Avalonia.Controls.Presenters;

namespace Avalonia.Material3.Controls;

/// <summary>Background content scope for a MaterialOverlayHost template. The host gates IsEnabled
/// during modal presentation; that also removes background descendants from automation navigation,
/// without hiding their rendered content or flattening native automation parent relationships.</summary>
public class MaterialOverlayContentPresenter : ContentPresenter
{
    protected override AutomationPeer OnCreateAutomationPeer() => new MaterialOverlayScopeAutomationPeer(this);
}
