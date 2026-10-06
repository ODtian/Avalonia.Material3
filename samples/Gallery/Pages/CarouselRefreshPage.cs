using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Material3.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace Gallery.Pages;

/// <summary>Package-only generic host. Image acquisition, retry and refresh operations live here, never in the library.</summary>
public sealed class CarouselRefreshPage : UserControl, IDisposable
{
    private readonly Func<int, CancellationToken, Task<IReadOnlyList<MaterialCarouselItem>>> _load;
    private readonly CancellationTokenSource _lifetime = new();
    private int _revision;
    private bool _disposed;
    public CarouselRefreshPage(Func<int, CancellationToken, Task<IReadOnlyList<MaterialCarouselItem>>>? load = null)
    {
        _load = load ?? DemoLoad;
        Items = new(CreatePictures(0));
        MultiBrowse = Carousel(MaterialCarouselLayout.MultiBrowse, "Multi-browse photographs");
        Uncontained = Carousel(MaterialCarouselLayout.Uncontained, "Uncontained photographs");
        Hero = Carousel(MaterialCarouselLayout.Hero, "Hero photographs");
        FullScreen = Carousel(MaterialCarouselLayout.FullScreen, "Full-screen photographs");
        FullScreen.Height = 360;
        Refresh = new MaterialPullToRefresh { Content = MultiBrowse, IsExpressive = true, Height = 300 };
        AutomationProperties.SetName(Refresh, "Refresh photographs");
        AutomationProperties.SetAutomationId(Refresh, "PhotographRefresh");
        Refresh.RefreshRequested += (_, _) => CurrentOperation = RefreshAsync();
        Result = new TextBlock { Text = "Ready: 6 photographs. Pull down or press F5.", TextWrapping = TextWrapping.Wrap };
        AutomationProperties.SetAutomationId(Result, "BrowseResult");
        MultiBrowse.CurrentItemChanged += (_, _) => Result.Text = MultiBrowse.PositionDescription;
        var actions = new WrapPanel { Orientation = Orientation.Horizontal };
        actions.Children.Add(Action("Previous", "BrowsePrevious", () => MultiBrowse.MovePrevious()));
        actions.Children.Add(Action("Next", "BrowseNext", () => MultiBrowse.MoveNext()));
        actions.Children.Add(Action("Refresh", "BrowseRefresh", () => Refresh.RequestRefresh()));
        actions.Children.Add(Action("Next refresh fails", "BrowseFailure", () => { NextRefreshFails = !NextRefreshFails; Result.Text = NextRefreshFails ? "Next refresh will fail" : "Next refresh will succeed"; }));
        actions.Children.Add(Action("Image error", "BrowseImageError", () => { if (MultiBrowse.CurrentItem is { } item) { item.ErrorMessage = "Image decoder unavailable; retry"; item.State = MaterialCarouselItemState.Failed; } }));
        actions.Children.Add(Action("Add image", "BrowseAdd", () => Items.Add(CreatePictures(++_revision)[0])));
        actions.Children.Add(Action("Remove current", "BrowseRemove", () => { if (MultiBrowse.CurrentItem is { } item) Items.Remove(item); }));
        actions.Children.Add(Action("Standard / Expressive refresh", "BrowseIndicator", () => Refresh.IsExpressive = !Refresh.IsExpressive));
        var panel = new StackPanel { Spacing = 12, Margin = new Thickness(16) };
        panel.Children.Add(new TextBlock { Text = "Carousel & pull-to-refresh / 图片集合", FontSize = 24, TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(actions);
        panel.Children.Add(Result);
        panel.Children.Add(new TextBlock { Text = "Multi-browse — adaptive large, medium and small masks. Swipe horizontally; pull vertically for new host content.", TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(Refresh);
        panel.Children.Add(new TextBlock { Text = "Uncontained — fixed-size items flow past the viewport", TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(Uncontained);
        panel.Children.Add(new TextBlock { Text = "Hero — one emphasized photograph, with neighboring peeks", TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(Hero);
        panel.Children.Add(new TextBlock { Text = "Full-screen — edge-to-edge vertical paging (Up/Down)", TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(FullScreen);
        Viewer = new ScrollViewer { Content = panel };
        Content = Viewer;
    }
    public ObservableCollection<MaterialCarouselItem> Items { get; private set; }
    public MaterialCarousel MultiBrowse { get; }
    public MaterialCarousel Uncontained { get; }
    public MaterialCarousel Hero { get; }
    public MaterialCarousel FullScreen { get; }
    public MaterialPullToRefresh Refresh { get; }
    public ScrollViewer Viewer { get; }
    public TextBlock Result { get; }
    public bool NextRefreshFails { get; set; }
    public Task CurrentOperation { get; private set; } = Task.CompletedTask;
    private MaterialCarousel Carousel(MaterialCarouselLayout layout, string name)
    {
        var carousel = new MaterialCarousel { Layout = layout, ItemsSource = Items, Height = 220, PreferredItemWidth = 186, ItemSpacing = 8 };
        AutomationProperties.SetName(carousel, name);
        AutomationProperties.SetAutomationId(carousel, "Carousel" + layout);
        carousel.ItemRetryRequested += (_, e) => _ = RecoverImage(e.Item);
        return carousel;
    }
    private async Task RecoverImage(MaterialCarouselItem item)
    {
        item.State = MaterialCarouselItemState.Loading;
        try
        {
            await Task.Delay(300, _lifetime.Token);
            if (_disposed || !Items.Contains(item)) return;
            item.Image = Picture(++_revision);
            item.ErrorMessage = null;
            item.State = MaterialCarouselItemState.Ready;
            Result.Text = "Recovered: " + item.Title;
        }
        catch (OperationCanceledException) { }
    }
    private async Task RefreshAsync()
    {
        var position = MultiBrowse.CurrentIndex;
        Result.Text = "Refreshing";
        try
        {
            var items = await _load(++_revision, _lifetime.Token);
            if (_disposed) return;
            Items = new(items);
            foreach (var carousel in new[] { MultiBrowse, Uncontained, Hero, FullScreen })
            {
                var index = carousel == MultiBrowse ? position : carousel.CurrentIndex;
                carousel.ItemsSource = Items;
                carousel.CurrentIndex = index;
            }
            Refresh.ResultMessage = $"{Items.Count} photographs updated";
            Refresh.Status = MaterialProgressStatus.Completed;
            Result.Text = Refresh.StatusDescription;
        }
        catch (Exception e) when (!_disposed)
        {
            Refresh.ResultMessage = e.Message + "; retry available";
            Refresh.Status = MaterialProgressStatus.Failed;
            Result.Text = Refresh.StatusDescription;
        }
        catch (OperationCanceledException) { }
    }
    private async Task<IReadOnlyList<MaterialCarouselItem>> DemoLoad(int revision, CancellationToken token)
    {
        await Task.Delay(650, token);
        if (NextRefreshFails) throw new InvalidOperationException("Offline");
        return CreatePictures(revision);
    }
    private static MaterialButton Action(string label, string id, Action action)
    {
        var button = new MaterialButton { Content = new TextBlock { Text = label, MaxWidth = 200, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center }, MaxWidth = 220, Margin = new Thickness(0, 0, 8, 4) };
        AutomationProperties.SetName(button, label);
        AutomationProperties.SetAutomationId(button, id);
        button.Click += (_, _) => action();
        return button;
    }
    public static IReadOnlyList<MaterialCarouselItem> CreatePictures(int revision) => Enumerable.Range(0, 6).Select(i =>
        new MaterialCarouselItem { Title = $"Landscape {i + 1} / 风景 · {revision}", Image = Picture(i + revision), Content = $"Image {i + 1}" }).ToList();
    public static IImage Picture(int index)
    {
        var colors = new[] { "#1B6B55", "#1C5B87", "#96642B", "#6750A4", "#755478", "#476936" };
        var group = new DrawingGroup();
        group.Children.Add(new GeometryDrawing { Brush = new SolidColorBrush(Color.Parse(colors[Math.Abs(index) % colors.Length])), Geometry = new RectangleGeometry(new Rect(0, 0, 400, 300)) });
        group.Children.Add(new GeometryDrawing { Brush = Brushes.LightGoldenrodYellow, Geometry = new EllipseGeometry(new Rect(240, 40, 70, 70)) });
        var mountains = new StreamGeometry();
        using (var path = mountains.Open())
        {
            path.BeginFigure(new Point(0, 280), true);
            path.LineTo(new Point(125, 110)); path.LineTo(new Point(205, 205));
            path.LineTo(new Point(310, 140)); path.LineTo(new Point(400, 280)); path.EndFigure(true);
        }
        group.Children.Add(new GeometryDrawing { Brush = Brushes.LightSeaGreen, Geometry = mountains });
        return new DrawingImage(group);
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _lifetime.Cancel();
        _lifetime.Dispose();
    }
}
