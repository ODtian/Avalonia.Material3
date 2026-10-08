using System.Globalization;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Material3.Controls;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Material3.Tests;

public class CalendarWeekdayScenarioTests
{
    [AvaloniaTheory]
    [InlineData(1)]
    [InlineData(2)]
    public void Weekday_text_wraps_its_line_height_and_is_centered_in_the_native_minimum_48_DIP_cell(double scale)
    {
        var picker = new MaterialDatePicker { Culture = CultureInfo.GetCultureInfo("en-US"), DisplayMonth = new(2024, 2, 1) };
        using var host = new GeometryHost(picker, 720, 1100);
        host.Theme.Typography = host.Theme.Typography with { Scale = scale }; host.Render();
        var label = picker.GetVisualDescendants().OfType<TextBlock>().Single(text => AutomationProperties.GetName(text) == "Sunday");
        Assert.Equal(24 * scale, label.Bounds.Height);
        var parent = Assert.IsAssignableFrom<Control>(label.GetVisualParent());
        Assert.True(parent.Bounds.Height >= 48);
        Assert.Equal(GeometryHost.Box(parent, host.Window).Center.Y, GeometryHost.Box(label, host.Window).Center.Y, 3);
    }

    [AvaloniaFact]
    public void Android_English_short_weekday_data_still_renders_the_native_narrow_labels()
    {
        var culture = new AndroidEnglishCulture();
        var picker = new MaterialDatePicker { Culture = culture, DisplayMonth = new(2024, 2, 1) };
        using var host = new GeometryHost(picker, 360, 800);
        var names = culture.DateTimeFormat.DayNames;
        var labels = picker.GetVisualDescendants().OfType<TextBlock>().Where(text => names.Contains(AutomationProperties.GetName(text))).ToArray();
        Assert.Equal(new[] { "S", "M", "T", "W", "T", "F", "S" }, labels.Select(text => text.Text));
        Assert.Equal(names, labels.Select(AutomationProperties.GetName));
    }

    [AvaloniaTheory]
    [InlineData("en-US", "S M T W T F S")]
    [InlineData("en-GB", "M T W T F S S")]
    [InlineData("zh-CN", "一 二 三 四 五 六 日")]
    public void Calendar_visible_weekdays_follow_native_narrow_labels_and_keep_complete_accessible_names(string cultureName, string expected)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var picker = new MaterialDatePicker { Culture = culture, DisplayMonth = new(2024, 2, 1) };
        using var host = new GeometryHost(picker, 360, 800);
        var format = culture.DateTimeFormat;
        var names = Enumerable.Range(0, 7).Select(index => format.GetDayName((DayOfWeek)(((int)format.FirstDayOfWeek + index) % 7))).ToArray();
        var labels = picker.GetVisualDescendants().OfType<TextBlock>().Where(text => names.Contains(AutomationProperties.GetName(text))).ToArray();
        Assert.Equal(7, labels.Length);
        Assert.Equal(expected.Split(' '), labels.Select(text => text.Text));
        Assert.Equal(names, labels.Select(AutomationProperties.GetName));
        Assert.All(labels, label => Assert.True(label.IsEffectivelyVisible && label.Bounds.Width >= 48));
    }

    private sealed class AndroidEnglishCulture() : CultureInfo("en-US")
    {
        public override DateTimeFormatInfo DateTimeFormat
        {
            get { var format = base.DateTimeFormat; format.ShortestDayNames = ["Su", "Mo", "Tu", "We", "Th", "Fr", "Sa"]; return format; }
            set => base.DateTimeFormat = value;
        }
    }
}
