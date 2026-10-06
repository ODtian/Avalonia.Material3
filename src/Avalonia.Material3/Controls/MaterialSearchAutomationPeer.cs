using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Avalonia.Material3.Controls;

internal sealed class MaterialSearchAutomationPeer : ControlAutomationPeer, IExpandCollapseProvider
{
    private readonly MaterialSearch _search;
    public MaterialSearchAutomationPeer(MaterialSearch search) : base(search)
    {
        _search = search;
        search.PropertyChanged += (_, change) =>
        {
            if (change.Property == MaterialSearch.IsOpenProperty)
                RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                    change.GetOldValue<bool>() ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed,
                    change.GetNewValue<bool>() ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
            if (change.Property == MaterialSearch.LabelProperty)
                RaisePropertyChangedEvent(AutomationElementIdentifiers.NameProperty, change.OldValue, GetName());
            if (change.Property == MaterialSearch.HasErrorProperty || change.Property == MaterialSearch.EffectiveErrorTextProperty)
            {
                RaisePropertyChangedEvent(AutomationElementIdentifiers.ItemStatusProperty, null, GetItemStatus());
                RaisePropertyChangedEvent(AutomationElementIdentifiers.HelpTextProperty, null, GetHelpText());
            }
            if (change.Property == MaterialSearch.SupportingTextProperty)
                RaisePropertyChangedEvent(AutomationElementIdentifiers.HelpTextProperty, change.OldValue, GetHelpText());
        };
    }
    protected override string GetClassNameCore() => nameof(MaterialSearch);
    protected override AutomationControlType GetAutomationControlTypeCore() =>
        _search.Mode is MaterialSearchMode.FilledAutocomplete or MaterialSearchMode.OutlinedAutocomplete ? AutomationControlType.ComboBox : AutomationControlType.Group;
    protected override string? GetNameCore() => base.GetNameCore() is { Length: > 0 } name ? name : _search.Label;
    protected override string? GetHelpTextCore() => base.GetHelpTextCore() is { Length: > 0 } help ? help : _search.Editor?.EffectiveSupportingText ?? _search.EffectiveErrorText ?? _search.SupportingText;
    protected override string? GetItemStatusCore() => base.GetItemStatusCore() is { Length: > 0 } status ? status : _search.HasError ? "Invalid" : null;
    public bool ShowsMenu => false;
    public ExpandCollapseState ExpandCollapseState => _search.IsOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
    public void Expand() { EnsureEnabled(); _search.Open(); }
    public void Collapse() { EnsureEnabled(); _search.Close(); }
}
