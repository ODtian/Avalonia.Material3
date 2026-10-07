using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.Material3.Controls;

// Single DatePicker AnimatedContent and DateRangePicker Crossfade retain distinct recipes.
internal sealed class MaterialPickerModePanel : Panel
{
    private readonly MaterialDatePicker _owner;
    private readonly Control _calendar, _inputs;
    private readonly MaterialMotionValue _calendarAlpha, _inputAlpha, _calendarY, _inputY, _height;
    private readonly MaterialMotionSettings _motion;
    private readonly TranslateTransform _calendarTransform = new(), _inputTransform = new();
    private bool _initialized, _pending;
    private Size _calendarSize, _inputSize;
    internal MaterialPickerModePanel(MaterialDatePicker owner, Control calendar, Control inputs)
    {
        _owner = owner; _calendar = calendar; _inputs = inputs; Children.Add(calendar); Children.Add(inputs);
        ClipToBounds = true; UseLayoutRounding = false;
        calendar.RenderTransform = _calendarTransform; inputs.RenderTransform = _inputTransform;
        _calendarAlpha = new(this, 1, value => { calendar.Opacity = Math.Clamp(value, 0, 1); InvalidateMeasure(); });
        _inputAlpha = new(this, 0, value => { inputs.Opacity = Math.Clamp(value, 0, 1); InvalidateMeasure(); });
        _calendarY = new(this, 0, value => _calendarTransform.Y = value);
        _inputY = new(this, 0, value => _inputTransform.Y = value);
        _height = new(this, 0, _ => InvalidateMeasure());
        _motion = new(this, Retarget);
        owner.PropertyChanged += (_, change) =>
        {
            if (change.Property == MaterialDatePicker.ModeProperty || change.Property == MaterialDatePicker.SelectionModeProperty)
            { Prepare(); }
        };
    }
    internal void Prepare()
    {
        _pending = true; _calendar.IsVisible = _inputs.IsVisible = true;
        _calendar.IsEnabled = !Input; _inputs.IsEnabled = Input; InvalidateMeasure();
    }
    private bool Input => _owner.Mode == MaterialDatePickerMode.Input;
    private bool Range => _owner.SelectionMode == MaterialDateSelectionMode.Range;
    private void Retarget()
    {
        if (!_initialized || _calendarSize.Height <= 0 || _inputSize.Height <= 0) return;
        _pending = true; InvalidateMeasure();
    }
    private void Start(bool instant)
    {
        _calendar.IsEnabled = !Input; _inputs.IsEnabled = Input;
        if (instant || _motion.FastEffects.IsInstant)
        {
            _calendarAlpha.Snap(Input ? 0 : 1); _inputAlpha.Snap(Input ? 1 : 0);
            _calendarY.Snap(0); _inputY.Snap(0); _height.Snap(Input ? _inputSize.Height : _calendarSize.Height); return;
        }
        if (Range)
        {
            _calendarY.Snap(0); _inputY.Snap(0);
            _calendarAlpha.Spring(Input ? 0 : 1, _motion.FastEffects);
            _inputAlpha.Spring(Input ? 1 : 0, _motion.FastEffects);
            return;
        }
        if (Input && _inputAlpha.Value <= 0) _inputY.Snap(_inputSize.Height);
        if (!Input && _calendarAlpha.Value <= 0) _calendarY.Snap(-48);
        _calendarY.Spring(Input ? -48 : 0, _motion.DefaultSpatial, 1);
        _inputY.Spring(Input ? 0 : _inputSize.Height, _motion.DefaultSpatial, 1);
        _calendarAlpha.Spring(Input ? 0 : 1, Input ? _motion.FastEffects : _motion.DefaultEffects);
        _inputAlpha.Spring(Input ? 1 : 0, Input ? _motion.DefaultEffects : _motion.FastEffects);
        _height.Spring(Input ? _inputSize.Height : _calendarSize.Height, _motion.DefaultSpatial, 1);
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        _calendar.IsVisible = _inputs.IsVisible = true;
        _calendar.Measure(availableSize); _inputs.Measure(new Size(availableSize.Width, double.PositiveInfinity));
        _calendarSize = _calendar.DesiredSize; _inputSize = _inputs.DesiredSize;
        if (!_initialized) { _initialized = true; Start(true); }
        else if (_pending) { _pending = false; Start(false); }
        var height = Range ? Math.Max(_calendarAlpha.Value > 0 ? _calendarSize.Height : 0, _inputAlpha.Value > 0 ? _inputSize.Height : 0) : Math.Max(0, _height.Value);
        return new(Math.Max(_calendarSize.Width, _inputSize.Width), height);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        _calendar.Arrange(new Rect(0, 0, finalSize.Width, _calendarSize.Height));
        _inputs.Arrange(new Rect(0, 0, finalSize.Width, _inputSize.Height));
        _calendar.IsVisible = !Input || _calendarAlpha.Value > 0;
        _inputs.IsVisible = Input || _inputAlpha.Value > 0;
        return finalSize;
    }
}
