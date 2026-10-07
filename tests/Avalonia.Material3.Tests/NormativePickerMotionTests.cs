using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Material3.Tests;

public class NormativePickerMotionTests
{
    [AvaloniaTheory]
    [InlineData(MaterialDateSelectionMode.Single)]
    [InlineData(MaterialDateSelectionMode.Range)]
    public async Task Date_mode_transition_uses_single_slide_and_range_crossfade_without_changing_the_draft(MaterialDateSelectionMode selection)
    {
        var picker = new MaterialDatePicker { DisplayMonth = new(2024, 2, 1), SelectedDate = new(2024, 2, 7),
            RangeEnd = selection == MaterialDateSelectionMode.Range ? new(2024, 2, 16) : null, SelectionMode = selection };
        using var host = new GeometryHost(picker, 360, 900);
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with {
            DefaultSpatial = new(.8, 100), DefaultEffects = new(1, 100), FastEffects = new(1, 100) } }; host.Render();
        picker.Mode = MaterialDatePickerMode.Input; host.Window.UpdateLayout();
        double Alpha() => picker.StartInput.GetVisualAncestors().Prepend(picker.StartInput).Aggregate(1d, (alpha, visual) => alpha * visual.Opacity);
        Assert.Equal(0, Alpha());
        var alphas = new List<double>(); var tops = new List<double>();
        for (var frame = 0; frame < 22; frame++)
        {
            await Task.Delay(16); host.Render(); alphas.Add(Alpha()); tops.Add(GeometryHost.Box(picker.StartInput, host.Window).Top);
        }
        Assert.Contains(alphas, alpha => alpha > 0 && alpha < 1);
        if (selection == MaterialDateSelectionMode.Single) Assert.True(tops.Distinct().Count() > 2);
        else Assert.Single(tops.Distinct());
        Assert.Equal(new DateOnly(2024, 2, 7), picker.SelectedDate);
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = true }; host.Render();
        Assert.Equal(1, Alpha());
    }

    [AvaloniaFact]
    public async Task Calendar_day_selection_changes_semantics_immediately_and_paints_default_effects_color_frames()
    {
        var picker = new MaterialDatePicker { DisplayMonth = new(2024, 2, 1), SelectedDate = new(2024, 2, 3),
            Culture = System.Globalization.CultureInfo.GetCultureInfo("en-US") };
        using var host = new GeometryHost(picker, 360, 640);
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with { DefaultEffects = new(1, 100) } }; host.Render();
        var day = picker.GetVisualDescendants().OfType<MaterialCalendarDay>().Single(d => d.Date.Day == 7);
        var box = GeometryHost.Box(day, host.Window);
        picker.SelectDate(new(2024, 2, 7));
        Assert.True(day.IsChecked); Assert.Equal(new DateOnly(2024, 2, 7), picker.SelectedDate);
        Assert.NotEqual(Color.Parse("#6750A4"), host.Pixel(box.Left + 12, box.Center.Y));
        var frames = new HashSet<Color>();
        for (var frame = 0; frame < 25; frame++)
        {
            await Task.Delay(16); frames.Add(host.Pixel(box.Left + 12, box.Center.Y));
        }
        Assert.True(frames.Count > 2);
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = true }; host.Render();
        Assert.Equal(Color.Parse("#6750A4"), host.Pixel(box.Left + 12, box.Center.Y));
    }
}
