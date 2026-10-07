using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Presenters;
using Avalonia.Media;

namespace Avalonia.Material3.Controls;

/// <summary>Calendar date action with native toggle/input semantics and a 48 DIP minimum target.
/// IsChecked is the endpoint state; IsInRange is the interval band, not a second selection.</summary>
public class MaterialCalendarDay : ToggleButton
{
    internal bool AnimateContainer { get; set; } = true;
    private readonly MaterialMotionSettings _motion;
    private readonly MaterialMotionBrush _containerColor, _contentColor;
    private MaterialCalendarCircle? _circle;
    private ContentPresenter? _content;
    public MaterialCalendarDay()
    {
        _containerColor = new(this, Brushes.Transparent, value => { if (_circle is not null) _circle.Background = value; });
        _contentColor = new(this, null, value => { if (_content is not null) _content.Foreground = value; });
        _motion = new(this, UpdateColors);
    }
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _circle = e.NameScope.Find<MaterialCalendarCircle>("Circle");
        _content = _circle?.Child as ContentPresenter;
        _containerColor.Snap(Background); _contentColor.Snap(Foreground);
    }
    private void UpdateColors()
    {
        if (_motion is null || _circle is null) return;
        if (AnimateContainer) _containerColor.Set(Background, _motion.DefaultEffects); else _containerColor.Snap(Background);
        if (IsInRange) _contentColor.Snap(Foreground); else _contentColor.Set(Foreground, _motion.DefaultEffects);
    }
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
        if (change.Property == BackgroundProperty || change.Property == ForegroundProperty || change.Property == IsCheckedProperty || change.Property == IsInRangeProperty) UpdateColors();
    }
}

/// <summary>Year action. Native ToggleButton peer communicates the selected year.</summary>
public class MaterialCalendarYear : ToggleButton
{
    private readonly MaterialMotionSettings _motion;
    private readonly MaterialMotionBrush _containerColor, _contentColor;
    private Border? _face;
    private ContentPresenter? _content;
    public MaterialCalendarYear()
    {
        _containerColor = new(this, Brushes.Transparent, value => { if (_face is not null) _face.Background = value; });
        _contentColor = new(this, null, value => { if (_content is not null) _content.Foreground = value; });
        _motion = new(this, UpdateColors);
    }
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e); _face = e.NameScope.Find<Border>("YearFace"); _content = _face?.Child as ContentPresenter;
        _containerColor.Snap(Background); _contentColor.Snap(Foreground);
    }
    private void UpdateColors()
    {
        if (_motion is null || _face is null) return;
        _containerColor.Set(Background, _motion.DefaultEffects); _contentColor.Set(Foreground, _motion.DefaultEffects);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BackgroundProperty || change.Property == ForegroundProperty || change.Property == IsCheckedProperty) UpdateColors();
    }
    public int Year { get; init; }
    protected override Type StyleKeyOverride => typeof(MaterialCalendarYear);
}
