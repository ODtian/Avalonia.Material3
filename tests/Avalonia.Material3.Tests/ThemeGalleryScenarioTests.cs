using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Themes;
using Avalonia.Media;
using Gallery.Pages;
using Xunit;

namespace Avalonia.Material3.Tests;

public class ThemeGalleryScenarioTests
{
    [AvaloniaFact]
    public void Gallery_user_changes_seed_mode_platform_fonts_and_shapes_with_real_input()
    {
        var theme = new MaterialTheme();
        Application.Current!.Styles.Add(theme);
        var page = new ThemeTokensPage(theme);
        var window = new Window { Width = 320, Height = 720, Content = page };
        try
        {
            window.Show();
            Click(page.SeedButton);
            Assert.NotNull(theme.SeedColor);
            Click(page.ModeButton);
            Assert.Equal(Avalonia.Styling.ThemeVariant.Dark, window.ActualThemeVariant);
            Click(page.PlatformButton);
            Assert.NotNull(theme.DynamicColors);
            Click(page.ScaleButton);
            Assert.Equal(1.5, theme.Typography.Scale);
            Assert.Equal(21, page.PreviewButton.FontSize);
            Click(page.ScaleButton);
            Assert.Equal(2, theme.Typography.Scale);
            Assert.Equal(28, page.PreviewButton.FontSize);
            Click(page.ShapeButton);
            Assert.Equal(new CornerRadius(12), page.PreviewButton.CornerRadius);
            Click(page.MotionButton);
            Assert.True(theme.Motion.ReduceMotion);
            Assert.True(page.MixedText.Bounds.Height >= page.MixedText.LineHeight);
            Assert.True(page.MixedText.Bounds.Width <= window.ClientSize.Width);
            window.Width = 1000;
            using var frame = window.CaptureRenderedFrame();
            Assert.True(page.PreviewButton.Bounds.Height >= 48);
            Assert.True(page.MixedText.Bounds.Width <= window.ClientSize.Width);
        }
        finally
        {
            window.Close();
            Application.Current.Styles.Remove(theme);
        }

        void Click(Control button)
        {
            button.BringIntoView();
            using var before = window.CaptureRenderedFrame();
            var center = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)!.Value;
            window.MouseDown(center, MouseButton.Left);
            window.MouseUp(center, MouseButton.Left);
            using var after = window.CaptureRenderedFrame();
        }
    }
}
