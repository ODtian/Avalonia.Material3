using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Tokens;
using Avalonia.VisualTree;
using Avalonia.Input;
using Avalonia.Headless;
using Xunit;

namespace Avalonia.Material3.Tests;

public class NormativeSecondaryMotionTests
{
    [AvaloniaFact]
    public async Task Pointer_press_expands_from_its_origin_before_reaching_the_far_edge()
    {
        using var host = new ButtonHost();
        host.Theme.Motion = new MaterialMotion();
        var button = new MaterialButton { Width = 180, Height = 48, Content = "", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left };
        host.Window.Content = new StackPanel { Margin = new Thickness(24), Children = { button } };
        host.Capture();
        var start = button.TranslatePoint(new Point(12, 24), host.Window)!.Value;
        var far = button.TranslatePoint(new Point(164, 24), host.Window)!.Value;
        host.Window.MouseMove(start);
        await Task.Delay(35);
        host.Capture();
        var rest = host.PixelAt(far);
        host.Window.MouseDown(start, MouseButton.Left);
        await Task.Delay(35);
        host.Capture();
        Assert.Equal(rest, host.PixelAt(far));
        await Task.Delay(230);
        host.Capture();
        Assert.NotEqual(rest, host.PixelAt(far));
        host.Window.MouseUp(start, MouseButton.Left);
    }
    private static MaterialMotion SlowSpatial() => new()
    {
        DurationShort4 = TimeSpan.FromMilliseconds(20),
        Springs = MaterialSpringScheme.Expressive with { FastSpatial = new(1, 100), DefaultSpatial = new(1, 100), FastEffects = new(1, 100) }
    };

    [AvaloniaFact]
    public async Task Radio_selection_paints_a_growing_dot_using_the_spatial_spring()
    {
        using var host = new ButtonHost();
        host.Theme.Motion = SlowSpatial();
        var radio = new MaterialRadioButton { Width = 48, Height = 48, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left };
        host.Window.Content = new StackPanel { Children = { radio } };
        host.Capture();
        var bounds = radio.Bounds;
        var center = radio.TranslatePoint(new Point(24, 24), host.Window)!.Value;
        var edge = radio.TranslatePoint(new Point(27, 24), host.Window)!.Value;
        var centerRest = host.PixelAt(center);
        var edgeRest = host.PixelAt(edge);
        radio.IsChecked = true;
        await Task.Delay(120);
        host.Capture();
        Assert.NotEqual(centerRest, host.PixelAt(center));
        Assert.Equal(edgeRest, host.PixelAt(edge));
        Assert.Equal(bounds, radio.Bounds);
        Assert.True(radio.IsChecked);
    }

    [AvaloniaFact]
    public async Task Switch_travel_follows_FastSpatial_rather_than_a_short_duration_tween()
    {
        using var host = new ButtonHost();
        host.Theme.Motion = SlowSpatial();
        var toggle = new MaterialSwitch { Width = 64, Height = 48, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left };
        host.Window.Content = new StackPanel { Children = { toggle } };
        host.Capture();
        toggle.IsChecked = true;
        await Task.Delay(100);
        host.Capture();
        var near = toggle.TranslatePoint(new Point(24, 24), host.Window)!.Value;
        var far = toggle.TranslatePoint(new Point(44, 24), host.Window)!.Value;
        Assert.Equal(Avalonia.Media.Colors.White, host.PixelAt(near));
        Assert.Equal(((Avalonia.Media.ISolidColorBrush)toggle.Background!).Color, host.PixelAt(far));
        Assert.True(toggle.IsChecked);
    }

    [AvaloniaFact]
    public async Task Field_label_follows_FastSpatial_and_keeps_native_editor_bounds()
    {
        using var host = new ButtonHost();
        host.Theme.Motion = SlowSpatial();
        var field = new MaterialTextField { Variant = MaterialTextFieldVariant.Outlined, Label = "Reference label" };
        host.Window.Content = new StackPanel { Children = { field } };
        host.Capture();
        var editor = field.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.TextPresenter>().Single();
        var editorBounds = new Rect(editor.Bounds.Size).TransformToAABB(editor.TransformToVisual(field)!.Value);
        var resting = field.GetVisualDescendants().OfType<TextBlock>().Single(text => text.Text == field.Label && text.IsEffectivelyVisible);
        var initialColor = ((Avalonia.Media.ISolidColorBrush)resting.Foreground!).Color;
        field.Focus();
        var focusedColor = ((Avalonia.Media.ISolidColorBrush)field.BorderBrush!).Color;
        await Task.Delay(100);
        host.Capture();
        var label = field.GetVisualDescendants().OfType<TextBlock>().Single(text => text.Text == field.Label && text.IsEffectivelyVisible);
        var shown = new Rect(label.Bounds.Size).TransformToAABB(label.TransformToVisual(field)!.Value);
        Assert.InRange(shown.Top, 10, 22);
        var shownColor = ((Avalonia.Media.ISolidColorBrush)label.Foreground!).Color;
        Assert.NotEqual(initialColor, shownColor);
        Assert.NotEqual(focusedColor, shownColor);
        Assert.Equal(editorBounds, new Rect(editor.Bounds.Size).TransformToAABB(editor.TransformToVisual(field)!.Value));
    }
}
