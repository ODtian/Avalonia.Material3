using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Styling;
using Gallery.Pages;
using Xunit;

namespace PackageConsumption.Tests;

public class CarouselGalleryPackageTests
{
    [AvaloniaTheory]
    [InlineData(320, 2)]
    [InlineData(900, 1)]
    public void Compiled_package_gallery_has_reachable_forms_at_large_fonts_and_preserves_position_on_resize(int width, int scale)
    {
        var theme = Application.Current!.Styles.OfType<MaterialTheme>().Single();
        var original = theme.Typography;
        theme.Typography = original with { Scale = scale };
        using var page = new CarouselRefreshPage();
        var window = new Window { Width = width, Height = 700, Content = page, RequestedThemeVariant = ThemeVariant.Light };
        window.Show();
        try
        {
            Save(window, $"m3-18-gallery-{width}-font{scale}00-light.png");
            page.MultiBrowse.CurrentIndex = 2;
            var current = page.MultiBrowse.CurrentItem;
            window.Width = Math.Max(260, width - 60);
            using var resize = window.CaptureRenderedFrame();
            Assert.Same(current, page.MultiBrowse.CurrentItem);
            page.Viewer.ScrollToEnd();
            using var end = window.CaptureRenderedFrame();
            Assert.True(page.Viewer.Offset.Y > 0);
            var position = page.FullScreen.TranslatePoint(new Point(0, 100), window)!.Value;
            Assert.InRange(position.Y, 0, 700);
            Save(window, $"m3-18-fullscreen-{width}.png");
            window.RequestedThemeVariant = ThemeVariant.Dark;
            page.Viewer.ScrollToHome();
            Save(window, $"m3-18-gallery-{width}-dark.png");
        }
        finally { window.Close(); theme.Typography = original; }
    }

    [AvaloniaFact]
    public void Fresh_package_renders_all_four_forms_and_both_refresh_feedback_indicators()
    {
        var theme = Application.Current!.Styles.OfType<MaterialTheme>().Single();
        var motion = theme.Motion;
        theme.Motion = motion with { ReduceMotion = true };
        var panel = new StackPanel { Spacing = 8, Margin = new Thickness(16) };
        foreach (var layout in Enum.GetValues<MaterialCarouselLayout>())
        {
            panel.Children.Add(new TextBlock { Text = layout.ToString() });
            panel.Children.Add(new MaterialCarousel { Layout = layout, ItemsSource = CarouselRefreshPage.CreatePictures(0), Height = 120, CurrentIndex = 0 });
        }
        var indicators = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 16 };
        foreach (var expressive in new[] { false, true })
            indicators.Children.Add(new MaterialPullToRefresh { IsExpressive = expressive, Width = 240, Height = 100, Status = MaterialProgressStatus.Running, Content = new Border { Background = Avalonia.Media.Brushes.Transparent } });
        panel.Children.Add(indicators);
        var window = new Window { Width = 700, Height = 780, Content = panel };
        window.Show();
        try
        {
            Save(window, "m3-18-pinned-forms-light.png");
            window.RequestedThemeVariant = ThemeVariant.Dark;
            Save(window, "m3-18-pinned-forms-dark.png");
            foreach (var carousel in panel.Children.OfType<MaterialCarousel>())
                Assert.True(carousel.Bounds.Width > 600 && carousel.Bounds.Height == 120);
        }
        finally { window.Close(); theme.Motion = motion; }
    }

    private static void Save(Window window, string name)
    {
        var directory = Environment.GetEnvironmentVariable("M3_ISSUE19_SCREENSHOTS");
        if (directory is null) return;
        Directory.CreateDirectory(directory);
        using var frame = window.CaptureRenderedFrame();
        frame!.Save(Path.Combine(directory, name), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
    }
}
