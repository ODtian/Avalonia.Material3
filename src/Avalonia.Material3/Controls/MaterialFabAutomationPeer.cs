using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Avalonia.Material3.Controls;

/// <summary>Invoke remains Button's foundation; hosted whole-toolbar FABs additionally expose disclosure.</summary>
internal sealed class MaterialFabAutomationPeer : ButtonAutomationPeer, IExpandCollapseProvider
{
    private readonly MaterialFab _fab;
    private volatile MaterialToolbar? _toolbar;
    private volatile bool _hasExpansion;
    public MaterialFabAutomationPeer(MaterialFab fab) : base(fab)
    {
        _fab = fab;
        fab.ToolbarExpansionChanged += (_, _) => Attach();
        Attach();
    }
    private void Attach()
    {
        if (_toolbar is not null) _toolbar.PropertyChanged -= ToolbarChanged;
        _toolbar = _fab.ToolbarExpansion;
        if (_toolbar is not null) _toolbar.PropertyChanged += ToolbarChanged;
        _hasExpansion = _toolbar is { CollapseBehavior: MaterialToolbarCollapseBehavior.WholeToolbar };
    }
    private void ToolbarChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == MaterialToolbar.CollapseBehaviorProperty)
            _hasExpansion = _toolbar is { CollapseBehavior: MaterialToolbarCollapseBehavior.WholeToolbar };
        if (e.Property == MaterialToolbar.IsExpandedProperty)
            RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                e.GetOldValue<bool>() ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed,
                e.GetNewValue<bool>() ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
    }
    public bool ShowsMenu => false;
    public ExpandCollapseState ExpandCollapseState => _toolbar?.IsExpanded == false ? ExpandCollapseState.Collapsed : ExpandCollapseState.Expanded;
    public void Expand() { EnsureEnabled(); _toolbar?.SetCurrentValue(MaterialToolbar.IsExpandedProperty, true); }
    public void Collapse() { EnsureEnabled(); _toolbar?.SetCurrentValue(MaterialToolbar.IsExpandedProperty, false); }
    // Native provider discovery occurs off the UI thread: inspect only the cached flag, never a StyledProperty.
    protected override object? GetProviderCore(Type providerType) => providerType == typeof(IExpandCollapseProvider) ? _hasExpansion ? this : null : base.GetProviderCore(providerType);
}
