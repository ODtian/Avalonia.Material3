using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Tokens;
using Xunit;

namespace Avalonia.Material3.Tests;

// Public wheel input, host scroll position and carousel value seams (docs/testing.md).
public class CarouselWheelScenarioTests
{
    [AvaloniaFact]
    public void Horizontal_carousel_bubbles_the_outward_wheel_to_a_horizontal_parent_at_its_edge()
    {
        var carousel = Carousel(MaterialCarouselLayout.MultiBrowse); carousel.Width = 400;
        var scroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal,
                Children = { carousel, new Border { Width = 900, Height = 200 } } }
        };
        using var host = new GeometryHost(scroll, 350, 250);
        Wheel(host, carousel, new Vector(-1, 0));
        Assert.Equal(1, carousel.CurrentIndex);
        Assert.Equal(0, scroll.Offset.X);
        carousel.CurrentIndex = 4; host.Render();
        Wheel(host, carousel, new Vector(-1, 0));
        Assert.Equal(4, carousel.CurrentIndex);
        Assert.True(scroll.Offset.X > 0);
    }

    [AvaloniaFact]
    public void Mostly_vertical_trackpad_input_scrolls_the_parent_of_a_horizontal_carousel()
    {
        var carousel = Carousel(MaterialCarouselLayout.MultiBrowse); carousel.CurrentIndex = 2;
        var scroll = VerticalHost(carousel);
        using var host = new GeometryHost(scroll, 400, 350);
        Wheel(host, carousel, new Vector(-.1, -1));
        Assert.Equal(2, carousel.CurrentIndex);
        Assert.True(scroll.Offset.Y > 0);
    }

    [AvaloniaFact]
    public void Full_screen_vertical_wheel_browses_and_bubbles_at_the_last_item()
    {
        var carousel = Carousel(MaterialCarouselLayout.FullScreen);
        var scroll = VerticalHost(carousel);
        using var host = new GeometryHost(scroll, 400, 350);
        Wheel(host, carousel, new Vector(0, -1));
        Assert.Equal(1, carousel.CurrentIndex);
        Assert.Equal(0, scroll.Offset.Y);
        Wheel(host, carousel, new Vector(0, 1));
        Assert.Equal(0, carousel.CurrentIndex);
        carousel.CurrentIndex = 4; host.Render();
        Wheel(host, carousel, new Vector(0, -1));
        Assert.Equal(4, carousel.CurrentIndex);
        Assert.True(scroll.Offset.Y > 0);
    }

    [AvaloniaFact]
    public void RTL_horizontal_and_shift_wheels_follow_the_physical_direction()
    {
        var carousel = Carousel(MaterialCarouselLayout.Hero);
        carousel.FlowDirection = Avalonia.Media.FlowDirection.RightToLeft; carousel.CurrentIndex = 2;
        using var host = new GeometryHost(VerticalHost(carousel), 400, 350);
        Wheel(host, carousel, new Vector(-1, 0));
        Assert.Equal(1, carousel.CurrentIndex);
        Wheel(host, carousel, new Vector(0, -1), RawInputModifiers.Shift);
        Assert.Equal(0, carousel.CurrentIndex);
        Wheel(host, carousel, new Vector(1, 0));
        Assert.Equal(1, carousel.CurrentIndex);
    }

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void Disabled_or_empty_vertical_carousel_preserves_parent_scrolling(bool disabled)
    {
        var carousel = Carousel(MaterialCarouselLayout.FullScreen);
        if (disabled) carousel.IsEnabled = false; else carousel.ItemsSource = [];
        var scroll = VerticalHost(carousel);
        using var host = new GeometryHost(scroll, 400, 350);
        Wheel(host, carousel, new Vector(0, -1));
        Assert.Equal(0, carousel.CurrentIndex);
        Assert.True(scroll.Offset.Y > 0);
    }

    [AvaloniaFact]
    public void Wheel_navigation_uses_the_existing_native_pager_spring()
    {
        var carousel = Carousel(MaterialCarouselLayout.Hero);
        carousel.ClearValue(MaterialCarousel.MotionDurationProperty);
        carousel.AnimationTime = TimeSpan.Zero;
        using var host = new GeometryHost(VerticalHost(carousel), 400, 350);
        host.Theme.Motion = new MaterialMotion(); host.Render();
        Wheel(host, carousel, new Vector(-1, 0));
        Assert.Equal(1, carousel.CurrentIndex);
        Assert.Equal(0, carousel.PresentationPosition);
        carousel.AnimationTime = TimeSpan.FromMilliseconds(100); host.Render();
        // Pinned PagerDefaults spring: damping1, stiffness200; published independent vector.
        Assert.Equal(.413064282489062, carousel.PresentationPosition, 6);
    }

    [AvaloniaTheory]
    [InlineData(.25, 4)]
    [InlineData(.1, 10)]
    public void Precision_wheel_accumulates_partial_ticks_and_resets_when_the_axis_changes(double delta, int ticks)
    {
        var carousel = Carousel(MaterialCarouselLayout.MultiBrowse);
        using var host = new GeometryHost(VerticalHost(carousel), 400, 350);
        for (var tick = 0; tick < ticks - 1; tick++) Wheel(host, carousel, new Vector(-delta, 0));
        Assert.Equal(0, carousel.CurrentIndex);
        Wheel(host, carousel, new Vector(-delta, 0));
        Assert.Equal(1, carousel.CurrentIndex);
        Wheel(host, carousel, new Vector(-2, 0));
        Assert.Equal(3, carousel.CurrentIndex);
        Wheel(host, carousel, new Vector(.75, 0));
        Assert.Equal(3, carousel.CurrentIndex);
        carousel.Layout = MaterialCarouselLayout.FullScreen; host.Render();
        Wheel(host, carousel, new Vector(0, .25));
        Assert.Equal(3, carousel.CurrentIndex);
    }

    [AvaloniaTheory]
    [InlineData(MaterialCarouselLayout.MultiBrowse)]
    [InlineData(MaterialCarouselLayout.Hero)]
    [InlineData(MaterialCarouselLayout.Uncontained)]
    public void Horizontal_and_shift_wheels_browse_while_plain_vertical_wheel_scrolls_the_host(MaterialCarouselLayout layout)
    {
        var carousel = Carousel(layout);
        var scroll = VerticalHost(carousel);
        using var host = new GeometryHost(scroll, 400, 350);
        Wheel(host, carousel, new Vector(-1, 0));
        Assert.Equal(1, carousel.CurrentIndex);
        Assert.Equal(0, scroll.Offset.Y);
        Wheel(host, carousel, new Vector(0, -1), RawInputModifiers.Shift);
        Assert.Equal(2, carousel.CurrentIndex);
        Assert.Equal(0, scroll.Offset.Y);
        Wheel(host, carousel, new Vector(0, -1));
        Assert.Equal(2, carousel.CurrentIndex);
        Assert.True(scroll.Offset.Y > 0);
    }

    private static MaterialCarousel Carousel(MaterialCarouselLayout layout) => new()
    {
        Layout = layout, Height = 200, MotionDuration = TimeSpan.Zero,
        ItemsSource = Enumerable.Range(0, 5).Select(i => new MaterialCarouselItem { Title = "Photo " + i }).ToArray()
    };

    private static ScrollViewer VerticalHost(MaterialCarousel carousel) => new()
    {
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        Content = new StackPanel { Children = { carousel, new Border { Height = 900 } } }
    };

    private static void Wheel(GeometryHost host, MaterialCarousel carousel, Vector delta, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        var point = carousel.TranslatePoint(new Point(150, 100), host.Window)!.Value;
        host.Window.MouseWheel(point, delta, modifiers); host.Render();
    }
}
