using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Runtime.InteropServices;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Material3.Tests;

public class NormativePickerMotionTests
{
    [AvaloniaFact]
    public async Task Clock_part_switch_crossfades_its_face_and_moves_the_hand_through_the_short_arc()
    {
        var picker = new MaterialTimePicker { SelectedTime = new(3, 0) };
        using var host = new GeometryHost(picker, 400, 700);
        var dial = picker.GetVisualDescendants().OfType<MaterialClockDial>().Single();
        var box = GeometryHost.Box(dial, host.Window);
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with {
            DefaultSpatial = new(.8, 100), DefaultEffects = new(1, 100) } }; host.Render();
        Assert.Equal(Color.Parse("#6750A4"), host.Pixel(box.Left + 229, box.Top + 145));
        picker.ActivePart = MaterialTimePickerPart.Minute;
        Assert.Equal(new TimeOnly(3, 0), picker.SelectedTime);
        var numbers = dial.GetVisualDescendants().OfType<MaterialClockNumber>().ToArray();
        Assert.All(numbers, number => Assert.Equal(0, number.Opacity));
        var crossed = false;
        var faded = false;
        var observed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        TimeSpan? started = null;
        var frames = new List<string>();
        using var bitmap = new RenderTargetBitmap(new PixelSize(256, 256), new Vector(96, 96));
        using var pixels = new WriteableBitmap(new PixelSize(256, 256), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        void Sample(TimeSpan time)
        {
            started ??= time;
            // Render the current displayed dial once per actual window frame. Pixel reads
            // from this image leave the animation clock at that same presented phase.
            bitmap.Render(dial);
            using var storage = pixels.Lock(); bitmap.CopyPixels(storage);
            var offset = 57 * storage.RowBytes + 199 * 4;
            var color = Color.FromRgb(Marshal.ReadByte(storage.Address, offset + 2),
                Marshal.ReadByte(storage.Address, offset + 1), Marshal.ReadByte(storage.Address, offset));
            if (color == Color.Parse("#6750A4")) crossed = true;
            if (numbers[0].Opacity is > 0 and < 1) faded = true;
            frames.Add($"{(time - started.Value).TotalMilliseconds:F0}:{color}:{numbers[0].Opacity:F3}");
            if (time - started.Value >= TimeSpan.FromMilliseconds(700)) observed.TrySetResult();
            else host.Window.RequestAnimationFrame(Sample);
        }
        host.Window.RequestAnimationFrame(Sample); host.Render();
        await observed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(crossed, "Hand must paint the short upper-right arc. " + string.Join(";", frames)); Assert.True(faded, "Minute face must paint intermediate opacity.");
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = true }; host.Render();
        Assert.Equal(Color.Parse("#6750A4"), host.Pixel(box.Left + 146, box.Top + 27));
    }

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
