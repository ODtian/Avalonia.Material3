using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Xunit;
using System.Text.Json;

namespace Avalonia.Material3.Tests;

// Agreed public host/input/caller text and painted-frame seams (docs/testing.md).
public class NormativeMotionScenarioTests
{
    [AvaloniaFact]
    public async Task Navigation_indicator_uses_independent_spatial_and_effects_springs_and_reduced_motion()
    {
        using var host = new ButtonHost();
        host.Window.Width = 480;
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with { FastSpatial = new(1, 50), DefaultEffects = new(1, 50) } };
        var icon = new Border { Width = 24, Height = 24, Background = Brushes.Red };
        var navigation = new MaterialNavigationBar();
        navigation.Items.Add(new MaterialNavigationItem { Content = "Home", Icon = new MaterialSymbol { Symbol = "home" } });
        navigation.Items.Add(new MaterialNavigationItem { Content = "Library", Icon = icon });
        navigation.SelectedIndex = 0;
        host.Window.Content = navigation; host.Capture();
        var point = icon.TranslatePoint(new Point(34, 12), host.Window)!.Value;
        var surface = host.PixelAt(point);
        navigation.SelectedIndex = 1;
        await Task.Delay(80); host.Capture();
        Assert.Equal(surface, host.PixelAt(point));
        await Task.Delay(700); host.Capture();
        Assert.NotEqual(surface, host.PixelAt(point));
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = true };
        navigation.SelectedIndex = 0; navigation.SelectedIndex = 1; host.Capture();
        Assert.NotEqual(surface, host.PixelAt(point));
    }

    [AvaloniaFact]
    public void Default_carousel_recipe_rebuild_is_immediate_and_default_pager_snap_uses_the_reference_spring()
    {
        using var host = new ButtonHost();
        host.Window.Width = 500;
        host.Theme.Motion = new MaterialMotion();
        var images = new List<Image>();
        var carousel = new MaterialCarousel
        {
            Height = 200, PreferredItemWidth = 120, AnimationTime = TimeSpan.Zero,
            ItemsSource = Enumerable.Range(0, 10).Select(i => new MaterialCarouselItem { Title = "Photo " + i }).ToArray(),
            ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<MaterialCarouselItem>((_, _) =>
            {
                var image = new Image { Source = CarouselRefreshScenarioTests.Picture(Brushes.Red), Stretch = Stretch.Fill };
                images.Add(image); return image;
            })
        };
        host.Window.Content = carousel; host.Capture();
        carousel.Layout = MaterialCarouselLayout.Hero; host.Capture();
        Assert.Equal(460, images[0].Bounds.Width, 4);
        carousel.Layout = MaterialCarouselLayout.MultiBrowse; host.Capture();
        carousel.MoveNext();
        carousel.AnimationTime = TimeSpan.FromMilliseconds(100); host.Capture();
        // CarouselDefaults.singleAdvanceFlingBehavior: damping1, StiffnessMediumLow200.
        Assert.Equal(.413064282489062, carousel.PresentationPosition, 6);
        var center = images[0].TranslatePoint(new Point(60, 100), host.Window)!.Value.X;
        Assert.InRange(center, 33.14, 33.16);
    }

    [AvaloniaFact]
    public void Carousel_masks_and_caller_content_follow_the_executed_pinned_Kotlin_vectors_at_start_middle_and_end()
    {
        using var host = new ButtonHost();
        host.Theme.Motion = new MaterialMotion();
        foreach (var reference in ReadCarouselVectors())
        {
            host.Window.Width = reference.Space;
            var images = new List<Image>();
            var carousel = new MaterialCarousel
            {
                Height = 200, Layout = Enum.Parse<MaterialCarouselLayout>(reference.Layout),
                PreferredItemWidth = reference.Preferred, ItemSpacing = reference.Gap,
                MotionDuration = TimeSpan.Zero, AnimationTime = TimeSpan.Zero,
                ItemsSource = Enumerable.Range(0, reference.Count).Select(i => new MaterialCarouselItem { Title = "Photo " + i }).ToArray(),
                ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<MaterialCarouselItem>((_, _) =>
                {
                    var image = new Image { Source = CarouselRefreshScenarioTests.Picture(Brushes.Red), Stretch = Stretch.Fill };
                    images.Add(image); return image;
                })
            };
            host.Window.Content = carousel; host.Capture();
            var index = (int)Math.Floor(reference.Position);
            carousel.CurrentIndex = index; host.Capture();
            if (reference.Position > index)
            {
                carousel.MotionDuration = TimeSpan.FromSeconds(1);
                carousel.MotionEasing = new Avalonia.Animation.Easings.LinearEasing();
                carousel.CurrentIndex = index + 1;
                carousel.AnimationTime = TimeSpan.FromSeconds(reference.Position - index);
                host.Capture();
            }
            for (var i = 0; i < reference.Count; i++)
            {
                var mask = reference.Masks[i];
                if (mask[0] + mask[1] < 0 || mask[0] > reference.Space) continue;
                Assert.InRange(Math.Abs(images[i].Bounds.Width - reference.Width), 0, .001);
                var center = images[i].TranslatePoint(new Point(images[i].Bounds.Width / 2, 100), host.Window)!.Value.X;
                Assert.True(Math.Abs(center - (mask[0] + mask[1] / 2)) <= .001,
                    $"{reference.Layout} space{reference.Space} count{reference.Count} position{reference.Position} item{i}: expected center{mask[0] + mask[1] / 2}, actual{center}.");
                var visibleCenter = (Math.Max(0, mask[0]) + Math.Min(reference.Space, mask[0] + mask[1])) / 2;
                if (visibleCenter > 1 && visibleCenter < reference.Space - 1)
                    Assert.Same(images[i], host.Window.InputHitTest(new Point(visibleCenter, 100)));
            }
        }
    }

    internal sealed record CarouselReferenceVector(string Layout, double Space, double Preferred, double Gap, int Count, double Position, double Width, double[][] Masks);
    internal static IReadOnlyList<CarouselReferenceVector> ReadCarouselVectors()
    {
        using var source = typeof(NormativeMotionScenarioTests).Assembly.GetManifestResourceStream("M3.CarouselKeylines.json")!;
        return JsonSerializer.Deserialize<List<CarouselReferenceVector>>(source, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    [AvaloniaFact]
    public void Multi_browse_uses_the_pinned_arrangement_and_continuous_keyline_masks_for_real_pointer_scroll()
    {
        using var host = new ButtonHost();
        host.Window.Width = 500;
        var images = new List<Image>();
        var carousel = new MaterialCarousel
        {
            Height = 200, PreferredItemWidth = 120, ItemSpacing = 0,
            MotionDuration = TimeSpan.Zero,
            ItemsSource = Enumerable.Range(0, 10).Select(i => new MaterialCarouselItem { Title = "Photo " + i }).ToArray(),
            ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<MaterialCarouselItem>((_, _) =>
            {
                var image = new Image { Source = CarouselRefreshScenarioTests.Picture(Brushes.Red), Stretch = Stretch.Fill };
                images.Add(image); return image;
            })
        };
        host.Window.Content = carousel;
        host.Capture();
        // Exact upstream MultiBrowseTest testMultiBrowse_doesNotResizeLargeWhenEnoughRoom.
        Assert.Equal(120, images[0].Bounds.Width, 4);
        Assert.Equal(60, images[0].TranslatePoint(new Point(60, 100), host.Window)!.Value.X, 4);
        host.Window.MouseDown(new Point(100, 100), MouseButton.Left);
        host.Window.MouseMove(new Point(40, 100));
        host.Capture();
        // Pinned KeylineList: anchor(size10, offset-5, unadjusted-60), focal(120,60,60).
        // Half-page scroll samples their continuous midpoint: mask65, center27.5.
        var center = images[0].TranslatePoint(new Point(60, 100), host.Window)!.Value;
        Assert.Equal(27.5, center.X, 4);
        Assert.Same(images[0], host.Window.InputHitTest(new Point(20, 100)));
        host.Window.MouseMove(new Point(100, 100));
        host.Capture();
        Assert.Equal(60, images[0].TranslatePoint(new Point(60, 100), host.Window)!.Value.X, 4);
        Assert.Equal(120, images[0].Bounds.Width, 4);
        host.Window.MouseUp(new Point(100, 100), MouseButton.Left);
    }

    [AvaloniaFact]
    public async Task Rapid_group_press_reversals_preserve_both_neighbor_labels_and_visible_width_motion()
    {
        using var host = new ButtonHost();
        host.Window.Width = 760;
        host.Theme.Motion = new MaterialMotion();
        var leftText = new TextBlock { Text = "First long action", TextWrapping = TextWrapping.Wrap };
        var rightText = new TextBlock { Text = "Third long action", TextWrapping = TextWrapping.Wrap };
        var left = new MaterialGroupButton { Content = leftText };
        var middle = new MaterialGroupButton { Content = "Second long action" };
        var right = new MaterialGroupButton { Content = rightText };
        host.Window.Content = new StackPanel { Children = { new MaterialButtonGroup { Children = { left, middle, right } } } };
        host.Capture();
        var leftSize = leftText.Bounds.Size;
        var rightSize = rightText.Bounds.Size;
        var originalWidth = middle.Bounds.Width;
        var greatestWidth = originalWidth;
        for (var press = 0; press < 12; press++)
        {
            var point = middle.TranslatePoint(new Point(middle.Bounds.Width / 2, middle.Bounds.Height / 2), host.Window)!.Value;
            host.Window.MouseDown(point, MouseButton.Left);
            for (var frame = 0; frame < 8; frame++)
            {
                await Task.Delay(8);
                host.Capture();
                greatestWidth = Math.Max(greatestWidth, middle.Bounds.Width);
                Assert.Equal(leftSize, leftText.Bounds.Size);
                Assert.Equal(rightSize, rightText.Bounds.Size);
                var labelPoint = leftText.TranslatePoint(new Point(leftText.Bounds.Width / 2, leftText.Bounds.Height / 2), host.Window)!.Value;
                Assert.NotNull(host.Window.InputHitTest(labelPoint));
            }
            host.Window.MouseUp(point, MouseButton.Left);
            await Task.Delay(8);
            host.Capture();
            Assert.Equal(leftSize, leftText.Bounds.Size);
            Assert.Equal(rightSize, rightText.Bounds.Size);
        }
        Assert.True(greatestWidth > originalWidth + 8, "Dense frames must display actual pressed width growth.");
    }
}
