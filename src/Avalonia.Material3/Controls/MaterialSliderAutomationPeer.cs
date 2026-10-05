using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Avalonia.Material3.Controls;

internal sealed class MaterialSliderAutomationPeer : ControlAutomationPeer, IRangeValueProvider
{
    private readonly MaterialSlider _slider;
    public MaterialSliderAutomationPeer(MaterialSlider owner) : base(owner)
    {
        _slider = owner;
        owner.PropertyChanged += (_, e) =>
        {
            if (e.Property == MaterialSlider.ValueProperty)
                RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty, e.OldValue, e.NewValue);
            else if (e.Property == MaterialSlider.MinimumProperty)
                RaisePropertyChangedEvent(RangeValuePatternIdentifiers.MinimumProperty, e.OldValue, e.NewValue);
            else if (e.Property == MaterialSlider.MaximumProperty)
                RaisePropertyChangedEvent(RangeValuePatternIdentifiers.MaximumProperty, e.OldValue, e.NewValue);
            else if (e.Property == MaterialSlider.IsEffectivelyEnabledProperty)
                RaisePropertyChangedEvent(RangeValuePatternIdentifiers.IsReadOnlyProperty, !(bool)e.OldValue!, !(bool)e.NewValue!);
        };
    }
    public bool IsReadOnly => !_slider.IsEffectivelyEnabled;
    public double Minimum => _slider.Minimum;
    public double Maximum => _slider.Maximum;
    public double Value => _slider.Value;
    public double SmallChange => _slider.KeyboardIncrement;
    public double LargeChange => SmallChange * 10;
    public void SetValue(double value)
    {
        if (IsReadOnly) throw new InvalidOperationException("The slider is disabled.");
        _slider.SetCurrentValue(MaterialSlider.ValueProperty, value);
    }
    protected override string? GetItemStatusCore() => base.GetItemStatusCore() ?? _slider.ValueText;
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Slider;
    protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => null;
}
