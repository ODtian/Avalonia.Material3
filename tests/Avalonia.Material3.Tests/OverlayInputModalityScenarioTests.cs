using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Material3.Controls;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Material3.Tests;

public class OverlayInputModalityScenarioTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Overlay_initial_and_restored_focus_keep_the_opening_input_modality(bool keyboard)
    {
        var entry = new MaterialButton { Content = "Open", Width = 100, Background = Brushes.Black, Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Top };
        var target = new MaterialButton { Content = "Action", Width = 100, Background = Brushes.Black, Foreground = Brushes.White };
        var overlay = new MaterialOverlayHost { Content = entry };
        using var host = new GeometryHost(overlay, 320, 240);
        MaterialOverlaySession? session = null;
        entry.Click += (_, _) => session = overlay.Show(target, new() { InitialFocus = target, ReturnFocus = entry });
        if (keyboard) { entry.Focus(NavigationMethod.Tab); host.Window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None); }
        else
        {
            var point = GeometryHost.Box(entry, host.Window).Center;
            host.Window.MouseDown(point, MouseButton.Left); host.Window.MouseUp(point, MouseButton.Left);
        }
        host.Window.MouseMove(new Point(300, 220)); host.Render();
        Assert.NotNull(session); Assert.True(target.IsFocused);
        var box = GeometryHost.Box(target, host.Window);
        Assert.Equal(keyboard, host.Pixel(box.Left + 8, box.Center.Y).R > 0);
        session.Dismiss(); host.Render(); Assert.True(entry.IsFocused);
        box = GeometryHost.Box(entry, host.Window);
        Assert.Equal(keyboard, host.Pixel(box.Left + 8, box.Center.Y).R > 0);
    }
}
