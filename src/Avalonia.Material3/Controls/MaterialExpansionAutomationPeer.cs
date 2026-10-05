using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace Avalonia.Material3.Controls;

internal interface IMaterialExpansion
{
    Control ExpansionControl { get; }
    bool IsExpanded { get; }
    void SetExpanded(bool expanded);
}

internal sealed class MaterialExpansionAutomationPeer : ControlAutomationPeer, IExpandCollapseProvider
{
    private readonly IMaterialExpansion _expansion;
    private readonly AutomationControlType _role;
    public MaterialExpansionAutomationPeer(Control owner, IMaterialExpansion expansion, AutomationControlType role) : base(owner)
    {
        _expansion = expansion;
        _role = role;
        expansion.ExpansionControl.PropertyChanged += (_, change) =>
        {
            if (change.Property.Name == nameof(IMaterialExpansion.IsExpanded))
                RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                    change.GetOldValue<bool>() ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed,
                    change.GetNewValue<bool>() ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
        };
    }
    protected override AutomationControlType GetAutomationControlTypeCore() => _role;
    protected override string GetClassNameCore() => Owner.GetType().Name;
    public bool ShowsMenu => _role == AutomationControlType.Menu;
    public ExpandCollapseState ExpandCollapseState => _expansion.IsExpanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
    public void Expand() { EnsureEnabled(); _expansion.SetExpanded(true); }
    public void Collapse() { EnsureEnabled(); _expansion.SetExpanded(false); }
    protected override object? GetProviderCore(Type providerType) => providerType == typeof(IExpandCollapseProvider) ? this : base.GetProviderCore(providerType);
}

internal sealed class MaterialExpansionButton : MaterialFab
{
    internal IMaterialExpansion? Expansion { get; set; }
    public static readonly StyledProperty<bool> IsExpandedProperty = AvaloniaProperty.Register<MaterialExpansionButton, bool>(nameof(IsExpanded));
    public bool IsExpanded { get => GetValue(IsExpandedProperty); set => SetValue(IsExpandedProperty, value); }
    protected override double GetIconSize(MaterialFabSize size) => IsExpanded ? 20 : base.GetIconSize(size);
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsExpandedProperty)
            RaisePropertyChanged(IconSizeProperty, IsExpanded ? 24 : 20, IconSize);
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new ExpansionButtonPeer(this);
    private sealed class ExpansionButtonPeer : ButtonAutomationPeer, IExpandCollapseProvider
    {
        private readonly MaterialExpansionButton _button;
        public ExpansionButtonPeer(MaterialExpansionButton button) : base(button)
        {
            _button = button;
            button.PropertyChanged += (_, change) =>
            {
                if (change.Property == IsExpandedProperty)
                    RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                        change.GetOldValue<bool>() ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed,
                        change.GetNewValue<bool>() ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
            };
        }
        public bool ShowsMenu => _button.Expansion is MaterialFabMenu;
        public ExpandCollapseState ExpandCollapseState => _button.IsExpanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
        public void Expand() { EnsureEnabled(); _button.Expansion?.SetExpanded(true); }
        public void Collapse() { EnsureEnabled(); _button.Expansion?.SetExpanded(false); }
        protected override object? GetProviderCore(Type providerType) => providerType == typeof(IExpandCollapseProvider) ? this : base.GetProviderCore(providerType);
    }
}
