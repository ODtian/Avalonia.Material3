using System.Globalization;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace Avalonia.Material3.Controls;

public enum MaterialDatePickerMode { Calendar, Input }
public enum MaterialDateSelectionMode { Single, Range }
public readonly record struct MaterialDateRange(DateOnly Start, DateOnly End);

/// <summary>Localizable application strings. Culture controls Gregorian date names, not these labels.</summary>
public sealed record MaterialDatePickerLabels
{
    public string Title { get; init; } = "Select date";
    public string StartDate { get; init; } = "Start date";
    public string EndDate { get; init; } = "End date";
    public string CalendarMode { get; init; } = "Show calendar";
    public string InputMode { get; init; } = "Enter date";
    public string PreviousMonth { get; init; } = "Previous month";
    public string NextMonth { get; init; } = "Next month";
    public string ChooseYear { get; init; } = "Choose year";
    public string Confirm { get; init; } = "OK";
    public string Cancel { get; init; } = "Cancel";
    public string InvalidDate { get; init; } = "Enter a valid date";
    public string UnavailableDate { get; init; } = "Date is not available";
    public string Incomplete { get; init; } = "Select a complete date";
    public string InvalidRange { get; init; } = "End date must be on or after start date";
    public string RangeStart { get; init; } = "Range start";
    public string RangeEnd { get; init; } = "Range end";
    public string InRange { get; init; } = "In selected range";
    public string Today { get; init; } = "Today";
    public string Selected { get; init; } = "Selected date";
}

/// <summary>A genuine Gregorian Material calendar / native date editor. Values are drafts;
/// only successful overlay confirmation returns a form result. It can also live docked in normal layout.</summary>
public class MaterialDatePicker : TemplatedControl
{
    public static readonly StyledProperty<DateOnly?> SelectedDateProperty = AvaloniaProperty.Register<MaterialDatePicker, DateOnly?>(nameof(SelectedDate), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<DateOnly?> RangeEndProperty = AvaloniaProperty.Register<MaterialDatePicker, DateOnly?>(nameof(RangeEnd), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<MaterialDatePickerMode> ModeProperty = AvaloniaProperty.Register<MaterialDatePicker, MaterialDatePickerMode>(nameof(Mode), validate: Enum.IsDefined);
    public static readonly StyledProperty<MaterialDateSelectionMode> SelectionModeProperty = AvaloniaProperty.Register<MaterialDatePicker, MaterialDateSelectionMode>(nameof(SelectionMode), validate: Enum.IsDefined);
    public static readonly StyledProperty<CultureInfo> CultureProperty = AvaloniaProperty.Register<MaterialDatePicker, CultureInfo>(nameof(Culture), CultureInfo.InvariantCulture, validate: c => c is not null);
    public static readonly StyledProperty<string?> InputFormatProperty = AvaloniaProperty.Register<MaterialDatePicker, string?>(nameof(InputFormat), validate: s => s is null || !string.IsNullOrWhiteSpace(s));
    public static readonly StyledProperty<string> DisplayFormatProperty = AvaloniaProperty.Register<MaterialDatePicker, string>(nameof(DisplayFormat), "ddd, MMM d", validate: s => !string.IsNullOrWhiteSpace(s));
    public static readonly StyledProperty<MaterialDatePickerLabels> LabelsProperty = AvaloniaProperty.Register<MaterialDatePicker, MaterialDatePickerLabels>(nameof(Labels), new(), validate: l => l is not null);
    public static readonly StyledProperty<DateOnly> DisplayMonthProperty = AvaloniaProperty.Register<MaterialDatePicker, DateOnly>(nameof(DisplayMonth), DateOnly.FromDateTime(DateTime.Today));
    public static readonly StyledProperty<DateOnly> MinimumDateProperty = AvaloniaProperty.Register<MaterialDatePicker, DateOnly>(nameof(MinimumDate), new(1900, 1, 1));
    public static readonly StyledProperty<DateOnly> MaximumDateProperty = AvaloniaProperty.Register<MaterialDatePicker, DateOnly>(nameof(MaximumDate), new(2100, 12, 31));
    public static readonly StyledProperty<DateOnly> TodayProperty = AvaloniaProperty.Register<MaterialDatePicker, DateOnly>(nameof(Today), DateOnly.FromDateTime(DateTime.Today));
    public static readonly StyledProperty<Func<DateOnly, bool>?> SelectableDateProperty = AvaloniaProperty.Register<MaterialDatePicker, Func<DateOnly, bool>?>(nameof(SelectableDate));
    public static readonly DirectProperty<MaterialDatePicker, Control> SurfaceProperty = AvaloniaProperty.RegisterDirect<MaterialDatePicker, Control>(nameof(Surface), p => p.Surface);
    public static readonly DirectProperty<MaterialDatePicker, bool> IsValidProperty = AvaloniaProperty.RegisterDirect<MaterialDatePicker, bool>(nameof(IsValid), p => p.IsValid);
    public static readonly DirectProperty<MaterialDatePicker, string?> ValidationMessageProperty = AvaloniaProperty.RegisterDirect<MaterialDatePicker, string?>(nameof(ValidationMessage), p => p.ValidationMessage);
    private bool _isValid, _updating, _ready;
    private string? _validationMessage;
    private readonly TextBlock _title = MaterialPickerSupport.Text("LabelLarge", "OnSurfaceVariant");
    private readonly TextBlock _headline = MaterialPickerSupport.Text("HeadlineLarge", "OnSurfaceVariant");
    private readonly TextBlock _error = MaterialPickerSupport.Text("BodySmall", "Error");
    private readonly Grid _header;
    private readonly MaterialIconButton _mode;
    private readonly Avalonia.Controls.Shapes.Path _keyboardIcon = new() { Width = 24, Height = 24, Stretch = Media.Stretch.Uniform,
        Data = Media.Geometry.Parse("M2,4 L22,4 L22,20 L2,20 Z M4,6 L4,18 L20,18 L20,6 Z M6,8 L8,8 L8,10 L6,10 Z M10,8 L12,8 L12,10 L10,10 Z M14,8 L16,8 L16,10 L14,10 Z M6,12 L8,12 L8,14 L6,14 Z M10,12 L12,12 L12,14 L10,14 Z M14,12 L18,12 L18,14 L14,14 Z") };
    private readonly Avalonia.Controls.Shapes.Path _calendarIcon = new() { Width = 24, Height = 24, Stretch = Media.Stretch.Uniform,
        Data = Media.Geometry.Parse("M3,4 L6,4 L6,2 L8,2 L8,4 L16,4 L16,2 L18,2 L18,4 L21,4 L21,22 L3,22 Z M5,10 L5,20 L19,20 L19,10 Z M7,12 L11,12 L11,16 L7,16 Z") };
    private readonly StackPanel _inputs;
    private readonly StackPanel _calendar = new() { Spacing = 4, Margin = new Thickness(12, 0, 12, 12) };
    private readonly Grid _days = new() { ColumnDefinitions = new ColumnDefinitions("*,*,*,*,*,*,*"), MinWidth = 336 };
    private readonly Grid _week = new() { ColumnDefinitions = new ColumnDefinitions("*,*,*,*,*,*,*"), MinWidth = 336 };
    private readonly UniformGrid _years = new() { Columns = 3 };
    private readonly ScrollViewer _calendarScroll;
    private readonly MaterialButton _previous, _next, _month;
    private bool _choosingYear;
    private DateOnly? _builtMonth;
    private CultureInfo? _builtCulture;
    private MaterialDialog? _dialog;
    public DateOnly? SelectedDate { get => GetValue(SelectedDateProperty); set => SetValue(SelectedDateProperty, value); }
    public DateOnly? RangeEnd { get => GetValue(RangeEndProperty); set => SetValue(RangeEndProperty, value); }
    public MaterialDatePickerMode Mode { get => GetValue(ModeProperty); set => SetValue(ModeProperty, value); }
    public MaterialDateSelectionMode SelectionMode { get => GetValue(SelectionModeProperty); set => SetValue(SelectionModeProperty, value); }
    public CultureInfo Culture { get => GetValue(CultureProperty); set => SetValue(CultureProperty, value); }
    public string? InputFormat { get => GetValue(InputFormatProperty); set => SetValue(InputFormatProperty, value); }
    public string DisplayFormat { get => GetValue(DisplayFormatProperty); set => SetValue(DisplayFormatProperty, value); }
    public MaterialDatePickerLabels Labels { get => GetValue(LabelsProperty); set => SetValue(LabelsProperty, value); }
    public DateOnly DisplayMonth { get => GetValue(DisplayMonthProperty); set => SetValue(DisplayMonthProperty, value); }
    public DateOnly MinimumDate { get => GetValue(MinimumDateProperty); set => SetValue(MinimumDateProperty, value); }
    public DateOnly MaximumDate { get => GetValue(MaximumDateProperty); set => SetValue(MaximumDateProperty, value); }
    public DateOnly Today { get => GetValue(TodayProperty); set => SetValue(TodayProperty, value); }
    /// <summary>Endpoint selection policy. Interior range dates are not validated as application business rules.</summary>
    public Func<DateOnly, bool>? SelectableDate { get => GetValue(SelectableDateProperty); set => SetValue(SelectableDateProperty, value); }
    public Control Surface { get; }
    // Establish local Text priority before native editing. Default-value coercion during error-style
    // reevaluation otherwise clears TextBox's Undo stack even though the visible string is unchanged.
    public MaterialTextField StartInput { get; } = new() { Variant = MaterialTextFieldVariant.Outlined, Text = "" };
    public MaterialTextField EndInput { get; } = new() { Variant = MaterialTextFieldVariant.Outlined, Text = "" };
    public bool IsValid => _isValid;
    public string? ValidationMessage => _validationMessage;
    public MaterialOverlaySession? Session => _dialog?.Session;
    public event EventHandler? DraftChanged;
    private CultureInfo DateCulture => MaterialPickerSupport.Gregorian(Culture);
    private string Pattern => InputFormat ?? DateCulture.DateTimeFormat.ShortDatePattern;
    protected override Type StyleKeyOverride => typeof(MaterialDatePicker);
    protected override AutomationPeer OnCreateAutomationPeer() => new DatePickerPeer(this);

    public MaterialDatePicker()
    {
        _mode = new MaterialIconButton();
        MaterialPickerSupport.Resource(_keyboardIcon, Avalonia.Controls.Shapes.Shape.FillProperty, "OnSurfaceVariantBrush");
        MaterialPickerSupport.Resource(_calendarIcon, Avalonia.Controls.Shapes.Shape.FillProperty, "OnSurfaceVariantBrush");
        _mode.Click += (_, _) => SetCurrentValue(ModeProperty,
            Mode == MaterialDatePickerMode.Calendar ? MaterialDatePickerMode.Input : MaterialDatePickerMode.Calendar);
        _inputs = new StackPanel { Spacing = 16, Margin = new Thickness(24, 10, 24, 24), Children = { StartInput, EndInput } };
        _previous = MaterialPickerSupport.Action(Labels.PreviousMonth, () => NavigateMonth(-1));
        _next = MaterialPickerSupport.Action(Labels.NextMonth, () => NavigateMonth(1));
        _month = MaterialPickerSupport.Action(Labels.ChooseYear, () => { _choosingYear = !_choosingYear; RefreshCalendar(); });
        _previous.Content = "‹"; _next.Content = "›";
        var navigation = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), MinHeight = 56 };
        Grid.SetColumn(_month, 1); Grid.SetColumn(_next, 2);
        navigation.Children.Add(_previous); navigation.Children.Add(_month); navigation.Children.Add(_next);
        _calendarScroll = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = new StackPanel { Children = { _week, _days } } };
        _calendar.Children.Add(navigation); _calendar.Children.Add(_calendarScroll);
        _calendar.Children.Add(new ScrollViewer { Content = _years, MaxHeight = 288, IsVisible = false });
        _header = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto"), ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Margin = new Thickness(24, 16, 12, 12), MinHeight = 92 };
        Grid.SetColumnSpan(_title, 2); Grid.SetRow(_headline, 1); Grid.SetRow(_mode, 1); Grid.SetColumn(_mode, 1);
        _headline.Margin = new Thickness(0, 8, 8, 0);
        _header.Children.Add(_title); _header.Children.Add(_headline); _header.Children.Add(_mode);
        Surface = new StackPanel { Spacing = 8, Children =
        {
            _header, _calendar, _inputs, _error
        }};
        _error.Margin = new Thickness(24, 0, 24, 12);
        AutomationProperties.SetLiveSetting(_error, AutomationLiveSetting.Polite);
        StartInput.TextChanged += (_, _) => ReadInput();
        EndInput.TextChanged += (_, _) => ReadInput();
        AddHandler(KeyDownEvent, CalendarKey);
        _ready = true;
        SynchronizeText(); Refresh();
    }

    private void SynchronizeText()
    {
        _updating = true;
        try
        {
            var start = SelectedDate?.ToString(Pattern, DateCulture) ?? "";
            var end = RangeEnd?.ToString(Pattern, DateCulture) ?? "";
            // Native TextBox programmatic assignments reset Undo even for an equal string.
            if (StartInput.Text != start) StartInput.SetCurrentValue(TextBox.TextProperty, start);
            if (EndInput.Text != end) EndInput.SetCurrentValue(TextBox.TextProperty, end);
        }
        finally { _updating = false; }
    }
    private bool Parse(string? text, out DateOnly value) => DateOnly.TryParseExact(
        MaterialPickerSupport.NormalizeDigits(text), Pattern, DateCulture, DateTimeStyles.None, out value);
    private void ReadInput()
    {
        if (_updating || !_ready) return;
        _updating = true;
        try
        {
            SetCurrentValue(SelectedDateProperty, Parse(StartInput.Text, out var start) ? start : null);
            if (SelectionMode == MaterialDateSelectionMode.Range)
                SetCurrentValue(RangeEndProperty, Parse(EndInput.Text, out var end) ? end : null);
        }
        finally { _updating = false; }
        Refresh();
    }
    private void Refresh()
    {
        if (!_ready) return;
        _title.Text = Labels.Title;
        _header.MinHeight = SelectionMode == MaterialDateSelectionMode.Range ? 100 : 92;
        _headline.Text = SelectedDate?.ToString(DisplayFormat, DateCulture) ?? Labels.Title;
        if (SelectionMode == MaterialDateSelectionMode.Range)
            _headline.Text += " – " + (RangeEnd?.ToString(DisplayFormat, DateCulture) ?? Labels.EndDate);
        MaterialPickerSupport.Resource(_headline, TextBlock.FontSizeProperty, (SelectionMode == MaterialDateSelectionMode.Range ? "TitleLarge" : "HeadlineLarge") + "FontSize");
        MaterialPickerSupport.Resource(_headline, TextBlock.FontFamilyProperty, (SelectionMode == MaterialDateSelectionMode.Range ? "TitleLarge" : "HeadlineLarge") + "FontFamily");
        MaterialPickerSupport.Resource(_headline, TextBlock.FontWeightProperty, (SelectionMode == MaterialDateSelectionMode.Range ? "TitleLarge" : "HeadlineLarge") + "FontWeight");
        MaterialPickerSupport.Resource(_headline, TextBlock.LineHeightProperty, (SelectionMode == MaterialDateSelectionMode.Range ? "TitleLarge" : "HeadlineLarge") + "LineHeight");
        MaterialPickerSupport.Resource(_headline, TextBlock.LetterSpacingProperty, (SelectionMode == MaterialDateSelectionMode.Range ? "TitleLarge" : "HeadlineLarge") + "LetterSpacing");
        _inputs.IsVisible = Mode == MaterialDatePickerMode.Input;
        EndInput.IsVisible = SelectionMode == MaterialDateSelectionMode.Range;
        StartInput.Label = Labels.StartDate; EndInput.Label = Labels.EndDate;
        StartInput.SupportingText = EndInput.SupportingText = Pattern;
        _mode.Content = Mode == MaterialDatePickerMode.Calendar ? _keyboardIcon : _calendarIcon;
        AutomationProperties.SetName(_mode, Mode == MaterialDatePickerMode.Calendar ? Labels.InputMode : Labels.CalendarMode);
        var startError = !Parse(StartInput.Text, out _) && !string.IsNullOrEmpty(StartInput.Text) ? Labels.InvalidDate : null;
        var endError = SelectionMode == MaterialDateSelectionMode.Range && !Parse(EndInput.Text, out _) && !string.IsNullOrEmpty(EndInput.Text) ? Labels.InvalidDate : null;
        if (SelectedDate is { } start && !IsDateAvailable(start)) startError = Labels.UnavailableDate;
        if (SelectionMode == MaterialDateSelectionMode.Range && RangeEnd is { } end && !IsDateAvailable(end)) endError = Labels.UnavailableDate;
        if (SelectedDate is { } s && RangeEnd is { } e && SelectionMode == MaterialDateSelectionMode.Range && e < s)
            endError = Labels.InvalidRange;
        StartInput.ErrorText = startError; EndInput.ErrorText = endError;
        var message = startError ?? endError ?? (SelectedDate is null || SelectionMode == MaterialDateSelectionMode.Range && RangeEnd is null ? Labels.Incomplete : null);
        SetAndRaise(ValidationMessageProperty, ref _validationMessage, message);
        SetAndRaise(IsValidProperty, ref _isValid, message is null);
        _error.Text = message; _error.IsVisible = message is not null;
        AutomationProperties.SetItemStatus(this, message ?? _headline.Text ?? "");
        if (_dialog is not null)
        {
            _dialog.IsConfirmEnabled = IsValid && IsEffectivelyEnabled;
            _dialog.ConfirmText = Labels.Confirm; _dialog.CancelText = Labels.Cancel;
            AutomationProperties.SetName(_dialog, Labels.Title);
        }
        RefreshCalendar();
        DraftChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool IsDateAvailable(DateOnly date) => MinimumDate <= MaximumDate && date >= MinimumDate && date <= MaximumDate && (SelectableDate?.Invoke(date) ?? true);

    /// <summary>Same selection path as calendar activation. Earlier endpoint starts a new range.</summary>
    public bool SelectDate(DateOnly date)
    {
        if (!IsEffectivelyEnabled || !IsDateAvailable(date)) return false;
        _updating = true;
        try
        {
            if (SelectionMode == MaterialDateSelectionMode.Single) SetCurrentValue(SelectedDateProperty, date);
            else if (SelectedDate is null || RangeEnd is not null || date < SelectedDate)
            { SetCurrentValue(SelectedDateProperty, date); SetCurrentValue(RangeEndProperty, null); }
            else SetCurrentValue(RangeEndProperty, date);
        }
        finally { _updating = false; }
        SynchronizeText(); Refresh(); return true;
    }
    public bool NavigateMonth(int offset)
    {
        if (!IsEffectivelyEnabled || MinimumDate > MaximumDate) return false;
        var index = (DisplayMonth.Year - 1) * 12 + DisplayMonth.Month - 1L + offset;
        if (index < 0 || index >= 9999 * 12) return false;
        var candidate = new DateOnly((int)(index / 12 + 1), (int)(index % 12 + 1), 1);
        if (candidate > MaximumDate || candidate.AddDays(DateTime.DaysInMonth(candidate.Year, candidate.Month) - 1) < MinimumDate) return false;
        SetCurrentValue(DisplayMonthProperty, candidate); return true;
    }
    private void RefreshCalendar()
    {
        _calendar.IsVisible = Mode == MaterialDatePickerMode.Calendar;
        _calendarScroll.IsVisible = !_choosingYear;
        _calendar.Children[2].IsVisible = _choosingYear;
        _month.Content = DisplayMonth.ToString("MMMM yyyy", DateCulture);
        AutomationProperties.SetName(_month, Labels.ChooseYear + ": " + _month.Content);
        AutomationProperties.SetName(_previous, Labels.PreviousMonth); AutomationProperties.SetName(_next, Labels.NextMonth);
        _previous.IsEnabled = MinimumDate <= MaximumDate && (DisplayMonth.Year > MinimumDate.Year || DisplayMonth.Year == MinimumDate.Year && DisplayMonth.Month > MinimumDate.Month);
        _next.IsEnabled = MinimumDate <= MaximumDate && (DisplayMonth.Year < MaximumDate.Year || DisplayMonth.Year == MaximumDate.Year && DisplayMonth.Month < MaximumDate.Month);
        var month = new DateOnly(DisplayMonth.Year, DisplayMonth.Month, 1);
        if (_builtMonth != month || !Equals(_builtCulture, Culture))
        {
            _builtMonth = month; _builtCulture = Culture;
            _week.Children.Clear(); _days.Children.Clear(); _days.RowDefinitions.Clear();
            for (var row = 0; row < 6; row++) _days.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var firstDay = DateCulture.DateTimeFormat.FirstDayOfWeek;
            for (var i = 0; i < 7; i++)
            {
                var day = (DayOfWeek)(((int)firstDay + i) % 7);
                var label = MaterialPickerSupport.Text("BodyLarge"); label.MinHeight = 48; label.TextAlignment = Media.TextAlignment.Center;
                label.Text = DateCulture.DateTimeFormat.GetShortestDayName(day);
                AutomationProperties.SetName(label, DateCulture.DateTimeFormat.GetDayName(day));
                Grid.SetColumn(label, i); _week.Children.Add(label);
            }
            var offset = ((int)month.DayOfWeek - (int)firstDay + 7) % 7;
            for (var day = 1; day <= DateTime.DaysInMonth(month.Year, month.Month); day++)
            {
                var date = new DateOnly(month.Year, month.Month, day);
                var button = new MaterialCalendarDay { Date = date, Content = day.ToString(Culture), MinHeight = 48, MinWidth = 48 };
                button.Click += (_, _) => SelectDate(date);
                var band = new Border { Height = 40, VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false };
                Grid.SetColumnSpan(button, 2);
                var cell = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), Children = { band, button } };
                Grid.SetRow(cell, (day - 1 + offset) / 7); Grid.SetColumn(cell, (day - 1 + offset) % 7);
                _days.Children.Add(cell);
            }
        }
        foreach (var cell in _days.Children.OfType<Grid>())
        {
            var band = (Border)cell.Children[0];
            var button = (MaterialCalendarDay)cell.Children[1];
            var date = button.Date;
            button.IsEnabled = IsDateAvailable(date);
            button.IsChecked = date == SelectedDate || SelectionMode == MaterialDateSelectionMode.Range && date == RangeEnd;
            button.IsToday = date == Today;
            button.IsInRange = SelectionMode == MaterialDateSelectionMode.Range && SelectedDate is { } start && RangeEnd is { } end && date >= start && date <= end;
            band.IsVisible = button.IsInRange && SelectedDate != RangeEnd;
            MaterialPickerSupport.Resource(band, Border.BackgroundProperty, "SecondaryContainerBrush");
            var isStart = date == SelectedDate;
            var isEnd = date == RangeEnd;
            Grid.SetColumn(band, isStart ? 1 : 0);
            Grid.SetColumnSpan(band, isStart || isEnd ? 1 : 2);
            band.CornerRadius = new(isStart ? 20 : 0, isEnd ? 20 : 0, isEnd ? 20 : 0, isStart ? 20 : 0);
            var status = date == SelectedDate ? SelectionMode == MaterialDateSelectionMode.Single ? Labels.Selected : Labels.RangeStart
                : date == RangeEnd && SelectionMode == MaterialDateSelectionMode.Range ? Labels.RangeEnd : button.IsInRange ? Labels.InRange : "";
            if (button.IsToday) status += " " + Labels.Today;
            AutomationProperties.SetName(button, date.ToString("D", DateCulture));
            AutomationProperties.SetItemStatus(button, status);
        }
        if (_choosingYear && _years.Children.Count == 0)
        {
            for (var year = MinimumDate.Year; year <= MaximumDate.Year; year++)
            {
                var selectedYear = year;
                var action = new MaterialCalendarYear { Year = year, Content = year.ToString(Culture) };
                action.Click += (_, _) =>
                {
                    _choosingYear = false;
                    SetCurrentValue(DisplayMonthProperty, new DateOnly(selectedYear, DisplayMonth.Month, 1));
                    RefreshCalendar();
                    _month.Focus();
                };
                _years.Children.Add(action);
            }
        }
        foreach (var year in _years.Children.OfType<MaterialCalendarYear>())
        { year.IsChecked = year.Year == DisplayMonth.Year; year.IsEnabled = year.Year >= MinimumDate.Year && year.Year <= MaximumDate.Year; }
    }
    private void CalendarKey(object? sender, KeyEventArgs e)
    {
        if (e.Source is not MaterialCalendarDay day || Mode != MaterialDatePickerMode.Calendar) return;
        var step = e.Key switch { Key.Left => FlowDirection == Media.FlowDirection.RightToLeft ? 1 : -1,
            Key.Right => FlowDirection == Media.FlowDirection.RightToLeft ? -1 : 1, Key.Up => -7, Key.Down => 7,
            Key.Home => -(((int)day.Date.DayOfWeek - (int)DateCulture.DateTimeFormat.FirstDayOfWeek + 7) % 7),
            Key.End => 6 - (((int)day.Date.DayOfWeek - (int)DateCulture.DateTimeFormat.FirstDayOfWeek + 7) % 7), _ => int.MinValue };
        DateOnly target;
        if (e.Key is Key.PageUp or Key.PageDown)
        {
            if (!NavigateMonth(e.Key == Key.PageUp ? -1 : 1)) return;
            target = new DateOnly(DisplayMonth.Year, DisplayMonth.Month, Math.Min(day.Date.Day, DateTime.DaysInMonth(DisplayMonth.Year, DisplayMonth.Month)));
        }
        else
        {
            if (step == int.MinValue || day.Date.DayNumber + step < 0 || day.Date.DayNumber + step > DateOnly.MaxValue.DayNumber) return;
            target = day.Date.AddDays(step);
            if (!IsDateAvailable(target)) return;
            SetCurrentValue(DisplayMonthProperty, new DateOnly(target.Year, target.Month, 1));
        }
        this.GetVisualDescendants().OfType<MaterialCalendarDay>().FirstOrDefault(d => d.Date == target)?.Focus();
        e.Handled = true;
    }

    /// <summary>Show an unparented picker. Host commits the DateOnly or MaterialDateRange result only when confirmed.</summary>
    public MaterialOverlaySession Show(MaterialOverlayHost host, MaterialOverlayOptions? options = null)
    {
        if (_dialog?.IsOpen == true) throw new InvalidOperationException("Picker is already open.");
        var dialog = new MaterialDialog { Content = this, Padding = new Thickness(0), MaxWidth = 360,
            ConfirmText = Labels.Confirm, CancelText = Labels.Cancel, IsConfirmEnabled = IsValid && IsEffectivelyEnabled };
        _dialog = dialog;
        AutomationProperties.SetName(dialog, Labels.Title);
        dialog.Confirming += (_, args) =>
        {
            args.Cancel = !IsValid || !IsEffectivelyEnabled;
            if (!args.Cancel) args.Value = SelectionMode == MaterialDateSelectionMode.Range
                ? new MaterialDateRange(SelectedDate!.Value, RangeEnd!.Value) : SelectedDate;
        };
        MaterialOverlaySession session;
        try { session = dialog.Show(host, options ?? new MaterialOverlayOptions { InitialFocus = Mode == MaterialDatePickerMode.Input ? StartInput : _month }); }
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
        if (change.Property != SelectedDateProperty && change.Property != RangeEndProperty &&
            change.Property != CultureProperty && change.Property != InputFormatProperty &&
            change.Property != DisplayFormatProperty && change.Property != ModeProperty &&
            change.Property != SelectionModeProperty && change.Property != LabelsProperty &&
            change.Property != DisplayMonthProperty && change.Property != MinimumDateProperty &&
            change.Property != MaximumDateProperty && change.Property != TodayProperty && change.Property != SelectableDateProperty && change.Property != IsEnabledProperty) return;
        if (change.Property == MinimumDateProperty || change.Property == MaximumDateProperty)
            _years.Children.Clear();
        if (change.Property == MinimumDateProperty || change.Property == MaximumDateProperty || change.Property == DisplayMonthProperty)
        {
            var month = new DateOnly(DisplayMonth.Year, DisplayMonth.Month, 1);
            if (MinimumDate <= MaximumDate)
            {
                var first = new DateOnly(MinimumDate.Year, MinimumDate.Month, 1);
                var last = new DateOnly(MaximumDate.Year, MaximumDate.Month, 1);
                if (month < first) month = first;
                if (month > last) month = last;
            }
            if (DisplayMonth != month) { SetCurrentValue(DisplayMonthProperty, month); return; }
        }
        if (change.Property == CultureProperty || change.Property == InputFormatProperty)
        {
            if (IsValid) SynchronizeText();
            else { ReadInput(); return; } // Retained native text now belongs to the new grammar.
        }
        else if (change.Property == SelectedDateProperty || change.Property == RangeEndProperty)
            SynchronizeText();
        Refresh();
        if (change.Property == ModeProperty && TopLevel.GetTopLevel(this) is not null)
        { if (Mode == MaterialDatePickerMode.Input) StartInput.Focus(); else _month.Focus(); }
    }
    private sealed class DatePickerPeer(MaterialDatePicker owner) : ControlAutomationPeer(owner)
    {
        protected override string? GetNameCore() => base.GetNameCore() ?? owner.Labels.Title;
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Calendar;
    }
}
