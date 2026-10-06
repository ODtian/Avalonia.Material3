using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Avalonia.Material3.Controls;

internal sealed class MaterialNavigationDrawerAutomationPeer : ControlAutomationPeer, ISelectionProvider, IExpandCollapseProvider
{
    private readonly MaterialNavigationDrawer _drawer;
    public MaterialNavigationDrawerAutomationPeer(MaterialNavigationDrawer drawer) : base(drawer)
    {
        _drawer = drawer;
        drawer.SelectionChanged += (_, _) => RaisePropertyChangedEvent(SelectionPatternIdentifiers.SelectionProperty, null, null);
        drawer.PropertyChanged += (_, change) =>
        {
            if (change.Property == MaterialNavigationDrawer.IsOpenProperty)
            {
                RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty, null, ExpandCollapseState);
                RaisePropertyChangedEvent(AutomationElementIdentifiers.ItemStatusProperty, null, GetItemStatus());
            }
            if (change.Property == MaterialNavigationDrawer.TitleProperty) RaisePropertyChangedEvent(AutomationElementIdentifiers.NameProperty, change.OldValue, GetName());
        };
    }
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.List;
    protected override string? GetNameCore() => base.GetNameCore() ?? _drawer.Title;
    protected override string GetItemStatusCore() => $"{_drawer.Mode.ToString().ToLowerInvariant()}-{(_drawer.IsOpen ? "open" : "closed")}";
    public bool CanSelectMultiple => false;
    public bool IsSelectionRequired => true;
    public bool ShowsMenu => false;
    public IReadOnlyList<AutomationPeer> GetSelection() => _drawer.SelectedItem is { } item && CreatePeerForElement(item) is { } peer ? [peer] : [];
    public ExpandCollapseState ExpandCollapseState => _drawer.IsOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
    public void Expand() { EnsureEnabled(); _drawer.SetCurrentValue(MaterialNavigationDrawer.IsOpenProperty, true); }
    public void Collapse() { EnsureEnabled(); _drawer.Close(); }
}
