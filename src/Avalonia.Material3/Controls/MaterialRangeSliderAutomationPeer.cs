using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Input;

namespace Avalonia.Material3.Controls;

internal sealed class MaterialRangeSliderAutomationPeer(MaterialRangeSlider owner) : ControlAutomationPeer(owner)
{
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
    protected override IReadOnlyList<AutomationPeer> GetChildrenCore() =>
        owner.Endpoints.Select(CreatePeerForElement).ToArray();
}

internal sealed class SliderEndpoint : Control
{
    private readonly MaterialRangeSlider _slider;
    private readonly bool _upper;
    public SliderEndpoint(MaterialRangeSlider slider, bool upper)
    {
        _slider = slider;
        _upper = upper;
        Focusable = true;
        FocusAdorner = null;
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsFocusedProperty)
        {
            if (IsFocused) _slider.ActiveUpper = _upper;
            (Parent as Control)?.InvalidateVisual();
        }
    }
    public override void Render(Avalonia.Media.DrawingContext context) =>
        context.DrawRectangle(Avalonia.Media.Brushes.Transparent, null, new Rect(Bounds.Size));
    protected override AutomationPeer OnCreateAutomationPeer() => new SliderEndpointAutomationPeer(this, _slider, _upper);
}

internal sealed class SliderEndpointAutomationPeer : ControlAutomationPeer, IRangeValueProvider
{
    private readonly MaterialRangeSlider _slider;
    private readonly bool _upper;
    public SliderEndpointAutomationPeer(Control owner, MaterialRangeSlider slider, bool upper) : base(owner)
    {
        _slider = slider;
        _upper = upper;
        slider.PropertyChanged += (_, e) =>
        {
            if (e.Property == (upper ? MaterialRangeSlider.UpperValueProperty : MaterialSlider.ValueProperty))
                RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty, e.OldValue, e.NewValue);
            if (e.Property == (upper ? MaterialSlider.ValueProperty : MaterialRangeSlider.UpperValueProperty))
                RaisePropertyChangedEvent(upper ? RangeValuePatternIdentifiers.MinimumProperty : RangeValuePatternIdentifiers.MaximumProperty, e.OldValue, e.NewValue);
            if (e.Property == MaterialSlider.MinimumProperty || e.Property == MaterialSlider.MaximumProperty)
                RaisePropertyChangedEvent(e.Property == MaterialSlider.MinimumProperty ? RangeValuePatternIdentifiers.MinimumProperty : RangeValuePatternIdentifiers.MaximumProperty, e.OldValue, e.NewValue);
            if (e.Property == MaterialSlider.IsEffectivelyEnabledProperty)
                RaisePropertyChangedEvent(RangeValuePatternIdentifiers.IsReadOnlyProperty, !(bool)e.OldValue!, !(bool)e.NewValue!);
        };
    }
    public bool IsReadOnly => !_slider.IsEffectivelyEnabled;
    public double Minimum => _upper ? _slider.LowerValue : _slider.Minimum;
    public double Maximum => _upper ? _slider.Maximum : _slider.UpperValue;
    public double Value => _upper ? _slider.UpperValue : _slider.LowerValue;
    public double SmallChange => _slider.KeyboardIncrement;
    public double LargeChange => SmallChange * 10;
    public void SetValue(double value)
    {
        if (IsReadOnly) throw new InvalidOperationException("The range slider is disabled.");
        _slider.SetCurrentValue(_upper ? MaterialRangeSlider.UpperValueProperty : MaterialSlider.ValueProperty, value);
    }
    protected override string? GetItemStatusCore() => base.GetItemStatusCore() ?? _slider.FormatValue(Value);
    protected override string GetNameCore() =>
        $"{CreatePeerForElement(_slider).GetName()} {(_upper ? _slider.UpperLabel : _slider.LowerLabel)}".Trim();
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Slider;
    protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => null;
}
