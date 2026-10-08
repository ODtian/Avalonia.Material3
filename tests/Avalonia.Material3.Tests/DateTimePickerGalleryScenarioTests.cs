using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Gallery.Pages;
using Xunit;

namespace Avalonia.Material3.Tests;

public class DateTimePickerGalleryScenarioTests
{
    [AvaloniaTheory]
    [InlineData(900, 1, false)]
    [InlineData(320, 2, true)]
    public void Package_gallery_executes_all_eight_modal_forms_and_adapts_live_theme_font_and_viewport(int width, int scale, bool dark)
    {
        using var host = new DialogHost(width, 800);
        host.Theme.Typography = host.Theme.Typography with { Scale = scale };
        host.Window.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
        var page = new DateTimePickersPage(host.Theme); host.Window.Content = page; host.Render();
        foreach (var id in new[] { "DateSingleEntry", "DateRangeEntry", "DateInputEntry", "DateRangeInputEntry", "Clock12Entry", "Clock24Entry", "Time12Entry", "Time24Entry" })
        {
            var entry = page.GetVisualDescendants().OfType<MaterialButton>().Single(b => AutomationProperties.GetAutomationId(b) == id);
            entry.BringIntoView(); host.Render(); host.Click(entry); host.Render();
            Assert.Equal(1, page.Overlay.OpenCount);
            var session = page.ActiveDatePicker?.Session ?? page.ActiveTimePicker!.Session!;
            if (page.ActiveDatePicker is { } date)
            {
                if (date.Mode == MaterialDatePickerMode.Input)
                {
                    date.StartInput.BringIntoView(); host.Render(); date.StartInput.Focus(); date.StartInput.SelectAll();
                    host.Window.KeyTextInput("02/29/2024"); host.Render();
                }
                Assert.True(date.IsValid);
                Assert.Contains(date.StartInput, date.Surface.GetVisualDescendants());
            }
            else
            {
                var time = page.ActiveTimePicker!;
                if (time.Mode == MaterialTimePickerMode.Input)
                {
                    time.HourInput.BringIntoView(); host.Render(); time.HourInput.Focus(); time.HourInput.SelectAll();
                    host.Window.KeyTextInput(time.Is24Hour ? "23" : "11"); host.Render();
                    time.MinuteInput.BringIntoView(); host.Render(); time.MinuteInput.Focus(); time.MinuteInput.SelectAll();
                    host.Window.KeyTextInput("59"); host.Render();
                    if (!time.Is24Hour) time.SetPeriod(true);
                }
                else { time.SelectHour(23); time.SelectMinute(59); }
                Assert.True(time.IsValid);
            }
            Save(host.Window, $"m3-17-{id}-{width}-font{scale}00-{(dark ? "dark" : "light")}.png");
            var confirm = page.Overlay.GetVisualDescendants().OfType<MaterialButton>()
                .Single(b => b.IsEffectivelyVisible && ControlAutomationPeer.CreatePeerForElement(b).GetName() == "OK");
            confirm.BringIntoView(); host.Render();
            var center = confirm.TranslatePoint(new Point(confirm.Bounds.Width / 2, confirm.Bounds.Height / 2), host.Window)!.Value;
            Assert.True(center.Y is >= 0 and <= 800, $"{id}: confirm center Y={center.Y}, bounds={confirm.Bounds}, host={host.Window.ClientSize}.");
            host.Click(confirm); host.Render();
            Assert.Equal(MaterialOverlayCloseReason.Confirmed, session.Completion.Result.Reason);
            Assert.True(entry.IsFocused);
            if (id.Contains("Time") || id.Contains("Clock")) Assert.Equal(new TimeOnly(23, 59), page.SavedTime);
        }
        var saved = page.SavedDate;
        page.OpenDate(MaterialDateSelectionMode.Single, MaterialDatePickerMode.Input); host.Render();
        page.ActiveDatePicker!.StartInput.Focus(); page.ActiveDatePicker.StartInput.SelectAll(); host.Window.KeyTextInput("02/28/2024"); host.Render();
        host.Key(PhysicalKey.Escape); host.Render();
        Assert.Equal(saved, page.SavedDate);
        Assert.Equal("Escape", page.LastResult);
        Assert.Equal(0, page.Overlay.OpenCount);
    }
    private static void Save(Window window, string name)
    {
        if (Environment.GetEnvironmentVariable("M3_ISSUE18_SCREENSHOTS") is not { } path) return;
        Directory.CreateDirectory(path);
        using var frame = window.CaptureRenderedFrame();
        frame!.Save(Path.Combine(path, name), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
    }
}
