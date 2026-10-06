using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Avalonia.Material3.Controls;

/// <summary>Calendar date action with native toggle/input semantics and a 48 DIP minimum target.
/// IsChecked is the endpoint state; IsInRange is the interval band, not a second selection.</summary>
public class MaterialCalendarDay : ToggleButton
{
    public static readonly StyledProperty<DateOnly> DateProperty = AvaloniaProperty.Register<MaterialCalendarDay, DateOnly>(nameof(Date));
    public static readonly StyledProperty<bool> IsTodayProperty = AvaloniaProperty.Register<MaterialCalendarDay, bool>(nameof(IsToday));
    public static readonly StyledProperty<bool> IsInRangeProperty = AvaloniaProperty.Register<MaterialCalendarDay, bool>(nameof(IsInRange));
    public DateOnly Date { get => GetValue(DateProperty); set => SetValue(DateProperty, value); }
    public bool IsToday { get => GetValue(IsTodayProperty); set => SetValue(IsTodayProperty, value); }
    public bool IsInRange { get => GetValue(IsInRangeProperty); set => SetValue(IsInRangeProperty, value); }
    protected override Type StyleKeyOverride => typeof(MaterialCalendarDay);
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsTodayProperty) PseudoClasses.Set(":today", IsToday);
        if (change.Property == IsInRangeProperty) PseudoClasses.Set(":in-range", IsInRange);
    }
}

/// <summary>Year action. Native ToggleButton peer communicates the selected year.</summary>
public class MaterialCalendarYear : ToggleButton
{
    public int Year { get; init; }
    protected override Type StyleKeyOverride => typeof(MaterialCalendarYear);
}
