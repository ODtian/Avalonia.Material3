using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.Styling;
using Xunit;

namespace Avalonia.Material3.Tests;

public class ButtonScenarioTests
{
    [AvaloniaFact]
    public void Keyboard_focus_ring_is_separated_from_the_button_container()
    {
        using var host = new ButtonHost();
        host.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        host.Window.KeyReleaseQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        var leftGap = host.Button.TranslatePoint(new Point(4, host.Button.Bounds.Height / 2), host.Window)!.Value;
        var rightGap = host.Button.TranslatePoint(new Point(host.Button.Bounds.Width - 5, host.Button.Bounds.Height / 2), host.Window)!.Value;
        Assert.Equal(Color.Parse("#FEF7FF"), host.PixelAt(leftGap));
        Assert.Equal(Color.Parse("#FEF7FF"), host.PixelAt(rightGap));
    }

    [AvaloniaFact]
    public void Host_updates_initial_design_tokens_on_existing_content()
    {
        using var host = new ButtonHost();
        host.Theme.LightColorScheme = MaterialColorScheme.Light with { Primary = Color.Parse("#006C4C") };
        host.Theme.Typography = new MaterialTypography { FontFamily = new FontFamily("Arial"), Scale = 1.5 };
        host.Theme.Shapes = new MaterialShapes { ButtonCornerRadius = 12, PressedButtonCornerRadius = 4 };
        host.Theme.Motion = new MaterialMotion { ReduceMotion = true };
        var initial = host.Capture();
        Assert.Equal(Color.Parse("#006C4C"), Assert.IsAssignableFrom<ISolidColorBrush>(host.Button.Background).Color);
        Assert.Equal(21, host.Button.FontSize);
        Assert.Equal(new FontFamily("Arial"), host.Button.FontFamily);
        Assert.Equal(new CornerRadius(12), host.Button.CornerRadius);
        Assert.True(host.Button.TryFindResource("M3.StateLayerDuration", out var duration));
        Assert.Equal(TimeSpan.Zero, duration);
        host.Window.MouseDown(host.Center, MouseButton.Left);
        Assert.Equal(new CornerRadius(4), host.Button.CornerRadius);
        Assert.NotEqual(initial, host.Capture());
        host.Window.MouseUp(host.Center, MouseButton.Left);
        Assert.Equal("Action completed", host.Result.Text);
    }

    [AvaloniaFact]
    public void Disabled_button_is_distinct_and_cannot_activate()
    {
        using var host = new ButtonHost();
        var enabled = host.Capture();
        host.Button.IsEnabled = false;
        Assert.NotEqual(enabled, host.Capture());
        var disabled = Assert.IsAssignableFrom<ISolidColorBrush>(host.Button.Background);
        Assert.Equal(Color.Parse("#1D1B20"), disabled.Color);
        Assert.Equal(0.1, disabled.Opacity);
        host.Window.MouseDown(host.Center, MouseButton.Left);
        host.Window.MouseUp(host.Center, MouseButton.Left);
        host.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        Assert.False(host.Button.IsFocused);
        host.Window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Assert.Equal("Waiting", host.Result.Text);
    }

    [AvaloniaFact]
    public void Pointer_user_sees_hover_and_pressed_feedback_before_release()
    {
        using var host = new ButtonHost();
        var idle = host.Capture();
        host.Window.MouseMove(host.Center);
        var hovered = host.Capture();
        Assert.NotEqual(idle, hovered);
        host.Window.MouseDown(host.Center, MouseButton.Left);
        Assert.True(host.Button.IsPressed);
        Assert.NotEqual(hovered, host.Capture());
        Assert.Equal("Waiting", host.Result.Text);
        host.Window.MouseUp(host.Center, MouseButton.Left);
        Assert.Equal("Action completed", host.Result.Text);
    }

    [AvaloniaFact]
    public void Keyboard_user_sees_focus_and_activates_with_space()
    {
        using var host = new ButtonHost();
        var idle = host.Capture();
        host.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        host.Window.KeyReleaseQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        Assert.True(host.Button.IsFocused);
        Assert.NotEqual(idle, host.Capture());
        host.Window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);
        Assert.True(host.Button.IsPressed);
        Assert.Equal("Waiting", host.Result.Text);
        host.Window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None);
        Assert.False(host.Button.IsPressed);
        Assert.Equal("Action completed", host.Result.Text);
    }

    [AvaloniaFact]
    public void Existing_button_follows_host_light_dark_and_light_changes()
    {
        using var host = new ButtonHost();
        var light = host.Capture();
        host.Window.RequestedThemeVariant = ThemeVariant.Dark;
        var dark = host.Capture();
        Assert.Equal(Color.Parse("#D0BCFF"), Assert.IsAssignableFrom<ISolidColorBrush>(host.Button.Background).Color);
        Assert.Equal(Color.Parse("#381E72"), Assert.IsAssignableFrom<ISolidColorBrush>(host.Button.Foreground).Color);
        Assert.NotEqual(light, dark);
        host.Window.RequestedThemeVariant = ThemeVariant.Light;
        Assert.Equal(light, host.Capture());
    }

    [AvaloniaFact]
    public void Host_loads_a_filled_button_and_receives_pointer_feedback()
    {
        using var host = new ButtonHost();
        host.Window.MouseDown(host.Center, MouseButton.Left);
        host.Window.MouseUp(host.Center, MouseButton.Left);
        Assert.Equal("Action completed", host.Result.Text);
        Assert.Equal(Color.Parse("#6750A4"), Assert.IsAssignableFrom<ISolidColorBrush>(host.Button.Background).Color);
        Assert.True(host.Button.Bounds.Height >= 48);
    }
}
