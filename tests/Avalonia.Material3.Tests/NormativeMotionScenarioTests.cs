using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Xunit;
using System.Text.Json;
using Avalonia.VisualTree;

namespace Avalonia.Material3.Tests;

// Agreed public host/input/caller text and painted-frame seams (docs/testing.md).
public class NormativeMotionScenarioTests
{
    [AvaloniaFact]
    public async Task Side_sheet_open_uses_the_pinned_ViewDragHelper_quintic_settle_recipe()
    {
        using var host = new ButtonHost(); host.Window.Width = 400; host.Window.Height = 500;
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with { DefaultSpatial = new(1, 1) } };
        var overlay = new MaterialOverlayHost { Content = new Border() };
        host.Window.Content = overlay; host.Capture();
        var sheet = new MaterialSideSheet { ExpandedExtent = 320, Content = "Details" };
        sheet.Show(overlay); host.Capture();
        await Task.Delay(150); host.Capture();
        Assert.InRange(sheet.TranslatePoint(default, host.Window)!.Value.X, 80, 260);
    }

    [AvaloniaFact]
    public void Standard_circular_progress_uses_the_pinned_linear_first_half_and_keeps_its_track_visible()
    {
        using var host = new ButtonHost(); host.Theme.Motion = new MaterialMotion();
        var progress = new MaterialCircularProgressIndicator { IsIndeterminate = true, AnimationTime = TimeSpan.Zero };
        host.Window.Content = progress; host.Capture();
        progress.AnimationTime = TimeSpan.FromMilliseconds(1500); host.Capture();
        var point = progress.TranslatePoint(new Point(7.3, 7.3), host.Window)!.Value;
        Assert.Equal(((ISolidColorBrush)progress.TrackBrush!).Color, host.PixelAt(point));
    }

    [AvaloniaFact]
    public async Task Programmatic_bottom_sheet_collapse_uses_fast_effects_after_a_spatial_expansion()
    {
        using var host = new ButtonHost(); host.Window.Height = 500;
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with { DefaultSpatial = new(1, 1), FastEffects = new(1, 3800) { IsInstant = true } } };
        var sheet = new MaterialBottomSheet { ExpandedExtent = 300 };
        host.Window.Content = new MaterialSheetHost { Content = new Border(), Sheet = sheet }; host.Capture();
        sheet.Expand(); host.Capture(); await Task.Delay(150); host.Capture();
        Assert.InRange(sheet.VisibleExtent, 56.1, 150);
        sheet.Collapse(); host.Capture();
        Assert.Equal(56, sheet.VisibleExtent);
    }

    [AvaloniaFact]
    public async Task Floating_action_menu_staggers_real_item_paint_and_hits_from_the_trigger_outward()
    {
        using var host = new ButtonHost(); host.Window.Width = 500; host.Window.Height = 500;
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with { SlowEffects = new(1, 50) } };
        var menu = new MaterialFabMenu();
        for (var i = 0; i < 4; i++) menu.Items.Add(new MaterialFabMenuItem { Content = "Create document " + i });
        host.Window.Content = new Grid { Children = { menu } }; host.Capture();
        menu.IsExpanded = true; host.Capture();
        await Task.Delay(120); host.Capture();
        var first = menu.Items[0]; var last = menu.Items[^1];
        var early = first.TranslatePoint(new Point(first.Bounds.Width / 2, first.Bounds.Height / 2), host.Window)!.Value;
        var late = last.TranslatePoint(new Point(last.Bounds.Width - 18, last.Bounds.Height / 2), host.Window)!.Value;
        var earlyHit = host.Window.InputHitTest(early) as Visual;
        var lateHit = host.Window.InputHitTest(late) as Visual;
        Assert.False(earlyHit == first || earlyHit is not null && first.IsVisualAncestorOf(earlyHit));
        Assert.True(lateHit == last || lateHit is not null && last.IsVisualAncestorOf(lateHit));
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = true }; host.Capture();
        early = first.TranslatePoint(new Point(first.Bounds.Width / 2, first.Bounds.Height / 2), host.Window)!.Value;
        earlyHit = host.Window.InputHitTest(early) as Visual;
        Assert.True(earlyHit == first || earlyHit is not null && first.IsVisualAncestorOf(earlyHit));
        menu.IsExpanded = false;
        Assert.False(first.IsEffectivelyEnabled);
    }

    [AvaloniaFact]
    public async Task Whole_toolbar_preserves_the_full_reference_slot_and_FAB_edge_during_dense_reversals()
    {
        using var host = new ButtonHost(); host.Window.Width = 600;
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with { FastSpatial = new(1, 50) } };
        var fab = new MaterialFab { Content = new MaterialSymbol { Symbol = "add" } };
        var toolbar = new MaterialToolbar { FloatingAction = fab, CollapseBehavior = MaterialToolbarCollapseBehavior.WholeToolbar };
        toolbar.Items.Add(new MaterialIconButton { Content = new MaterialSymbol { Symbol = "home" } });
        host.Window.Content = new Grid { Children = { toolbar } }; host.Capture();
        var size = toolbar.Bounds.Size;
        var edge = fab.TranslatePoint(new Point(fab.Bounds.Width, fab.Bounds.Height / 2), host.Window)!.Value;
        toolbar.IsExpanded = false;
        for (var frame = 0; frame < 30; frame++)
        {
            await Task.Delay(8); host.Capture();
            Assert.Equal(size, toolbar.Bounds.Size);
            var next = fab.TranslatePoint(new Point(fab.Bounds.Width, fab.Bounds.Height / 2), host.Window)!.Value;
            Assert.Equal(edge.X, next.X, 4); Assert.Equal(edge.Y, next.Y, 4);
            if (frame == 10) toolbar.IsExpanded = true;
            if (frame == 20) toolbar.IsExpanded = false;
        }
    }

    [AvaloniaFact]
    public void Ordinary_floating_action_size_configuration_uses_the_reference_immediate_geometry()
    {
        using var host = new ButtonHost();
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with { FastSpatial = new(1, 50) } };
        var fab = new MaterialFab { Content = new MaterialSymbol { Symbol = "add" } };
        host.Window.Content = new StackPanel { Children = { fab } }; host.Capture();
        fab.Size = MaterialFabSize.Medium; host.Capture();
        Assert.Equal(90, fab.Bounds.Width);
        Assert.Equal(90, fab.Bounds.Height);
    }

    [AvaloniaFact]
    public async Task Modal_sheet_slides_on_open_and_preserves_its_painted_pose_when_the_session_closes()
    {
        using var host = new ButtonHost();
        host.Window.Width = 400; host.Window.Height = 500;
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with { DefaultSpatial = new(1, 50), FastEffects = new(1, 50) } };
        var overlay = new MaterialOverlayHost { Content = new Border { Background = Brushes.Red } };
        host.Window.Content = overlay; host.Capture();
        var sheet = new MaterialBottomSheet { ExpandedExtent = 300, IsPartialEnabled = false, Content = new Border { Background = Brushes.Blue } };
        var session = sheet.Show(overlay); host.Capture();
        Assert.Equal(1, overlay.OpenCount);
        Assert.True(sheet.TranslatePoint(default, host.Window)!.Value.Y > 400);
        await Task.Delay(100); host.Capture();
        Assert.True(sheet.TranslatePoint(default, host.Window)!.Value.Y > 300);
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = true }; host.Capture();
        Assert.Equal(200, sheet.TranslatePoint(default, host.Window)!.Value.Y, 4);
        var point = sheet.TranslatePoint(new Point(100, 200), host.Window)!.Value;
        var paint = host.PixelAt(point);
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with { FastEffects = new(1, 50) } }; host.Capture();
        Assert.True(session.Dismiss()); host.Capture();
        Assert.Equal(0, overlay.OpenCount);
        Assert.Null(sheet.Parent);
        Assert.Equal(paint, host.PixelAt(point));
    }

    [AvaloniaFact]
    public async Task Wide_rail_expansion_and_reversal_present_the_default_spatial_width_without_a_minimum_width_jump()
    {
        using var host = new ButtonHost();
        host.Window.Width = 480;
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with { DefaultSpatial = new(1, 50) } };
        var item = new MaterialNavigationItem { Content = "Library" };
        var rail = new MaterialNavigationRail { Items = { item } };
        host.Window.Content = rail; host.Capture();
        Assert.Equal(96, rail.HeaderWidth);
        rail.IsExpanded = true;
        await Task.Delay(80); host.Capture();
        Assert.InRange(rail.HeaderWidth, 98, 170);
        Assert.InRange(Math.Abs(item.Bounds.Width - rail.HeaderWidth), 0, .001);
        var intermediate = rail.HeaderWidth;
        rail.IsExpanded = false; host.Capture();
        Assert.InRange(rail.HeaderWidth, 96.01, Math.Min(190, intermediate + 30));
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = true }; host.Capture();
        Assert.Equal(96, rail.HeaderWidth);
        Assert.Equal(96, item.Bounds.Width);
    }

    [AvaloniaFact]
    public async Task Navigation_icon_color_follows_the_pinned_effects_spring_before_reaching_its_selected_role()
    {
        using var host = new ButtonHost();
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with { DefaultEffects = new(1, 50) } };
        var icon = new MaterialSymbol { Symbol = "home", Filled = true };
        var navigation = new MaterialNavigationBar
        {
            Items = { new MaterialNavigationItem { Content = "First" }, new MaterialNavigationItem { Content = "Second", Icon = icon } },
            SelectedIndex = 0
        };
        host.Window.Content = navigation; host.Capture();
        var size = icon.Bounds;
        Assert.True(host.Theme.Resources.TryGetResource("M3.OnSecondaryContainerBrush", Avalonia.Styling.ThemeVariant.Light, out var role));
        var selectedColor = ((ISolidColorBrush)role!).Color;
        navigation.SelectedIndex = 1;
        await Task.Delay(80); host.Capture();
        Assert.NotEqual(selectedColor, ((ISolidColorBrush)icon.Foreground!).Color);
        Assert.Equal(size, icon.Bounds);
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = true }; host.Capture();
        Assert.Equal(selectedColor, ((ISolidColorBrush)icon.Foreground!).Color);
    }

    [AvaloniaFact]
    public async Task Secondary_tab_underline_moves_as_one_opaque_indicator_through_the_space_between_destinations()
    {
        using var host = new ButtonHost();
        host.Window.Width = 480;
        host.Theme.Motion = new MaterialMotion { Springs = MaterialSpringScheme.Expressive with { DefaultSpatial = new(1, 50) } };
        var first = new MaterialNavigationItem { Content = "First tab" };
        var second = new MaterialNavigationItem { Content = "Second tab" };
        var tabs = new MaterialTabs { Variant = MaterialTabVariant.Secondary, Items = { first, second }, SelectedIndex = 0 };
        host.Window.Content = tabs; host.Capture();
        var a = first.TranslatePoint(new Point(first.Bounds.Width / 2, first.Bounds.Height - 1), host.Window)!.Value;
        var b = second.TranslatePoint(new Point(second.Bounds.Width / 2, second.Bounds.Height - 1), host.Window)!.Value;
        var ink = host.PixelAt(a); var surface = host.PixelAt(b);
        Assert.NotEqual(ink, surface);
        tabs.SelectedIndex = 1;
        await Task.Delay(80); host.Capture();
        Assert.Equal(ink, host.PixelAt(a));
        Assert.Equal(surface, host.PixelAt(b));
        await Task.Delay(700); host.Capture();
        Assert.Equal(surface, host.PixelAt(a));
        Assert.Equal(ink, host.PixelAt(b));
    }

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
