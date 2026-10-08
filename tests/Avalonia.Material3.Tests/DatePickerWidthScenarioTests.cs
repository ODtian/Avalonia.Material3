using System.Globalization;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Material3.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using Gallery.Pages;
using Xunit;

namespace Avalonia.Material3.Tests;

// Public picker values, real input, host bounds and rendered-frame seams (docs/testing.md).
public class DatePickerWidthScenarioTests
{
    [AvaloniaTheory]
    [InlineData(1)]
    [InlineData(2)]
    public void Explicit_wide_range_retains_its_width_and_reflows_enlarged_headlines(double fontScale)
    {
        var picker = new MaterialDatePicker
        {
            SelectionMode = MaterialDateSelectionMode.Range, Width = 600,
            HorizontalAlignment = HorizontalAlignment.Left, Culture = CultureInfo.GetCultureInfo("en-US"),
            DisplayMonth = new(2024, 2, 1)
        };
        using var host = new GeometryHost(picker, 900, 800);
        host.Theme.Typography = host.Theme.Typography with { Scale = fontScale }; host.Render();
        picker.SelectDate(new(2024, 9, 11)); picker.SelectDate(new(2024, 9, 18)); host.Render();
        Assert.Equal(600, picker.Bounds.Width);
        var headlines = picker.GetVisualDescendants().OfType<TextBlock>()
            .Where(t => t.IsEffectivelyVisible && t.Text is "Sep 11, 2024" or "Sep 18, 2024").ToArray();
        Assert.Equal(2, headlines.Length);
        foreach (var headline in headlines)
            Assert.True(headline.TextLayout.Width <= headline.Bounds.Width + .01);
        picker.Mode = MaterialDatePickerMode.Input; host.Render();
        Assert.Equal(600, picker.Bounds.Width);
        Assert.Equal(picker.StartInput.Bounds.Width, picker.EndInput.Bounds.Width);
    }

    [AvaloniaFact]
    public void Cross_month_range_uses_the_native_actual_month_cell_edges_and_endpoint_halves()
    {
        var picker = new MaterialDatePicker
        {
            SelectionMode = MaterialDateSelectionMode.Range, Width = 360,
            Culture = CultureInfo.GetCultureInfo("en-US"), DisplayMonth = new(2024, 2, 1),
            SelectedDate = new(2024, 2, 29), RangeEnd = new(2024, 3, 2)
        };
        using var host = new GeometryHost(picker, 360, 800);
        Rect Day(DateOnly date) => GeometryHost.Box(picker.GetVisualDescendants().OfType<MaterialCalendarDay>()
            .Single(day => day.Date == date), host.Window);
        var first = Day(new(2024, 2, 29)); var middle = Day(new(2024, 3, 1)); var last = Day(new(2024, 3, 2));
        var band = Color.Parse("#E8DEF8"); var surface = Color.Parse("#ECE6F0");
        // Pinned SelectedRangeInfo clips to Feb's last actual cell and Mar's first actual cell.
        Assert.Equal(band, host.Pixel(first.Right - 1, first.Top + 5));
        Assert.Equal(surface, host.Pixel(first.Right + 1, first.Top + 5));
        Assert.Equal(surface, host.Pixel(middle.Left - 1, middle.Top + 5));
        Assert.Equal(band, host.Pixel(middle.Left + 1, middle.Top + 5));
        Assert.Equal(band, host.Pixel(last.Left + 14, last.Top + 5));
        Assert.Equal(surface, host.Pixel(last.Left + 34, last.Top + 5));
    }

    [AvaloniaTheory]
    [InlineData(MaterialDateSelectionMode.Single, 900, 360)]
    [InlineData(MaterialDateSelectionMode.Range, 900, 360)]
    [InlineData(MaterialDateSelectionMode.Single, 280, 280)]
    [InlineData(MaterialDateSelectionMode.Range, 280, 280)]
    public void Auto_width_stays_at_the_native_calendar_container_when_selection_headlines_change(
        MaterialDateSelectionMode selection, double hostWidth, double expectedWidth)
    {
        var picker = new MaterialDatePicker
        {
            SelectionMode = selection, HorizontalAlignment = HorizontalAlignment.Left,
            Culture = CultureInfo.GetCultureInfo("en-US"), DisplayMonth = new(2024, 2, 1)
        };
        using var host = new GeometryHost(picker, hostWidth, 720);
        Assert.Equal(expectedWidth, picker.Bounds.Width);
        foreach (var date in new DateOnly[] { new(2024, 2, 29), new(2024, 3, 2), new(2024, 9, 11) })
        {
            picker.SelectDate(date); host.Render();
            Assert.Equal(expectedWidth, picker.Bounds.Width);
        }
        picker.Mode = MaterialDatePickerMode.Input; host.Render();
        picker.StartInput.Focus(); picker.StartInput.SelectAll(); host.Window.KeyTextInput("09122024"); host.Render();
        Assert.Equal(new DateOnly(2024, 9, 12), picker.SelectedDate);
        Assert.Equal(expectedWidth, picker.Bounds.Width);
    }

    [AvaloniaFact]
    public void Gallery_docked_range_keeps_its_width_when_real_date_clicks_cross_a_month()
    {
        using var host = new DialogHost(900, 800);
        var page = new DateTimePickersPage(host.Theme);
        var picker = page.DockedRange;
        ((Panel)picker.Parent!).Children.Remove(picker);
        host.Window.Content = picker; host.Render();
        Assert.Equal(360, picker.Bounds.Width);
        foreach (var date in new DateOnly[] { new(2024, 2, 29), new(2024, 3, 2) })
        {
            var day = picker.GetVisualDescendants().OfType<MaterialCalendarDay>().Single(d => d.Date == date);
            day.BringIntoView(); host.Render();
            var point = day.TranslatePoint(new Point(24, 24), host.Window)!.Value;
            host.Window.MouseDown(point, MouseButton.Left); host.Window.MouseUp(point, MouseButton.Left); host.Render();
            Assert.Equal(360, picker.Bounds.Width);
        }
        Assert.Equal(new DateOnly(2024, 2, 29), picker.SelectedDate);
        Assert.Equal(new DateOnly(2024, 3, 2), picker.RangeEnd);
    }
}
