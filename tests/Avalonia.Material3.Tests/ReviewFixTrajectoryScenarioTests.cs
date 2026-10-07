using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Headless;
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

    [AvaloniaFact]
    public async Task Sheet_short_drag_settles_and_keeps_its_full_content_viewport()
    {
        var sheet = new MaterialBottomSheet { ExpandedExtent = 300, PeekExtent = 160,
            VelocityThreshold = 100000, Content = new Border { Height = 600 } };
        using var host = new GeometryHost(new MaterialSheetHost { Sheet = sheet }, 400, 400);
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with { DefaultSpatial = new(1, 100) } }; host.Render();
        var body = sheet.GetVisualDescendants().OfType<ScrollViewer>().Single(c => c.Name == "PART_BodyScroll");
        var viewport = body.Viewport;
        var handle = sheet.GetVisualDescendants().OfType<MaterialSheetDragHandle>().Single();
        var start = handle.TranslatePoint(new Point(handle.Bounds.Width / 2, handle.Bounds.Height / 2), host.Window)!.Value;
        host.Window.MouseDown(start, MouseButton.Left);
        host.Window.MouseMove(start - new Vector(0, 20), RawInputModifiers.LeftMouseButton); host.Render();
        Assert.True(sheet.IsDragging, $"Handle {handle.Bounds}; point {start}; extent {sheet.VisibleExtent}; state {sheet.State}");
        host.Window.MouseUp(start - new Vector(0, 20), MouseButton.Left);
        Assert.True(sheet.IsSettling);
        await Task.Delay(60); host.Render();
        Assert.InRange(sheet.VisibleExtent, 160.01, 299.99);
        Assert.Equal(viewport, body.Viewport);
        sheet.Expand(); await Task.Delay(60); host.Render();
        Assert.Equal(viewport, body.Viewport);
    }

    [AvaloniaFact]
    public async Task Uncontained_carousel_retains_release_velocity_in_its_rendered_position()
    {
        var carousel = new MaterialCarousel { Layout = MaterialCarouselLayout.Uncontained,
            ItemsSource = Enumerable.Range(0, 8).Select(i => new MaterialCarouselItem { Title = i.ToString() }) };
        using var host = new GeometryHost(carousel, 400, 200);
        host.Theme.Motion = new MaterialMotion(); host.Render();
        var start = new Point(220, 80);
        host.Window.MouseDown(start, MouseButton.Left); await Task.Delay(25);
        host.Window.MouseMove(start - new Vector(35, 0), RawInputModifiers.LeftMouseButton);
        host.Window.MouseUp(start - new Vector(35, 0), MouseButton.Left);
        var released = carousel.PresentationPosition;
        await Task.Delay(80); host.Render();
        Assert.True(carousel.PresentationPosition > released + .01);
    }
}
