using System.Globalization;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;

namespace Avalonia.Material3.Controls;

public enum MaterialTimePickerMode { Clock, Input }
public enum MaterialTimePickerPart { Hour, Minute }
public enum MaterialTimePickerLayout { Vertical, Horizontal }
public sealed record MaterialTimePickerLabels
{
    public string Title { get; init; } = "Select time";
    public string Hour { get; init; } = "Hour";
    public string Minute { get; init; } = "Minute";
    public string ClockMode { get; init; } = "Show clock";
    public string InputMode { get; init; } = "Enter time";
    public string Confirm { get; init; } = "OK";
    public string Cancel { get; init; } = "Cancel";
    public string InvalidHour { get; init; } = "Enter a valid hour";
    public string InvalidMinute { get; init; } = "Enter a valid minute";
    public string UnavailableTime { get; init; } = "Time is not available";
    public string Incomplete { get; init; } = "Select a complete time";
    public string IncreaseTime { get; init; } = "Increase time";
    public string DecreaseTime { get; init; } = "Decrease time";
}

/// <summary>Native Material editing with the standard TimeInput recipe, not a reimplemented editor.</summary>
public class MaterialTimeInputField : MaterialTextField
{
    protected override Type StyleKeyOverride => typeof(MaterialTimeInputField);
}

/// <summary>Minute-precision civil time with analog and native keyboard surfaces. Host owns form persistence.</summary>
public class MaterialTimePicker : TemplatedControl
{
    public static readonly StyledProperty<TimeOnly?> SelectedTimeProperty = AvaloniaProperty.Register<MaterialTimePicker, TimeOnly?>(nameof(SelectedTime), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<MaterialTimePickerMode> ModeProperty = AvaloniaProperty.Register<MaterialTimePicker, MaterialTimePickerMode>(nameof(Mode), validate: Enum.IsDefined);
    public static readonly StyledProperty<MaterialTimePickerPart> ActivePartProperty = AvaloniaProperty.Register<MaterialTimePicker, MaterialTimePickerPart>(nameof(ActivePart), validate: Enum.IsDefined);
    public static readonly StyledProperty<MaterialTimePickerLayout> LayoutProperty = AvaloniaProperty.Register<MaterialTimePicker, MaterialTimePickerLayout>(nameof(Layout), validate: Enum.IsDefined);
    public static readonly StyledProperty<bool> Is24HourProperty = AvaloniaProperty.Register<MaterialTimePicker, bool>(nameof(Is24Hour));
    public static readonly StyledProperty<CultureInfo> CultureProperty = AvaloniaProperty.Register<MaterialTimePicker, CultureInfo>(nameof(Culture), CultureInfo.InvariantCulture, validate: c => c is not null);
    public static readonly StyledProperty<string?> DisplayFormatProperty = AvaloniaProperty.Register<MaterialTimePicker, string?>(nameof(DisplayFormat), validate: s => s is null || !string.IsNullOrWhiteSpace(s));
    public static readonly StyledProperty<TimeOnly?> MinimumTimeProperty = AvaloniaProperty.Register<MaterialTimePicker, TimeOnly?>(nameof(MinimumTime));
    public static readonly StyledProperty<TimeOnly?> MaximumTimeProperty = AvaloniaProperty.Register<MaterialTimePicker, TimeOnly?>(nameof(MaximumTime));
    public static readonly StyledProperty<MaterialTimePickerLabels> LabelsProperty = AvaloniaProperty.Register<MaterialTimePicker, MaterialTimePickerLabels>(nameof(Labels), new(), validate: l => l is not null);
    public static readonly DirectProperty<MaterialTimePicker, Control> SurfaceProperty = AvaloniaProperty.RegisterDirect<MaterialTimePicker, Control>(nameof(Surface), p => p.Surface);
    public static readonly DirectProperty<MaterialTimePicker, bool> IsValidProperty = AvaloniaProperty.RegisterDirect<MaterialTimePicker, bool>(nameof(IsValid), p => p.IsValid);
    public static readonly DirectProperty<MaterialTimePicker, string?> ValidationMessageProperty = AvaloniaProperty.RegisterDirect<MaterialTimePicker, string?>(nameof(ValidationMessage), p => p.ValidationMessage);
    private bool _valid, _ready, _updating, _pm;
    private TimeOnly _lastTime;
    private string? _message;
    private readonly TextBlock _title = MaterialPickerSupport.Text("LabelMedium", "OnSurfaceVariant");
    private readonly TextBlock _error = MaterialPickerSupport.Text("BodySmall", "Error");
    private readonly TextBlock _display = MaterialPickerSupport.Text("BodyLarge", "OnSurfaceVariant");
    private readonly MaterialButton _mode;
    private readonly MaterialTimePeriodButton _am, _pmButton;
    private readonly MaterialTimeSelector _hourSelector = new(), _minuteSelector = new();
    private readonly MaterialClockDial _dial = new();
    private readonly Grid _clock = new() { RowDefinitions = new RowDefinitions("Auto,Auto,Auto"), ColumnDefinitions = new ColumnDefinitions("Auto,Auto") };
    private readonly Grid _clockSelectors;
    private readonly WrapPanel _adjust;
    private readonly Grid _fields;
    private readonly MaterialTimePeriodPanel _period;
    private MaterialDialog? _dialog;
    public TimeOnly? SelectedTime { get => GetValue(SelectedTimeProperty); set => SetValue(SelectedTimeProperty, value); }
    public MaterialTimePickerMode Mode { get => GetValue(ModeProperty); set => SetValue(ModeProperty, value); }
    public MaterialTimePickerPart ActivePart { get => GetValue(ActivePartProperty); set => SetValue(ActivePartProperty, value); }
    public MaterialTimePickerLayout Layout { get => GetValue(LayoutProperty); set => SetValue(LayoutProperty, value); }
    public bool Is24Hour { get => GetValue(Is24HourProperty); set => SetValue(Is24HourProperty, value); }
    public CultureInfo Culture { get => GetValue(CultureProperty); set => SetValue(CultureProperty, value); }
    public string? DisplayFormat { get => GetValue(DisplayFormatProperty); set => SetValue(DisplayFormatProperty, value); }
    public TimeOnly? MinimumTime { get => GetValue(MinimumTimeProperty); set => SetValue(MinimumTimeProperty, value); }
    public TimeOnly? MaximumTime { get => GetValue(MaximumTimeProperty); set => SetValue(MaximumTimeProperty, value); }
    public MaterialTimePickerLabels Labels { get => GetValue(LabelsProperty); set => SetValue(LabelsProperty, value); }
    public Control Surface { get; }
    public MaterialTimeInputField HourInput { get; } = new();
    public MaterialTimeInputField MinuteInput { get; } = new();
    public bool IsValid => _valid;
    public string? ValidationMessage => _message;
    public MaterialOverlaySession? Session => _dialog?.Session;
    public event EventHandler? DraftChanged;
    protected override Type StyleKeyOverride => typeof(MaterialTimePicker);
    protected override AutomationPeer OnCreateAutomationPeer() => new TimePickerPeer(this);

    public MaterialTimePicker()
    {
        _mode = MaterialPickerSupport.Action(Labels.InputMode, () => SetCurrentValue(ModeProperty, Mode == MaterialTimePickerMode.Clock ? MaterialTimePickerMode.Input : MaterialTimePickerMode.Clock));
        _am = new MaterialTimePeriodButton { Content = "AM" }; _am.Click += (_, _) => SetPeriod(false);
        _pmButton = new MaterialTimePeriodButton { Content = "PM" }; _pmButton.Click += (_, _) => SetPeriod(true);
        _period = new MaterialTimePeriodPanel(_am, _pmButton) { MinWidth = 52, HorizontalAlignment = HorizontalAlignment.Center };
        var separator = Separator(72);
        _hourSelector.Click += (_, _) =>
        {
            SetCurrentValue(ActivePartProperty, MaterialTimePickerPart.Hour);
            _hourSelector.SetCurrentValue(MaterialButton.IsCheckedProperty, true);
        };
        _minuteSelector.Click += (_, _) =>
        {
            SetCurrentValue(ActivePartProperty, MaterialTimePickerPart.Minute);
            _minuteSelector.SetCurrentValue(MaterialButton.IsCheckedProperty, true);
        };
        _hourSelector.VerticalAlignment = _minuteSelector.VerticalAlignment = VerticalAlignment.Top;
        var clockSeparator = Separator(80);
        _hourSelector.PropertyChanged += (_, change) =>
        {
            if (change.Property == BoundsProperty) clockSeparator.MinHeight = Math.Max(80, _hourSelector.Bounds.Height);
        };
        _clockSelectors = new Grid { UseLayoutRounding = false, FlowDirection = Avalonia.Media.FlowDirection.LeftToRight,
            ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto,Auto"), Children = { _hourSelector, clockSeparator, _minuteSelector } };
        Grid.SetColumn(clockSeparator, 1); Grid.SetColumn(_minuteSelector, 2);
        _dial.HorizontalAlignment = HorizontalAlignment.Center;
        _clock.Children.Add(_clockSelectors); _clock.Children.Add(_dial);
        _adjust = new WrapPanel { Children = {
            MaterialPickerSupport.Action("−", () => Adjust(-1)), MaterialPickerSupport.Action("+", () => Adjust(1)) } };
        AutomationProperties.SetName(_adjust.Children[0], "Decrease time"); AutomationProperties.SetName(_adjust.Children[1], "Increase time");
        _clock.Children.Add(_adjust);
        _dial.ValueSelected += (_, args) =>
        {
            if (args.Part == MaterialTimePickerPart.Hour) SelectHour(Is24Hour ? args.Value : args.Value % 12 + (_pm ? 12 : 0), args.Complete);
            else SelectMinute(args.Value);
        };
        _fields = new Grid { FlowDirection = Avalonia.Media.FlowDirection.LeftToRight,
            ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto,Auto"), Children = { HourInput, separator, MinuteInput, _period } };
        Grid.SetColumn(separator, 1); Grid.SetColumn(MinuteInput, 2);
        Grid.SetColumn(_period, 3);
        var inputs = new StackPanel { Spacing = 8, Children = { _fields, _clock } };
        Surface = new StackPanel { Margin = new Thickness(24), Spacing = 12,
            Children = { _title, inputs, _display, _mode, _error } };
        AutomationProperties.SetLiveSetting(_error, AutomationLiveSetting.Polite);
        HourInput.TextChanged += (_, _) => ReadInput(); MinuteInput.TextChanged += (_, _) => ReadInput();
        HourInput.GotFocus += (_, _) => SetCurrentValue(ActivePartProperty, MaterialTimePickerPart.Hour);
        MinuteInput.GotFocus += (_, _) => SetCurrentValue(ActivePartProperty, MaterialTimePickerPart.Minute);
        HourInput.KeyDown += (sender, e) => { if (e.Key == Key.Enter && HourValid(out _)) { MinuteInput.Focus(); e.Handled = true; } };
        _ready = true; SynchronizeText(); Refresh();
    }
    private static Border Separator(double height)
    {
        var text = MaterialPickerSupport.Text("DisplayLarge");
        text.Text = ":"; text.TextAlignment = Avalonia.Media.TextAlignment.Center;
        // This is the pinned DisplaySeparator optical offset, not a font-specific margin patch.
        text.RenderTransform = new Avalonia.Media.TranslateTransform(0, -4);
        return new Border { Width = 24, MinHeight = height, VerticalAlignment = VerticalAlignment.Top, Child = text };
    }
    private void SynchronizeText()
    {
        _updating = true;
        try
        {
            if (SelectedTime is { } time) { _lastTime = time; _pm = time.Hour >= 12; }
            HourInput.SetCurrentValue(TextBox.TextProperty, SelectedTime is null ? "" : (Is24Hour ? _lastTime.Hour : (_lastTime.Hour + 11) % 12 + 1).ToString("00", Culture));
            MinuteInput.SetCurrentValue(TextBox.TextProperty, SelectedTime is null ? "" : _lastTime.Minute.ToString("00", Culture));
        }
        finally { _updating = false; }
    }
    private bool HourValid(out int hour) => int.TryParse(MaterialPickerSupport.NormalizeDigits(HourInput.Text), NumberStyles.None, CultureInfo.InvariantCulture, out hour)
        && hour >= (Is24Hour ? 0 : 1) && hour <= (Is24Hour ? 23 : 12);
    private bool MinuteValid(out int minute) => int.TryParse(MaterialPickerSupport.NormalizeDigits(MinuteInput.Text), NumberStyles.None, CultureInfo.InvariantCulture, out minute) && minute is >= 0 and <= 59;
    private void ReadInput()
    {
        if (_updating || !_ready) return;
        _updating = true;
        try
        {
            var value = HourValid(out var hour) && MinuteValid(out var minute)
                ? new TimeOnly(Is24Hour ? hour : hour % 12 + (_pm ? 12 : 0), minute) : (TimeOnly?)null;
            SetCurrentValue(SelectedTimeProperty, value);
            if (value is { } time) _lastTime = time;
        }
        finally { _updating = false; }
        Refresh();
    }
    /// <summary>Inclusive bounds; min &gt; max is an explicitly supported overnight interval.</summary>
    public bool IsTimeAvailable(TimeOnly time)
    {
        if (time.Ticks % TimeSpan.TicksPerMinute != 0) return false;
        if (MinimumTime is { } min && MaximumTime is { } max && min > max) return time >= min || time <= max;
        return (MinimumTime is null || time >= MinimumTime) && (MaximumTime is null || time <= MaximumTime);
    }
    public bool SetPeriod(bool pm)
    {
        if (Is24Hour || !IsEffectivelyEnabled) return false;
        _pm = pm; ReadInput(); return true;
    }
    public bool SelectHour(int hour, bool advanceToMinutes = true)
    {
        if (!IsEffectivelyEnabled || hour is < 0 or > 23) return false;
        SetCurrentValue(SelectedTimeProperty, new TimeOnly(hour, _lastTime.Minute));
        if (advanceToMinutes) SetCurrentValue(ActivePartProperty, MaterialTimePickerPart.Minute);
        return true;
    }
    public bool SelectMinute(int minute)
    {
        if (!IsEffectivelyEnabled || minute is < 0 or > 59) return false;
        SetCurrentValue(SelectedTimeProperty, new TimeOnly(_lastTime.Hour, minute)); return true;
    }
    private void Adjust(int step)
    {
        if (ActivePart == MaterialTimePickerPart.Hour) SelectHour((_lastTime.Hour + step + 24) % 24, false);
        else SelectMinute((_lastTime.Minute + step + 60) % 60);
    }
    private void Refresh()
    {
        if (!_ready) return;
        _title.Text = Labels.Title;
        HourInput.Label = Labels.Hour; MinuteInput.Label = Labels.Minute;
        _fields.IsVisible = Mode == MaterialTimePickerMode.Input;
        _clock.IsVisible = Mode == MaterialTimePickerMode.Clock;
        AutomationProperties.SetName(_adjust.Children[0], Labels.DecreaseTime); AutomationProperties.SetName(_adjust.Children[1], Labels.IncreaseTime);
        var horizontal = Layout == MaterialTimePickerLayout.Horizontal;
        _clock.RowDefinitions[2].Height = horizontal ? new GridLength(1, GridUnitType.Star) : GridLength.Auto;
        Grid.SetRow(_dial, horizontal ? 0 : 1); Grid.SetColumn(_dial, horizontal ? 1 : 0); Grid.SetRowSpan(_dial, horizontal ? 3 : 1);
        Grid.SetRow(_adjust, 2);
        _dial.Margin = horizontal ? new Thickness(36, 0, 0, 0) : new Thickness(0, 36, 0, 24);
        var horizontalPeriod = Mode == MaterialTimePickerMode.Clock && horizontal;
        var periodParent = Mode == MaterialTimePickerMode.Input ? _fields : horizontal ? _clock : _clockSelectors;
        if (_period.Parent != periodParent)
        {
            if (_period.Parent is Panel previous) previous.Children.Remove(_period);
            periodParent.Children.Add(_period);
        }
        Grid.SetColumn(_period, horizontalPeriod ? 0 : 3); Grid.SetRow(_period, horizontalPeriod ? 1 : 0);
        _period.Columns = horizontalPeriod ? 2 : 1;
        _period.MinWidth = horizontalPeriod ? 216 : 52;
        _period.Margin = horizontalPeriod ? new Thickness(0, 16, 0, 0) : new Thickness(4, 0, 0, 0);
        _period.VerticalAlignment = VerticalAlignment.Top;
        _hourSelector.Content = (Is24Hour ? _lastTime.Hour : (_lastTime.Hour + 11) % 12 + 1).ToString("00", Culture);
        _minuteSelector.Content = _lastTime.Minute.ToString("00", Culture);
        AutomationProperties.SetName(_hourSelector, Labels.Hour + ": " + _hourSelector.Content);
        AutomationProperties.SetName(_minuteSelector, Labels.Minute + ": " + _minuteSelector.Content);
        _hourSelector.IsChecked = ActivePart == MaterialTimePickerPart.Hour;
        _minuteSelector.IsChecked = ActivePart == MaterialTimePickerPart.Minute;
        _hourSelector.MinWidth = Is24Hour ? 114 : 96;
        _dial.Is24Hour = Is24Hour; _dial.Culture = Culture;
        _dial.ActivePart = ActivePart;
        _dial.Value = ActivePart == MaterialTimePickerPart.Hour ? _lastTime.Hour : _lastTime.Minute;
        _dial.ValueLabel = ActivePart == MaterialTimePickerPart.Hour ? Labels.Hour : Labels.Minute;
        _period.IsVisible = !Is24Hour;
        _am.Content = string.IsNullOrEmpty(Culture.DateTimeFormat.AMDesignator) ? "AM" : Culture.DateTimeFormat.AMDesignator;
        _pmButton.Content = string.IsNullOrEmpty(Culture.DateTimeFormat.PMDesignator) ? "PM" : Culture.DateTimeFormat.PMDesignator;
        AutomationProperties.SetName(_am, (string)_am.Content); AutomationProperties.SetName(_pmButton, (string)_pmButton.Content);
        _am.IsChecked = !_pm; _pmButton.IsChecked = _pm;
        var hourError = !HourValid(out _) && !string.IsNullOrEmpty(HourInput.Text) ? Labels.InvalidHour : null;
        var minuteError = !MinuteValid(out _) && !string.IsNullOrEmpty(MinuteInput.Text) ? Labels.InvalidMinute : null;
        HourInput.ErrorText = hourError; MinuteInput.ErrorText = minuteError;
        var error = hourError ?? minuteError ?? (SelectedTime is null ? Labels.Incomplete : !IsTimeAvailable(SelectedTime.Value) ? Labels.UnavailableTime : null);
        SetAndRaise(ValidationMessageProperty, ref _message, error);
        SetAndRaise(IsValidProperty, ref _valid, error is null);
        _error.Text = error; _error.IsVisible = error is not null;
        _display.Text = SelectedTime?.ToString(DisplayFormat ?? (Is24Hour ? "HH:mm" : "hh:mm tt"), Culture) ?? "";
        AutomationProperties.SetItemStatus(this, error ?? _display.Text ?? "");
        _mode.Content = Mode == MaterialTimePickerMode.Clock ? Labels.InputMode : Labels.ClockMode;
        AutomationProperties.SetName(_mode, (string)_mode.Content);
        if (_dialog is not null)
        {
            _dialog.IsConfirmEnabled = IsValid && IsEffectivelyEnabled;
            _dialog.ConfirmText = Labels.Confirm; _dialog.CancelText = Labels.Cancel;
            _dialog.MaxWidth = Layout == MaterialTimePickerLayout.Horizontal ? 584 : 400;
            AutomationProperties.SetName(_dialog, Labels.Title);
        }
        DraftChanged?.Invoke(this, EventArgs.Empty);
    }
    public MaterialOverlaySession Show(MaterialOverlayHost host, MaterialOverlayOptions? options = null)
    {
        if (_dialog?.IsOpen == true) throw new InvalidOperationException("Picker is already open.");
        var dialog = new MaterialDialog { Content = this, Padding = new Thickness(0), MaxWidth = Layout == MaterialTimePickerLayout.Horizontal ? 584 : 400,
            ConfirmText = Labels.Confirm, CancelText = Labels.Cancel, IsConfirmEnabled = IsValid && IsEffectivelyEnabled };
        _dialog = dialog;
        AutomationProperties.SetName(dialog, Labels.Title);
        dialog.Confirming += (_, args) => { args.Cancel = !IsValid || !IsEffectivelyEnabled; if (!args.Cancel) args.Value = SelectedTime; };
        MaterialOverlaySession session;
        try { session = dialog.Show(host, options ?? new MaterialOverlayOptions { InitialFocus = Mode == MaterialTimePickerMode.Input ? HourInput : _hourSelector }); }
        catch { dialog.Content = null; _dialog = null; throw; }
        session.Closed += (_, _) => { dialog.Content = null; _dialog = null; };
        return session;
    }
    public Task<MaterialOverlayResult> ShowAsync(MaterialOverlayHost host, MaterialOverlayOptions? options = null) => Show(host, options).Completion;
    public bool Confirm() => IsEffectivelyEnabled && (_dialog?.Confirm() ?? false);
    public bool Cancel() => _dialog?.Cancel() ?? false;
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (!_ready || _updating) return;
        if (change.Property != SelectedTimeProperty && change.Property != ModeProperty && change.Property != ActivePartProperty &&
            change.Property != Is24HourProperty && change.Property != LayoutProperty && change.Property != CultureProperty && change.Property != DisplayFormatProperty &&
            change.Property != MinimumTimeProperty && change.Property != MaximumTimeProperty && change.Property != LabelsProperty && change.Property != IsEnabledProperty) return;
        if (change.Property == SelectedTimeProperty || (change.Property == Is24HourProperty || change.Property == CultureProperty) && IsValid) SynchronizeText();
        Refresh();
    }
    private sealed class TimePickerPeer(MaterialTimePicker owner) : ControlAutomationPeer(owner)
    {
        protected override string? GetNameCore() => base.GetNameCore() ?? owner.Labels.Title;
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
    }
}
