using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Styling;
using Gallery.Pages;
using Xunit;

namespace Avalonia.Material3.Tests;

public class NavigationGalleryScenarioTests
{
    [AvaloniaFact]
    public void Gallery_navigates_actual_content_marks_badges_read_and_adapts_the_same_rail_in_place()
    {
        var theme = new MaterialTheme();
        Application.Current!.Styles.Add(theme);
        var page = new ContentNavigationPage(theme);
        var window = new Window { Width = 1000, Height = 900, Content = page };
        try
        {
            window.Show();
            Capture(window, "gallery-light");
            var library = (MaterialNavigationItem)page.MainTabs.Items[1]!;
            Click(window, library);
            Assert.Same(library.PageContent, page.MainTabs.SelectedContent);
            Assert.Contains("Library", page.Status.Text);
            Click(window, page.MarkRead);
            Assert.Null(library.BadgeDescription);
            Assert.Equal(0, ((MaterialBadge)library.Badge!).Count);
            Click(window, page.ModeButton);
            Assert.Equal(ThemeVariant.Dark, window.RequestedThemeVariant);
            Capture(window, "gallery-dark-library");
            page.ResponsiveRail.SelectedIndex = 1;
            var railItem = page.ResponsiveRail.SelectedItem!;
            railItem.Focus(NavigationMethod.Tab);
            window.Width = 320;
            window.Height = 680;
            window.Measure(new Size(320, 680));
            window.Arrange(new Rect(0, 0, 320, 680));
            Capture(window, "gallery-narrow");
            Assert.False(page.ResponsiveRail.IsExpanded);
            Assert.Same(railItem, page.ResponsiveRail.SelectedItem);
            Assert.True(railItem.IsFocused);
            var focusedPosition = railItem.TranslatePoint(new Point(0, railItem.Bounds.Height / 2), window)!.Value;
            Assert.InRange(focusedPosition.Y, 0, 680);
            page.Scroller.ScrollToHome();
            Capture(window, null);
            Click(window, page.FontButton);
            Click(window, page.LongLabelButton);
            Capture(window, "gallery-320-font200-long");
            Assert.Equal(2, theme.Typography.Scale);
            Assert.Equal(10, page.Previews.Count);
            page.Scroller.ScrollToEnd();
            Capture(window, "gallery-bottom-320");
            Assert.True(page.Scroller.Offset.Y > 0);
        }
        finally { window.Close(); Application.Current!.Styles.Remove(theme); }
    }

    private static void Click(Window window, Control control)
    {
        var point = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
    }
    private static void Capture(Window window, string? name)
    {
        using var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No gallery frame");
        var directory = Environment.GetEnvironmentVariable("M3_ISSUE14_SCREENSHOTS");
        if (directory is not null && name is not null) { Directory.CreateDirectory(directory); frame.Save(Path.Combine(directory, name + ".png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default); }
    }
}
