using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using System.Globalization;

namespace Avalonia.Material3.Controls;

public enum SliderValueLabelVisibility { Never, OnInteraction, Always }

/// <summary>A Material continuous or discrete slider. Step zero permits continuous values.</summary>
public class MaterialSlider : TemplatedControl
{
    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<MaterialSlider, double>(nameof(Minimum), 0, validate: double.IsFinite);
    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<MaterialSlider, double>(nameof(Maximum), 100, validate: double.IsFinite);
    public static readonly StyledProperty<double> StepProperty =
        AvaloniaProperty.Register<MaterialSlider, double>(nameof(Step), 0,
            validate: value => double.IsFinite(value) && value >= 0);
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<MaterialSlider, double>(nameof(Value), 0, defaultBindingMode: BindingMode.TwoWay,
            validate: double.IsFinite, coerce: (owner, value) => ((MaterialSlider)owner).Normalize(value));

    public static readonly StyledProperty<bool> ShowMarksProperty =
        AvaloniaProperty.Register<MaterialSlider, bool>(nameof(ShowMarks));
    public static readonly StyledProperty<IReadOnlyList<double>> MarksProperty =
        AvaloniaProperty.Register<MaterialSlider, IReadOnlyList<double>>(nameof(Marks), Array.Empty<double>(),
            validate: values => values is not null && values.All(double.IsFinite));
    public static readonly StyledProperty<string> LabelFormatProperty =
        AvaloniaProperty.Register<MaterialSlider, string>(nameof(LabelFormat), "0.##", validate: IsValidFormat);
    public static readonly StyledProperty<SliderValueLabelVisibility> ValueLabelVisibilityProperty =
        AvaloniaProperty.Register<MaterialSlider, SliderValueLabelVisibility>(nameof(ValueLabelVisibility),
            SliderValueLabelVisibility.OnInteraction, validate: Enum.IsDefined);
    public static readonly StyledProperty<IBrush?> ValueIndicatorBrushProperty =
        AvaloniaProperty.Register<MaterialSlider, IBrush?>(nameof(ValueIndicatorBrush));
    public static readonly StyledProperty<IBrush?> ValueIndicatorForegroundProperty =
        AvaloniaProperty.Register<MaterialSlider, IBrush?>(nameof(ValueIndicatorForeground));

    public static readonly StyledProperty<double> DisabledActiveOpacityProperty =
        AvaloniaProperty.Register<MaterialSlider, double>(nameof(DisabledActiveOpacity), 0.38,
            validate: value => double.IsFinite(value) && value is >= 0 and <= 1);
    public static readonly StyledProperty<double> DisabledInactiveOpacityProperty =
        AvaloniaProperty.Register<MaterialSlider, double>(nameof(DisabledInactiveOpacity), 0.12,
            validate: value => double.IsFinite(value) && value is >= 0 and <= 1);
    public double DisabledActiveOpacity { get => GetValue(DisabledActiveOpacityProperty); set => SetValue(DisabledActiveOpacityProperty, value); }
    public double DisabledInactiveOpacity { get => GetValue(DisabledInactiveOpacityProperty); set => SetValue(DisabledInactiveOpacityProperty, value); }

    public bool ShowMarks { get => GetValue(ShowMarksProperty); set => SetValue(ShowMarksProperty, value); }
    public IReadOnlyList<double> Marks { get => GetValue(MarksProperty); set => SetValue(MarksProperty, value); }
    public string LabelFormat { get => GetValue(LabelFormatProperty); set => SetValue(LabelFormatProperty, value); }
    public SliderValueLabelVisibility ValueLabelVisibility { get => GetValue(ValueLabelVisibilityProperty); set => SetValue(ValueLabelVisibilityProperty, value); }
    public IBrush? ValueIndicatorBrush { get => GetValue(ValueIndicatorBrushProperty); set => SetValue(ValueIndicatorBrushProperty, value); }
    public IBrush? ValueIndicatorForeground { get => GetValue(ValueIndicatorForegroundProperty); set => SetValue(ValueIndicatorForegroundProperty, value); }
    public string ValueText => FormatValue(Value);
    internal string FormatValue(double value) => value.ToString(LabelFormat, CultureInfo.CurrentCulture);
    private static bool IsValidFormat(string format)
    {
        if (format is null) return false;
        try { _ = 0d.ToString(format, CultureInfo.CurrentCulture); return true; }
        catch (FormatException) { return false; }
    }

    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<MaterialSlider, Orientation>(nameof(Orientation), Orientation.Horizontal, validate: Enum.IsDefined);
    public static readonly StyledProperty<bool> ReverseDirectionProperty =
        AvaloniaProperty.Register<MaterialSlider, bool>(nameof(ReverseDirection));
    public static readonly StyledProperty<bool> CenteredTrackProperty =
        AvaloniaProperty.Register<MaterialSlider, bool>(nameof(CenteredTrack));
    public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }
    public bool ReverseDirection { get => GetValue(ReverseDirectionProperty); set => SetValue(ReverseDirectionProperty, value); }
    public bool CenteredTrack { get => GetValue(CenteredTrackProperty); set => SetValue(CenteredTrackProperty, value); }
    internal bool IsReversed => ReverseDirection ^ (Orientation == Orientation.Horizontal && FlowDirection == FlowDirection.RightToLeft);
    internal double Fraction(double value)
    {
        if (Maximum == Minimum) return 0;
        var span = Maximum - Minimum;
        return double.IsFinite(span) ? (value - Minimum) / span : (value / 2 - Minimum / 2) / (Maximum / 2 - Minimum / 2);
    }
    internal double ValueAtFraction(double fraction) => fraction <= 0 ? Minimum : fraction >= 1 ? Maximum :
        Minimum * (1 - fraction) + Maximum * fraction;
    internal double KeyboardIncrement => Step > 0 ? Step : Maximum / 100 - Minimum / 100;
    internal double PointFraction(Point point)
    {
        var fraction = Orientation == Orientation.Horizontal
            ? (point.X - 24) / Math.Max(1, Bounds.Width - 48)
            : (Bounds.Height - 24 - point.Y) / Math.Max(1, Bounds.Height - 48);
        // Pointer coordinates are already logical: Avalonia mirrors the LTR/RTL visual boundary.
        return Math.Clamp(ReverseDirection ? 1 - fraction : fraction, 0, 1);
    }

    private MaterialSliderPresenter? _presenter;
    private IPointer? _dragPointer;
    internal bool IsDragging => _dragPointer is not null;
    internal IReadOnlyList<Control> Endpoints => _presenter?.Endpoints ?? [];
    internal bool ActiveUpper { get; set; }
    internal double ActiveValue => ActiveUpper && this is MaterialRangeSlider range ? range.UpperValue : Value;
    internal void SetActiveValue(double value)
    {
        if (ActiveUpper && this is MaterialRangeSlider range) range.SetCurrentValue(MaterialRangeSlider.UpperValueProperty, value);
        else SetCurrentValue(ValueProperty, value);
    }

    // Keep the setting at local priority even when no binding was supplied. Theme
    // reapplication must not discard a SetCurrentValue made against a default value.
    public MaterialSlider() => SetValue(ValueProperty, 0d);

    static MaterialSlider()
    {
        FocusableProperty.OverrideDefaultValue<MaterialSlider>(true);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new MaterialSliderAutomationPeer(this);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _presenter?.Attach(null);
        _presenter = e.NameScope.Find<MaterialSliderPresenter>("PART_Surface");
        _presenter?.Attach(this);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!IsEffectivelyEnabled || _dragPointer is not null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        if (this is MaterialRangeSlider range)
        {
            var fraction = PointFraction(e.GetPosition(this));
            var lowerFraction = Fraction(Value);
            var upperDistance = Math.Abs(fraction - Fraction(range.UpperValue));
            var lowerDistance = Math.Abs(fraction - lowerFraction);
            ActiveUpper = upperDistance < lowerDistance ||
                (upperDistance == lowerDistance && fraction > lowerFraction);
            if (Endpoints.Count == 2) Endpoints[ActiveUpper ? 1 : 0].Focus();
        }
        else Focus();
        _dragPointer = e.Pointer;
        e.Pointer.Capture(this);
        SetFromPoint(e.GetPosition(this));
        PseudoClasses.Set(":pressed", true);
        _presenter?.InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (e.Pointer == _dragPointer && IsEffectivelyEnabled) SetFromPoint(e.GetPosition(this));
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (e.Pointer != _dragPointer) return;
        if (!IsEffectivelyEnabled) { CancelDrag(); return; }
        SetFromPoint(e.GetPosition(this));
        _dragPointer = null;
        e.Pointer.Capture(null);
        PseudoClasses.Set(":pressed", false);
        _presenter?.InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _dragPointer = null;
        PseudoClasses.Set(":pressed", false);
        _presenter?.InvalidateVisual();
    }

    private void CancelDrag()
    {
        var pointer = _dragPointer;
        _dragPointer = null;
        pointer?.Capture(null);
        PseudoClasses.Set(":pressed", false);
        _presenter?.InvalidateVisual();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        CancelDrag();
        base.OnDetachedFromVisualTree(e);
    }

    private void SetFromPoint(Point point)
    {
        var fraction = PointFraction(point);
        SetActiveValue(ValueAtFraction(fraction));
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!IsEffectivelyEnabled) return;
        var direction = IsReversed ? -1 : 1;
        double? next = e.Key switch
        {
            Key.Left or Key.Down => MoveSteps(-direction),
            Key.Right or Key.Up => MoveSteps(direction),
            Key.PageDown => MoveSteps(-10),
            Key.PageUp => MoveSteps(10),
            Key.Home => Minimum,
            Key.End => Maximum,
            _ => null
        };
        if (next is null) return;
        SetActiveValue(next.Value);
        e.Handled = true;
    }

    internal double StepIndex(double value)
    {
        var delta = value - Minimum;
        return double.IsFinite(delta) ? delta / Step : (value / 2 - Minimum / 2) / (Step / 2);
    }

    private double MoveSteps(int steps)
    {
        if (Step == 0) return Math.Clamp(ActiveValue + KeyboardIncrement * steps, Minimum, Maximum);
        var position = StepIndex(ActiveValue);
        if (!double.IsFinite(position)) return Math.Clamp(ActiveValue + Step * steps, Minimum, Maximum);
        var index = steps < 0 ? Math.Ceiling(position) + steps : Math.Floor(position) + steps;
        var next = Minimum + index * Step;
        if (!double.IsFinite(next)) next = (Minimum / 2 + (index / 2) * Step) * 2;
        return Math.Clamp(next, Minimum, Maximum);
    }

    public double Minimum { get => GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public double Maximum { get => GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
    public double Step { get => GetValue(StepProperty); set => SetValue(StepProperty, value); }
    public double Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    internal virtual double Normalize(double value)
    {
        var min = Math.Min(Minimum, Maximum);
        var max = Math.Max(Minimum, Maximum);
        value = Math.Clamp(value, min, max);
        if (Step == 0 || min == max) return value;
        var count = Math.Floor(StepIndex(value));
        // At sub-ULP step sizes, the representable double already is the closest
        // representable setting. Avoid infinity * zero and preserve finite values.
        if (!double.IsFinite(count)) return value;
        var lower = min + count * Step;
        if (!double.IsFinite(lower)) lower = (min / 2 + (count / 2) * Step) * 2;
        var upper = Math.Min(max, lower + Step);
        return value - lower < upper - value ? lower : upper;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsEffectivelyEnabledProperty && !IsEffectivelyEnabled) CancelDrag();
        _presenter?.InvalidateVisual();
        _presenter?.InvalidateArrange();
        if (change.Property == ValueLabelVisibilityProperty || change.Property == FontSizeProperty || change.Property == FontFamilyProperty || change.Property == OrientationProperty)
            _presenter?.InvalidateMeasure();
        if (change.Property == MinimumProperty && Minimum > Maximum)
            SetCurrentValue(MaximumProperty, Minimum);
        else if (change.Property == MaximumProperty && Maximum < Minimum)
            SetCurrentValue(MinimumProperty, Maximum);
        if (change.Property == MinimumProperty || change.Property == MaximumProperty || change.Property == StepProperty)
            CoerceValue(ValueProperty);
    }
}
