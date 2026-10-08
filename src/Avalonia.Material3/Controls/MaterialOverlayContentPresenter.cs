using Avalonia.Automation.Peers;
using Avalonia.Controls.Presenters;

namespace Avalonia.Material3.Controls;

/// <summary>Background content scope for a MaterialOverlayHost template. The host gates effective input eligibility
/// during modal presentation; that also removes background descendants from automation navigation,
/// without hiding their rendered content or flattening native automation parent relationships.</summary>
public class MaterialOverlayContentPresenter : ContentPresenter, IMaterialInputScope
{
    protected override bool IsEnabledCore => base.IsEnabledCore && MaterialModalPaintScope.IsInputScopeOpen(this);
    void IMaterialInputScope.RefreshInputScope() => UpdateIsEffectivelyEnabled();
    protected override AutomationPeer OnCreateAutomationPeer() => new MaterialOverlayScopeAutomationPeer(this);
}
