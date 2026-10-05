using Avalonia.Automation.Peers;
using Avalonia.Data;

namespace Avalonia.Material3.Controls;

/// <summary>A Material slider with independently editable, ordered endpoints.</summary>
public class MaterialRangeSlider : MaterialSlider
{
    /// <summary>The lower endpoint; synchronized with inherited Value for slider template reuse.</summary>
    public static readonly StyledProperty<double> LowerValueProperty =
        AvaloniaProperty.Register<MaterialRangeSlider, double>(nameof(LowerValue), 0,
            defaultBindingMode: BindingMode.TwoWay, validate: double.IsFinite,
            coerce: (owner, value) => ((MaterialRangeSlider)owner).Normalize(value));
    public static readonly StyledProperty<double> UpperValueProperty =
        AvaloniaProperty.Register<MaterialRangeSlider, double>(nameof(UpperValue), 100,
            defaultBindingMode: BindingMode.TwoWay, validate: double.IsFinite,
            coerce: (owner, value) => ((MaterialRangeSlider)owner).NormalizeUpper(value));

    public static readonly StyledProperty<string> LowerLabelProperty =
        AvaloniaProperty.Register<MaterialRangeSlider, string>(nameof(LowerLabel), "Lower value", validate: value => value is not null);
    public static readonly StyledProperty<string> UpperLabelProperty =
        AvaloniaProperty.Register<MaterialRangeSlider, string>(nameof(UpperLabel), "Upper value", validate: value => value is not null);

    private SliderEndpoint[]? _endpointControls;
    internal IReadOnlyList<SliderEndpoint> EndpointControls =>
        _endpointControls ??= [new SliderEndpoint(this, false), new SliderEndpoint(this, true)];

    static MaterialRangeSlider() => FocusableProperty.OverrideDefaultValue<MaterialRangeSlider>(false);
    public MaterialRangeSlider()
    {
        SetValue(LowerValueProperty, 0d);
        SetValue(UpperValueProperty, 100d);
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new MaterialRangeSliderAutomationPeer(this);
    public string LowerLabel { get => GetValue(LowerLabelProperty); set => SetValue(LowerLabelProperty, value); }
    public string UpperLabel { get => GetValue(UpperLabelProperty); set => SetValue(UpperLabelProperty, value); }

    public double LowerValue { get => GetValue(LowerValueProperty); set => SetValue(LowerValueProperty, value); }
    public double UpperValue { get => GetValue(UpperValueProperty); set => SetValue(UpperValueProperty, value); }

    internal override double Normalize(double value) => Math.Min(base.Normalize(value), Math.Clamp(UpperValue, Minimum, Maximum));
    private double NormalizeUpper(double value) => Math.Max(base.Normalize(value), Math.Clamp(Value, Minimum, Maximum));

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == LowerValueProperty) SetCurrentValue(ValueProperty, LowerValue);
        else if (change.Property == ValueProperty) SetCurrentValue(LowerValueProperty, Value);
        if (change.Property == MinimumProperty || change.Property == MaximumProperty || change.Property == StepProperty)
        {
            CoerceValue(UpperValueProperty);
            CoerceValue(ValueProperty);
            CoerceValue(LowerValueProperty);
        }
    }
}
