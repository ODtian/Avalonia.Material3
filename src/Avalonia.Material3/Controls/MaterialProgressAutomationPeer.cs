using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Avalonia.Material3.Controls;

internal sealed class MaterialProgressAutomationPeer : ControlAutomationPeer, IRangeValueProvider
{
    private readonly MaterialProgressIndicator _indicator;
    private volatile bool _indeterminate;
    public MaterialProgressAutomationPeer(MaterialProgressIndicator indicator) : base(indicator)
    {
        _indicator = indicator;
        _indeterminate = indicator.EffectiveIndeterminate;
        indicator.PropertyChanged += (_, change) =>
        {
            _indeterminate = indicator.EffectiveIndeterminate;
            if (change.Property == MaterialProgressIndicator.ValueProperty)
                RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty, change.OldValue, Value);
            if (change.Property == MaterialProgressIndicator.StatusDescriptionProperty)
                RaisePropertyChangedEvent(AutomationElementIdentifiers.ItemStatusProperty, change.OldValue, change.NewValue);
            if (change.Property == MaterialProgressIndicator.StatusProperty)
                RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty, null, Value);
        };
    }
    public bool IsReadOnly => true;
    public double Minimum => 0;
    public double Maximum => 1;
    public double Value => _indicator.EffectiveValue;
    public double SmallChange => 0;
    public double LargeChange => 0;
    public void SetValue(double value) => throw new InvalidOperationException("Progress is read-only and driven by the host.");
    protected override object? GetProviderCore(Type providerType) =>
        providerType == typeof(IRangeValueProvider) && _indeterminate ? null : base.GetProviderCore(providerType);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ProgressBar;
    protected override string? GetItemStatusCore() => base.GetItemStatusCore() ?? _indicator.StatusDescription;
    protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => null;
}
