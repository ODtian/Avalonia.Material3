using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Avalonia.VisualTree;
using Gallery.Pages;
using Xunit;

namespace Avalonia.Material3.Tests;

public class CarouselGalleryScenarioTests
{
    [AvaloniaFact]
    public async Task Gallery_host_replaces_images_on_success_shows_failure_and_recovers_without_losing_browse_position()
    {
        var completion = new TaskCompletionSource<IReadOnlyList<MaterialCarouselItem>>();
        using var page = new CarouselRefreshPage((_, _) => completion.Task);
        using var host = new BrowseHost(page, 420, 800);
        page.MultiBrowse.CurrentIndex = 2;
        page.Refresh.RequestRefresh();
        Assert.Equal(MaterialProgressStatus.Running, page.Refresh.Status);
        var replacement = Enumerable.Range(0, 7).Select(i => new MaterialCarouselItem { Title = $"Updated {i}", Image = CarouselRefreshScenarioTests.Picture(Avalonia.Media.Brushes.Blue) }).ToList();
        completion.SetResult(replacement);
        await page.CurrentOperation;
        host.Render();
        Assert.Equal(MaterialProgressStatus.Completed, page.Refresh.Status);
        Assert.Equal(2, page.MultiBrowse.CurrentIndex);
        Assert.Equal("Updated 2", page.MultiBrowse.CurrentItem!.Title);
        Assert.Contains(page.MultiBrowse.GetVisualDescendants().OfType<Image>(), i => i.Source == replacement[2].Image);
        completion = new();
        page.Refresh.RequestRefresh();
        completion.SetException(new InvalidOperationException("Offline"));
        await page.CurrentOperation;
        Assert.Equal(MaterialProgressStatus.Failed, page.Refresh.Status);
        Assert.Contains("Offline", page.Result.Text);
        completion = new();
        page.Refresh.RequestRefresh();
        completion.SetResult(replacement);
        await page.CurrentOperation;
        Assert.Equal(MaterialProgressStatus.Completed, page.Refresh.Status);
        page.Viewer.ScrollToEnd();
        host.Render();
        Assert.True(page.Viewer.Offset.Y > 0);
        var location = page.FullScreen.TranslatePoint(new Point(0, 100), host.Window)!.Value;
        Assert.InRange(location.Y, 0, 800);
    }
}
