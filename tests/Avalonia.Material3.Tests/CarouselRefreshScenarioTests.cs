using System.Collections.ObjectModel;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Material3.Tokens;
using Avalonia.Styling;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using System.ComponentModel;
using System.Windows.Input;
using Xunit;

namespace Avalonia.Material3.Tests;

public class CarouselRefreshScenarioTests
{
    [AvaloniaFact]
    public void Keyboard_browsing_updates_host_position_and_stops_at_collection_boundaries()
    {
        var items = new ObservableCollection<MaterialCarouselItem>
        {
            new() { Title = "Mountain 山", Image = Picture(Brushes.Green) },
            new() { Title = "Ocean 海", Image = Picture(Brushes.Blue) },
            new() { Title = "Desert 沙", Image = Picture(Brushes.Orange) }
        };
        var carousel = new MaterialCarousel { ItemsSource = items, Height = 200 };
        using var host = new BrowseHost(carousel);
        var result = "";
        carousel.CurrentItemChanged += (_, _) => result = carousel.CurrentItem?.Title;
        carousel.Focus();
        host.Key(PhysicalKey.ArrowRight);
        Assert.Equal(1, carousel.CurrentIndex);
        Assert.Equal("Ocean 海", result);
        host.Key(PhysicalKey.End);
        Assert.Equal(2, carousel.CurrentIndex);
        Assert.False(carousel.CanMoveNext);
        host.Key(PhysicalKey.ArrowRight);
        Assert.Equal(2, carousel.CurrentIndex);
        host.Key(PhysicalKey.Home);
        Assert.Equal(0, carousel.CurrentIndex);
        Assert.False(carousel.CanMovePrevious);
        Assert.Contains(carousel.GetVisualDescendants().OfType<Image>(), i => ReferenceEquals(i.Source, items[0].Image));
    }

    public static IImage Picture(IBrush color) => new DrawingImage(new GeometryDrawing
    {
        Brush = color, Geometry = new RectangleGeometry(new Rect(0, 0, 240, 200))
    });

    [AvaloniaTheory]
    [InlineData(MaterialCarouselLayout.MultiBrowse, 243, 93)]
    [InlineData(MaterialCarouselLayout.Uncontained, 186, 186)]
    [InlineData(MaterialCarouselLayout.Hero, 344, 48)]
    [InlineData(MaterialCarouselLayout.FullScreen, 400, 400)]
    public void Pinned_forms_render_distinct_image_masks_and_resize_without_losing_current_item(MaterialCarouselLayout layout, double firstWidth, double secondWidth)
    {
        var items = Enumerable.Range(0, 6).Select(i => new MaterialCarouselItem { Title = $"Image {i}", Image = Picture(Brushes.Green) }).ToList();
        var carousel = new MaterialCarousel { ItemsSource = items, Layout = layout, PreferredItemWidth = 186, ItemSpacing = 8, Height = 200 };
        using var host = new BrowseHost(carousel);
        var images = carousel.GetVisualDescendants().OfType<Image>().ToList();
        var masks = images.Select(i => i.GetVisualAncestors().OfType<Border>().First()).ToList();
        Assert.Equal(firstWidth, masks[0].Bounds.Width);
        Assert.Equal(secondWidth, masks[1].Bounds.Width);
        if (layout == MaterialCarouselLayout.FullScreen)
            Assert.Equal(200, masks[1].Bounds.Y);
        else Assert.Equal(firstWidth + 8, masks[1].Bounds.X);
        carousel.CurrentIndex = 3;
        var current = carousel.CurrentItem;
        host.Window.Width = 280;
        host.Render();
        Assert.Same(current, carousel.CurrentItem);
        Assert.Equal(3, carousel.CurrentIndex);
        Assert.Contains(images, i => i.Source == current!.Image);
    }

    [AvaloniaTheory]
    [InlineData(MaterialCarouselLayout.MultiBrowse)]
    [InlineData(MaterialCarouselLayout.Uncontained)]
    [InlineData(MaterialCarouselLayout.Hero)]
    [InlineData(MaterialCarouselLayout.FullScreen)]
    public void Touch_tracks_image_geometry_before_release_then_advances_one_item(MaterialCarouselLayout layout)
    {
        var carousel = new MaterialCarousel { Layout = layout, ItemsSource = Enumerable.Range(0, 5).Select(i => new MaterialCarouselItem { Title = $"Photo {i}", Image = Picture(Brushes.Green) }), Height = 240 };
        using var host = new BrowseHost(carousel);
        var image = carousel.GetVisualDescendants().OfType<Image>().First();
        var border = image.GetVisualAncestors().OfType<Border>().First();
        var before = border.Bounds;
        var start = host.At(carousel, new Point(220, 160));
        var end = start - (layout == MaterialCarouselLayout.FullScreen ? new Vector(0, 100) : new Vector(100, 0));
        using var touch = host.Window.TouchBegin(start);
        host.Window.TouchMove(touch, end);
        host.Render();
        Assert.NotEqual(before, border.Bounds);
        Assert.Equal(0, carousel.CurrentIndex);
        host.Window.TouchEnd(touch, end);
        host.Render();
        Assert.Equal(1, carousel.CurrentIndex);
    }

    [AvaloniaFact]
    public void Pointer_drag_cancel_secondary_button_disabled_and_RTL_keyboard_keep_navigation_contract()
    {
        var carousel = new MaterialCarousel { ItemsSource = Enumerable.Range(0, 4).Select(i => new MaterialCarouselItem { Title = $"Photo {i}" }), Height = 200 };
        using var host = new BrowseHost(carousel);
        var start = host.At(carousel, new Point(240, 100));
        host.Window.MouseDown(start, MouseButton.Left);
        host.Window.MouseMove(start - new Vector(110, 0));
        host.Key(PhysicalKey.Escape);
        host.Window.MouseUp(start - new Vector(110, 0), MouseButton.Left);
        Assert.Equal(0, carousel.CurrentIndex);
        host.Window.MouseDown(start, MouseButton.Right);
        host.Window.MouseMove(start - new Vector(110, 0));
        host.Window.MouseUp(start - new Vector(110, 0), MouseButton.Right);
        Assert.Equal(0, carousel.CurrentIndex);
        host.Window.MouseDown(start, MouseButton.Left);
        host.Window.MouseMove(start - new Vector(110, 0));
        host.Window.MouseUp(start - new Vector(110, 0), MouseButton.Left);
        Assert.Equal(1, carousel.CurrentIndex);
        carousel.FlowDirection = FlowDirection.RightToLeft;
        carousel.Focus();
        host.Key(PhysicalKey.ArrowLeft);
        Assert.Equal(2, carousel.CurrentIndex);
        carousel.IsEnabled = false;
        host.Key(PhysicalKey.End);
        Assert.Equal(2, carousel.CurrentIndex);
    }

    [AvaloniaFact]
    public void Host_image_loading_failure_retry_and_recovery_keep_item_identity_and_show_real_results()
    {
        var item = new MaterialCarouselItem { Title = "Forest 林", State = MaterialCarouselItemState.Loading };
        var items = new ObservableCollection<MaterialCarouselItem> { item, new() { Title = "Ocean" } };
        var carousel = new MaterialCarousel { ItemsSource = items, Height = 220 };
        using var host = new BrowseHost(carousel);
        Assert.Contains(carousel.GetVisualDescendants().OfType<MaterialLoadingIndicator>(), p => p.IsEffectivelyVisible);
        item.ErrorMessage = "Image unavailable / Retry";
        item.State = MaterialCarouselItemState.Failed;
        host.Render();
        Assert.Contains(carousel.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == item.ErrorMessage && t.IsEffectivelyVisible);
        carousel.ItemRetryRequested += (_, e) => { Assert.Same(item, e.Item); item.State = MaterialCarouselItemState.Loading; };
        var retry = carousel.GetVisualDescendants().OfType<MaterialButton>().Single(b => AutomationProperties.GetName(b) == "Retry Forest 林");
        var position = host.At(retry, new Rect(retry.Bounds.Size).Center);
        host.Window.MouseDown(position, MouseButton.Left);
        host.Window.MouseUp(position, MouseButton.Left);
        Assert.Equal(MaterialCarouselItemState.Loading, item.State);
        item.Image = Picture(Brushes.Green);
        item.State = MaterialCarouselItemState.Ready;
        host.Render();
        Assert.Contains(carousel.GetVisualDescendants().OfType<Image>(), i => i.Source == item.Image && i.IsEffectivelyVisible);
        Assert.Same(item, carousel.CurrentItem);
        items.Insert(0, new() { Title = "New photograph" });
        Assert.Same(item, carousel.CurrentItem);
        Assert.Equal(1, carousel.CurrentIndex);
        items.Remove(item);
        Assert.Equal("Ocean", carousel.CurrentItem!.Title);
        items.Clear();
        Assert.Null(carousel.CurrentItem);
        Assert.False(carousel.CanMoveNext);
        Assert.Contains(carousel.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "No items");
    }

    [AvaloniaFact]
    public void Real_pull_requires_threshold_and_release_then_host_busy_completion_updates_images()
    {
        var items = new ObservableCollection<MaterialCarouselItem> { new() { Title = "Before", Image = Picture(Brushes.Green) } };
        var carousel = new MaterialCarousel { ItemsSource = items, Height = 200 };
        var refresh = new MaterialPullToRefresh { Content = carousel };
        using var host = new BrowseHost(refresh);
        var requests = 0;
        refresh.RefreshRequested += (_, _) => requests++;
        var start = host.At(refresh, new Point(180, 40));
        using (var touch = host.Window.TouchBegin(start))
        {
            host.Window.TouchMove(touch, start + new Vector(0, 140));
            host.Render();
            Assert.Equal(.875, refresh.DistanceFraction);
            Assert.False(refresh.IsArmed);
            host.Window.TouchEnd(touch, start + new Vector(0, 140));
        }
        Assert.Equal(0, requests);
        using (var touch = host.Window.TouchBegin(start))
        {
            host.Window.TouchMove(touch, start + new Vector(0, 200));
            host.Render();
            Assert.True(refresh.IsArmed);
            Assert.Equal(0, requests);
            host.Window.TouchEnd(touch, start + new Vector(0, 200));
        }
        Assert.Equal(1, requests);
        Assert.Equal(MaterialProgressStatus.Running, refresh.Status);
        using (var touch = host.Window.TouchBegin(start))
        {
            host.Window.TouchMove(touch, start + new Vector(0, 220));
            host.Window.TouchEnd(touch, start + new Vector(0, 220));
        }
        Assert.Equal(1, requests);
        items[0].Image = Picture(Brushes.Blue);
        items[0].Title = "New content / 新内容";
        refresh.ResultMessage = "1 new photograph";
        refresh.Status = MaterialProgressStatus.Completed;
        host.Render();
        Assert.Equal(0, refresh.DistanceFraction);
        Assert.Contains(refresh.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "Completed: 1 new photograph" && t.IsEffectivelyVisible);
        Assert.Contains(carousel.GetVisualDescendants().OfType<Image>(), i => i.Source == items[0].Image);
    }

    [AvaloniaFact]
    public void Accessible_scroll_and_refresh_actions_report_named_position_busy_failure_and_recovery()
    {
        var carousel = new MaterialCarousel { ItemsSource = Enumerable.Range(0, 3).Select(i => new MaterialCarouselItem { Title = $"Photograph {i}" }), Height = 200 };
        AutomationProperties.SetName(carousel, "Holiday photographs");
        var refresh = new MaterialPullToRefresh { Content = carousel };
        AutomationProperties.SetName(refresh, "Refresh photographs");
        using var host = new BrowseHost(refresh);
        var peer = ControlAutomationPeer.CreatePeerForElement(carousel)!;
        Assert.Equal(AutomationControlType.List, peer.GetAutomationControlType());
        Assert.Equal("Holiday photographs", peer.GetName());
        var scroll = Assert.IsAssignableFrom<IScrollProvider>(peer.GetProvider<IScrollProvider>());
        scroll.Scroll(ScrollAmount.SmallIncrement, ScrollAmount.NoAmount);
        Assert.Equal(1, carousel.CurrentIndex);
        Assert.Equal("2 of 3: Photograph 1; Ready", peer.GetItemStatus());
        scroll.SetScrollPercent(100, -1);
        Assert.Equal(2, carousel.CurrentIndex);
        var refreshPeer = ControlAutomationPeer.CreatePeerForElement(refresh)!;
        Assert.Equal("Refresh photographs", refreshPeer.GetName());
        var invoke = Assert.IsAssignableFrom<IInvokeProvider>(refreshPeer.GetProvider<IInvokeProvider>());
        invoke.Invoke();
        Assert.Equal("Refreshing", refreshPeer.GetItemStatus());
        refresh.ResultMessage = "Offline; retry";
        refresh.Status = MaterialProgressStatus.Failed;
        Assert.Equal("Failed: Offline; retry", refreshPeer.GetItemStatus());
        invoke.Invoke();
        refresh.ResultMessage = "Three photographs updated";
        refresh.Status = MaterialProgressStatus.Completed;
        Assert.Equal("Completed: Three photographs updated", refreshPeer.GetItemStatus());
        carousel.IsEnabled = false;
        Assert.Throws<ElementNotEnabledException>(() => scroll.SetScrollPercent(0, -1));
        refresh.IsEnabled = false;
        Assert.Throws<ElementNotEnabledException>(invoke.Invoke);
    }

    [AvaloniaFact]
    public void Theme_motion_plays_a_controlled_settle_and_runtime_reduced_motion_finishes_without_changing_item()
    {
        var carousel = new MaterialCarousel { ItemsSource = Enumerable.Range(0, 4).Select(i => new MaterialCarouselItem { Title = $"Photo {i}", Image = Picture(Brushes.Green) }), AnimationTime = TimeSpan.Zero, Height = 200 };
        using var host = new BrowseHost(carousel);
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = false };
        carousel.MoveNext();
        Assert.Equal(1, carousel.CurrentIndex);
        Assert.Equal(0, carousel.PresentationPosition);
        carousel.AnimationTime = TimeSpan.FromMilliseconds(80);
        host.Render();
        Assert.InRange(carousel.PresentationPosition, .01, .99);
        host.Theme.Motion = host.Theme.Motion with { ReduceMotion = true };
        host.Render();
        Assert.Equal(1, carousel.PresentationPosition);
        var current = carousel.CurrentItem;
        host.Window.RequestedThemeVariant = ThemeVariant.Dark;
        host.Theme.Typography = new() { Scale = 2 };
        host.Render();
        Assert.Same(current, carousel.CurrentItem);
        Assert.Equal(Color.Parse("#141218"), ((ISolidColorBrush)carousel.Background!).Color);
    }

    [AvaloniaFact]
    public void Multi_browse_masks_a_stable_large_image_instead_of_squeezing_its_content_while_dragging()
    {
        var carousel = new MaterialCarousel { ItemsSource = Enumerable.Range(0, 5).Select(i => new MaterialCarouselItem { Title = $"Photo {i}", Image = Picture(Brushes.Green) }), ItemSpacing = 8, Height = 200 };
        using var host = new BrowseHost(carousel);
        var images = carousel.GetVisualDescendants().OfType<Image>().ToList();
        Assert.Equal(243, images[1].Bounds.Width);
        var start = host.At(carousel, new Point(220, 100));
        using var touch = host.Window.TouchBegin(start);
        host.Window.TouchMove(touch, start - new Vector(90, 0));
        host.Render();
        Assert.Equal(243, images[1].Bounds.Width);
        Assert.InRange(images[1].GetVisualAncestors().OfType<Border>().First().Bounds.Width, 94, 242);
        host.Window.TouchEnd(touch, start - new Vector(90, 0));
    }

    [AvaloniaFact]
    public void Clicking_a_visible_peek_selects_its_image_and_a_custom_item_action_keeps_its_owner()
    {
        var items = Enumerable.Range(0, 4).Select(i => new MaterialCarouselItem { Title = $"Photo {i}", Image = Picture(Brushes.Green) }).ToList();
        var carousel = new MaterialCarousel { ItemsSource = items, Height = 200, MotionDuration = TimeSpan.Zero };
        using var host = new BrowseHost(carousel);
        var image = carousel.GetVisualDescendants().OfType<Image>().Skip(1).First();
        var mask = image.GetVisualAncestors().OfType<Border>().First();
        var point = host.At(mask, new Point(mask.Bounds.Width / 2, 60));
        host.Window.MouseDown(point, MouseButton.Left);
        host.Window.MouseUp(point, MouseButton.Left);
        Assert.Equal(1, carousel.CurrentIndex);
        var result = "";
        carousel.ItemTemplate = new FuncDataTemplate<MaterialCarouselItem>((item, _) =>
        {
            var button = new MaterialButton { Content = "Open " + item!.Title };
            button.Click += (_, _) => result = item.Title;
            return button;
        });
        host.Render();
        var action = carousel.GetVisualDescendants().OfType<MaterialButton>().Single(b => Equals(b.Content, "Open Photo 1"));
        point = host.At(action, new Rect(action.Bounds.Size).Center);
        host.Window.MouseDown(point, MouseButton.Left);
        host.Window.MouseUp(point, MouseButton.Left);
        Assert.Equal("Photo 1", result);
        Assert.Equal(1, carousel.CurrentIndex);
    }

    [AvaloniaTheory]
    [InlineData("escape")]
    [InlineData("disable")]
    [InlineData("boundary")]
    [InlineData("capture")]
    [InlineData("detach")]
    public void An_armed_pull_cancels_without_starting_host_work(string cancellation)
    {
        var refresh = new MaterialPullToRefresh { Content = new Border { Background = Brushes.Green } };
        using var host = new BrowseHost(refresh);
        var requests = 0;
        refresh.RefreshRequested += (_, _) => requests++;
        var start = host.At(refresh, new Point(100, 40));
        IPointer? activePointer = null;
        refresh.PointerPressed += (_, e) => activePointer = e.Pointer;
        using var touch = host.Window.TouchBegin(start);
        host.Window.TouchMove(touch, start + new Vector(0, 180));
        Assert.True(refresh.IsArmed);
        if (cancellation == "escape") { refresh.Focus(); host.Key(PhysicalKey.Escape); }
        if (cancellation == "disable") refresh.IsEnabled = false;
        if (cancellation == "boundary") refresh.IsAtStart = false;
        if (cancellation == "capture") activePointer!.Capture(null);
        if (cancellation == "detach") host.Window.Content = null;
        host.Window.TouchEnd(touch, start + new Vector(0, 180));
        Assert.Equal(0, requests);
        Assert.Equal(0, refresh.DistanceFraction);
        Assert.False(refresh.IsPulling);
        Assert.False(refresh.IsArmed);
    }

    [AvaloniaFact]
    public void Scroll_boundary_horizontal_navigation_exact_threshold_and_keyboard_retry_do_not_conflict()
    {
        var viewer = new ScrollViewer { Content = new Border { Height = 900, Background = Brushes.Green } };
        var refresh = new MaterialPullToRefresh { Content = viewer };
        using var host = new BrowseHost(refresh);
        viewer.Offset = new Vector(0, 100);
        host.Render();
        var start = host.At(refresh, new Point(140, 30));
        using (var touch = host.Window.TouchBegin(start))
        {
            host.Window.TouchMove(touch, start + new Vector(0, 200));
            host.Window.TouchEnd(touch, start + new Vector(0, 200));
        }
        Assert.Equal(MaterialProgressStatus.Idle, refresh.Status);
        viewer.Offset = default;
        host.Render();
        using (var touch = host.Window.TouchBegin(start))
        {
            host.Window.TouchMove(touch, start + new Vector(0, 160));
            Assert.False(refresh.IsArmed);
            host.Window.TouchEnd(touch, start + new Vector(0, 160));
        }
        Assert.Equal(MaterialProgressStatus.Idle, refresh.Status);
        refresh.Focus();
        host.Key(PhysicalKey.F5);
        Assert.Equal(MaterialProgressStatus.Running, refresh.Status);
        refresh.Status = MaterialProgressStatus.Failed;
        refresh.ResultMessage = "Offline";
        host.Key(PhysicalKey.F5);
        Assert.Equal(MaterialProgressStatus.Running, refresh.Status);
    }

    [AvaloniaFact]
    public void Position_and_busy_bindings_update_the_host_and_continue_accepting_host_changes()
    {
        var model = new BrowseModel();
        var carousel = new MaterialCarousel { DataContext = model, ItemsSource = Enumerable.Range(0, 4).Select(i => new MaterialCarouselItem { Title = $"Photo {i}" }), Height = 200 };
        carousel.Bind(MaterialCarousel.CurrentIndexProperty, new Binding(nameof(BrowseModel.Position)));
        var refresh = new MaterialPullToRefresh { DataContext = model, Content = carousel };
        refresh.Bind(MaterialPullToRefresh.StatusProperty, new Binding(nameof(BrowseModel.Status)));
        using var host = new BrowseHost(refresh);
        carousel.MoveNext();
        Assert.Equal(1, model.Position);
        model.Position = 3;
        Assert.Equal(3, carousel.CurrentIndex);
        refresh.RequestRefresh();
        Assert.Equal(MaterialProgressStatus.Running, model.Status);
        model.Status = MaterialProgressStatus.Completed;
        Assert.Equal(MaterialProgressStatus.Completed, refresh.Status);
        carousel.MovePrevious();
        Assert.Equal(2, model.Position);
    }

    [AvaloniaFact]
    public void Uncontained_release_keeps_a_free_fractional_position_instead_of_snapping_to_an_item()
    {
        var carousel = new MaterialCarousel { Layout = MaterialCarouselLayout.Uncontained, ItemsSource = Enumerable.Range(0, 6).Select(i => new MaterialCarouselItem { Title = $"Photo {i}" }), Height = 200 };
        using var host = new BrowseHost(carousel);
        var start = host.At(carousel, new Point(200, 100));
        using (var touch = host.Window.TouchBegin(start))
        {
            host.Window.TouchMove(touch, start - new Vector(70, 0));
            host.Window.TouchEnd(touch, start - new Vector(70, 0));
        }
        Assert.Equal(0, carousel.CurrentIndex);
        Assert.InRange(carousel.PresentationPosition, .35, .4);
        using (var touch = host.Window.TouchBegin(start))
        {
            host.Window.TouchMove(touch, start - new Vector(70, 0));
            host.Window.TouchEnd(touch, start - new Vector(70, 0));
        }
        Assert.Equal(1, carousel.CurrentIndex);
        Assert.InRange(carousel.PresentationPosition, .7, .8);
    }

    [AvaloniaFact]
    public void Carousel_default_spacing_and_refresh_default_threshold_match_the_pinned_reference()
    {
        Assert.Equal(0, new MaterialCarousel().ItemSpacing);
        Assert.Equal(80, new MaterialPullToRefresh().Threshold);
    }

    [AvaloniaFact]
    public void Escape_cancels_a_pull_that_started_over_a_keyboard_focusable_carousel()
    {
        var carousel = new MaterialCarousel { ItemsSource = [new MaterialCarouselItem { Title = "Photo" }], Height = 200 };
        var refresh = new MaterialPullToRefresh { Content = carousel };
        using var host = new BrowseHost(refresh);
        var start = host.At(carousel, new Point(100, 20));
        using var touch = host.Window.TouchBegin(start);
        host.Window.TouchMove(touch, start + new Vector(0, 190));
        Assert.True(refresh.IsArmed);
        host.Key(PhysicalKey.Escape);
        host.Window.TouchEnd(touch, start + new Vector(0, 190));
        Assert.Equal(MaterialProgressStatus.Idle, refresh.Status);
    }

    [AvaloniaFact]
    public void Standard_refresh_renders_pinned_container_role_and_level_two_elevation()
    {
        var refresh = new MaterialPullToRefresh { Status = MaterialProgressStatus.Running, Content = new Border() };
        using var host = new BrowseHost(refresh);
        var container = refresh.GetVisualDescendants().OfType<Border>().Single(b => b.Bounds.Width == 40 && b.Bounds.Height == 40);
        Assert.Equal(Color.Parse("#ECE6F0"), ((ISolidColorBrush)container.Background!).Color);
        Assert.True(container.BoxShadow.Count > 0);
        host.Window.RequestedThemeVariant = ThemeVariant.Dark;
        host.Render();
        Assert.Equal(Color.Parse("#2B2930"), ((ISolidColorBrush)container.Background!).Color);
    }

    [AvaloniaFact]
    public void Long_mixed_language_error_at_large_fonts_keeps_the_named_retry_action_reachable()
    {
        var item = new MaterialCarouselItem { Title = "Forest / 林", State = MaterialCarouselItemState.Failed, ErrorMessage = string.Concat(Enumerable.Repeat("Image unavailable 图像不可用；Please retry。", 12)) };
        var carousel = new MaterialCarousel { ItemsSource = [item], Height = 220 };
        using var host = new BrowseHost(carousel, 320, 320);
        host.Theme.Typography = new() { Scale = 2 };
        host.Render();
        carousel.ItemRetryRequested += (_, _) => item.State = MaterialCarouselItemState.Loading;
        var retry = carousel.GetVisualDescendants().OfType<MaterialButton>().Single();
        var position = host.At(retry, new Rect(retry.Bounds.Size).Center);
        Assert.InRange(position.Y, 0, 320);
        host.Window.MouseDown(position, MouseButton.Left);
        host.Window.MouseUp(position, MouseButton.Left);
        Assert.Equal(MaterialCarouselItemState.Loading, item.State);
    }

    [AvaloniaFact]
    public void Disabled_image_collection_visibly_mutes_generated_image_and_caption_once_and_rejects_input()
    {
        var carousel = new MaterialCarousel { ItemsSource = [new MaterialCarouselItem { Title = "Forest / 林", Image = Picture(Brushes.Green) }, new() { Title = "Ocean" }], Height = 220 };
        using var host = new BrowseHost(carousel);
        carousel.IsEnabled = false;
        host.Render();
        var image = carousel.GetVisualDescendants().OfType<Image>().First();
        Assert.Equal(.38, image.Opacity);
        var caption = carousel.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text == "Forest / 林");
        Assert.Equal(.38, caption.Opacity);
        Assert.False(carousel.MoveNext());
        Assert.Equal(0, carousel.CurrentIndex);
    }

    [AvaloniaTheory]
    [InlineData(400)]
    [InlineData(900)]
    public void Every_current_item_becomes_a_large_focal_image_including_near_the_trailing_boundary(int width)
    {
        var items = Enumerable.Range(0, 6).Select(i => new MaterialCarouselItem { Title = $"Photo {i}", Image = Picture(Brushes.Green) }).ToList();
        var carousel = new MaterialCarousel { ItemsSource = items, Height = 220, ItemSpacing = 8, MotionDuration = TimeSpan.Zero };
        using var host = new BrowseHost(carousel, width);
        for (var index = 0; index < items.Count; index++)
        {
            carousel.CurrentIndex = index;
            host.Render();
            var images = carousel.GetVisualDescendants().OfType<Image>().ToList();
            var current = images.Single(image => image.Source == items[index].Image);
            var currentMask = current.GetVisualAncestors().OfType<Border>().First();
            var largestMask = images.Select(image => image.GetVisualAncestors().OfType<Border>().First().Bounds.Width).Max();
            Assert.Equal(largestMask, currentMask.Bounds.Width);
        }
    }

    [AvaloniaFact]
    public void A_host_localized_empty_message_updates_both_visible_and_accessible_collection_feedback()
    {
        var carousel = new MaterialCarousel { Height = 200 };
        using var host = new BrowseHost(carousel);
        var peer = ControlAutomationPeer.CreatePeerForElement(carousel)!;
        carousel.EmptyText = "暂无图片 / No photographs yet";
        host.Render();
        Assert.Equal("暂无图片 / No photographs yet", peer.GetItemStatus());
        Assert.Contains(carousel.GetVisualDescendants().OfType<TextBlock>(), text => text.Text == carousel.EmptyText);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Busy_feedback_remains_inside_a_minimum_height_viewport(bool expressive)
    {
        var refresh = new MaterialPullToRefresh { IsExpressive = expressive, Status = MaterialProgressStatus.Running, Height = 48 };
        using var host = new BrowseHost(refresh);
        Control indicator = expressive
            ? refresh.GetVisualDescendants().OfType<MaterialLoadingIndicator>().Single()
            : refresh.GetVisualDescendants().OfType<Border>().Single(border => border.Bounds.Size == new Size(40, 40));
        Assert.InRange(indicator.Bounds.Top, 0, 48);
        Assert.InRange(indicator.Bounds.Bottom, 0, 48);
    }
}

public sealed class BrowseModel : INotifyPropertyChanged
{
    private int _position;
    private MaterialProgressStatus _status;
    public int Position { get => _position; set { _position = value; PropertyChanged?.Invoke(this, new(nameof(Position))); } }
    public MaterialProgressStatus Status { get => _status; set { _status = value; PropertyChanged?.Invoke(this, new(nameof(Status))); } }
    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed class BrowseHost : IDisposable
{
    public MaterialTheme Theme { get; } = new();
    public Window Window { get; }
    public BrowseHost(Control content, double width = 400, double height = 320)
    {
        Application.Current!.Styles.Add(Theme);
        Window = new Window { Width = width, Height = height, Content = content };
        Window.Show();
        Render();
    }
    public void Render()
    {
        Window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
    }
    public void Key(PhysicalKey key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        Window.KeyPressQwerty(key, modifiers);
        Window.KeyReleaseQwerty(key, modifiers);
        Render();
    }
    public Point At(Control control, Point point) => control.TranslatePoint(point, Window)!.Value;
    public void Dispose() { Window.Close(); Application.Current!.Styles.Remove(Theme); }
}
