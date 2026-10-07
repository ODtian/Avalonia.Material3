using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Avalonia.Media;
using Avalonia.Material3.Tokens;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Material3.Tests;

public class ReviewFixTrajectoryScenarioTests
{
    [AvaloniaFact]
    public void Picker_focus_keeps_the_reference_surface_outline_and_uses_opacity_indication()
    {
        Control[] controls = [new MaterialCalendarYear { Content = "2024" }, new MaterialTimeSelector { Content = "07" },
            new MaterialClockNumber { Content = "12" }];
        foreach (var control in controls)
        {
            using var host = new GeometryHost(control, 120, 96);
            var styled = (Avalonia.Controls.Primitives.TemplatedControl)control;
            var outline = styled.BorderThickness;
            control.Focus(NavigationMethod.Tab); host.Render();
            Assert.Equal(outline, styled.BorderThickness);
        }
    }

    [AvaloniaFact]
    public async Task Fab_menu_trigger_color_tracks_its_fast_spatial_geometry_progress()
    {
        var menu = new MaterialFabMenu();
        using var host = new GeometryHost(menu, 300, 300);
        var trigger = menu.GetVisualDescendants().OfType<MaterialFab>().Single();
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with { FastSpatial = new(1, 100) } };
        host.Render();
        var point = trigger.TranslatePoint(new Point(12, trigger.Bounds.Height / 2), host.Window)!.Value;
        var original = host.Pixel(point.X, point.Y);
        menu.IsExpanded = true; host.Render();
        var entering = host.Pixel(point.X, point.Y);
        await Task.Delay(80); host.Render();
        var middle = host.Pixel(point.X, point.Y);
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = true }; host.Render();
        var completed = host.Pixel(point.X, point.Y);
        Assert.NotEqual(completed, entering);
        Assert.NotEqual(original, middle); Assert.NotEqual(completed, middle);
    }
}
