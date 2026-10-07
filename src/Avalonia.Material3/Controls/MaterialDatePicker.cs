using System.Globalization;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Diagnostics;
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
    private string _title = "Select date";
    internal bool HasCustomTitle { get; private init; }
    public string Title { get => _title; init { _title = value; HasCustomTitle = true; } }
    private string _startDate = "Start date";
    internal bool HasCustomStartDate { get; private init; }
    public string StartDate { get => _startDate; init { _startDate=value; HasCustomStartDate=true; } }
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
    private readonly MaterialDatePickerHeader _header;
    private readonly MaterialIconButton _mode;
    private readonly MaterialSymbol _inputIcon = new() { Symbol = "edit", Size = 24, Filled=true };
    private readonly MaterialSymbol _calendarIcon = new() { Symbol = "date_range", Size = 24, Filled=true };
    private readonly Grid _inputs;
    private readonly StackPanel _calendar = new() { Margin = new Thickness(12, 0) };
    private MaterialCalendarMonthView? _days;
    private readonly StackPanel _singleMonth = new();
    private readonly MaterialCalendarWeekRow _week = new();
    private readonly MaterialCalendarRangeView _rangeCalendar;
    private readonly Panel _calendarModes = new();
    private readonly ScrollViewer _singleCalendarScroll;
    private readonly Border _divider = new() { Height = 1 };
    private readonly MaterialCalendarYearsView _years = new();
    private readonly ScrollViewer _yearScroll;
    private readonly Media.RotateTransform _yearMenuRotation = new();
    private readonly ScrollViewer _calendarScroll;
    private readonly MaterialCalendarYearPanel _yearPanel;
    private readonly MaterialCalendarMonthPanel _monthPanel;
    private readonly MaterialPickerModePanel _modePanel;
    private readonly MaterialButton _previous, _next, _month;
    private bool _choosingYear;
    private DateOnly? _builtMonth;
    private CultureInfo? _builtCulture;
    private CultureInfo? _rangeCulture;
    private CultureInfo? _dateCulture;
    private string? _defaultInputPattern;
    private CancellationTokenSource? _inputFocusRequest;
    private MaterialDialog? _dialog;
    public DateOnly? SelectedDate { get => GetValue(SelectedDateProperty); set => SetValue(SelectedDateProperty, value); }
    public DateOnly? RangeEnd { get => GetValue(RangeEndProperty); set => SetValue(RangeEndProperty, value); }
    public MaterialDatePickerMode Mode { get => GetValue(ModeProperty); set => SetValue(ModeProperty, value); }
    public MaterialDateSelectionMode SelectionMode { get => GetValue(SelectionModeProperty); set => SetValue(SelectionModeProperty, value); }
    public CultureInfo Culture { get => GetValue(CultureProperty); set => SetValue(CultureProperty, value); }
    public string? InputFormat { get => GetValue(InputFormatProperty); set => SetValue(InputFormatProperty, value); }
    public string DisplayFormat { get => GetValue(DisplayFormatProperty); set { var previous=DisplayFormat; SetValue(DisplayFormatProperty,value); if(_ready&&previous==value)Refresh(); } }
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
    internal CultureInfo DateCulture => _dateCulture ??= MaterialPickerSupport.Gregorian(Culture);
    private string Pattern => InputFormat ?? (_defaultInputPattern ??= DefaultInputPattern());
    internal string EffectiveInputPattern => Pattern;
    protected override Type StyleKeyOverride => typeof(MaterialDatePicker);
    protected override AutomationPeer OnCreateAutomationPeer() => new DatePickerPeer(this);

    public MaterialDatePicker()
    {
        _mode = new MaterialIconButton();
        MaterialPickerSupport.Resource(_inputIcon, MaterialSymbol.ForegroundProperty, "OnSurfaceVariantBrush");
        MaterialPickerSupport.Resource(_calendarIcon, MaterialSymbol.ForegroundProperty, "OnSurfaceVariantBrush");
        _mode.Click += (_, _) => SetCurrentValue(ModeProperty,
            Mode == MaterialDatePickerMode.Calendar ? MaterialDatePickerMode.Input : MaterialDatePickerMode.Calendar);
        _inputs = new Grid { Margin = new Thickness(24, 10, 24, 0), ColumnDefinitions = new ColumnDefinitions("*,8,*"), Children = { StartInput, EndInput } };
        Grid.SetColumn(EndInput, 2);
        _previous = new MaterialIconButton(); _previous.Click += (_, _) => NavigateMonth(-1);
        _next = new MaterialIconButton(); _next.Click += (_, _) => NavigateMonth(1);
        _month = MaterialPickerSupport.Action(Labels.ChooseYear, () => { _choosingYear = !_choosingYear; RefreshCalendar(); });
        _month.HorizontalAlignment = HorizontalAlignment.Left;
        MaterialPickerSupport.Resource(_month, ForegroundProperty, "OnSurfaceVariantBrush");
        _month.ContentTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<string>((text, _) =>
            new MaterialYearMenuContent(
                new TextBlock {Text=text,TextWrapping=Media.TextWrapping.Wrap,VerticalAlignment=VerticalAlignment.Center},
                new MaterialSymbol {Symbol="arrow_drop_down",Size=24,RenderTransform=_yearMenuRotation,RenderTransformOrigin=RelativePoint.Center}));
        _previous.Content = new MaterialSymbol { Symbol = "chevron_left", Size = 24 };
        _next.Content = new MaterialSymbol { Symbol = "chevron_right", Size = 24 };
        var navigation = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"), MinHeight = 56 };
        Grid.SetColumn(_previous, 1); Grid.SetColumn(_next, 2);
        navigation.Children.Add(_previous); navigation.Children.Add(_month); navigation.Children.Add(_next);
        _monthPanel = new(_singleMonth);
        _calendarScroll = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = _monthPanel };
        _yearScroll = new ScrollViewer { Content = _years, MaxHeight = 335 };
        MaterialPickerSupport.Resource(_yearScroll, BackgroundProperty, "SurfaceContainerHighBrush");
        _yearPanel = new(new StackPanel {Children={_week,_calendarScroll}}, _yearScroll);
        _calendar.Children.Add(navigation); _calendar.Children.Add(_yearPanel);
        _rangeCalendar = new(this);
        _singleCalendarScroll = new() { Content = _calendar, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        _calendarModes.Children.Add(_singleCalendarScroll);
        _header = new(_title, _headline, _mode);
        _modePanel = new(this, _calendarModes, _inputs);
        MaterialPickerSupport.Resource(_divider, Border.BackgroundProperty, "OutlineVariantBrush");
        Surface = new MaterialDatePickerPanel(_header, _divider, _modePanel, _error);
        _error.Margin = new Thickness(24, 0, 24, 12);
        AutomationProperties.SetLiveSetting(_error, AutomationLiveSetting.Polite);
        StartInput.TextChanged += (_, _) => ReadInput();
        EndInput.TextChanged += (_, _) => ReadInput();
        _ = new MaterialDateInputEditing(this,StartInput);
        _ = new MaterialDateInputEditing(this,EndInput);
        AddHandler(KeyDownEvent, CalendarKey);
        DetachedFromVisualTree += (_,_) => { _inputFocusRequest?.Cancel(); _inputFocusRequest?.Dispose(); _inputFocusRequest=null; };
        AttachedToVisualTree += (_,_) => { if(Mode==MaterialDatePickerMode.Input&&_dialog is null)RequestInputFocus(); };
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
        var range = SelectionMode == MaterialDateSelectionMode.Range;
        PseudoClasses.Set(":range", range);
        _title.Text = range && !Labels.HasCustomTitle ?
            Mode == MaterialDatePickerMode.Calendar ? "Select dates" : "Enter dates" : Labels.Title;
        _header.Range = range; _header.InvalidateMeasure();
        var format = this.GetDiagnostic(DisplayFormatProperty).Priority > BindingPriority.Style ? DefaultHeadlineFormat : DisplayFormat;
        _headline.Text = SelectedDate?.ToString(format, DateCulture) ?? (range ? Labels.StartDate : Mode==MaterialDatePickerMode.Calendar?Labels.Selected:"Entered date");
        if (range)
        {
            var endText = RangeEnd?.ToString(format, DateCulture) ?? Labels.EndDate;
            _header.SetRangeText(_headline.Text!, endText);
            _headline.Text += " - " + endText;
        }
        MaterialPickerSupport.Resource(_headline, TextBlock.FontSizeProperty, (SelectionMode == MaterialDateSelectionMode.Range ? "TitleLarge" : "HeadlineLarge") + "FontSize");
        MaterialPickerSupport.Resource(_headline, TextBlock.FontFamilyProperty, (SelectionMode == MaterialDateSelectionMode.Range ? "TitleLarge" : "HeadlineLarge") + "FontFamily");
        MaterialPickerSupport.Resource(_headline, TextBlock.FontWeightProperty, (SelectionMode == MaterialDateSelectionMode.Range ? "TitleLarge" : "HeadlineLarge") + "FontWeight");
        MaterialPickerSupport.Resource(_headline, TextBlock.LineHeightProperty, (SelectionMode == MaterialDateSelectionMode.Range ? "TitleLarge" : "HeadlineLarge") + "LineHeight");
        MaterialPickerSupport.Resource(_headline, TextBlock.LetterSpacingProperty, (SelectionMode == MaterialDateSelectionMode.Range ? "TitleLarge" : "HeadlineLarge") + "LetterSpacing");
        EndInput.IsVisible = SelectionMode == MaterialDateSelectionMode.Range;
        Grid.SetColumnSpan(StartInput, range ? 1 : 3);
        var calendar = range ? (Control)_rangeCalendar : _singleCalendarScroll;
        if (_calendarModes.Children.Count == 0 || _calendarModes.Children[0] != calendar)
        { _calendarModes.Children.Clear(); _calendarModes.Children.Add(calendar); }
        StartInput.SetValue(MaterialTextField.LabelProperty,range||Labels.HasCustomStartDate?Labels.StartDate:"Date",BindingPriority.Style);
        EndInput.SetValue(MaterialTextField.LabelProperty,Labels.EndDate,BindingPriority.Style);
        StartInput.SetValue(TextBox.PlaceholderTextProperty,Pattern.ToUpper(DateCulture),BindingPriority.Style);
        EndInput.SetValue(TextBox.PlaceholderTextProperty,Pattern.ToUpper(DateCulture),BindingPriority.Style);
        _mode.Content = Mode == MaterialDatePickerMode.Calendar ? _inputIcon : _calendarIcon;
        AutomationProperties.SetName(_mode, Mode == MaterialDatePickerMode.Calendar ? Labels.InputMode : Labels.CalendarMode);
        var startError = InputComplete(StartInput.Text) && !Parse(StartInput.Text, out _) ? Labels.InvalidDate : null;
        var endError = SelectionMode == MaterialDateSelectionMode.Range && InputComplete(EndInput.Text) && !Parse(EndInput.Text, out _) ? Labels.InvalidDate : null;
        if (SelectedDate is { } start && !IsDateAvailable(start)) startError = Labels.UnavailableDate;
        if (SelectionMode == MaterialDateSelectionMode.Range && RangeEnd is { } end && !IsDateAvailable(end)) endError = Labels.UnavailableDate;
        if (SelectedDate is { } s && RangeEnd is { } e && SelectionMode == MaterialDateSelectionMode.Range && e < s)
            endError = Labels.InvalidRange;
        StartInput.SetValue(MaterialTextField.ErrorTextProperty,startError,BindingPriority.Style);
        EndInput.SetValue(MaterialTextField.ErrorTextProperty,endError,BindingPriority.Style);
        StartInput.SetValue(MarginProperty,new Thickness(0,0,0,startError is null?16:12),BindingPriority.Style);
        EndInput.SetValue(MarginProperty,new Thickness(0,0,0,endError is null?16:12),BindingPriority.Style);
        var message = startError ?? endError ?? (SelectedDate is null || SelectionMode == MaterialDateSelectionMode.Range && RangeEnd is null ? Labels.Incomplete : null);
        SetAndRaise(ValidationMessageProperty, ref _validationMessage, message);
        SetAndRaise(IsValidProperty, ref _isValid, message is null);
        _error.Text = message; _error.IsVisible = message is not null && startError is null && endError is null && message != Labels.Incomplete;
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
        if (SelectionMode == MaterialDateSelectionMode.Single && offset != 0) _monthPanel.Capture(Math.Sign(offset));
        SetCurrentValue(DisplayMonthProperty, candidate);
        if (SelectionMode == MaterialDateSelectionMode.Single && offset != 0) _monthPanel.Scroll();
        return true;
    }
    internal void RefreshDay(MaterialCalendarDay button)
    {
        var date = button.Date;
        button.IsEnabled = IsDateAvailable(date);
        button.AnimateContainer = SelectionMode == MaterialDateSelectionMode.Single || date == SelectedDate;
        button.IsInRange = SelectionMode == MaterialDateSelectionMode.Range && SelectedDate is { } start && RangeEnd is { } end && date >= start && date <= end;
        button.IsChecked = date == SelectedDate || SelectionMode == MaterialDateSelectionMode.Range && date == RangeEnd;
        button.IsToday = date == Today;
        var status = date == SelectedDate ? SelectionMode == MaterialDateSelectionMode.Single ? Labels.Selected : Labels.RangeStart
            : date == RangeEnd && SelectionMode == MaterialDateSelectionMode.Range ? Labels.RangeEnd : button.IsInRange ? Labels.InRange : "";
        if (button.IsToday) status += " " + Labels.Today;
        AutomationProperties.SetName(button, date.ToString("D", DateCulture));
        AutomationProperties.SetItemStatus(button, status);
    }
    private string DefaultHeadlineFormat
    {
        get
        {
            return DateCulture.DateTimeFormat.LongDatePattern.Replace("dddd","",StringComparison.Ordinal)
                .Replace("MMMM","MMM",StringComparison.Ordinal).Trim(' ',',','،');
        }
    }
    private bool InputComplete(string? text) => InputFormat is not null ? !string.IsNullOrEmpty(text) : text?.Count(char.IsDigit)==8;
    private string DefaultInputPattern()
    {
        var source=new string(DateCulture.DateTimeFormat.ShortDatePattern.Where(c=>c is 'd' or 'M' or 'y' or '/' or '-' or '.').ToArray());
        var result=new System.Text.StringBuilder();
        for(var i=0;i<source.Length;i++)
        {
            var c=source[i];
            if(c is 'd' or 'M' or 'y')
            {
                while(i+1<source.Length&&source[i+1]==c)i++;
                result.Append(c,c=='y'?4:2);
                if(i+1<source.Length&&source[i+1] is 'd' or 'M' or 'y')result.Append('/');
            }
            else result.Append(c);
        }
        return result.ToString().TrimEnd('.');
    }
    private void RefreshCalendar()
    {
        if (SelectionMode == MaterialDateSelectionMode.Single) _yearPanel.Update(_choosingYear);
        _previous.IsVisible = _next.IsVisible = !_choosingYear;
        _yearMenuRotation.Angle = _choosingYear ? 180 : 0;
        _month.Content = DisplayMonth.ToString(DateCulture.DateTimeFormat.YearMonthPattern, DateCulture);
        AutomationProperties.SetName(_month, Labels.ChooseYear + ": " + _month.Content);
        AutomationProperties.SetName(_previous, Labels.PreviousMonth); AutomationProperties.SetName(_next, Labels.NextMonth);
        _previous.IsEnabled = MinimumDate <= MaximumDate && (DisplayMonth.Year > MinimumDate.Year || DisplayMonth.Year == MinimumDate.Year && DisplayMonth.Month > MinimumDate.Month);
        _next.IsEnabled = MinimumDate <= MaximumDate && (DisplayMonth.Year < MaximumDate.Year || DisplayMonth.Year == MaximumDate.Year && DisplayMonth.Month < MaximumDate.Month);
        var month = new DateOnly(DisplayMonth.Year, DisplayMonth.Month, 1);
        var cultureChanged = !Equals(_builtCulture,Culture);
        var rebuild = _builtMonth != month || cultureChanged;
        if (SelectionMode == MaterialDateSelectionMode.Single && rebuild)
        {
            _builtMonth = month; _builtCulture = Culture;
            if(cultureChanged)_week.Refresh(this);
            _week.MinimumCellSize = 48;
            if (_days is not null) _singleMonth.Children.Remove(_days);
            _days = new(this, month); _days.CellSizeChanged += size => _week.MinimumCellSize = size;
            _singleMonth.Children.Add(_days);
        }
        _days?.Refresh();
        if (SelectionMode == MaterialDateSelectionMode.Range)
        {
            _rangeCalendar.Refresh(!Equals(_rangeCulture, Culture));
            _rangeCulture = Culture;
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
            _yearScroll.Offset = new Vector(0, Math.Max(0, (DisplayMonth.Year - MinimumDate.Year - 3) / 3) * _years.RowStride);
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
        if (SelectionMode == MaterialDateSelectionMode.Range) _rangeCalendar.FocusDate(target);
        else this.GetVisualDescendants().OfType<MaterialCalendarDay>().FirstOrDefault(d => d.Date == target)?.Focus();
        e.Handled = true;
    }

    /// <summary>Show an unparented picker. Host commits the DateOnly or MaterialDateRange result only when confirmed.</summary>
    public MaterialOverlaySession Show(MaterialOverlayHost host, MaterialOverlayOptions? options = null)
    {
        if (_dialog?.IsOpen == true) throw new InvalidOperationException("Picker is already open.");
        var dialog = new MaterialDialog { Content = this, Padding = new Thickness(0), MaxWidth = 360, MaxHeight = 568,
            ConfirmText = Labels.Confirm, CancelText = Labels.Cancel, IsConfirmEnabled = IsValid && IsEffectivelyEnabled };
        _dialog = dialog;
        dialog.UseDatePickerTemplate();
        AutomationProperties.SetName(dialog, Labels.Title);
        dialog.Confirming += (_, args) =>
        {
            args.Cancel = !IsValid || !IsEffectivelyEnabled;
            if (!args.Cancel) args.Value = SelectionMode == MaterialDateSelectionMode.Range
                ? new MaterialDateRange(SelectedDate!.Value, RangeEnd!.Value) : SelectedDate;
        };
        MaterialOverlaySession session;
        try { session = dialog.Show(host, options ?? new MaterialOverlayOptions { InitialFocus = Mode == MaterialDatePickerMode.Input ? _mode : SelectionMode == MaterialDateSelectionMode.Range ? _mode : _month }); }
        catch { dialog.Content = null; _dialog = null; throw; }
        session.Closed += (_, _) => { dialog.Content = null; _dialog = null; };
        if(Mode==MaterialDatePickerMode.Input&&session.Options.TakeFocus&&session.Options.InitialFocus!=StartInput&&options?.InitialFocus is null)RequestInputFocus();
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
            if (change.Property == CultureProperty) { _dateCulture = null; _defaultInputPattern=null; }
            if (IsValid) SynchronizeText();
            else { ReadInput(); return; } // Retained native text now belongs to the new grammar.
        }
        else if (change.Property == SelectedDateProperty || change.Property == RangeEndProperty)
            SynchronizeText();
        if (change.Property == ModeProperty) _modePanel.Prepare();
        Refresh();
        if (change.Property == ModeProperty && TopLevel.GetTopLevel(this) is not null)
        { if (Mode == MaterialDatePickerMode.Input) RequestInputFocus(); else { _inputFocusRequest?.Cancel(); if (SelectionMode == MaterialDateSelectionMode.Range) _mode.Focus(); else _month.Focus(); } }
    }
    private async void RequestInputFocus()
    {
        if(Session?.Options.TakeFocus==false)return;
        _inputFocusRequest?.Cancel();_inputFocusRequest?.Dispose();
        using var request=new CancellationTokenSource();_inputFocusRequest=request;
        try
        {
            await Task.Delay(300,request.Token);
            if(Mode==MaterialDatePickerMode.Input&&IsEffectivelyEnabled&&IsEffectivelyVisible&&(_dialog is null||Session?.IsTop==true))StartInput.Focus();
        }
        catch(OperationCanceledException) { }
        finally { if(_inputFocusRequest==request)_inputFocusRequest=null; }
    }
    private sealed class DatePickerPeer(MaterialDatePicker owner) : ControlAutomationPeer(owner)
    {
        protected override string? GetNameCore() => base.GetNameCore() ?? owner.Labels.Title;
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Calendar;
    }
}
