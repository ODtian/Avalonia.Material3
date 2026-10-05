using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Avalonia.Material3.Controls;

internal sealed class MaterialButtonAutomationPeer : ButtonAutomationPeer, IToggleProvider
{
    private readonly MaterialButton _button;
    // Native automation queries provider availability off the UI thread. Do not read a StyledProperty there.
    private volatile bool _isToggle;

    public MaterialButtonAutomationPeer(MaterialButton button) : base(button)
    {
        _button = button;
        _isToggle = button.IsToggle;
        button.PropertyChanged += (_, change) =>
        {
            if (change.Property == MaterialButton.IsToggleProperty)
                _isToggle = change.GetNewValue<bool>();
            if (change.Property == MaterialButton.IsCheckedProperty && _isToggle)
                RaisePropertyChangedEvent(TogglePatternIdentifiers.ToggleStateProperty,
                    change.GetOldValue<bool>() ? ToggleState.On : ToggleState.Off,
                    change.GetNewValue<bool>() ? ToggleState.On : ToggleState.Off);
        };
    }

    public ToggleState ToggleState => _button.IsChecked ? ToggleState.On : ToggleState.Off;

    public void Toggle()
    {
        EnsureEnabled();
        if (_button.IsToggle)
            Invoke();
    }

    protected override object? GetProviderCore(Type providerType) =>
        providerType == typeof(IToggleProvider) && !_isToggle ? null : base.GetProviderCore(providerType);
}
