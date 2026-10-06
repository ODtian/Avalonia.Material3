using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Avalonia.Material3.Controls;

internal sealed class MaterialGroupAutomationPeer : ControlAutomationPeer, ISelectionProvider
{
    private readonly MaterialButtonGroup _group;
    private volatile bool _selectionAvailable;
    private bool _multiple, _required;
    private IReadOnlyList<AutomationPeer> _selection;
    public MaterialGroupAutomationPeer(MaterialButtonGroup group) : base(group)
    {
        _group = group;
        _selectionAvailable = group.SelectionMode != MaterialGroupSelectionMode.None;
        _multiple = CanSelectMultiple; _required = IsSelectionRequired; _selection = GetSelection();
        group.SelectionChanged += (_, _) =>
        {
            var selection = GetSelection();
            RaisePropertyChangedEvent(SelectionPatternIdentifiers.SelectionProperty, _selection, selection);
            _selection = selection;
        };
        group.PropertyChanged += (_, e) =>
        {
            if (e.Property == MaterialButtonGroup.SelectionModeProperty)
                _selectionAvailable = e.GetNewValue<MaterialGroupSelectionMode>() != MaterialGroupSelectionMode.None;
            if (e.Property == MaterialButtonGroup.SelectionModeProperty || e.Property == MaterialButtonGroup.AllowEmptySelectionProperty)
            {
                var multiple = CanSelectMultiple; var required = IsSelectionRequired;
                if (_multiple != multiple) RaisePropertyChangedEvent(SelectionPatternIdentifiers.CanSelectMultipleProperty, _multiple, multiple);
                if (_required != required) RaisePropertyChangedEvent(SelectionPatternIdentifiers.IsSelectionRequiredProperty, _required, required);
                _multiple = multiple; _required = required;
            }
        };
    }
    public bool CanSelectMultiple => _group.SelectionMode == MaterialGroupSelectionMode.Multiple;
    public bool IsSelectionRequired => !_group.AllowEmptySelection && _group.SelectionMode == MaterialGroupSelectionMode.Single;
    public IReadOnlyList<AutomationPeer> GetSelection() => _group.SelectedItems.Select(button => CreatePeerForElement(button)!).ToArray();
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
    protected override object? GetProviderCore(Type providerType) => providerType == typeof(ISelectionProvider) && !_selectionAvailable ? null : base.GetProviderCore(providerType);
}

internal sealed class MaterialGroupItemAutomationPeer : ButtonAutomationPeer, ISelectionItemProvider, IToggleProvider
{
    private readonly MaterialGroupButton _button;
    private volatile bool _selectionAvailable;
    private volatile MaterialGroupSelectionMode _mode;
    private bool _selected;
    public MaterialGroupItemAutomationPeer(MaterialGroupButton button) : base(button)
    {
        _button = button;
        _selectionAvailable = button.IsToggle;
        _mode = button.Group?.SelectionMode ?? MaterialGroupSelectionMode.None;
        _selected = button.IsChecked;
        button.GroupStateChanged += () => { _mode = button.Group?.SelectionMode ?? MaterialGroupSelectionMode.None; _selectionAvailable = button.IsToggle; };
        button.PropertyChanged += (_, e) =>
        {
            if (e.Property == MaterialButton.IsToggleProperty) _selectionAvailable = e.GetNewValue<bool>();
            if (e.Property == MaterialButton.IsCheckedProperty && _selected != button.IsChecked)
            {
                var old = _selected; _selected = button.IsChecked;
                RaisePropertyChangedEvent(SelectionItemPatternIdentifiers.IsSelectedProperty, old, _selected);
                if (_mode == MaterialGroupSelectionMode.Multiple)
                    RaisePropertyChangedEvent(TogglePatternIdentifiers.ToggleStateProperty, old ? ToggleState.On : ToggleState.Off, _selected ? ToggleState.On : ToggleState.Off);
            }
        };
    }
    public bool IsSelected => _button.IsChecked;
    public ISelectionProvider? SelectionContainer => _button.Group is { } group ? CreatePeerForElement(group)?.GetProvider<ISelectionProvider>() : null;
    public ToggleState ToggleState => _button.IsChecked ? ToggleState.On : ToggleState.Off;
    public void Toggle() { EnsureEnabled(); Invoke(); }
    protected override AutomationControlType GetAutomationControlTypeCore() => _mode switch
    { MaterialGroupSelectionMode.Single => AutomationControlType.RadioButton, MaterialGroupSelectionMode.Multiple => AutomationControlType.CheckBox, _ => AutomationControlType.Button };
    public void AddToSelection()
    {
        EnsureEnabled();
        if (_button.Group is { SelectionMode: MaterialGroupSelectionMode.Single } group && group.SelectedItems.Any(button => button != _button))
            throw new InvalidOperationException("A single-selection group already has a selected option. Use Select to replace it.");
        _button.SetCurrentValue(MaterialButton.IsCheckedProperty, true);
    }
    public void RemoveFromSelection()
    {
        EnsureEnabled();
        if (_button.IsChecked && _button.Group is { SelectionMode: MaterialGroupSelectionMode.Single, AllowEmptySelection: false })
            throw new InvalidOperationException("This group requires a selection.");
        _button.SetCurrentValue(MaterialButton.IsCheckedProperty, false);
    }
    public void Select()
    {
        EnsureEnabled();
        if (_button.Group is { } group)
            foreach (var other in group.Buttons.Where(other => other != _button)) other.SetCurrentValue(MaterialButton.IsCheckedProperty, false);
        _button.SetCurrentValue(MaterialButton.IsCheckedProperty, true);
    }
    protected override object? GetProviderCore(Type providerType) =>
        (providerType == typeof(ISelectionItemProvider) && !_selectionAvailable) || (providerType == typeof(IToggleProvider) && _mode != MaterialGroupSelectionMode.Multiple) ? null : base.GetProviderCore(providerType);
}
