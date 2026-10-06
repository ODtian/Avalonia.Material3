using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Avalonia.Material3.Controls;

internal sealed class MaterialNavigationAutomationPeer : ControlAutomationPeer, ISelectionProvider
{
    private readonly MaterialNavigation _navigation;
    public MaterialNavigationAutomationPeer(MaterialNavigation owner) : base(owner)
    {
        _navigation = owner;
        owner.SelectionChanged += (_, _) => RaisePropertyChangedEvent(SelectionPatternIdentifiers.SelectionProperty, null, null);
    }
    protected override AutomationControlType GetAutomationControlTypeCore() => _navigation is MaterialTabs ? AutomationControlType.Tab : AutomationControlType.List;
    public bool CanSelectMultiple => false;
    public bool IsSelectionRequired => true;
    public IReadOnlyList<AutomationPeer> GetSelection() => _navigation.SelectedItem is { } selected && CreatePeerForElement(selected) is { } peer ? new[] { peer } : Array.Empty<AutomationPeer>();
}

internal sealed class MaterialNavigationItemAutomationPeer : ButtonAutomationPeer, ISelectionItemProvider
{
    private readonly MaterialNavigationItem _item;
    public MaterialNavigationItemAutomationPeer(MaterialNavigationItem item) : base(item)
    {
        _item = item;
        item.PropertyChanged += (_, change) =>
        {
            if (change.Property == MaterialNavigationItem.IsSelectedProperty)
                RaisePropertyChangedEvent(SelectionItemPatternIdentifiers.IsSelectedProperty, change.OldValue, change.NewValue);
            if (change.Property == MaterialNavigationItem.BadgeDescriptionProperty)
                RaisePropertyChangedEvent(AutomationElementIdentifiers.NameProperty, null, GetName());
        };
    }
    protected override AutomationControlType GetAutomationControlTypeCore() => _item.Owner is MaterialTabs ? AutomationControlType.TabItem : AutomationControlType.ListItem;
    protected override string? GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrWhiteSpace(_item.BadgeDescription) ? name : $"{name}, {_item.BadgeDescription}";
    }
    public bool IsSelected => _item.IsSelected;
    public ISelectionProvider? SelectionContainer => _item.Owner is { } owner ? CreatePeerForElement(owner) as ISelectionProvider : null;
    public void Select()
    {
        EnsureEnabled();
        _item.Owner?.Activate(_item);
    }
    public void AddToSelection()
    {
        EnsureEnabled();
        if (!IsSelected) throw new InvalidOperationException("Single-selection navigation cannot add a second selection. Use Select.");
    }
    public void RemoveFromSelection()
    {
        EnsureEnabled();
        if (IsSelected) throw new InvalidOperationException("Navigation requires a selection. Select another destination instead.");
    }
}
