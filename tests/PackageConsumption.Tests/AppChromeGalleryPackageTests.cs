using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Styling;
using Avalonia.Threading;
using Gallery.Pages;
using Xunit;

namespace PackageConsumption.Tests;

public class AppChromeGalleryPackageTests
{
    [AvaloniaTheory]
    [InlineData(320, 2)]
    [InlineData(1000, 1)]
    public void Compiled_gallery_is_package_consumption_and_keeps_real_saved_page_after_return(int width, int scale)
    {
        Assert.Equal("Gallery", typeof(AppChromePage).Assembly.GetName().Name);
        Assert.Equal("Avalonia.Material3", typeof(MaterialTopAppBar).Assembly.GetName().Name);
        var theme = Application.Current!.Styles.OfType<MaterialTheme>().Single();
        var original = theme.Typography;
        theme.Typography = original with { Scale = scale };
        var gallery = new AppChromePage(theme);
        var window = new Window { Width = width, Height = 700, Content = gallery, RequestedThemeVariant = ThemeVariant.Light };
        window.Show();
        try
        {
            Capture(window, "");
            gallery.OpenNavigation(); Capture(window, "");
            ((IInvokeProvider)ControlAutomationPeer.CreatePeerForElement((MaterialNavigationItem)gallery.Drawer.Items[1]!)!).Invoke();
            ((IInvokeProvider)ControlAutomationPeer.CreatePeerForElement(gallery.SaveButton)!).Invoke();
            ((IInvokeProvider)ControlAutomationPeer.CreatePeerForElement(gallery.DetailsButton)!).Invoke();
            Assert.Equal("Details", gallery.CurrentPage);
            ((IInvokeProvider)ControlAutomationPeer.CreatePeerForElement(gallery.NavigationButton)!).Invoke();
            Capture(window, $"m3-16-package-gallery-{width}-font{scale}00-light.png");
            Assert.Equal("Library", gallery.CurrentPage);
            Assert.Contains("Library saved", gallery.Status.Text);
            Assert.True(gallery.PageScroll.Viewport.Height >= 48);
            window.RequestedThemeVariant = ThemeVariant.Dark;
            Capture(window, $"m3-16-package-gallery-{width}-font{scale}00-dark.png");
        }
        finally { window.Close(); theme.Typography = original; }
    }

    [AvaloniaFact]
    public void Fresh_package_renders_all_eight_pinned_top_forms_and_actual_bottom_slots()
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto"), Margin = new Thickness(12) };
        for (var i = 0; i < 8; i++)
        {
            var variant = i switch { 0 => MaterialTopAppBarVariant.Small, 1 => MaterialTopAppBarVariant.CenterAligned, 2 => MaterialTopAppBarVariant.Medium, 3 => MaterialTopAppBarVariant.Large, 4 or 5 => MaterialTopAppBarVariant.MediumFlexible, _ => MaterialTopAppBarVariant.LargeFlexible };
            var bar = new MaterialTopAppBar { Title = "Collection 收藏", Variant = variant, Subtitle = i is 5 or 7 ? "Updated today" : null, CenterTitle = i == 7, NavigationContent = new MaterialIconButton { Content = "☰" }, Actions = new MaterialIconButton { Content = "✓" } };
            var container = new StackPanel { Margin = new Thickness(4), Children = { new TextBlock { Text = variant + (i is 5 or 7 ? " + subtitle" : ""), FontSize = 14 }, bar } };
            Grid.SetRow(container, i % 4); Grid.SetColumn(container, i / 4); grid.Children.Add(container);
        }
        var bottom = new MaterialBottomAppBar { Actions = new MaterialIconButton { Content = "✓" }, FloatingAction = new MaterialFab { Content = "+" } };
        Grid.SetRow(bottom, 4); Grid.SetColumnSpan(bottom, 2); grid.Children.Add(bottom);
        var window = new Window { Width = 1000, Height = 820, Content = grid, RequestedThemeVariant = ThemeVariant.Light };
        window.Show();
        try
        {
            Capture(window, "m3-16-pinned-forms-light.png");
            Assert.Equal(80, bottom.Bounds.Height);
            window.RequestedThemeVariant = ThemeVariant.Dark;
            Capture(window, "m3-16-pinned-forms-dark.png");
        }
        finally { window.Close(); }
    }
    private static void Capture(Window window, string name)
    {
        Dispatcher.UIThread.RunJobs(); window.UpdateLayout();
        using var bitmap = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No rendered frame.");
        var directory = Environment.GetEnvironmentVariable("M3_ISSUE17_SCREENSHOTS");
        if (directory is null || name.Length == 0) return;
        Directory.CreateDirectory(directory);
        bitmap.Save(Path.Combine(directory, name), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
    }
}
