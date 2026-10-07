using System.Globalization;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Presenters;
using Avalonia.Material3.Controls;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Material3.Tests;

public class DateTimePickerScenarioTests
{
    [AvaloniaFact]
    public void Time_dialog_accepts_a_consumer_control_theme_template()
    {
        using var host = new DialogHost();
        var picker = new MaterialTimePicker { SelectedTime = new(7,7) };
        var session = picker.Show(host.Overlay); host.Render();
        session.Content.Theme = new ControlTheme(typeof(MaterialDialog))
        {
            Setters = { new Setter(TemplatedControl.TemplateProperty,new FuncControlTemplate<MaterialDialog>((owner,_)=>
                new StackPanel {Children={new TextBlock {Text="Consumer time surface"},new ContentPresenter {Content=owner.Content}}})) }
        };
        host.Render();
        Assert.Single(session.Content.GetVisualDescendants().OfType<TextBlock>(),t=>t.IsEffectivelyVisible&&t.Text=="Consumer time surface");
        Assert.True(picker.Cancel());
    }
    [AvaloniaFact]
    public void Time_dialog_default_title_reserves_its_gap_and_tracks_mode_while_custom_title_wins()
    {
        using var host = new DialogHost();
        var picker = new MaterialTimePicker { SelectedTime = new(7,7), Layout = MaterialTimePickerLayout.Vertical };
        picker.Show(host.Overlay); host.Render();
        var dialog=host.Window.GetVisualDescendants().OfType<MaterialDialog>().Single();
        var title=dialog.GetVisualDescendants().OfType<TextBlock>().Single(t=>t.IsEffectivelyVisible&&t.Text=="Select time");
        var hour=picker.GetVisualDescendants().OfType<MaterialTimeSelector>().First();
        Assert.Equal(20,GeometryHost.Box(hour,host.Window).Top-GeometryHost.Box(title,host.Window).Bottom);
        Assert.Equal(Avalonia.Media.Color.Parse("#1D1B20"),((Avalonia.Media.ISolidColorBrush)title.Foreground!).Color);
        picker.Mode=MaterialTimePickerMode.Input;host.Render();Assert.Equal("Enter time",title.Text);
        picker.Labels=picker.Labels with {Title="Select time"};host.Render();Assert.Equal("Select time",title.Text);
    }
    [AvaloniaFact]
    public void Short_horizontal_time_dialog_keeps_its_footer_inside_the_content_insets()
    {
        using var host = new DialogHost(800, 350);
        var picker = new MaterialTimePicker { Height = 230, Layout = MaterialTimePickerLayout.Horizontal, SelectedTime = new(7, 7) };
        picker.Show(host.Overlay); host.Render();
        var dialog = host.Window.GetVisualDescendants().OfType<MaterialDialog>().Single();
        var mode = host.Window.GetVisualDescendants().OfType<MaterialIconButton>().Single(b => ControlAutomationPeer.CreatePeerForElement(b).GetName() == picker.Labels.InputMode);
        var confirm = host.Button(dialog, picker.Labels.Confirm);
        var box = GeometryHost.Box(dialog, host.Window);
        Assert.Equal(24, GeometryHost.Box(mode, host.Window).Left - box.Left);
        Assert.Equal(24, box.Right - GeometryHost.Box(confirm, host.Window).Right);
    }
    [AvaloniaFact]
    public void Horizontal_clock_dial_does_not_stretch_the_standard_time_selector_recipe()
    {
        using var host = new DialogHost();
        var picker = new MaterialTimePicker { Layout = MaterialTimePickerLayout.Horizontal, Is24Hour = true, SelectedTime = new(23, 59) };
        picker.Show(host.Overlay); host.Render();
        var hour = picker.GetVisualDescendants().OfType<MaterialTimeSelector>()
            .Single(b => ControlAutomationPeer.CreatePeerForElement(b).GetName()!.StartsWith("Hour:"));
        Assert.Equal(96, hour.Bounds.Width);
        Assert.Equal(80, hour.Bounds.Height);
    }

    [AvaloniaFact]
    public void Calendar_mode_action_is_a_visible_touch_target_and_switches_to_native_editor_focus()
    {
        using var host = new DialogHost();
        var picker = new MaterialDatePicker { SelectedDate = new(2024, 2, 29), DisplayMonth = new(2024, 2, 1) };
        var session = picker.Show(host.Overlay); host.Render();
        var action = picker.GetVisualDescendants().OfType<MaterialIconButton>().Single(b => ControlAutomationPeer.CreatePeerForElement(b).GetName() == "Enter date");
        action.BringIntoView(); host.Render();
        Assert.True(action.IsEffectivelyVisible);
        Assert.True(action.Bounds.Width >= 48); Assert.True(action.Bounds.Height >= 48);
        var icon = Assert.IsAssignableFrom<Control>(action.Content);
        Assert.True(icon.Bounds.Width >= 20); Assert.True(icon.Bounds.Height >= 20);
        var brush = icon switch
        {
            Avalonia.Controls.Shapes.Shape shape => shape.Fill,
            MaterialSymbol symbol => symbol.Foreground,
            PathIcon path => path.Foreground,
            _ => icon.GetValue(TextBlock.ForegroundProperty)
        };
        Assert.NotNull(brush);
        Assert.Equal(Color.Parse("#49454F"), ((ISolidColorBrush)brush).Color);
        using (var frame = host.Window.CaptureRenderedFrame())
        using (var pixels = new MemoryStream())
        {
            frame!.Save(pixels, Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
            pixels.Position = 0;
            using var bitmap = SkiaSharp.SKBitmap.Decode(pixels);
            var top = icon.TranslatePoint(default, host.Window)!.Value;
            var ink = 0;
            for (var y = (int)top.Y; y < top.Y + icon.Bounds.Height; y++)
                for (var x = (int)top.X; x < top.X + icon.Bounds.Width; x++)
                {
                    var color = bitmap.GetPixel(x, y);
                    if (color.Red == 0x49 && color.Green == 0x45 && color.Blue == 0x4f) ink++;
                }
            Assert.True(ink > 10, "The mode action needs actual rendered semantic icon ink, not just a named empty target.");
        }
        var point = host.Center(action);
        using var touch = host.Window.TouchBegin(point); host.Render(); host.Window.TouchEnd(touch, point); host.Render();
        Assert.Equal(MaterialDatePickerMode.Input, picker.Mode);
        Assert.True(picker.StartInput.IsFocused);
        Assert.Equal(new DateOnly(2024, 2, 29), picker.SelectedDate);
        Assert.True(session.IsOpen);
    }

    [AvaloniaFact]
    public void Public_template_surface_keeps_native_editing_and_live_semantic_theme_and_type_metrics()
    {
        using var host = new DialogHost();
        var picker = new MaterialDatePicker { SelectedDate = new(2024, 2, 29), DisplayMonth = new(2024, 2, 1), Mode = MaterialDatePickerMode.Input, InputFormat = "yyyy-MM-dd" };
        picker.Template = new FuncControlTemplate<MaterialDatePicker>((owner, _) => new ContentPresenter { Content = owner.Surface });
        picker.Show(host.Overlay); host.Render();
        Assert.Equal(AutomationControlType.Calendar, ControlAutomationPeer.CreatePeerForElement(picker).GetAutomationControlType());
        picker.StartInput.SelectAll(); host.Window.KeyTextInput("2024-02-28"); host.Render();
        Assert.Equal(new DateOnly(2024, 2, 28), picker.SelectedDate);
        Assert.True(picker.Cancel());
        var time = new MaterialTimePicker { SelectedTime = new(14, 7), Is24Hour = true };
        time.Show(host.Overlay); host.Render();
        var dial = time.GetVisualDescendants().OfType<MaterialClockDial>().Single();
        Assert.Equal(256, dial.Bounds.Width);
        Assert.Equal(Color.Parse("#E6E0E9"), ((ISolidColorBrush)dial.DialBrush!).Color);
        host.Window.RequestedThemeVariant = ThemeVariant.Dark;
        host.Theme.Typography = host.Theme.Typography with { Scale = 2 }; host.Render();
        Assert.Equal(512, dial.Bounds.Width);
        Assert.Equal(Color.Parse("#36343B"), ((ISolidColorBrush)dial.DialBrush!).Color);
        Assert.Equal(90, time.HourInput.FontSize);
        time.IsEnabled = false; host.Render(); Assert.False(time.Confirm());
        Assert.False(ControlAutomationPeer.CreatePeerForElement(time.HourInput).IsEnabled());
    }

    [AvaloniaFact]
    public void Modal_clock_dial_stays_synchronized_and_native_number_tap_and_touch_cancel_do_not_submit()
    {
        using var host = new DialogHost();
        var picker = new MaterialTimePicker { Is24Hour = true, SelectedTime = new(0, 0) };
        var session = picker.Show(host.Overlay); host.Render();
        picker.SelectHour(23); picker.SelectMinute(59); host.Render();
        var dial = picker.GetVisualDescendants().OfType<MaterialClockDial>().Single();
        Assert.Equal(59, dial.Value);
        var zero = dial.GetVisualDescendants().OfType<MaterialClockNumber>().Single(n => n.Value == 0);
        var point = host.Center(zero);
        using var touch = host.Window.TouchBegin(point); host.Render();
        host.Window.TouchEnd(touch, point); host.Render();
        Assert.Equal(new TimeOnly(23, 0), picker.SelectedTime);
        Assert.True(session.IsOpen);
        picker.Cancel(); Assert.Null(session.Completion.Result.Value);
    }

    [AvaloniaTheory]
    [InlineData("en-US", "MM/dd/yyyy", "02/29/2024", true, 2024, 2, 29)]
    [InlineData("en-US", "MM/dd/yyyy", "02/29/2023", false, 0, 0, 0)]
    [InlineData("en-US", "MM/dd/yyyy", "03/04/2024", true, 2024, 3, 4)]
    [InlineData("en-GB", "dd/MM/yyyy", "03/04/2024", true, 2024, 4, 3)]
    [InlineData("zh-CN", "yyyy/MM/dd", "2024/02/29", true, 2024, 2, 29)]
    [InlineData("de-DE", "dd.MM.yyyy", "29.02.2024", true, 2024, 2, 29)]
    [InlineData("de-DE", "dd.MM.yyyy", "29.13.2024", false, 0, 0, 0)]
    [InlineData("ar-SA", "yyyy/MM/dd", "٢٠٢٤/٠٢/٢٩", true, 2024, 2, 29)]
    [InlineData("en-US", "MM/dd/yyyy", "０２/２９/２０２４", true, 2024, 2, 29)]
    [InlineData("en-US", "MM/dd/yyyy", "02/29/2100", false, 0, 0, 0)]
    public void Explicit_culture_and_Gregorian_patterns_have_independent_boundary_vectors(string culture, string format, string text, bool valid, int year, int month, int day)
    {
        using var host = new DialogHost();
        var picker = new MaterialDatePicker { Culture = CultureInfo.GetCultureInfo(culture), InputFormat = format, Mode = MaterialDatePickerMode.Input };
        picker.Show(host.Overlay); host.Render();
        picker.StartInput.Focus(); host.Window.KeyTextInput(text); host.Render();
        Assert.Equal(valid, picker.IsValid);
        Assert.Equal(valid ? new DateOnly(year, month, day) : (DateOnly?)null, picker.SelectedDate);
        Assert.Equal(text, picker.StartInput.Text);
    }

    [AvaloniaFact]
    public void Date_input_and_calendar_share_inclusive_limits_and_predicate_and_range_order()
    {
        using var host = new DialogHost();
        var picker = new MaterialDatePicker { Mode = MaterialDatePickerMode.Input, SelectionMode = MaterialDateSelectionMode.Range,
            InputFormat = "yyyy-MM-dd", MinimumDate = new(2024, 2, 1), MaximumDate = new(2024, 2, 29),
            SelectableDate = date => date != new DateOnly(2024, 2, 20) };
        picker.Show(host.Overlay); host.Render();
        Type(picker.EndInput, "2024-02-29"); Assert.False(picker.IsValid);
        Type(picker.StartInput, "2024-02-20"); Assert.False(picker.IsValid); Assert.True(picker.StartInput.HasError);
        Assert.False(picker.SelectDate(new(2024, 2, 20)));
        Type(picker.StartInput, "2024-02-01"); Assert.True(picker.IsValid);
        Type(picker.EndInput, "2024-01-31"); Assert.False(picker.IsValid);
        Type(picker.EndInput, "2024-02-01"); Assert.True(picker.IsValid);
        Assert.True(picker.Confirm());
        void Type(MaterialTextField field, string value) { field.Focus(); field.SelectAll(); host.Window.KeyTextInput(value); host.Render(); }
    }

    [AvaloniaFact]
    public void Calendar_keyboard_range_reset_year_actions_and_automation_are_real()
    {
        using var host = new DialogHost();
        var picker = new MaterialDatePicker { DisplayMonth = new(2024, 2, 1), SelectionMode = MaterialDateSelectionMode.Range, Culture = CultureInfo.GetCultureInfo("en-GB") };
        picker.Show(host.Overlay); host.Render();
        var day = picker.GetVisualDescendants().OfType<MaterialCalendarDay>().Single(d => d.Date == new DateOnly(2024, 2, 20));
        host.Click(day); host.Render();
        day.Focus(); host.Key(PhysicalKey.ArrowLeft); host.Key(PhysicalKey.Space); host.Render();
        Assert.Equal(new DateOnly(2024, 2, 19), picker.SelectedDate); Assert.Null(picker.RangeEnd);
        var selected = picker.GetVisualDescendants().OfType<MaterialCalendarDay>().Single(d => d.Date == new DateOnly(2024, 2, 19));
        var peer = ControlAutomationPeer.CreatePeerForElement(selected);
        Assert.Contains("2024", peer.GetName());
        Assert.IsAssignableFrom<IToggleProvider>(peer);
        host.Click(picker.GetVisualDescendants().OfType<MaterialButton>().Single(b => ControlAutomationPeer.CreatePeerForElement(b).GetName()!.StartsWith("Choose year")));
        var year = picker.GetVisualDescendants().OfType<MaterialCalendarYear>().Single(y => y.Year == 2024);
        year.BringIntoView(); host.Render(); host.Click(year);
        Assert.Equal(2024, picker.DisplayMonth.Year);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Civil_date_extremes_do_not_overflow_month_navigation(bool maximum)
    {
        using var host = new DialogHost();
        var date = maximum ? DateOnly.MaxValue : DateOnly.MinValue;
        var picker = new MaterialDatePicker { MinimumDate = date, MaximumDate = date, DisplayMonth = date, SelectedDate = date };
        picker.Show(host.Overlay); host.Render();
        Assert.True(picker.IsValid); Assert.False(picker.NavigateMonth(maximum ? 1 : -1));
        Assert.True(picker.Confirm());
    }

    [AvaloniaFact]
    public void Cancel_veto_back_detach_and_repeat_confirmation_never_publish_an_unconfirmed_draft()
    {
        using var host = new DialogHost(); host.Entry.Focus();
        var picker = new MaterialDatePicker { SelectedDate = new(2024, 2, 29), DisplayMonth = new(2024, 2, 1) };
        var session = picker.Show(host.Overlay); host.Render();
        var count = 0; session.Closed += (_, _) => count++;
        session.Closing += Veto;
        Assert.False(picker.Confirm()); Assert.True(session.IsOpen); Assert.Equal(0, count);
        session.Closing -= Veto;
        Assert.True(host.Overlay.RequestBack()); Assert.Null(session.Completion.Result.Value); Assert.True(host.Entry.IsFocused);
        Assert.Equal(1, count); Assert.False(picker.Confirm());
        var second = picker.Show(host.Overlay); host.Render();
        host.Window.Content = null; host.Render();
        Assert.Equal(MaterialOverlayCloseReason.HostDetached, second.Completion.Result.Reason);
        Assert.Null(second.Completion.Result.Value);
        static void Veto(object? sender, MaterialOverlayClosingEventArgs e) => e.Cancel = true;
    }

    [AvaloniaTheory]
    [InlineData(false, "12", "00", false, 0, 0)]
    [InlineData(false, "12", "00", true, 12, 0)]
    [InlineData(false, "02", "07", true, 14, 7)]
    [InlineData(true, "23", "59", false, 23, 59)]
    [InlineData(true, "٢٣", "٥٩", false, 23, 59)]
    public void Native_time_fields_match_midnight_noon_period_and_digit_vectors(bool hour24, string hour, string minute, bool pm, int expectedHour, int expectedMinute)
    {
        using var host = new DialogHost();
        var picker = new MaterialTimePicker { Mode = MaterialTimePickerMode.Input, Is24Hour = hour24 };
        var session = picker.Show(host.Overlay); host.Render();
        picker.HourInput.Focus(); host.Window.KeyTextInput(hour); host.Render();
        picker.MinuteInput.Focus(); host.Window.KeyTextInput(minute); host.Render();
        if (!hour24) picker.SetPeriod(pm);
        Assert.True(picker.IsValid);
        Assert.True(picker.Confirm());
        Assert.Equal(new TimeOnly(expectedHour, expectedMinute), session.Completion.Result.Value);
    }

    [AvaloniaFact]
    public void Time_limits_include_overnight_endpoints_and_invalid_native_edits_cannot_submit_stale_values()
    {
        using var host = new DialogHost();
        var picker = new MaterialTimePicker { Mode = MaterialTimePickerMode.Input, Is24Hour = true,
            MinimumTime = new(22, 0), MaximumTime = new(2, 0), SelectedTime = new(23, 59) };
        picker.Show(host.Overlay); host.Render();
        Assert.True(picker.IsValid);
        picker.HourInput.Focus(); picker.HourInput.SelectAll(); host.Window.KeyTextInput("24"); host.Render();
        Assert.False(picker.IsValid); Assert.False(picker.Confirm()); Assert.Equal("24", picker.HourInput.Text);
        picker.SelectedTime = new(2, 0); host.Render(); Assert.True(picker.IsValid);
        picker.SelectedTime = new(2, 1); host.Render(); Assert.False(picker.IsValid);
        picker.SelectedTime = new(22, 0); host.Render(); Assert.True(picker.IsValid);
        picker.SelectedTime = new(22, 0, 1); host.Render(); Assert.False(picker.IsValid);
        picker.SelectedTime = new(23, 59); host.Render();
        picker.MinuteInput.Focus(); picker.MinuteInput.SelectAll(); host.Window.KeyTextInput("60"); host.Render();
        Assert.False(picker.IsValid); Assert.False(picker.Confirm());
    }

    [AvaloniaFact]
    public void Changing_culture_while_date_is_invalid_preserves_the_native_draft_for_correction()
    {
        using var host = new DialogHost();
        var picker = new MaterialDatePicker { Mode = MaterialDatePickerMode.Input, InputFormat = "MM/dd/yyyy", Culture = CultureInfo.GetCultureInfo("en-US") };
        picker.Show(host.Overlay); host.Render();
        picker.StartInput.Focus(); host.Window.KeyTextInput("13/40/2024"); host.Render();
        picker.Culture = CultureInfo.GetCultureInfo("en-GB"); host.Render();
        Assert.Equal("13/40/2024", picker.StartInput.Text);
        Assert.False(picker.IsValid);
    }

    [AvaloniaFact]
    public void Touch_clock_drag_can_choose_non_tick_minute_and_cancel_restores_the_draft()
    {
        using var host = new DialogHost();
        var picker = new MaterialTimePicker { SelectedTime = new TimeOnly(14, 7), Is24Hour = true, ActivePart = MaterialTimePickerPart.Minute };
        picker.Show(host.Overlay); host.Render();
        var dial = picker.GetVisualDescendants().OfType<MaterialClockDial>().Single();
        var outcomes = new List<string>();
        dial.ValueSelected += (_, e) => outcomes.Add($"{e.Value}/{e.Complete}/{e.Cancelled}");
        var center = dial.TranslatePoint(new Point(128, 128), host.Window)!.Value;
        using (var touch = host.Window.TouchBegin(center))
        {
            host.Render();
            host.Window.TouchMove(touch, center + new Vector(-10.56, -100.45)); host.Render();
            Assert.Equal(new TimeOnly(14, 59), picker.SelectedTime);
        }
        host.Render();
        Assert.Equal(new TimeOnly(14, 7), picker.SelectedTime);
        using var finish = host.Window.TouchBegin(center);
        host.Render();
        host.Window.TouchMove(finish, center + new Vector(-10.56, -100.45)); host.Render();
        host.Window.TouchEnd(finish, center + new Vector(-10.56, -100.45)); host.Render();
        Assert.True(picker.SelectedTime == new TimeOnly(14, 59), string.Join(", ", outcomes));
    }

    [AvaloniaFact]
    public void Analog_clock_has_two_hour_rings_and_native_keyboard_reaches_every_minute()
    {
        using var host = new DialogHost();
        var picker = new MaterialTimePicker { Is24Hour = true, SelectedTime = new TimeOnly(0, 0) };
        picker.Show(host.Overlay); host.Render();
        var hour = picker.GetVisualDescendants().OfType<MaterialClockNumber>().Single(n => n.Value == 23);
        host.Click(hour); host.Render();
        Assert.Equal(new TimeOnly(23, 0), picker.SelectedTime);
        Assert.Equal(MaterialTimePickerPart.Minute, picker.ActivePart);
        var minute = picker.GetVisualDescendants().OfType<MaterialClockNumber>().Single(n => n.Value == 55);
        minute.Focus();
        host.Key(PhysicalKey.ArrowRight); host.Render();
        Assert.Equal(new TimeOnly(23, 1), picker.SelectedTime);
        Assert.True(picker.Confirm());
    }

    [AvaloniaFact]
    public void Time_keyboard_errors_gate_confirmation_and_period_and_format_preserve_civil_time()
    {
        using var host = new DialogHost();
        var picker = new MaterialTimePicker { Mode = MaterialTimePickerMode.Input, SelectedTime = new TimeOnly(14, 7) };
        var session = picker.Show(host.Overlay); host.Render();
        Assert.Equal("02", picker.HourInput.Text);
        picker.HourInput.Focus(); picker.HourInput.SelectAll(); host.Window.KeyTextInput("99"); host.Render();
        Assert.False(picker.IsValid); Assert.False(picker.Confirm());
        Assert.Equal("99", picker.HourInput.Text);
        picker.HourInput.SelectAll(); host.Window.KeyTextInput("12"); host.Render();
        Assert.True(picker.IsValid);
        Assert.Equal(new TimeOnly(12, 7), picker.SelectedTime);
        picker.SetPeriod(false);
        Assert.Equal(new TimeOnly(0, 7), picker.SelectedTime);
        picker.Is24Hour = true; host.Render();
        Assert.Equal("00", picker.HourInput.Text);
        picker.HourInput.SelectAll(); host.Window.KeyTextInput("23"); host.Render();
        Assert.True(picker.Confirm());
        Assert.Equal(new TimeOnly(23, 7), session.Completion.Result.Value);
    }

    [AvaloniaFact]
    public void Docked_calendar_range_crosses_month_and_revalidates_live_limits_without_a_modal()
    {
        using var host = new DialogHost();
        var picker = new MaterialDatePicker { SelectionMode = MaterialDateSelectionMode.Range,
            DisplayMonth = new DateOnly(2024, 2, 1), MinimumDate = new DateOnly(2024, 2, 1),
            MaximumDate = new DateOnly(2024, 3, 2), Culture = CultureInfo.GetCultureInfo("en-GB") };
        ((StackPanel)host.Overlay.Content!).Children.Add(picker); host.Render();
        var start = picker.GetVisualDescendants().OfType<MaterialCalendarDay>().Single(d => d.Date == new DateOnly(2024, 2, 29));
        host.Click(start);
        Assert.False(picker.IsValid);
        host.Click(picker.GetVisualDescendants().OfType<MaterialButton>().Single(b => ControlAutomationPeer.CreatePeerForElement(b).GetName() == "Next month"));
        var end = picker.GetVisualDescendants().OfType<MaterialCalendarDay>().Single(d => d.Date == new DateOnly(2024, 3, 2));
        host.Click(end);
        Assert.True(picker.IsValid);
        Assert.Equal(new DateOnly(2024, 2, 29), picker.SelectedDate);
        Assert.Equal(new DateOnly(2024, 3, 2), picker.RangeEnd);
        Assert.Equal(0, host.Overlay.OpenCount);
        picker.MaximumDate = new DateOnly(2024, 3, 1);
        Assert.False(picker.IsValid);
        Assert.False(end.IsEnabled);
    }

    [AvaloniaFact]
    public void Native_date_input_corrects_leap_error_and_commits_only_after_confirmation()
    {
        using var host = new DialogHost();
        DateOnly? saved = new(2024, 1, 1);
        var picker = new MaterialDatePicker
        {
            Mode = MaterialDatePickerMode.Input, Culture = CultureInfo.GetCultureInfo("en-US"),
            InputFormat = "MM/dd/yyyy", SelectedDate = saved
        };
        var session = picker.Show(host.Overlay);
        session.Closed += (_, result) => { if (result.Reason == MaterialOverlayCloseReason.Confirmed) saved = (DateOnly)result.Value!; };
        host.Render();
        picker.StartInput.Focus(); picker.StartInput.SelectAll();
        host.Window.KeyTextInput("02/29/2023"); host.Render();
        Assert.False(picker.IsValid);
        Assert.False(picker.Confirm());
        Assert.Equal(new DateOnly(2024, 1, 1), saved);
        Assert.Equal("02/29/2023", picker.StartInput.Text);
        Assert.True(picker.StartInput.HasError);
        picker.StartInput.SelectAll(); host.Window.KeyTextInput("02/29/2024"); host.Render();
        Assert.True(picker.IsValid);
        Assert.Equal(new DateOnly(2024, 2, 29), picker.SelectedDate);
        Assert.True(picker.Confirm());
        Assert.Equal(new DateOnly(2024, 2, 29), saved);
    }
}
