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

    [AvaloniaFact]
    public async Task Native_hour_tap_finishes_the_hour_hand_before_advancing_the_face()
    {
        var picker = new MaterialTimePicker { SelectedTime = new TimeOnly(3, 0) };
        using var host = new GeometryHost(picker, 400, 640);
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with { DefaultSpatial = new(1, 100) } }; host.Render();
        var dial = picker.GetVisualDescendants().OfType<MaterialClockDial>().Single();
        var six = dial.GetVisualDescendants().OfType<MaterialClockNumber>().Single(c => c.Value == 6);
        var point = GeometryHost.Box(six, host.Window).Center;
        host.Window.MouseDown(point, MouseButton.Left); host.Window.MouseUp(point, MouseButton.Left);
        Assert.Equal(6, picker.SelectedTime?.Hour);
        Assert.Equal(MaterialTimePickerPart.Hour, picker.ActivePart);
        await Task.Delay(90); host.Render();
        Assert.Equal(MaterialTimePickerPart.Hour, picker.ActivePart);
        for (var i = 0; i < 50 && picker.ActivePart == MaterialTimePickerPart.Hour; i++)
        { await Task.Delay(20); host.Render(); }
        Assert.Equal(MaterialTimePickerPart.Minute, picker.ActivePart);
        picker.SelectHour(9);
        Assert.Equal(MaterialTimePickerPart.Minute, picker.ActivePart);
    }

    [AvaloniaFact]
    public async Task Fullscreen_search_surface_interpolates_dimensions_corner_and_header_together()
    {
        var search = new MaterialSearch { Mode = MaterialSearchMode.Bar, ViewPresentation = MaterialSearchViewPresentation.FullScreen };
        using var host = new GeometryHost(search, 800, 500);
        host.Theme.Motion = new MaterialMotion(); host.Render();
        var surface = search.GetVisualDescendants().OfType<Border>().Single(c => c.Name == "SearchContainer");
        var closed = surface.Bounds.Size;
        search.Open(); await Task.Delay(200); host.Render();
        Assert.InRange(surface.Bounds.Height, closed.Height + 1, 499);
        Assert.InRange(surface.Bounds.Width, closed.Width + 1, 799);
        Assert.InRange(surface.CornerRadius.TopLeft, .01, 27.99);
        Assert.InRange(search.HeaderHeight, 56.01, 71.99);
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = true }; host.Render();
        Assert.Equal(new Size(800, 500), surface.Bounds.Size);
        Assert.Equal(72, search.HeaderHeight);
    }

    [AvaloniaTheory]
    [InlineData(MaterialAppBarScrollBehavior.EnterAlways)]
    [InlineData(MaterialAppBarScrollBehavior.ExitUntilCollapsed)]
    public async Task App_bar_settles_an_intermediate_collapse_after_native_scroll_release(MaterialAppBarScrollBehavior behavior)
    {
        var scroll = new ScrollViewer { Content = new Border { Height = 1500 } };
        var bar = new MaterialTopAppBar { Variant = MaterialTopAppBarVariant.Large, Title = "Page",
            ScrollBehavior = behavior, ScrollSource = scroll };
        var layout = new DockPanel { Children = { bar, scroll } }; DockPanel.SetDock(bar, Avalonia.Controls.Dock.Top);
        using var host = new GeometryHost(layout, 400, 400);
        host.Theme.Motion = new MaterialMotion(); host.Render();
        var point = new Point(200, 250);
        host.Window.MouseDown(point, MouseButton.Left);
        scroll.Offset = new Vector(0, 30); host.Render();
        Assert.InRange(bar.CollapsedFraction, .1, .49);
        host.Window.MouseUp(point, MouseButton.Left);
        // Native scroll presenters may publish a final inertial offset after release.
        await Task.Delay(20); scroll.Offset = new Vector(0, 31); host.Render();
        if (behavior == MaterialAppBarScrollBehavior.ExitUntilCollapsed) Assert.InRange(bar.CollapsedFraction, .1, .49);
        for (var i = 0; i < 40 && bar.CollapsedFraction is > .01 and < .99; i++)
        { await Task.Delay(20); host.Render(); }
        Assert.True(bar.CollapsedFraction < .01 || bar.CollapsedFraction > .99);
    }
}
