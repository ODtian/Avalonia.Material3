using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Tokens;
using Xunit;

namespace Avalonia.Material3.Tests;

// Public host/template measurement, native input/layout and detached-control lifetime seams.
public class FinalQualityReviewScenarioTests
{
    [AvaloniaFact]
    public async Task Symmetric_group_press_preserves_the_middle_action_axis_at_dense_frames()
    {
        using var host = new ButtonHost();
        host.Window.Width = 720;
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with { FastSpatial = new(1, 50) } };
        var first = new MaterialGroupButton { Content = "First long action" };
        var middle = new MaterialGroupButton { Content = "Second long action" };
        var last = new MaterialGroupButton { Content = "Third long action" };
        host.Window.Content = new StackPanel { Margin = new Thickness(16), Children = { new MaterialButtonGroup { Children = { first, middle, last } } } };
        host.Capture();
        var axis = CenterInHost(middle, host.Window);
        var width = middle.Bounds.Width;
        host.Window.MouseDown(axis, MouseButton.Left);
        var greatestWidth = width;
        for (var frame = 0; frame < 100; frame++)
        {
            await Task.Delay(8);
            host.Capture();
            greatestWidth = Math.Max(greatestWidth, middle.Bounds.Width);
            var center = CenterInHost(middle, host.Window);
            Assert.Equal(axis.X, center.X, 5);
            Assert.Equal(axis.Y, center.Y, 5);
        }
        Assert.True(greatestWidth > width + 8, "The host must display real width expansion around the stable middle axis.");
        host.Window.MouseUp(axis, MouseButton.Left);
    }

    private static Point CenterInHost(Control control, Window window) =>
        control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;

    [AvaloniaFact]
    public async Task Growing_floating_action_keeps_caller_icon_on_both_container_axes_at_dense_frames()
    {
        using var host = new ButtonHost();
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with { FastSpatial = new(1, 50) } };
        var icon = new Border { Width = 24, Height = 24, Background = Avalonia.Media.Brushes.Red };
        var fab = new MaterialFab { Content = icon };
        var toolbar = new MaterialToolbar { FloatingAction = fab, CollapseBehavior = MaterialToolbarCollapseBehavior.WholeToolbar };
        toolbar.Items.Add(new MaterialIconButton { Content = "Action" });
        host.Window.Content = new StackPanel { Children = { toolbar } };
        host.Capture();
        var initial = fab.Bounds.Width;
        toolbar.IsExpanded = false;
        var greatest = initial;
        for (var frame = 0; frame < 80; frame++)
        {
            await Task.Delay(8);
            host.Capture();
            greatest = Math.Max(greatest, fab.Bounds.Width);
            var containerCenter = CenterInHost(fab, host.Window);
            var iconCenter = CenterInHost(icon, host.Window);
            Assert.Equal(containerCenter.X, iconCenter.X, 5);
            Assert.Equal(containerCenter.Y, iconCenter.Y, 5);
        }
        Assert.True(greatest > initial + 10);
    }

    [AvaloniaFact]
    public void Clock_part_changes_release_removed_native_marks_while_the_dial_stays_alive()
    {
        using var host = new ButtonHost();
        var dial = new MaterialClockDial();
        host.Window.Content = dial;
        host.Capture();
        var removed = new List<WeakReference<MaterialClockNumber>>();
        for (var cycle = 0; cycle < 20; cycle++)
        {
            ReplaceMarks(dial, removed);
            host.Capture();
        }
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        Assert.Equal(240, removed.Count);
        Assert.All(removed, mark => Assert.False(mark.TryGetTarget(out _), "A removed native mark must leave the live dial's lifetime."));
        Assert.Equal(12, dial.Children.OfType<MaterialClockNumber>().Count());
        GC.KeepAlive(dial);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ReplaceMarks(MaterialClockDial dial, List<WeakReference<MaterialClockNumber>> previous)
    {
        previous.AddRange(dial.Children.OfType<MaterialClockNumber>().Select(mark => new WeakReference<MaterialClockNumber>(mark)));
        dial.ActivePart = dial.ActivePart == MaterialTimePickerPart.Hour ? MaterialTimePickerPart.Minute : MaterialTimePickerPart.Hour;
    }

    [AvaloniaFact]
    public void Carousel_custom_content_remeasures_the_changed_recipe_and_settled_reduction_once()
    {
        using var host = new ButtonHost();
        host.Window.Width = 400;
        host.Theme.Motion = new MaterialMotion();
        var content = new List<MeasuredContent>();
        var carousel = new MaterialCarousel
        {
            Height = 200, ItemSpacing = 8, AnimationTime = TimeSpan.Zero, MotionDuration = TimeSpan.FromMilliseconds(200),
            ItemsSource = Enumerable.Range(0, 6).Select(i => new MaterialCarouselItem { Title = "Item " + i }).ToArray(),
            ItemTemplate = new FuncDataTemplate<MaterialCarouselItem>((_, _) =>
            {
                var control = new MeasuredContent(); content.Add(control); return control;
            })
        };
        host.Window.Content = carousel;
        host.Capture();
        AssertCarouselSize(content[0].LastMeasured, 187.2);
        var narrowTextHeight = content[0].WrappedText.DesiredSize.Height;
        carousel.Layout = MaterialCarouselLayout.Hero;
        host.Capture();
        AssertCarouselSize(content[0].LastMeasured, 352);
        Assert.True(content[0].WrappedText.DesiredSize.Height < narrowTextHeight, "The wider recipe must reflow caller-owned text into fewer lines.");
        var enlargedMeasures = content[0].Measures;
        for (var frame = 1; frame <= 12; frame++)
        {
            carousel.AnimationTime = TimeSpan.FromMilliseconds(frame * 200d / 12);
            host.Capture();
            Assert.Equal(enlargedMeasures, content[0].Measures);
        }
        AssertCarouselSize(content[0].Bounds.Size, 352);
        Assert.All(content, control => AssertCarouselSize(control.Bounds.Size, 352));
        carousel.Layout = MaterialCarouselLayout.MultiBrowse;
        host.Capture();
        for (var frame = 1; frame < 12; frame++)
        {
            carousel.AnimationTime = TimeSpan.FromMilliseconds(200 + frame * 200d / 12);
            host.Capture();
            Assert.Equal(enlargedMeasures, content[0].Measures);
        }
        carousel.AnimationTime = TimeSpan.FromMilliseconds(400);
        host.Capture();
        AssertCarouselSize(content[0].LastMeasured, 187.2);
        AssertCarouselSize(content[0].DesiredSize, 187.2);
        Assert.Equal(narrowTextHeight, content[0].WrappedText.DesiredSize.Height);
        AssertCarouselSize(content[0].Bounds.Size, 187.2);
        Assert.All(content, control => AssertCarouselSize(control.Bounds.Size, 187.2));
        Assert.Equal(enlargedMeasures + 1, content[0].Measures);
        Assert.Equal(6, content.Count);
    }
    private static void AssertCarouselSize(Size size, double width)
    { Assert.Equal(width, size.Width, 4); Assert.Equal(200, size.Height); }

    private sealed class MeasuredContent : Panel
    {
        public TextBlock WrappedText { get; } = new()
        {
            Text = string.Join(' ', Enumerable.Repeat("MMMMMM", 12)), TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            FontFamily = new Avalonia.Media.FontFamily("Arial"), FontSize = 16
        };
        public Size LastMeasured { get; private set; }
        public int Measures { get; private set; }
        public MeasuredContent() => Children.Add(WrappedText);
        protected override Size MeasureOverride(Size availableSize)
        {
            Measures++; LastMeasured = availableSize; WrappedText.Measure(availableSize); return availableSize;
        }
        protected override Size ArrangeOverride(Size finalSize) { WrappedText.Arrange(new Rect(finalSize)); return finalSize; }
    }
}
