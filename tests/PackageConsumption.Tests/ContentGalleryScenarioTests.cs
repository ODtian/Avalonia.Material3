using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Material3.Tokens;
using Avalonia.Styling;
using Gallery.Pages;
using Xunit;

namespace PackageConsumption.Tests;

public class ContentGalleryScenarioTests
{
    [AvaloniaFact]
    public void Packaged_content_page_owns_selection_actions_and_narrow_scaled_layout()
    {
        var page = new ContentHierarchyPage();
        var window = new Window { Width = 740, Height = 1800, Content = page, RequestedThemeVariant = ThemeVariant.Light };
        var theme = Application.Current!.Styles.OfType<MaterialTheme>().Last();
        var originalTypography = theme.Typography;
        window.Show();
        try
        {
            Render(window);
            var row = page.Rows[1];
            Click(window, row, new Point(24, row.Bounds.Height / 2));
            Assert.Equal("Selected entry-2", page.Result.Text);
            Assert.True(row.IsSelected);
            var action = Assert.IsType<MaterialButton>(row.Trailing);
            Click(window, action);
            Assert.Equal("Opened entry-2", page.Result.Text);
            Assert.True(row.IsSelected);
            Click(window, page.WidthButton);
            Click(window, page.FontButton);
            Click(window, page.ThemeButton);
            Render(window);
            Assert.Equal(320, page.List.MaxWidth);
            Assert.Equal(24, page.Rows[0].FontSize);
            Assert.Equal(ThemeVariant.Dark, window.ActualThemeVariant);
            Assert.True(page.Rows[2].Bounds.Height > page.Rows[0].Bounds.Height);
            Assert.All(page.Rows, r => Assert.True(r.Bounds.Width <= 320));
            var longRow = page.Rows[2];
            longRow.Focus();
            window.KeyPressQwerty(PhysicalKey.ArrowUp, RawInputModifiers.Alt);
            window.KeyReleaseQwerty(PhysicalKey.ArrowUp, RawInputModifiers.Alt);
            Assert.Same(longRow, page.List.Children[1]);
            Assert.Contains("Reordered entry-3", page.Result.Text);
        }
        finally
        {
            theme.Typography = originalTypography;
            window.Close();
        }
    }

    private static void Render(Window window)
    {
        using var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
    }
    private static void Click(Window window, Control control, Point? local = null)
    {
        var point = control.TranslatePoint(local ?? new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        Render(window);
    }
}
