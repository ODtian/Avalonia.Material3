using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Media;
using Avalonia.Styling;

namespace Gallery.Pages;

/// <summary>Package-only, business-free host form: drafts never replace saved values on cancellation.</summary>
public sealed class DateTimePickersPage : UserControl
{
    private readonly MaterialTheme _theme;
    private readonly TextBlock _result = new() { Text = "Waiting", TextWrapping = TextWrapping.Wrap };
    private readonly string[] _cultures = ["en-US", "en-GB", "zh-CN", "de-DE", "ar-SA"];
    private int _cultureIndex;
    public MaterialOverlayHost Overlay { get; }
    public MaterialDatePicker DockedSingle { get; }
    public MaterialDatePicker DockedRange { get; }
    public MaterialDatePicker? ActiveDatePicker { get; private set; }
    public MaterialTimePicker? ActiveTimePicker { get; private set; }
    public DateOnly SavedDate { get; private set; } = new(2024, 2, 29);
    public MaterialDateRange SavedRange { get; private set; } = new(new(2024, 2, 29), new(2024, 3, 2));
    public TimeOnly SavedTime { get; private set; } = new(14, 7);
    public string LastResult => _result.Text ?? "";
    public CultureInfo Culture => CultureInfo.GetCultureInfo(_cultures[_cultureIndex]);
    public DateTimePickersPage(MaterialTheme theme)
    {
        _theme = theme;
        Overlay = new MaterialOverlayHost();
        AutomationProperties.SetAutomationId(_result, "PickerResult");
        AutomationProperties.SetLiveSetting(_result, AutomationLiveSetting.Polite);
        DockedSingle = Date(MaterialDateSelectionMode.Single, MaterialDatePickerMode.Calendar);
        DockedRange = Date(MaterialDateSelectionMode.Range, MaterialDatePickerMode.Calendar);
        var page = new StackPanel { Spacing = 12, Margin = new Thickness(16) };
        page.Children.Add(new TextBlock { Text = "Dates & times / 日期和时间", FontSize = 24, TextWrapping = TextWrapping.Wrap });
        page.Children.Add(new TextBlock { Text = "Gregorian civil dates; explicit formats; native editing; host-owned values. Cancel keeps saved values. Docked surfaces have no modal state.", TextWrapping = TextWrapping.Wrap });
        page.Children.Add(_result);
        var settings = new WrapPanel();
        settings.Children.Add(Action("Theme", "PickerTheme", () =>
            TopLevel.GetTopLevel(this)!.RequestedThemeVariant = TopLevel.GetTopLevel(this)!.ActualThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark));
        settings.Children.Add(Action("100 / 200% font", "PickerFont", () => _theme.Typography = _theme.Typography with { Scale = _theme.Typography.Scale == 1 ? 2 : 1 }));
        settings.Children.Add(Action("Culture / 文化", "PickerCulture", () =>
        {
            _cultureIndex = (_cultureIndex + 1) % _cultures.Length;
            foreach (var picker in new[] { DockedSingle, DockedRange, ActiveDatePicker }.OfType<MaterialDatePicker>())
            { picker.Culture = Culture; picker.InputFormat = Pattern(); }
            if (ActiveTimePicker is { } time) time.Culture = Culture;
        }));
        page.Children.Add(settings);
        var forms = new WrapPanel();
        forms.Children.Add(Action("Modal single", "DateSingleEntry", () => OpenDate(MaterialDateSelectionMode.Single, MaterialDatePickerMode.Calendar)));
        forms.Children.Add(Action("Modal range", "DateRangeEntry", () => OpenDate(MaterialDateSelectionMode.Range, MaterialDatePickerMode.Calendar)));
        forms.Children.Add(Action("Date input", "DateInputEntry", () => OpenDate(MaterialDateSelectionMode.Single, MaterialDatePickerMode.Input)));
        forms.Children.Add(Action("Range input", "DateRangeInputEntry", () => OpenDate(MaterialDateSelectionMode.Range, MaterialDatePickerMode.Input)));
        forms.Children.Add(Action("Clock 12h", "Clock12Entry", () => OpenTime(false, MaterialTimePickerMode.Clock)));
        forms.Children.Add(Action("Clock 24h", "Clock24Entry", () => OpenTime(true, MaterialTimePickerMode.Clock)));
        forms.Children.Add(Action("Time input 12h", "Time12Entry", () => OpenTime(false, MaterialTimePickerMode.Input)));
        forms.Children.Add(Action("Time input 24h", "Time24Entry", () => OpenTime(true, MaterialTimePickerMode.Input)));
        page.Children.Add(forms);
        page.Children.Add(new TextBlock { Text = "Docked single / range: February–March 2024, min 1 Feb, max 31 Mar, 20 Feb unavailable.", TextWrapping = TextWrapping.Wrap });
        page.Children.Add(DockedSingle);
        page.Children.Add(Action("Apply docked single", "ApplyDockedDate", () =>
        { if (DockedSingle.IsValid) { SavedDate = DockedSingle.SelectedDate!.Value; Report("Confirmed date: " + SavedDate.ToString("yyyy-MM-dd")); } }));
        page.Children.Add(DockedRange);
        page.Children.Add(Action("Apply docked range", "ApplyDockedRange", () =>
        {
            if (DockedRange.IsValid) { SavedRange = new(DockedRange.SelectedDate!.Value, DockedRange.RangeEnd!.Value);
                Report($"Confirmed range: {SavedRange.Start:yyyy-MM-dd}..{SavedRange.End:yyyy-MM-dd}"); }
        }));
        Overlay.Content = new ScrollViewer { Content = page, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        Content = Overlay;
    }
    private MaterialButton Action(string text, string id, System.Action action)
    {
        var button = new MaterialButton { Content = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap }, Variant = MaterialButtonVariant.Outlined };
        AutomationProperties.SetName(button, text); AutomationProperties.SetAutomationId(button, id);
        button.Click += (_, _) => action(); return button;
    }
    private string Pattern() => _cultures[_cultureIndex] switch
    { "en-US" => "MM/dd/yyyy", "en-GB" => "dd/MM/yyyy", "de-DE" => "dd.MM.yyyy", _ => "yyyy/MM/dd" };
    private MaterialDatePicker Date(MaterialDateSelectionMode selection, MaterialDatePickerMode mode)
    {
        var picker = new MaterialDatePicker { SelectionMode = selection, Mode = mode, Culture = Culture,
            InputFormat = Pattern(), DisplayFormat = "ddd, MMM d", DisplayMonth = new(2024, 2, 1),
            MinimumDate = new(2024, 2, 1), MaximumDate = new(2024, 3, 31),
            Today = new(2024, 2, 29), SelectableDate = date => date != new DateOnly(2024, 2, 20),
            SelectedDate = selection == MaterialDateSelectionMode.Single ? SavedDate : SavedRange.Start,
            RangeEnd = selection == MaterialDateSelectionMode.Range ? SavedRange.End : null,
            HorizontalAlignment = HorizontalAlignment.Left };
        AutomationProperties.SetAutomationId(picker.StartInput, "DateStartInput");
        AutomationProperties.SetAutomationId(picker.EndInput, "DateEndInput");
        return picker;
    }
    public MaterialOverlaySession OpenDate(MaterialDateSelectionMode selection, MaterialDatePickerMode mode)
    {
        ActiveDatePicker = Date(selection, mode);
        var session = ActiveDatePicker.Show(Overlay);
        session.Closed += (_, result) =>
        {
            if (result.Reason == MaterialOverlayCloseReason.Confirmed && result.Value is DateOnly date)
            { SavedDate = date; Report("Confirmed date: " + date.ToString("yyyy-MM-dd")); }
            else if (result.Reason == MaterialOverlayCloseReason.Confirmed && result.Value is MaterialDateRange range)
            { SavedRange = range; Report($"Confirmed range: {range.Start:yyyy-MM-dd}..{range.End:yyyy-MM-dd}"); }
            else Report(result.Reason.ToString());
            ActiveDatePicker = null;
        };
        return session;
    }
    public MaterialOverlaySession OpenTime(bool is24Hour, MaterialTimePickerMode mode)
    {
        ActiveTimePicker = new MaterialTimePicker { Is24Hour = is24Hour, Mode = mode, SelectedTime = SavedTime, Culture = Culture,
            Layout = is24Hour && Overlay.Bounds.Width >= 720 ? MaterialTimePickerLayout.Horizontal : MaterialTimePickerLayout.Vertical };
        AutomationProperties.SetAutomationId(ActiveTimePicker.HourInput, "TimeHourInput");
        AutomationProperties.SetAutomationId(ActiveTimePicker.MinuteInput, "TimeMinuteInput");
        var session = ActiveTimePicker.Show(Overlay);
        session.Closed += (_, result) =>
        {
            if (result.Reason == MaterialOverlayCloseReason.Confirmed && result.Value is TimeOnly time)
            { SavedTime = time; Report("Confirmed time: " + time.ToString("HH:mm")); }
            else Report(result.Reason.ToString());
            ActiveTimePicker = null;
        };
        return session;
    }
    private void Report(string text) { _result.Text = text; AutomationProperties.SetName(_result, text); }
}
