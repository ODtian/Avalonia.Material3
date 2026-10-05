using Avalonia;
using Avalonia.Controls;
using Avalonia.Automation;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.LogicalTree;
using Gallery.Pages;
using Xunit;

namespace PackageConsumption.Tests;

public class ExpressiveButtonsGalleryTests
{
    [AvaloniaFact]
    public void Published_gallery_scrolls_to_icon_sizes_and_the_mixed_language_long_label_action()
    {
        var page = new ExpressiveButtonsPage();
        var viewer = new ScrollViewer { Content = page };
        var window = new Window { Width = 800, Height = 600, RequestedThemeVariant = ThemeVariant.Light, Content = viewer };
        window.Show();
        try
        {
            using var initial = window.CaptureRenderedFrame();
            window.MouseWheel(new Point(400, 300), new Vector(0, -12));
            using var scrolled = window.CaptureRenderedFrame();
            Assert.True(viewer.Offset.Y > 0);
            viewer.ScrollToEnd();
            using var bottom = window.CaptureRenderedFrame();
            var longLabel = page.GetLogicalDescendants().OfType<MaterialButton>()
                .Single(button => AutomationProperties.GetName(button) == "Save changes and continue");
            var point = longLabel.TranslatePoint(new Point(longLabel.Bounds.Width / 2, longLabel.Bounds.Height / 2), window)!.Value;
            Assert.InRange(point.Y, 0, 600);
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
            Assert.Equal("Save changes and continue: completed", page.Result.Text);
            SaveFrame(window, "m3-03-gallery-icons-and-long-label.png");
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(320)]
    [InlineData(1100)]
    public void Published_gallery_keeps_mixed_labels_and_all_targets_inside_the_viewport_at_200_percent_font(int width)
    {
        var theme = Application.Current!.Styles.OfType<MaterialTheme>().Single();
        var typography = theme.Typography;
        theme.Typography = typography with { Scale = 2 };
        var page = new ExpressiveButtonsPage();
        var window = new Window { Width = width, Height = 850, RequestedThemeVariant = ThemeVariant.Light, Content = new ScrollViewer { Content = page } };
        window.Show();
        try
        {
            using var frame = window.CaptureRenderedFrame();
            foreach (var button in page.GetLogicalDescendants().OfType<MaterialButton>())
            {
                Assert.True(button.Bounds.Width >= 48 && button.Bounds.Height >= 48);
                Assert.True(button.Bounds.Width <= width - 48, $"{button.Content}: width {button.Bounds.Width} exceeds available {width - 48}");
            }
            SaveFrame(window, $"m3-03-gallery-{width}-font200.png");
        }
        finally { window.Close(); theme.Typography = typography; }
    }

    [AvaloniaFact]
    public void Published_package_gallery_reports_actions_selection_theme_font_and_disable_changes()
    {
        var theme = Application.Current!.Styles.OfType<MaterialTheme>().Single();
        var originalTypography = theme.Typography;
        var page = new ExpressiveButtonsPage();
        var window = new Window { Width = 1100, Height = 850, RequestedThemeVariant = ThemeVariant.Light, Content = new ScrollViewer { Content = page } };
        window.Show();
        try
        {
            SaveFrame(window, "m3-03-gallery-light.png");
            Click(window, page.ActionButton);
            Assert.Equal("Action completed (1): confirmed", page.Result.Text);
            Click(window, page.FavoriteButton);
            Assert.True(page.FavoriteButton.IsChecked);
            Assert.Equal("Favorite: selected", page.Result.Text);
            Click(window, page.ThemeButton);
            Assert.Equal(ThemeVariant.Dark, window.ActualThemeVariant);
            Assert.Equal(Color.Parse("#D0BCFF"), Assert.IsAssignableFrom<ISolidColorBrush>(page.ActionButton.Background).Color);
            SaveFrame(window, "m3-03-gallery-dark-selected.png");
            Click(window, page.FontScaleButton);
            Assert.Equal(28, page.ActionButton.FontSize);
            Click(window, page.DisableButton);
            Assert.False(page.ActionButton.IsEffectivelyEnabled);
            Assert.False(page.FavoriteButton.IsEffectivelyEnabled);
            Click(window, page.ActionButton);
            Assert.Equal("Favorite: selected", page.Result.Text);
            Click(window, page.DisableButton);
            page.ActionButton.Focus();
            window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            window.KeyReleaseQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            Assert.Equal("Action completed (2): confirmed", page.Result.Text);
            using var frame = window.CaptureRenderedFrame();
            Assert.NotNull(frame);
        }
        finally { window.Close(); theme.Typography = originalTypography; }
    }

    private static void SaveFrame(Window window, string name)
    {
        var path = Environment.GetEnvironmentVariable("M3_ISSUE4_SCREENSHOTS");
        if (string.IsNullOrEmpty(path)) return;
        Directory.CreateDirectory(path);
        using var frame = window.CaptureRenderedFrame();
        frame!.Save(Path.Combine(path, name), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
    }

    private static void Click(Window window, MaterialButton button)
    {
        using var frame = window.CaptureRenderedFrame();
        var point = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)!.Value;
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
    }
}
