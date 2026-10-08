using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Material3.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Material3.Tests;

public class PaintBoundaryScenarioTests
{
    [AvaloniaFact]
    public void Autocomplete_candidate_surface_paints_elevation_outside_its_foreground_reveal_viewport()
    {
        var search = new MaterialSearch { Width = 240, Margin = new Thickness(20), Mode = MaterialSearchMode.FilledAutocomplete, IsOpen = true,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            Candidates = new[] { new MaterialSearchToken("one", "Candidate") } };
        using var host = new GeometryHost(new Border { Background = Brushes.White, Child = search }, 400, 400);
        var candidates = search.GetVisualDescendants().OfType<ListBox>().Single();
        var box = GeometryHost.Box(candidates, host.Window);
        Assert.InRange(host.Pixel(box.Center.X, box.Bottom + .01).R, 0, 254);
    }

    [AvaloniaFact]
    public void Floating_toolbar_with_fab_paints_the_native_expanded_elevation_outside_its_container()
    {
        var toolbar = new MaterialToolbar { Width = 280, Background = Brushes.Magenta, Anchor = MaterialActionAnchor.TopStart,
            FloatingAction = new MaterialFab { Content = new MaterialSymbol { Symbol = "add" } } };
        toolbar.Items.Add(new MaterialIconButton { Content = new MaterialSymbol { Symbol = "edit" } });
        using var host = new GeometryHost(new Border { Background = Brushes.White, Child = toolbar }, 400, 200);
        var surface = toolbar.GetVisualDescendants().OfType<Border>().Single(border => border.Background == Brushes.Magenta);
        var box = GeometryHost.Box(surface, host.Window);
        Assert.InRange(host.Pixel(box.Center.X, box.Bottom + .01).R, 0, 254);
        toolbar.IsExpanded = false; host.Render();
        Assert.Equal(Colors.White, host.Pixel(box.Center.X, box.Bottom + .01));
    }

    [AvaloniaFact]
    public void Standard_sheet_uses_the_declared_host_viewport_for_partial_surface_paint_and_hit_testing()
    {
        var sheet = new MaterialBottomSheet { ExpandedExtent = 300, PeekExtent = 56, Background = Brushes.Magenta };
        var layout = new MaterialSheetHost { AvailableSize = new Size(300, 300), Sheet = sheet, Content = new Border { Background = Brushes.White } };
        using var host = new GeometryHost(layout, 500, 500);
        Assert.Equal(Colors.Magenta, host.Pixel(150, 290));
        Assert.NotEqual(Colors.Magenta, host.Pixel(150, 330));
        Assert.DoesNotContain(host.Window.GetVisualsAt(new Point(150, 330)), visual => sheet == visual || sheet.IsVisualAncestorOf(visual));
        layout.AvailableSize = new Size(300, 240); host.Render();
        Assert.Equal(Colors.Magenta, host.Pixel(150, 230));
        Assert.NotEqual(Colors.Magenta, host.Pixel(150, 270));
    }

    [AvaloniaFact]
    public void Floating_toolbar_preserves_a_live_host_clip_when_shadow_reveal_rearranges_and_detaches()
    {
        var toolbar = new MaterialToolbar { Width = 280, Background = Brushes.Magenta, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top, FloatingAction = new MaterialFab { Content = new MaterialSymbol { Symbol = "add" } } };
        toolbar.Items.Add(new MaterialIconButton { Content = new MaterialSymbol { Symbol = "edit" } });
        toolbar.Items.Add(new MaterialIconButton { Content = new MaterialSymbol { Symbol = "share" } });
        var panel = new StackPanel { Children = { toolbar } };
        using var host = new GeometryHost(panel, 400, 200);
        var paintedSurface = toolbar.GetVisualDescendants().OfType<Border>().Single(border => border.Background == Brushes.Magenta);
        paintedSurface.Clip = new RectangleGeometry(new Rect(0, 0, 20, 100));
        toolbar.Width = 300; host.Render();
        var probe = toolbar.TranslatePoint(new Point(80, 20), host.Window)!.Value;
        Assert.NotEqual(Colors.Magenta, host.Pixel(probe.X, probe.Y));
        paintedSurface.Clip = new RectangleGeometry(new Rect(0, 0, 400, 100));
        toolbar.Width = 320; host.Render();
        Assert.Equal(Colors.Magenta, host.Pixel(probe.X, probe.Y));
        panel.Children.Remove(toolbar); panel.Children.Add(toolbar); host.Render();
        Assert.Equal(Colors.Magenta, host.Pixel(probe.X, probe.Y));
    }

    [AvaloniaFact]
    public void Chip_visual_fills_its_public_width_and_grows_without_shadow_padding_in_measure()
    {
        var chip = new MaterialChip { Width = 160, Content = "Scope", IsChecked = true, ChipVariant = MaterialChipVariant.Filter, Background = Brushes.Magenta,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top };
        using var host = new GeometryHost(chip, 300, 180);
        Assert.Equal(48, chip.Bounds.Height);
        Assert.Equal(Colors.Magenta, host.Pixel(1, 24));
        host.Theme.Typography = host.Theme.Typography with { Scale = 2 }; host.Render();
        Assert.Equal(Colors.Magenta, host.Pixel(1, chip.Bounds.Height / 2));
    }

    [AvaloniaFact]
    public void Fab_menu_item_mask_uses_its_entire_native_container_height_without_an_old_shadow_inset()
    {
        var item = new MaterialFabMenuItem { Content = "Reply", Width = 120, Background = Brushes.Magenta };
        var menu = new MaterialFabMenu { IsExpanded = true, Items = { item } };
        using var host = new GeometryHost(menu, 360, 320);
        host.Theme.States = host.Theme.States with { FocusStateLayerOpacity = 0, HoverStateLayerOpacity = 0 }; host.Render();
        var box = GeometryHost.Box(item, host.Window);
        Assert.Equal(Colors.Magenta, host.Pixel(box.Center.X, box.Top + 1));
    }

    [AvaloniaFact]
    public void Carousel_image_and_caption_keep_authored_paint_under_modal_and_real_disabling_keeps_its_feedback()
    {
        var image = new DrawingImage { Drawing = new GeometryDrawing { Brush = Brushes.Magenta, Geometry = new RectangleGeometry(new Rect(0, 0, 120, 120)) } };
        var carousel = new MaterialCarousel { Width = 240, Height = 180, ItemsSource = new[] { new MaterialCarouselItem { Image = image, Title = "Image caption", Content = "Description" } },
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top };
        var overlay = new MaterialOverlayHost { Content = carousel };
        using var host = new GeometryHost(overlay, 600, 500);
        var enabled = Painted(carousel);
        var dialog = new MaterialDialog { Width = 160, Height = 120 }; dialog.Show(overlay); host.Render();
        Assert.False(carousel.IsEffectivelyEnabled); Assert.Equal(enabled, Painted(carousel));
        dialog.Cancel(); host.Render(); carousel.IsEnabled = false; host.Render();
        Assert.NotEqual(enabled, Painted(carousel));
    }

    [AvaloniaFact]
    public void Top_app_bar_navigation_keeps_its_authored_role_under_modal_while_cached_activation_stays_blocked()
    {
        var icon = new MaterialIconButton { Content = new MaterialSymbol { Symbol = "menu" } };
        var bar = new MaterialTopAppBar { Title = "Title", NavigationContent = icon, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top };
        var overlay = new MaterialOverlayHost { Content = bar };
        using var host = new GeometryHost(overlay, 600, 500);
        host.Theme.States = host.Theme.States with { FocusStateLayerOpacity = 0, HoverStateLayerOpacity = 0 }; host.Render();
        var enabled = Painted(icon);
        var cached = ControlAutomationPeer.CreatePeerForElement(icon)!.GetProvider<IInvokeProvider>()!;
        var dialog = new MaterialDialog { Width = 160, Height = 120 }; dialog.Show(overlay); host.Render();
        Assert.False(icon.IsEffectivelyEnabled); Assert.ThrowsAny<Exception>(() => cached.Invoke());
        Assert.Equal(enabled, Painted(icon));
        dialog.Cancel(); host.Render(); icon.IsEnabled = false; host.Render(); Assert.NotEqual(enabled, Painted(icon));
    }

    private static byte[] Painted(Control control)
    {
        using var bitmap = new RenderTargetBitmap(new PixelSize((int)Math.Ceiling(control.Bounds.Width), (int)Math.Ceiling(control.Bounds.Height)), new Vector(96, 96));
        bitmap.Render(control); using var stream = new MemoryStream(); bitmap.Save(stream, PngBitmapEncoderOptions.Default); return stream.ToArray();
    }
}
