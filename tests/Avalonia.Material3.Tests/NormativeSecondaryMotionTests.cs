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
    public async Task Refresh_indicator_travels_to_its_threshold_before_fully_entering_the_viewport()
    {
        using var host = new ButtonHost();
        host.Theme.Motion = new MaterialMotion();
        var refresh = new MaterialPullToRefresh { Width = 240, Height = 160, Content = new Border { Background = Avalonia.Media.Brushes.Blue } };
        host.Window.Content = refresh;
        host.Capture();
        var nearThreshold = refresh.TranslatePoint(new Point(120, 70), host.Window)!.Value;
        var rest = host.PixelAt(nearThreshold);
        Assert.True(refresh.RequestRefresh());
        await Task.Delay(25);
        host.Capture();
        var early = host.PixelAt(nearThreshold);
        // The standard shadow can darken blue while the opaque indicator remains above this point.
        Assert.Equal(rest.R, early.R); Assert.Equal(rest.G, early.G);
        await Task.Delay(220);
        host.Capture();
        Assert.NotEqual(rest, host.PixelAt(nearThreshold));
        Assert.Equal(1, refresh.DistanceFraction);
    }

    [AvaloniaTheory]
    [InlineData(MaterialTopAppBarVariant.Small)]
    [InlineData(MaterialTopAppBarVariant.Large)]
    public async Task App_bar_color_uses_the_matching_one_or_two_row_source_recipe(MaterialTopAppBarVariant variant)
    {
        using var host = new ButtonHost();
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with { DefaultEffects = new(1, 100) } };
        var bar = new MaterialTopAppBar { Variant = variant, ScrollBehavior = MaterialAppBarScrollBehavior.ExitUntilCollapsed,
            Background = Avalonia.Media.Brushes.Black, ScrolledBackground = Avalonia.Media.Brushes.White };
        host.Window.Content = bar;
        host.Capture();
        bar.ApplyScrollDelta((bar.ExpandedHeight - bar.CollapsedHeight) / 2, 32);
        if (variant == MaterialTopAppBarVariant.Small) await Task.Delay(100);
        host.Capture();
        var color = ((Avalonia.Media.ISolidColorBrush)bar.CurrentBackground!).Color;
        // FastOutLinearIn(.5)=.324815, then black→white Oklab lerp gives sRGB#343434.
        Assert.InRange(color.R, variant == MaterialTopAppBarVariant.Small ? (byte)14 : (byte)48,
            variant == MaterialTopAppBarVariant.Small ? (byte)100 : (byte)56);
        Assert.Equal(color.R, color.G);
        Assert.Equal(color.G, color.B);
    }

    [AvaloniaFact]
    public async Task Disabled_checkbox_programmatic_selection_finishes_painting_its_mark()
    {
        using var host = new ButtonHost();
        host.Theme.Motion = new MaterialMotion();
        var checkbox = new MaterialCheckBox { IsEnabled = false, IsThreeState = true, Width = 48, Height = 48,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left };
        host.Window.Content = new StackPanel { Children = { checkbox } };
        host.Capture();
        checkbox.IsChecked = null;
        await Task.Delay(450);
        host.Capture();
        var mark = checkbox.TranslatePoint(new Point(24, 24), host.Window)!.Value;
        var box = checkbox.TranslatePoint(new Point(24, 21), host.Window)!.Value;
        Assert.NotEqual(host.PixelAt(box), host.PixelAt(mark));
        Assert.Null(checkbox.IsChecked);
    }

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

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Radio_selection_paints_a_growing_dot_using_the_spatial_spring(bool enabled)
    {
        using var host = new ButtonHost();
        host.Theme.Motion = SlowSpatial();
        var radio = new MaterialRadioButton { Width = 48, Height = 48, IsEnabled = enabled, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left };
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
